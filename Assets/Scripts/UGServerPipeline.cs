using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.IO;
using Newtonsoft.Json;
using System.Linq;


public class UGServerPipeline : MonoBehaviour
{
    [SerializeField] string uri = "http://10.10.48.17:8080";
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] UGDataWriterScript writer;
    [SerializeField] UGInferenceRunnerScript inference;

    // ID of the saved gesture being trained (set by the gesture-creation code).
    // When set: all of that gesture's saved recordings are sent, and the returned model is
    // saved with the gesture under a unique name (no more one_hand.onnx / two_hands.onnx overwrites).
    // When empty: the old behaviour is used.
    [Tooltip("ID of the saved gesture to train. Leave empty for the old behaviour.")]
    public string gestureId;

    GestureRecord ActiveGesture => string.IsNullOrEmpty(gestureId) ? null : GestureLibrary.Get(gestureId);

    private string twoHandEndpoint = "train_model_two_hands/";
    private string oneHandEndpoint = "train_model_one_hand/";
    string apiUrl;
    private void Start()
    {
        apiUrl = uri;
    }

    public void SendTrainingData()
    {
        GestureRecord gesture = ActiveGesture;
        HandMode mode = gesture != null ? gesture.settings.handMode : writer.recordingHandMode;
        if (mode == HandMode.TwoHands)
        {
            apiUrl = uri + twoHandEndpoint;
        }
        else
        {
            apiUrl = uri + oneHandEndpoint;
        }
        StartCoroutine(SendJsonData());

    }
    IEnumerator SendJsonData()
    {
        GestureRecord gesture = ActiveGesture;
        string jsonData;

        if (gesture != null)
        {
            // Saved gesture: send ALL its recordings, including ones from earlier sessions
            List<string> paths = GestureLibrary.RecordingPaths(gesture.id).Where(File.Exists).ToList();
            if (paths.Count == 0)
            {
                text.text = "Record some examples first!";
                yield break;
            }
            jsonData = File.ReadAllText(paths[0]);
            for (int i = 1; i < paths.Count; i++)
            {
                jsonData = CombineJsonFiles(jsonData, paths[i]);
            }
        }
        else
        {
            // Load the JSON file from Resources (or wherever it's located)
            string jsonPath = writer.writePath;
            // Read the JSON file
            jsonData = File.ReadAllText(jsonPath);
            // iterate over all but the last one which we read above
            for (int i = 0; i < writer.writePaths.Count - 1; i++)
            {
                jsonData = CombineJsonFiles(jsonData, writer.writePaths[i]);
            }
        }

        // Create a UnityWebRequest POST request
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");

        // Set the request headers and body (content type is JSON)
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request and wait for a response
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {

            // Get the binary data from the response (which is the file)
            byte[] fileData = request.downloadHandler.data;

            if (gesture != null)
            {
                // Saved gesture: store under a unique name and link it to the gesture
                GestureLibrary.SaveTrainedModel(gesture.id, fileData);
                text.text = "File Downloaded! Loading Model";
                // LoadGesture (not LoadModel) so the gesture's saved threshold/settings and loadedGestureId apply right away
                text.text = inference.LoadGesture(GestureLibrary.Get(gesture.id))
                    ? $"Trained and saved \"{gesture.displayName}\"!"
                    : "Failed to load model!";
                yield break;
            }

            // Save the file to a directory
            string directory = Application.dataPath + "/../serverModels/";

            if (Application.platform == RuntimePlatform.Android)
            {
                // Save to persistent data path on Android to avoid permission issues and persist data
                directory = Application.persistentDataPath + "/serverModels/";
            }
            // Create directory if it doesn't exist
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string filePath = directory;
            if (writer.recordingHandMode == HandMode.TwoHands)
            {
                filePath += "two_hands.onnx";
            }
            else
            {
                filePath += "one_hand.onnx";
            }
            File.WriteAllBytes(filePath, fileData);


            // Debug.Log("File saved at: " + filePath);
            text.text = "File Downloaded! Loading Model";
            if (inference.LoadModel(filePath, writer.recordingHandMode))
            {
                text.text = "Loaded model!";

            }
            else
            {
                text.text = "Failed to load model!";
            }

        }
        else
        {
            Debug.LogError("Error sending data: " + request.error);
        }

    }


    string CombineJsonFiles(string data, string file2)
    {
        // Read the JSON files
        string json1 = data;
        string json2 = File.ReadAllText(file2);

        // Parse the JSON into lists
        List<object> list1 = JsonConvert.DeserializeObject<List<object>>(json1);
        List<object> list2 = JsonConvert.DeserializeObject<List<object>>(json2);

        // Combine the lists
        list1.AddRange(list2);

        // Convert back to a JSON string
        return JsonConvert.SerializeObject(list1, Formatting.Indented);
    }
    // clear the data buffer
    public void clearData()
    {
        writer.writePaths = new();
    }
}
