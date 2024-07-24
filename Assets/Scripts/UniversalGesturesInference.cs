using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq; // idk if its good to have this installed

public class UniversalGesturesInference : MonoBehaviour {
    public GameObject handObject;
    public string modelName;
    private Dictionary<string, float[,]> weights;

    void Start() {
        LoadWeights("../JsonData/modelWeights.json");
    }

    void LoadWeights(string filePath) {
        try {
            string jsonString = File.ReadAllText(filePath);
            JObject json = JObject.Parse(jsonString);

            weights = new Dictionary<string, float[,]>();

            foreach (var item in json) {
                string key = item.Key;

                JArray valueArray = (JArray)item.Value;
                int rows = valueArray.Count;
                int cols = valueArray[0].Count();
                float[,] valueMatrix = new float[rows, cols];

                for (int i = 0; i < rows; i++) {
                    for (int j = 0; j < cols; j++) {
                        valueMatrix[i, j] = (float)valueArray[i][j];
                    }
                }

                weights[key] = valueMatrix;
            }
        }
        catch (Exception e) {
            Debug.LogError("Error loading weights: " + e.Message);
        }
    }

    public float GetInference() {
        float[] inputVector = CreateInputVector();
        float output = RunInference(inputVector);
        return output;
    }

    float[] CreateInputVector() {
        List<float> inputVector = TestingSkeleton.handData.ToList();
        return inputVector.ToArray();
    }

    float RunInference(float[] inputVector) {
        // again also assuming a two-layer neural network, can change to be general if needed (did this based off test data)
        float[] fc1Output = new float[weights["fc1.weight"].GetLength(0)];
        
        for (int i = 0; i < fc1Output.Length; i++) {
            for (int j = 0; j < inputVector.Length; j++) {
                fc1Output[i] += inputVector[j] * weights["fc1.weight"][i, j];
            }
            fc1Output[i] = (float)Math.Tanh(fc1Output[i]); // Activation function (not too sure, test to see if it works)
        }

        float[] fc2Output = new float[weights["fc2.weight"].GetLength(0)];
        
        for (int i = 0; i < fc2Output.Length; i++) {
            for (int j = 0; j < fc1Output.Length; j++) {
                fc2Output[i] += fc1Output[j] * weights["fc2.weight"][i, j];
            }
            fc2Output[i] = (float)Math.Tanh(fc2Output[i]); // Activation function (not sure same as above)
        }

        return fc2Output[0];
    }
}
