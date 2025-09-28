using Unity.Barracuda;
using UnityEngine;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;


public class PrototypeGestureRecog : MonoBehaviour
{
    // ---- Start of initialization / class definitions ----
    [Header("Model/Layer Settings")]
    public NNModel trained_mlp;
    public string target_layer_name = "fc3_1"; //target layer name for N-dim fingerprint
    public string json_data_directory = "JsonData/"; // where all input JSON tensors are stored (1x17)
    public string specific_test_json = "JsonData/thumbsupLily.json"; //testing purposes (single JSON file)
    public bool testing_mode = true; // if true, only runs inference on specific_test_json file (manually toggle this for now)

    [SerializeField] private Model runtime_model;
    [SerializeField] private IWorker worker;
    

    [Serializable]
    public class GestureStats
    {
        // Class to hold gesture statistics regarding classwise means and sample count
        public int sample_count;
        public float[] class_mean;

        public GestureStats(int fingerprint_dim)
        {
            // Constructor to initialize gesture stats for a given fingerprint dim
            sample_count = 0;
            class_mean = new float[fingerprint_dim]; // the "average" fingerprint for this gesture clas
        }

        public void UpdateClassMean(float[] new_fingerprint)
        {
            if (new_fingerprint == null || new_fingerprint.Length != class_mean.Length)
            {
                Debug.LogError("New fingerprint is null or has incorrect dimension.");
                return;
            }

            for (int i = 0; i < class_mean.Length; i++)
            {
                // Update class mean by taking a running average
                class_mean[i] += (new_fingerprint[i] - class_mean[i]) / new_count;
            }
            sample_count++;
        } 
    }

    [Serializable]
    public class SingleGesture
    {
        public float[] raw_input; // original 1x17 input from the JSON;
        public string gestureName; // name of the Gesture the input corresponds to
    }

    // Dictionary to map each gesture label to its corresponding GestureStats
    private readonly Dictionary<string, GestureStats> gestureStatsDict =
    new Dictionary<string, GestureStats>(StringComparer.OrdinalIgnoreCase);

    [Serializable]
    public class HandDataSample
    {
        public float confidence;
        public float[] handData;
    }

    [Serializable]
    public class HandDataWrapper
    {
        // Can't directly serialize arrays, need a wrapper class HandDataWrapper to do so
        public HandDataSample[] samples; // Array of multiple hand data samples from the JSON
    }


 


// --- All Class Definitions and initializations end here ---

    void Start()
    {
        runtime_model = ModelLoader.Load(trained_mlp);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtime_model);

        if (testing_mode)
        {
            string baseName = ExtractGestureNameFromFileName(specific_test_json);
            Debug.Log($"Testing mode enabled. Running inference on single JSON file: {baseName}");

            SingleGesture test_gesture = LoadGestureSample(specific_test_json);
            if (test_gesture.raw_input != null)
            {
                float[] fingerprint = RunInference(test_gesture.raw_input);
                UpdateClass(test_gesture.gestureName, fingerprint); //update the class statistics with the new fingerprint
                Debug.Log($"Processed gesture: {test_gesture.gestureName}");

                // Now want to compare the fingerprint to all other stored fingerprints to get confidences
                
            }
            else
            {
                Debug.LogError("Failed to load gesture data from the specified JSON file.");
            }
        }

        else
        {
            foreach (string path in Directory.GetFiles(json_data_directory, "*.json"))
            {
                string gestureName = ExtractGestureNameFromFileName(path);
                Debug.Log($"Processing file: {path} with gesture label: {gestureName}");
                SingleGesture gesture = LoadGestureSample(path);
                if (gesture.raw_input != null)
                {
                    float[] fingerprint = RunInference(gesture.raw_input);
                    UpdateClass(gesture.gestureName, fingerprint); //update the class statistics with the new fingerprint
                }
                else
                {
                    Debug.LogError($"Failed to load gesture data from file: {path}");
                }
            }
        }        
    }

    private string ExtractGestureNameFromFileName(string filePath)
    {
        string baseName = Path.GetFileNameWithoutExtension(filePath);
        // split on first underscore/dash/space to isolate label prefix
        var m = Regex.Match(baseName, @"^([^_\-\s]+)");
        return m.Success ? m.Groups[1].Value : baseName;
    }

   
    private SingleGesture LoadGestureSample(string path)
    {
        string json = File.ReadAllText(path);
        // Wrap the array in an object since Unity JsonUtility doesn't handle root arrays
        string wrappedJson = $"{{\"samples\":{json}}}";
        HandDataWrapper wrapper = JsonUtility.FromJson<HandDataWrapper>(wrappedJson);
        
        // Extract gesture name from filename
        string gestureName = ExtractGestureNameFromFileName(path);
        
        // For now, just take the first sample's handData and return it
        float[] raw_input = wrapper.samples.Length > 0 ? wrapper.samples[0].handData : null;
        return new SingleGesture { gesture_name = gestureName, raw_input = raw_input };
    }

    public float[] RunInference(float[] input_arr)
    {
        // gets the N-dim fingerprint from the target layer of the MLP (128-dim)
        Tensor input_tensor = new Tensor(1, input_arr.Length, input_arr);
        worker.Execute(input_tensor);
        Tensor n_dim_fingerprint = worker.PeekOutput(target_layer_name); // layer containing N-dim fingerprint 
        float[] activation_data = n_dim_fingerprint.ToReadOnlyArray(); // convert tensor values into a float array for Unity to use

        //Clean up unused tensors to avoid memory leaks
        input_tensor.Dispose();
        n_dim_fingerprint.Dispose();

        return activation_data; // Return the N-dim fingerprint we are interested in
    }

    public void UpdateClass(string gestureName, float[] fingerprint)
    {
        if (!gestureStatsDict.TryGetValue(gestureName, out GestureStats stats))
        {
            // If gesture is not present in the dictionary, initialize a new GestureStats object
            stats = new GestureStats(fingerprint.Length);
            gestureStatsDict[gestureName] = stats;
        }
        stats.UpdateClassMean(fingerprint);
    }


    public float[] ComputeDistances(float[] fingerprint)
    {
        // Comupute the distances between the input fingerprint all all other stored class means
        List<float> distances = new List<float>();
        foreach (var kvp in gestureStatsDict)
        {
            string gestureName = kvp.Key;
            GestureStats stats = kvp.Value;

            // Compute Euclidean distance between input fingerprint and class mean
            float sum_sq = 0f;
            for (int i = 0; i < fingerprint.Length; i++)
            {
                float diff = fingerprint[i] - stats.class_mean[i];
                sum_sq += diff * diff;
            }
            float distance = Mathf.Sqrt(sum_sq);
            distances.Add(distance);
        }


        // Perform softmax to get confidence scores
        float sum_exp = 0f;
        for (int i = 0; i < distances.Count; i++)
        {
            sum_exp += Mathf.Exp(distances[i]);
        }

        for (int i = 0; i < distances.Count; i++)
        {
            distances[i] = Mathf.Exp(distances[i]) / sum_exp; // softmax element-wise normalization
        }
    
        return distances.ToArray();
    }


    void OnDestroy()
    {
        worker.Dispose();
    }
}