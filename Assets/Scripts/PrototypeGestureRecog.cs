using Unity.Barracuda;
using UnityEngine;


public class PrototypeGestureRecog : MonoBehaviour
{
    public NNModel trained_mlp;
    public string target_layer_name = "fc3_1"; //target layer name for N-dim fingerprint 
    [SerializeField] private Model runtime_model;
    [SerializeField] private IWorker worker;


    void Start()
    {
        runtime_model = ModelLoader.Load(trained_mlp);
        worker = WorkerFactory.CreateWorker(WorkerType.Compute, runtime_model);
    }


    public float[] run_inference(float[] input_arr)
    {
        Tensor input_tensor = new Tensor(1, input_arr.Length, input_arr);
        worker.Execute(input_tensor);
        Tensor output_tensor = worker.PeekOutput(target_layer_name); // final output (not used
        Tensor n_dim_fingerprint = worker.PeekOutput("fc3_1"); // layer containing N-dim fingerprint 
        float[] activation_data = n_dim_fingerprint.ToReadOnlyArray(); //final output data transferred to array

        //Clean up unused tensors to avoid memory leaks
        input_tensor.Dispose();
        output_tensor.Dispose();
        n_dim_fingerprint.Dispose();

        return activation_data;

    }

    void OnDestroy()
    {
        worker.Dispose();
    }
}