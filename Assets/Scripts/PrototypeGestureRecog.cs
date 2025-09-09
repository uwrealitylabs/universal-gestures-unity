using Unity.Barracuda;
using UnityEngine;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;


public class PrototypeGestureRecog : MonoBehaviour
{
    [Header("Model/Layer Settings")]
    public NNModel trained_mlp;
    public string target_layer_name = "fc3_1"; //target layer name for N-dim fingerprint
    public string json_data_directory = "JsonData/"; // where all input JSON tensors are stored (1x17)
    public string specific_test_json = "JsonData/thumbsupLily.json"; //testing purposes (single JSON file)
    public bool testing_mode = true; // if true, only runs inference on specific_test_json file

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
            class_mean = new float[fingerprint_dim];
        }

        public void UpdateClassMean(float[] new_fingerprint)
        {
            if (new_fingerprint == null || new_fingerprint.Length != class_mean.Length)
            {
                Debug.LogError("New fingerprint received is null or has incorrect dimension.");
                return;
            }

            int new_count = sample_count + 1;
            for (int i = 0; i < class_mean.Length; i++)
            {
                // Update class mean using running average formula
                class_mean[i] += (new_fingerprint[i] - class_mean[i]) / new_count;
            }
            sample_count = new_count;
        }
    }

    // Dictionary to map each gesture label to its corresponding GestureStats
    private readonly Dictionary<string, GestureStats> gestureStatsDict =
    new Dictionary<string, GestureStats>(StringComparer.OrdinalIgnoreCase);



    void Start()
    {
        runtime_model = ModelLoader.Load(trained_mlp);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtime_model);

        if (testing_mode)
        {
            string baseName = ExtractGestureNameFromFileName(specific_test_json);
            Debug.Log($"Testing mode enabled. Running inference on single JSON file: {baseName}");
            // TODOs (WIP):
            // 1. Read JSON file to extract the handData tensor (WIP)
            // 2. Call RunInference with the handData tensor to get the fingerprint
            // 3. Call UpdateClass with the gestureName and fingerprint to update the class statistics
        }
        else
        {
            foreach (string path in Directory.GetFiles(json_data_directory, "*.json"))
            {
                string gestureName = ExtractGestureNameFromFileName(path);
                // TODOs (WIP):
                // 1. Read JSON file to extract the handData tensor (WIP)
                // 2. Call RunInference with the handData tensor to get the fingerprint
                // 3. Call UpdateClass with the gestureName and fingerprint to update the class statistics
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


    // ------ Public Runtime Methods ------
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

        return activation_data; // Activation data with L2 norm applied
    }

    public void UpdateClass(string gestureName, float[] fingerprint)
    {
        // get gestureName from the input JSON filename (e.g bunny_ears.json -> gestureName = "bunny_ears")
        if (!gestureStatsDict.TryGetValue(gestureName, out GestureStats stats))
        {
            stats = new GestureStats(fingerprint.Length);
            gestureStatsDict[gestureName] = stats;
        }
        stats.UpdateClassMean(fingerprint);
    }

    void OnDestroy()
    {
        worker.Dispose();
    }
}