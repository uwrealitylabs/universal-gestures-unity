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
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtime_model);
    }


    public float[] RunInference(float[] input_arr, bool l2_normalize = true)
    {
        Tensor input_tensor = new Tensor(1, input_arr.Length, input_arr);
        worker.Execute(input_tensor);
        Tensor n_dim_fingerprint = worker.PeekOutput(target_layer_name); // layer containing N-dim fingerprint 
        float[] activation_data = n_dim_fingerprint.ToReadOnlyArray(); // convert tensor values into a float array for Unity to use

        //Clean up unused tensors to avoid memory leaks
        input_tensor.Dispose();
        n_dim_fingerprint.Dispose();

        if (l2_normalize)
        {
            activation_data = DetermineL2Norm(activation_data);
        }

        return activation_data;
    }

    private float[] DetermineL2Norm(float[] emb)
    {
        float norm_val = 0f;
        for (int i = 0; i < emb.Length; i++)
        {
            norm_val += emb[i] * emb[i];
        }

        norm_val = Mathf.Sqrt(norm_val) + 1e-10f;
        for (int i = 0; i < emb.Length; i++)
        {
            emb[i] /= norm_val;
        }
    
        return emb;
    }

    void OnDestroy()
    {
        worker.Dispose();
    }
}