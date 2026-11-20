using System;
using SD = System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq; // idk if its good to have this installed
using Unity.Barracuda;
using TMPro;
using Unity.Barracuda.ONNX;


// Replaces HandMode enum in JsonWriter.cs in release
// Enum for selecting which hand(s) the model should use for inference
public enum HandMode
{
    LeftHand,
    RightHand,
    TwoHands
}

public class UGInferenceRunnerScript : MonoBehaviour
{
    // Public parameters
    [Header("Setup")]
    public NNModel modelAsset;
    public HandMode inferenceHandMode;
    public Boolean useTransformData;
    public GameObject dataExtractorObject; // the game object that has the data extractor script attached
    private UGDataExtractorScript dataExtractor; // the data extractor script, taken from dataExtractorObject
    public float inferenceInterval = 0.5f; // how often to run inference (in seconds)

    [Header("Run Function on Detection")]
    [SerializeField] private UnityEvent functionToRun;
    [Range(0.0f, 1.0f)]
    public float thresholdConfidenceLevel;
    public Boolean loopFunctionWhilePoseIsHeld;


    // Inference variables
    private Model m_RuntimeModel;
    private float inferenceTimer = 0;
    private IWorker worker;
    private Tensor inputTensor;
    private Tensor outputTensor;
    [HideInInspector]
    public float inferenceOutput;

    // Run function variables
    private Boolean eventTriggered = false;

    // Saved gesture currently loaded (null if using modelAsset or nothing)
    [HideInInspector]
    public string loadedGestureId;
    private int outputIndex = 0;

    void Start()
    {
        dataExtractor = dataExtractorObject.GetComponent<UGDataExtractorScript>();
        bool configurationIsValid = ValidateConfiguration();
        if (!configurationIsValid)
        {
            Debug.LogError("UGInferenceRunnerScript: Configuration is not valid, inference will not run. See console logs for more information.");
            gameObject.SetActive(false);
            return;
        }

        if (modelAsset != null && worker == null) // a saved gesture may already have been loaded
        {
            SetupInference();
        }
        // else: idle until LoadGesture / LoadModel is called (e.g. a saved gesture on startup)
    }

    void Update()
    {
        if (worker == null)
        {
            return; // no model loaded yet
        }
        // Run inference every inferenceInterval seconds
        inferenceTimer += Time.deltaTime;
        if (inferenceTimer >= inferenceInterval)
        {
            RunInference();
        }

        RunFunctionIfPoseDetected();
    }

    bool ValidateConfiguration()
    {
        return ValidateModel() && ValidateDataExtractor();
    }

    bool ValidateModel()
    {
        // A model can now come from a saved gesture at runtime, so an empty modelAsset is allowed
        if (modelAsset == null)
        {
            Debug.Log("UGInferenceRunnerScript: no modelAsset set; waiting for a saved gesture to be loaded.");
        }
        return true;
    }

    bool ValidateDataExtractor()
    {
        // Check if dataExtractor is set and enabled for the correct hand mode
        if (dataExtractor == null)
        {
            Debug.LogError("UGInferenceRunnerScript: dataExtractor is not set.");
            return false;
        }
        if (inferenceHandMode == HandMode.LeftHand && !dataExtractor.leftHandDataEnabled)
        {
            Debug.LogError("UGInferenceRunnerScript: dataExtractor is not enabled for left hand data gathering.");
            return false;
        }
        else if (inferenceHandMode == HandMode.RightHand && !dataExtractor.rightHandDataEnabled)
        {
            Debug.LogError("UGInferenceRunnerScript: dataExtractor is not enabled for right hand data gathering.");
            return false;
        }
        else if (inferenceHandMode == HandMode.TwoHands && !dataExtractor.twoHandDataEnabled)
        {
            Debug.LogError("UGInferenceRunnerScript: dataExtractor is not enabled for two hands data gathering.");
            return false;
        }
        return true;
    }

