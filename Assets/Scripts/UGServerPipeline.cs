using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.IO;
using Newtonsoft.Json;


public class UGServerPipeline : MonoBehaviour
{
    [SerializeField] string uri = "http://10.10.48.17:8080";
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] UGDataWriterScript writer;
    [SerializeField] UGInferenceRunnerScript inference;

    private string twoHandEndpoint = "train_model_two_hands/";
    private string oneHandEndpoint = "train_model_one_hand/";
    private bool isUploading = false;

    // Unity stops coroutines when this object is disabled, so make sure the upload flag doesn't get stuck
    private void OnDisable()
    {
        StopAllCoroutines();
        isUploading = false;
    }

    public void SendTrainingData()
    {
        if (isUploading)
        {
            text.text = "Upload in progress";
            return;
        }
        if (writer.IsRecordingPendingOrActive)
        {
            text.text = "Recording in progress";
            return;
        }

        // Snapshot the session's recordings so later clears/mode switches can't affect this upload
        List<string> paths = writer.writePaths.Where(File.Exists).ToList();
        if (paths.Count == 0)
        {
            text.text = "No recordings to send";
            return;
        }

        string jsonData;
        try
        {
            jsonData = MergeJsonFiles(paths);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is JsonException)
        {
            Debug.LogError("Error reading recordings: " + e.Message);
            text.text = "Failed to read recordings!";
            return;
        }
        if (jsonData == null)
        {
            text.text = "No recordings to send";
            return;
        }

        // Capture the hand mode once so endpoint, model file and LoadModel all agree
        HandMode handMode = writer.recordingHandMode;
        string apiUrl = uri + (handMode == HandMode.TwoHands ? twoHandEndpoint : oneHandEndpoint);

        isUploading = true;
        text.text = "Uploading training data...";
        StartCoroutine(SendJsonData(jsonData, apiUrl, handMode));
    }

    IEnumerator SendJsonData(string jsonData, string apiUrl, HandMode handMode)
    {
        try
        {
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                // Set the request headers and body (content type is JSON)
                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Send the request and wait for a response
                yield return request.SendWebRequest();

                HandleResponse(request, handMode);
            }
        }
        finally
        {
            isUploading = false;
        }
    }

    void HandleResponse(UnityWebRequest request, HandMode handMode)
    {
        if (request.result == UnityWebRequest.Result.Success)
        {

            // Get the binary data from the response (which is the file)
            byte[] fileData = request.downloadHandler.data;

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
            if (handMode == HandMode.TwoHands)
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
            if (inference.LoadModel(filePath, handMode))
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
            text.text = "Upload failed: " + request.error;
        }

    }


    // Merges the JSON arrays in the given files into one JSON array.
    // Returns null if none of the files contain any entries.
    string MergeJsonFiles(List<string> paths)
    {
        List<object> combined = new();
        foreach (string path in paths)
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                continue;
            }
            List<object> entries = JsonConvert.DeserializeObject<List<object>>(json);
            if (entries != null)
            {
                combined.AddRange(entries);
            }
        }

        if (combined.Count == 0)
        {
            return null;
        }
        return JsonConvert.SerializeObject(combined, Formatting.Indented);
    }

    // clear the data buffer (recorded files stay on disk)
    public void clearData()
    {
        writer.ResetSession();
    }
}
