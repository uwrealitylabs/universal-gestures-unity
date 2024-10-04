using System;
using System.IO;
using System.Text;
using UnityEngine;
using TMPro;


// -- JSON File Writer --
// JSON files currently saved at: /universal-gestures-unity/JsonData/{gestureName}.json
// JSON file will look like this:
// [
//    {"confidence":0, "handData":[...]},
//    {"confidence":1, "handData":[...]},
//    ...
// ]

// SHIFT to record positive data (confidence = 1)
// TAB to record negative data (confidence = 0)
// If SHIFT and TAB presseed at same time, no data will be recorded

public class JsonWriter : MonoBehaviour
{
    [SerializeField] private RecordingStatusUI recordingStatusUI;
    public string gestureName;
    class GestureData
    {
        public int confidence; // confidence of gesture (label)
        public float[] handData; // float array of hand position data (features)
    }

    // JsonWrite(gestureData) writes gestureData to json file with name "{gestureName}.json" in JsonData directory.  If file doesn't exist, creates it.
    void JsonWrite(GestureData gestureData)
    {
        string prefix = ",\n    "; // Prefix & Suffix for each entry for proper json formatting
        string suffix = "\n]";
        string jsonDir = Application.dataPath + "/../JsonData/"; // Current directory to save json files
        // Check if running on Android
        if (Application.platform == RuntimePlatform.Android)
        {
            // Save to persistent data path on Android to avoid permission issues and persist data
            jsonDir = Application.persistentDataPath + "/JsonData/";
            // Create JsonData directory if it doesn't exist
            if (!Directory.Exists(jsonDir))
            {
                Directory.CreateDirectory(jsonDir);
            }
        }
        string path = jsonDir + gestureName + ".json";
        if (!File.Exists(path))
        {
            File.Create(path);
        }
        FileStream stream = new FileStream(path, FileMode.Open);
        if (stream.Length == 0)
        {
            prefix = "[\n    ";
        }
        stream.Position = Math.Max(stream.Length - 2, 0);
        string jsonString = prefix + JsonUtility.ToJson(gestureData) + suffix;
        byte[] insertBytes = Encoding.ASCII.GetBytes(jsonString);
        stream.Write(insertBytes);
        Debug.Log("Writing to " + gestureName + ".json: '" + jsonString + "'");
        stream.Close();
    }

    void LateUpdate()
    {
        // When SHIFT or TAB pressed:
        // - Retrieve hand data from TestingSkeleton 
        // - If SHIFT, set confidence to 1, else 0
        // - Write data to json file

        // Temporary code to test two hand data collection
        // Automatically starts recording positive data
        GestureData gestureData = new GestureData();
        gestureData.confidence = 1;
        // gestureData.handData = TestingSkeleton.handData;
        // replace above line with below line to test two hand data collection
        gestureData.handData = TestingSkeletonTwoHands.handData;
        JsonWrite(gestureData);
        recordingStatusUI.recordingStatus = RecordingStatus.RecordingPositive;

        // if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Tab))
        // {
        //     GestureData gestureData = new GestureData();
        //     gestureData.confidence = 1;

        //     gestureData.handData = TestingSkeleton.handData;

        //     JsonWrite(gestureData);

        //     recordingStatusUI.recordingStatus = RecordingStatus.RecordingPositive;
        // }
        // else if (Input.GetKey(KeyCode.Tab) && !Input.GetKey(KeyCode.LeftShift))
        // {
        //     GestureData gestureData = new GestureData();
        //     gestureData.confidence = 0;

        //     gestureData.handData = TestingSkeleton.handData;

        //     JsonWrite(gestureData);

        //     recordingStatusUI.recordingStatus = RecordingStatus.RecordingNegative;
        // }
        // else recordingStatusUI.recordingStatus = RecordingStatus.NotRecording;

        recordingStatusUI.targetFile = gestureName + ".json";
    }
}