    void SetupInference()
    {
        // Initialize model and worker needed to run inference
        // see docs for more information on this script: https://docs.unity3d.com/Packages/com.unity.barracuda%401.0/manual/GettingStarted.html
        m_RuntimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, m_RuntimeModel);
        int modelInputSize;
        if (inferenceHandMode == HandMode.LeftHand || inferenceHandMode == HandMode.RightHand)
        {
            if (useTransformData)
            {
                modelInputSize = UGDataExtractorScript.ONE_HAND_NUM_FEATURES + UGDataExtractorScript.ONE_HAND_TRANSFORM_NUM_FEATURES;
            }
            else
            {
                modelInputSize = UGDataExtractorScript.ONE_HAND_NUM_FEATURES;
            }
        }
        else
        {
            modelInputSize = UGDataExtractorScript.TWO_HAND_NUM_FEATURES;
        }
        inputTensor = new Tensor(1, 0, 0, modelInputSize);
    }

    void OnDestroy()
    {
        // Cleanup resources (outputTensor belongs to the worker, so it isn't disposed here)
        inputTensor?.Dispose();
        worker?.Dispose();
    }

    void RunInference()
    {
        inferenceTimer = 0;
        // select hand data based on inferenceHandMode
        float[] handData;
        if (inferenceHandMode == HandMode.LeftHand)
        {
            handData = dataExtractor.leftHandData;
            if (useTransformData)
            {
                handData = handData.Concat(dataExtractor.leftHandTransformData).ToArray();
            }
        }
        else if (inferenceHandMode == HandMode.RightHand)
        {
            handData = dataExtractor.rightHandData;
            if (useTransformData)
            {
                handData = handData.Concat(dataExtractor.rightHandTransformData).ToArray();
            }
        }
        else
        {
            handData = dataExtractor.twoHandsData;
        }

        if (handData == null)
        {
            return; // hand not tracked yet
        }
        if (inputTensor.length != handData.Length)
        {
            Debug.LogWarning("Model input size is not equal to size of data. Check that Inference Hand Mode and Use Transform Data are set correctly");
        }
        // update input tensor with new hand data
        for (int i = 0; i < handData.Length; i++)
        {
            inputTensor[i] = handData[i];
        }

        //Debug.Log("Inference Tensor Size " + inputTensor.length);
        worker.Execute(inputTensor);
        outputTensor = worker.PeekOutput();
        inferenceOutput = outputTensor[Mathf.Clamp(outputIndex, 0, outputTensor.length - 1)];
        // Debug.Log("Inference Output (UGInferenceRunnerScript): " + inferenceOutput);
    }

    void RunFunctionIfPoseDetected()
    {
        if (loopFunctionWhilePoseIsHeld)
        {
            // loops function while the pose is being held
            if (inferenceOutput >= thresholdConfidenceLevel)
            {
                functionToRun.Invoke();
            }
        }
        else
        {
            // triggers function once if the pose is detected
            // function can be triggered again only after the pose is not being held anymore
            if (!eventTriggered && inferenceOutput >= thresholdConfidenceLevel)
            {
                functionToRun.Invoke();
                eventTriggered = true;
            }
            else if (eventTriggered && inferenceOutput < thresholdConfidenceLevel)
            {
                eventTriggered = false;
            }
        }
    }

    // load model at runtime
    public bool LoadModel(string filePath, HandMode newHandMode)
    {
        // Start loading the NNModel asset using Addressables

        // Check if file exists
        if (File.Exists(filePath))
        {
            // change paramters
            inferenceHandMode = newHandMode;
            // clean up old inference (outputTensor belongs to the old worker)
            inputTensor?.Dispose();
            outputTensor = null;
            // Dispose of the existing worker if necessary
            if (worker != null)
            {
                worker.Dispose();
            }

            // setup new inference
            var nnModel = LoadNNModel(filePath, "name");
            var loadedModel = ModelLoader.Load(nnModel);


            // Set the loaded model as the runtime model and create a new worker
            m_RuntimeModel = loadedModel;
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, m_RuntimeModel);

            // Remake tensor
            int modelInputSize;
            if (inferenceHandMode == HandMode.LeftHand || inferenceHandMode == HandMode.RightHand)
            {
                if (useTransformData)
                {
                    modelInputSize = UGDataExtractorScript.ONE_HAND_NUM_FEATURES + UGDataExtractorScript.ONE_HAND_TRANSFORM_NUM_FEATURES;
                }
                else
                {
                    modelInputSize = UGDataExtractorScript.ONE_HAND_NUM_FEATURES;
                }
            }
            else
            {
                modelInputSize = UGDataExtractorScript.TWO_HAND_NUM_FEATURES;
            }

            inputTensor = new Tensor(1, 0, 0, modelInputSize);

            // Debug.Log("Model loaded successfully from: " + filePath);
            return true;
        }
        else
        {
            Debug.LogError("Model file not found at path: " + filePath);
            return false;
        }

    }
    /// <summary>
    /// Loads a saved gesture: applies its saved settings, then loads its .onnx from disk.
    /// Works with the training server offline.
    /// </summary>
    public bool LoadGesture(GestureRecord gesture)
    {
        if (gesture == null || !gesture.IsTrained)
        {
            return false;
        }
        useTransformData = gesture.settings.useTransformData;
        thresholdConfidenceLevel = gesture.settings.thresholdConfidenceLevel;
        loopFunctionWhilePoseIsHeld = gesture.settings.loopFunctionWhilePoseIsHeld;
        inferenceInterval = gesture.settings.inferenceInterval;
        outputIndex = gesture.model.outputIndex;

        bool ok = LoadModel(GestureLibrary.ModelPath(gesture), gesture.settings.handMode);
        loadedGestureId = ok ? gesture.id : null;
        inferenceOutput = 0;
        eventTriggered = false;
        if (ok && dataExtractor != null && !ValidateDataExtractor())
        {
            Debug.LogWarning($"UGInferenceRunnerScript: enable {gesture.settings.handMode} data on the data extractor " +
                             $"to recognise \"{gesture.displayName}\".");
        }
        return ok;
    }

    /// <summary>Stops recognition (e.g. the loaded gesture was deleted or isn't trained yet).</summary>
    public void UnloadModel()
    {
        inputTensor?.Dispose();
        inputTensor = null;
        outputTensor = null;
        worker?.Dispose();
        worker = null;
        loadedGestureId = null;
        inferenceOutput = 0;
        eventTriggered = false;
    }

    NNModel LoadNNModel(string modelPath, string modelName)
    {
        var converter = new ONNXModelConverter(true);
        Model model = converter.Convert(modelPath);
        NNModelData modelData = ScriptableObject.CreateInstance<NNModelData>();
        using (var memoryStream = new MemoryStream())
        using (var writer = new BinaryWriter(memoryStream))
        {
            ModelWriter.Save(writer, model);
            modelData.Value = memoryStream.ToArray();
        }
        modelData.name = "Data";
        modelData.hideFlags = HideFlags.HideInHierarchy;
        NNModel result = ScriptableObject.CreateInstance<NNModel>();
        result.modelData = modelData;
        result.name = modelName;
        return result;
    }
}
