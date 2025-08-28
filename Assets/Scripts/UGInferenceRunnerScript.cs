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
using UnityEditor;
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
    public enum ModelType
    {
        Auto,      // Automatic detection
        Static,    // Force static model
        Dynamic,   // Force dynamic model
    }
    
    [Header("Model Configuration")]
    [Tooltip("Model type detection: Auto (recommended), or force Static/Dynamic if needed")]
    public ModelType modelTypeOverride = ModelType.Auto;

    // Public parameters
    [Header("Setup")]
    public NNModel modelAsset;
    public HandMode inferenceHandMode;
    public Boolean useTransformData;
    public GameObject dataExtractorObject; // the game object that has the data extractor script attached
    private UGDataExtractorScript dataExtractor; // the data extractor script, taken from dataExtractorObject
    [Tooltip("How often to run inference(in seconds).")]
    public float inferenceInterval = 0.5f;
    
    [Header("Dynamic Gesture Settings")]
    [Tooltip("Optional: Link to UGDataWriterScript to auto-sync sample rate. If not set, uses default 15Hz.")]
    public GameObject dataWriterObject; // link to data writer for auto-sync
    private UGDataWriterScript dataWriter; // Reference to data writer script

    [Header("Run Function on Detection")]
    [SerializeField] private UnityEvent functionToRun;
    [Range(0.0f, 1.0f)]
    public float thresholdConfidenceLevel;
    public Boolean loopFunctionWhilePoseIsHeld;

    private const int ExpectedFeatureCount = 17; //Exactly 17 features per timestep
    private int SequenceLength = 15; // Will be read from model shape at runtime
    private List<float[]> dataBuffer = new List<float[]>();
    
    // Timing parameters to match training data collection rate
    private float DataCollectionInterval = 0.0667f; // Default 15 Hz, will be synced from DataWriter if available
    private float dataCollectionTimer = 0f;

    // Inference variables
    private Model m_RuntimeModel;
    private ModelType m_ModelType;
    private float inferenceTimer = 0;
    private IWorker worker;
    private Tensor inputTensor;
    private Tensor outputTensor;
    [HideInInspector]
    public float inferenceOutput;

    // Run function variables
    private Boolean eventTriggered = false;

    void Start()
    {
        dataExtractor = dataExtractorObject.GetComponent<UGDataExtractorScript>();
        
        // Try to get DataWriter reference for sample rate sync
        if (dataWriterObject != null)
        {
            dataWriter = dataWriterObject.GetComponent<UGDataWriterScript>();
            if (dataWriter != null)
            {
                SyncSampleRateFromDataWriter();
            }
        }
        else
        {
            // Try to find DataWriter in the scene automatically
            dataWriter = FindObjectOfType<UGDataWriterScript>();
            if (dataWriter != null)
            {
                Debug.Log("UGInferenceRunnerScript: Auto-detected UGDataWriterScript for sample rate sync");
                SyncSampleRateFromDataWriter();
            }
            else
            {
                Debug.Log("UGInferenceRunnerScript: No DataWriter found, using default sample rate (15 Hz)");
            }
        }
        
        bool configurationIsValid = ValidateConfiguration();
        if (!configurationIsValid)
        {
            Debug.LogError("UGInferenceRunnerScript: Configuration is not valid, inference will not run. See console logs for more information.");
            gameObject.SetActive(false);
            return;
        }

        SetupInference();
    }

    void Update()
    {
        // Continuously sync sample rate if DataWriter is available
        if (dataWriter != null && m_ModelType == ModelType.Dynamic)
        {
            SyncSampleRateFromDataWriter();
        }
        
        if (m_ModelType == ModelType.Dynamic)
        {
            dataCollectionTimer += Time.deltaTime;
            if (dataCollectionTimer >= DataCollectionInterval)
            {
                BufferHandData();
                dataCollectionTimer = 0f;
            }
        }

        inferenceTimer += Time.deltaTime;
        if (inferenceTimer >= inferenceInterval)
        {
            bool canRunInference = m_ModelType == ModelType.Static || (m_ModelType == ModelType.Dynamic && dataBuffer.Count >= 10);
            
            if (canRunInference)
            {
                RunInference();
            }
            inferenceTimer = 0;
        }

        RunFunctionIfPoseDetected();
    }

    void SyncSampleRateFromDataWriter()
    {
        if (dataWriter == null) return;
        
        // Calculate sample interval from DataWriter settings
        // Same calculation as in DataWriter: snapshotInterval = dynamicGestureDuration / snapshotsPerGesture
        float newInterval = dataWriter.dynamicGestureDuration / Mathf.Max(1, dataWriter.snapshotsPerGesture);
        
        // Only update if significantly different (avoid floating point comparison issues)
        if (Mathf.Abs(newInterval - DataCollectionInterval) > 0.001f)
        {
            DataCollectionInterval = newInterval;
            float sampleRate = 1f / DataCollectionInterval;
            Debug.Log($"UGInferenceRunnerScript: Synced sample rate from DataWriter: {sampleRate:F1} Hz ({dataWriter.snapshotsPerGesture} snapshots / {dataWriter.dynamicGestureDuration:F2}s = {DataCollectionInterval:F4}s interval)");
        }
    }

    bool ValidateConfiguration()
    {
        return ValidateModel() && ValidateDataExtractor();
    }

    bool ValidateModel()
    {
        // Check if model is set
        if (modelAsset == null)
        {
            Debug.LogError("UGInferenceRunnerScript: modelAsset is not set.");
            return false;
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
        
        // Check if OVRHands are assigned
        if (dataExtractor.leftOVRHand == null || dataExtractor.rightOVRHand == null)
        {
            Debug.LogError("UGInferenceRunnerScript: OVRHand references are not set in dataExtractor. Please assign LeftOVRHand and RightOVRHand in the Unity Inspector.");
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
        
        // Log hand tracking setup status
        Debug.Log($"Hand tracking validation: LeftOVRHand={dataExtractor.leftOVRHand != null}, RightOVRHand={dataExtractor.rightOVRHand != null}");
        Debug.Log($"Selected inference mode: {inferenceHandMode}");
        
        return true;
    }

    void SetupInference()
    {
        // Initialize model and worker needed to run inference
        // see docs for more information on this script: https://docs.unity3d.com/Packages/com.unity.barracuda%401.0/manual/GettingStarted.html
        m_RuntimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, m_RuntimeModel);

        // --- MODIFIED: Detect model type based on inputs and override setting ---
        if (modelTypeOverride != ModelType.Auto)
        {
            // Manual override
            m_ModelType = modelTypeOverride;
            Debug.Log($"Model type manually set to: {modelTypeOverride}");
        }
        else
        {
            // Automatic detection based on tensor rank
            var firstInput = m_RuntimeModel.inputs[0];
            bool isConv2D = firstInput.shape.Length == 8;  // Conv2D models are imported as 8D
            bool isStatic = !isConv2D;
            
            if (isConv2D)
            {
                m_ModelType = ModelType.Dynamic;
                Debug.Log($"Dynamic Conv2D model detected (8D tensor). Shape: [{string.Join(",", firstInput.shape)}]");
                Debug.Log("Conv2D models are imported as 8D tensors in Unity Barracuda");
            }
            else if (isStatic)
            {
                m_ModelType = ModelType.Static;
                Debug.Log($"Static model detected. Input shape: [{string.Join(",", firstInput.shape)}], rank={firstInput.shape.Length}");
            }
        }

        // Initialize tensors based on detected model type
        int featuresPerFrame;
        if (m_ModelType == ModelType.Dynamic)
        {
            // Dynamic models always use exactly 17 features (single hand, no transform data)
            featuresPerFrame = ExpectedFeatureCount;
            if (inferenceHandMode == HandMode.TwoHands)
            {
                Debug.LogError("Dynamic gesture model currently only supports single hand mode");
                return;
            }
        }
        else if (inferenceHandMode == HandMode.LeftHand || inferenceHandMode == HandMode.RightHand)
        {
            featuresPerFrame = useTransformData
                ? UGDataExtractorScript.ONE_HAND_NUM_FEATURES + UGDataExtractorScript.ONE_HAND_TRANSFORM_NUM_FEATURES
                : UGDataExtractorScript.ONE_HAND_NUM_FEATURES;
        }
        else
        {
            featuresPerFrame = UGDataExtractorScript.TWO_HAND_NUM_FEATURES;
        }

        if (m_ModelType == ModelType.Dynamic)
        {
            
            // Conv2D model - read exact 8D shape from model and create tensor
            var onnxShape = m_RuntimeModel.inputs[0].shape;
            
            // Read actual sequence length from model (axis H = index 5)
            SequenceLength = onnxShape[5] > 0 ? onnxShape[5] : 15; // Default to 15 if dynamic
            
            // Clone and replace dynamic dimensions with actual values
            int[] inputShape = (int[])onnxShape.Clone();
            
            // Unity imports Conv2D as 8D: S R N T D H W C
            // For Conv2D: sequence goes in H (axis 5), features in C (axis 7)
            if (inputShape[5] == 0 || inputShape[5] == -1) inputShape[5] = SequenceLength;  // H axis
            if (inputShape[7] == 0 || inputShape[7] == -1) inputShape[7] = ExpectedFeatureCount;  // C axis
            
            inputTensor = new Tensor(inputShape);
        }
        else // Static
        {
            inputTensor = new Tensor(1, 1, 1, featuresPerFrame);
        }
    }

    void OnDestroy()
    {
        // Cleanup resources
        inputTensor?.Dispose();
        outputTensor?.Dispose();
        worker?.Dispose();
    }

    private void FillConv2DInputTensorFromBuffer(int currentSequenceLength)
    {   
        int[] tensorShape = inputTensor.shape.ToArray();
        int seqDim = tensorShape[5];   // H axis (sequence length)
        int featDim = tensorShape[7];  // C axis (features)
        int bufLen = dataBuffer.Count;

        // Clear tensor using underlying data array
        float[] tensorData = inputTensor.data.Download(inputTensor.shape);
        Array.Clear(tensorData, 0, tensorData.Length);
        
        // Copy most recent frames to tensor (sliding window)
        int copyCount = Mathf.Min(bufLen, seqDim);
        int bufferStart = Mathf.Max(0, bufLen - copyCount);

        // Calculate stride for direct array indexing
        // For 8D tensor [S,R,N,T,D,H,W,C], we only vary H and C
        // Index = H * tensorShape[7] + C (all other dimensions are 1)
        int featureStride = 1;
        int sequenceStride = tensorShape[7];  // Skip entire feature dimension
        
        for (int t = 0; t < copyCount; t++)
        {
            var frame = dataBuffer[bufferStart + t];
            for (int f = 0; f < Mathf.Min(featDim, frame.Length); f++)
            {
                int index = t * sequenceStride + f * featureStride;
                tensorData[index] = frame[f];
            }
        }
        
        // Upload the modified data back to tensor
        inputTensor.data.Upload(tensorData, inputTensor.shape);
    }

    // Buffer data for dynamic models
    private void BufferHandData()
    {
        // Check if hand tracking is enabled
        bool isLeftHandTracked = dataExtractor.leftOVRHand != null && dataExtractor.leftOVRHand.IsTracked;
        bool isRightHandTracked = dataExtractor.rightOVRHand != null && dataExtractor.rightOVRHand.IsTracked;
        
        float[] currentFrameData = null;
        if (inferenceHandMode == HandMode.LeftHand)
        {
            if (!isLeftHandTracked)
            {
                // Log once when hand tracking state changes
                if (dataBuffer.Count > 0 && dataBuffer[dataBuffer.Count - 1].Sum(Math.Abs) > 0)
                {
                    Debug.LogWarning("Left hand tracking lost");
                }
                return;
            }
            currentFrameData = dataExtractor.leftHandData;
            // NOTE: Dynamic model expects exactly 17 features, transform data is not used
        }
        else if (inferenceHandMode == HandMode.RightHand)
        {
            if (!isRightHandTracked)
            {
                // Log once when hand tracking state changes
                if (dataBuffer.Count > 0 && dataBuffer[dataBuffer.Count - 1].Sum(Math.Abs) > 0)
                {
                    Debug.LogWarning("Right hand tracking lost");
                }
                return;
            }
            currentFrameData = dataExtractor.rightHandData;
            // NOTE: Dynamic model expects exactly 17 features, transform data is not used
        }
        else // TwoHands
        {
            Debug.LogError("Dynamic gesture model currently only supports single hand mode");
            return;
        }

        // Validate data before adding to buffer
        if (currentFrameData != null && currentFrameData.Length >= ExpectedFeatureCount)
        {
            // Clone only the first 17 features
            float[] clonedData = new float[ExpectedFeatureCount];
            System.Array.Copy(currentFrameData, clonedData, ExpectedFeatureCount);
            dataBuffer.Add(clonedData);
        }
        else if (currentFrameData != null)
        {
            Debug.LogWarning($"Dynamic gesture: Expected at least {ExpectedFeatureCount} features, got {currentFrameData.Length}");
        }
        else
        {
            Debug.LogWarning("Dynamic gesture: currentFrameData is null");
        }

        // Ensure buffer does not exceed max length
        while (dataBuffer.Count > SequenceLength)
        {
            dataBuffer.RemoveAt(0);
        }
    }

    void RunInference()
    {
        if (m_ModelType == ModelType.Dynamic)
        {
            RunInference_Dynamic();
        }
        else if (m_ModelType == ModelType.Static)
        {
            RunInference_Static();
        }
    }

    // Inference logic for static models
    private void RunInference_Static()
    {
        // Select hand data for the current frame
        float[] handData;
        if (inferenceHandMode == HandMode.LeftHand)
        {
            handData = dataExtractor.leftHandData;
            if (useTransformData) handData = handData.Concat(dataExtractor.leftHandTransformData).ToArray();
        }
        else if (inferenceHandMode == HandMode.RightHand)
        {
            handData = dataExtractor.rightHandData;
            if (useTransformData) handData = handData.Concat(dataExtractor.rightHandTransformData).ToArray();
        }
        else
        {
            handData = dataExtractor.twoHandsData;
        }

        // Update input tensor with new hand data
        for (int i = 0; i < handData.Length; i++)
        {
            inputTensor[i] = handData[i];
        }

        worker.Execute(inputTensor);
        worker.FlushSchedule(true);          // waits & frees temp buffers
        outputTensor = worker.PeekOutput();
        inferenceOutput = outputTensor[0]; // Static model already applies sigmoid
    }

    // Inference logic for dynamic models
    private void RunInference_Dynamic()
    {
        int currentSequenceLength = dataBuffer.Count;

        if (currentSequenceLength < 10)
        {
            inferenceOutput = 0.0f;
            return;
        }

        FillConv2DInputTensorFromBuffer(currentSequenceLength);
        try
        {   
            try 
            {
                worker.Execute(inputTensor).FlushSchedule(true);
            }
            catch (System.Exception ex)
            {
                Debug.Log("Flush issue");
                inferenceOutput = 0.0f;
                return;
            }
            
            outputTensor = worker.PeekOutput();

            if (outputTensor != null)
            {
                float rawOutput = outputTensor[0];
                
                if (float.IsNaN(rawOutput) || float.IsInfinity(rawOutput))
                {
                    inferenceOutput = 0.0f;
                }
                else
                {
                    inferenceOutput = 1.0f / (1.0f + Mathf.Exp(-rawOutput));
                }
            }
            else
            {
                inferenceOutput = 0.0f;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in RunInference_Dynamic: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            inferenceOutput = 0.0f;
        }
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
            // clean up old inference
            inputTensor.Dispose();
            outputTensor.Dispose();
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

