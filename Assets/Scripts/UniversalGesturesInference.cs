using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq; // idk if its good to have this installed
using Unity.Barracuda;
using TMPro;

public class UniversalGesturesInference : MonoBehaviour
{
    public NNModel modelAsset;
    private Model m_RuntimeModel;
    public GameObject handObject;
    private float inferenceTimer;
    private IWorker worker;
    public string modelName;
    private Dictionary<string, float[,]> weights;
    private Tensor inputTensor;
    private Tensor outputTensor;
    public float inferenceOutput { get; private set; }
    //  [SerializeField] private TextMeshProUGUI inferenceText; //moved to AnalyticsDisplay

    void Start()
    {
        // see docs for more information on this script: https://docs.unity3d.com/Packages/com.unity.barracuda%401.0/manual/GettingStarted.html
        Debug.Log("Testing Inference");
        inferenceTimer = 0;
        m_RuntimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, m_RuntimeModel);
        inputTensor = new Tensor(1, 0, 0, 17);



        // LoadWeights("../JsonData/modelWeights.json");
    }

    void Update()
    {
        // run inference every second
        inferenceTimer += Time.deltaTime;
        if (inferenceTimer >= 1)
        {
            inferenceTimer = 0;
            // update input tensor with new hand data
            for (int i = 0; i < TestingSkeleton.handData.Length; i++)
            {
                inputTensor[i] = TestingSkeleton.handData[i];
            }
            worker.Execute(inputTensor);
            outputTensor = worker.PeekOutput();
            inferenceOutput = outputTensor[0];
            Debug.Log("Inference Output: " + inferenceOutput);
            //inferenceText.text = "Inference Output: " + inferenceOutput; //moved to AnalyticsDisplay
        }
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     float inference = GetInference();
        //     Debug.Log("Inference: " + inference);
        // }
    }

    void OnDestroy()
    {
        inputTensor.Dispose();
        outputTensor.Dispose();
        worker.Dispose();
    }

    // Below are deprecated methods for manual inference
    void LoadWeights(string filePath)
    {
        try
        {
            string jsonString = File.ReadAllText(filePath);
            JObject json = JObject.Parse(jsonString);

            weights = new Dictionary<string, float[,]>();

            foreach (var item in json)
            {
                string key = item.Key;

                JArray valueArray = (JArray)item.Value;
                int rows = valueArray.Count;
                int cols = valueArray[0].Count();
                float[,] valueMatrix = new float[rows, cols];

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        valueMatrix[i, j] = (float)valueArray[i][j];
                    }
                }

                weights[key] = valueMatrix;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error loading weights: " + e.Message);
        }
    }

    public float GetInference()
    {
        float[] inputVector = CreateInputVector();
        // Debug.Log("Input Vector: " + inputVector);
        float output = RunInference(inputVector);
        return output;
    }

    float[] CreateInputVector()
    {
        float[] inputVector = TestingSkeleton.handData;
        // Print out inputVector to see if it's correct
        for (int i = 0; i < inputVector.Length; i++)
        {
            Debug.Log("Input Vector[" + i + "]: " + inputVector[i]);
        }
        return inputVector;
    }

    float RunInference(float[] inputVector)
    {
        // again also assuming a two-layer neural network, can change to be general if needed (did this based off test data)
        float[] fc1Output = new float[weights["fc1.weight"].GetLength(0)];

        for (int i = 0; i < fc1Output.Length; i++)
        {
            for (int j = 0; j < inputVector.Length; j++)
            {
                fc1Output[i] += inputVector[j] * weights["fc1.weight"][i, j];
            }
            fc1Output[i] = (float)Math.Tanh(fc1Output[i]); // Activation function (not too sure, test to see if it works)
        }

        float[] fc2Output = new float[weights["fc2.weight"].GetLength(0)];

        for (int i = 0; i < fc2Output.Length; i++)
        {
            for (int j = 0; j < fc1Output.Length; j++)
            {
                fc2Output[i] += fc1Output[j] * weights["fc2.weight"][i, j];
            }
            fc2Output[i] = (float)Math.Tanh(fc2Output[i]); // Activation function (not sure same as above)
        }

        return fc2Output[0];
    }
}
