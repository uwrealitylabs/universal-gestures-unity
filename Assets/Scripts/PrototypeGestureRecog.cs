using Unity.Barracuda;
using UnityEngine;
using System;
using System.Collections.Generic;

public class PrototypeGestureRecog : MonoBehaviour
{
    [Header("Model/Layer Settings")]
    public NNModel trained_mlp;
    public string target_layer_name = "fc3_1"; //target layer name for N-dim fingerprint 

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
    // I am assuming that we are able to get the gesture label as a string somehow... (not sure how this will work specifically)
    private readonly Dictionary<string, GestureStats> gestureStatsDict =
    new Dictionary<string, GestureStats>(StringComparer.OrdinalIgnoreCase);



    void Start()
    {
        runtime_model = ModelLoader.Load(trained_mlp);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtime_model);
    }


    // ------ Public Runtime Methods ------
    public float[] RunInference(float[] input_arr)
    {
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