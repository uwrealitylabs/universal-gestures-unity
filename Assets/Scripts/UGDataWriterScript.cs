using System;
using System.IO;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using TMPro;


// -- JSON File Writer --
// Press the Rec. Pos. or Rec. Neg. buttons when scene is running to
// start recording positive or negative data.

// Upon pressing the buttons, recording will start after a delay of recordingStartDelay seconds
// and will continue for recordingDuration seconds.

// Each recording is saved to its own json file with name "{gestureName}_{timestamp}.json"
// in JsonData directory.

// JSON files currently saved at: /universal-gestures-unity/JsonData/{gestureName}.json
// JSON file will look like this:
// [
//    {"confidence":0, "handData":[...]},
//    {"confidence":1, "handData":[...]},
//    ...
// ]


public enum RecordingStatus
{
    NotRecording,
    RecordingNegative,
    RecordingPositive
}

public enum GestureType
{
    Static,
    Dynamic
}

public class UGDataWriterScript : MonoBehaviour
{
    // Public parameters
    public GameObject dataExtractorObject;
    public HandMode recordingHandMode;
    public GestureType gestureType = GestureType.Dynamic; // Default to static gestures
    // recordingHandMode = OneHand to record data for one hand, TwoHands to record data for two hands
    public float recordingDuration = 10.0f; // Duration of recording in seconds
    public float recordingStartDelay = 3.0f; // Delay before recording starts
    
    // Dynamic gesture recording parameters
    public float dynamicGestureDuration = 1.0f; // Duration of each dynamic gesture
    public int snapshotsPerGesture = 15; // Number of snapshots to capture per dynamic gesture
    
    public string gestureName;

    private RecordingStatusUI recordingStatusUI;
    private UGDataExtractorScript dataExtractor;
    private RecordingStatus desiredRecordingStatus; // Whether to record positive or negative data
    private float timeToStartRecording = -1; // Time to start recording (used to delay recording start)
    private float startRecordingTime; // Time when data recording started
    [HideInInspector]
    public RecordingStatus recordingStatus;
    [HideInInspector]
    public string writePath;
    [HideInInspector]
    public List<string> writePaths = new();
    [HideInInspector]
    public string recordingFileName; // Name of file to save data to
    
    // For dynamic gesture recording
    private List<float[]> currentGestureSequence = new List<float[]>();
    private float lastSnapshotTime = 0f;
    private float snapshotInterval = 0f;
    
    // Static gesture data class (original)
    [System.Serializable]
    class StaticGestureData
    {
        public int confidence; // confidence of gesture (label)
        public float[] handData; // float array of hand position data (features)
    }
    
    // Dynamic gesture data class (new)
    [System.Serializable]
    class DynamicGestureData
    {
        public int confidence; // confidence of gesture (label)
        public List<float[]> sequenceData; // list of hand snapshots representing the gesture sequence
        
        public DynamicGestureData()
        {
            sequenceData = new List<float[]>();
        }
    }

    void Start()
    {
        if (dataExtractorObject == null)
        {
            Debug.LogError("UGDataWriterScript: dataExtractorObject is not set. Please set it in the inspector.");
            gameObject.SetActive(false);
            return;
        }
        dataExtractor = dataExtractorObject.GetComponent<UGDataExtractorScript>();
    }

    // JsonWrite for static gestures, writes gestureData to json file with name "{gestureName}.json" in JsonData directory.  If file doesn't exist, creates it.
    void JsonWriteStatic(StaticGestureData gestureData)
    {
        string prefix = ",\n    "; // Prefix & Suffix for each entry for proper json formatting
        string suffix = "\n]";
        string jsonDir = Application.dataPath + "/../JsonData/"; // Current directory to save json files
        // Check if running on Android
        if (Application.platform == RuntimePlatform.Android)
        {
            // Save to persistent data path on Android to avoid permission issues and persist data
            jsonDir = Application.persistentDataPath + "/JsonData/";
        }
        // Create JsonData directory if it doesn't exist
        if (!Directory.Exists(jsonDir))
        {
            Directory.CreateDirectory(jsonDir);
        }
        // record file name includes timestamp
        string path = jsonDir + recordingFileName;
        writePath = path;

        if (!File.Exists(path))
        {
            FileStream s = File.Create(path);
            s.Close();
            writePaths.Add(path);
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
        // Debug.Log("Writing to " + gestureName + ".json: '" + jsonString + "'");
        stream.Close();
    }
    
    // JsonWrite for dynamic gestures
    void JsonWriteDynamic(DynamicGestureData gestureData)
    {
        string prefix = ",\n    "; // Prefix & Suffix for each entry for proper json formatting
        string suffix = "\n]";
        string jsonDir = Application.dataPath + "/../JsonData/"; // Current directory to save json files
        // Check if running on Android
        if (Application.platform == RuntimePlatform.Android)
        {
            // Save to persistent data path on Android to avoid permission issues and persist data
            jsonDir = Application.persistentDataPath + "/JsonData/";
        }
        // Create JsonData directory if it doesn't exist
        if (!Directory.Exists(jsonDir))
        {
            Directory.CreateDirectory(jsonDir);
        }
        // record file name includes timestamp and dynamic indicator
        string path = jsonDir + "dynamic_" + recordingFileName;
        writePath = path;

        if (!File.Exists(path))
        {
            FileStream s = File.Create(path);
            s.Close();
            writePaths.Add(path);
        }
        FileStream stream = new FileStream(path, FileMode.Open);
        if (stream.Length == 0)
        {
            prefix = "[\n    ";
        }
        stream.Position = Math.Max(stream.Length - 2, 0);
        
        // Use custom serialization for the sequence data since JsonUtility doesn't handle nested lists well
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append(prefix);
        jsonBuilder.Append("{\"confidence\":");
        jsonBuilder.Append(gestureData.confidence);
        jsonBuilder.Append(",\"sequenceData\":[");
        
        for (int i = 0; i < gestureData.sequenceData.Count; i++)
        {
            float[] snapshot = gestureData.sequenceData[i];
            jsonBuilder.Append("[");
            for (int j = 0; j < snapshot.Length; j++)
            {
                jsonBuilder.Append(snapshot[j].ToString("F6"));
                if (j < snapshot.Length - 1)
                {
                    jsonBuilder.Append(",");
                }
            }
            jsonBuilder.Append("]");
            if (i < gestureData.sequenceData.Count - 1)
            {
                jsonBuilder.Append(",");
            }
        }
        
        jsonBuilder.Append("]}");
        jsonBuilder.Append(suffix);
        
        byte[] insertBytes = Encoding.ASCII.GetBytes(jsonBuilder.ToString());
        stream.Write(insertBytes);
        stream.Close();
    }

    void LateUpdate()
    {
        // Start recording if timeToStartRecording is set and current time is greater than timeToStartRecording
        if (timeToStartRecording > 0 && Time.time >= timeToStartRecording)
        {
            StartRecording();
            timeToStartRecording = -1;
        }

        // Record data if recordingStatus is not NotRecording
        if (recordingStatus != RecordingStatus.NotRecording)
        {
            // Select hand data based on recordingHandMode
            float[] handData;
            if (recordingHandMode == HandMode.LeftHand)
            {
                handData = dataExtractor.leftHandData;
            }
            else if (recordingHandMode == HandMode.RightHand)
            {
                handData = dataExtractor.rightHandData;
            }
            else
            {
                handData = dataExtractor.twoHandsData;
            }
            
            // Branch based on gesture type
            if (gestureType == GestureType.Static)
            {
                // Original static gesture recording
                StaticGestureData gestureData = new StaticGestureData();
                gestureData.handData = handData;
                
                // Set confidence based on recordingStatus
                if (recordingStatus == RecordingStatus.RecordingPositive)
                {
                    gestureData.confidence = 1;
                }
                else if (recordingStatus == RecordingStatus.RecordingNegative)
                {
                    gestureData.confidence = 0;
                }
                
                JsonWriteStatic(gestureData);
                
                // If time since recording started is greater than duration, stop recording
                if (Time.time - startRecordingTime >= recordingDuration)
                {
                    StopRecording();
                }
            }
            else // Dynamic gesture recording
            {
                // Check if it's time to capture a new snapshot
                if (Time.time >= lastSnapshotTime + snapshotInterval)
                {
                    lastSnapshotTime = Time.time;
                    
                    // Add snapshot to current gesture sequence
                    float[] snapshotCopy = new float[handData.Length];
                    Array.Copy(handData, snapshotCopy, handData.Length);
                    currentGestureSequence.Add(snapshotCopy);
                    
                    // If we have collected enough snapshots, save the gesture and start a new one
                    if (currentGestureSequence.Count >= snapshotsPerGesture)
                    {
                        SaveDynamicGesture();
                        currentGestureSequence.Clear();
                    }
                }
                
                // If time since recording started is greater than duration, stop recording
                if (Time.time - startRecordingTime >= recordingDuration)
                {
                    // Save any partial gesture with at least 3 snapshots
                    if (currentGestureSequence.Count >= 3)
                    {
                        SaveDynamicGesture();
                    }
                    StopRecording();
                }
            }
        }
    }
    
    // Save a dynamic gesture sequence
    private void SaveDynamicGesture()
    {
        DynamicGestureData gestureData = new DynamicGestureData();
        gestureData.sequenceData = new List<float[]>(currentGestureSequence);
        
        // Set confidence based on recordingStatus
        if (recordingStatus == RecordingStatus.RecordingPositive)
        {
            gestureData.confidence = 1;
        }
        else if (recordingStatus == RecordingStatus.RecordingNegative)
        {
            gestureData.confidence = 0;
        }
        
        JsonWriteDynamic(gestureData);
    }

    // Begins delay before positive data recording starts
    public void StartRecordingPositiveIntent()
    {
        desiredRecordingStatus = RecordingStatus.RecordingPositive;
        timeToStartRecording = Time.time + recordingStartDelay;
    }

    // Begins delay before negative data recording starts
    public void StartRecordingNegativeIntent()
    {
        desiredRecordingStatus = RecordingStatus.RecordingNegative;
        timeToStartRecording = Time.time + recordingStartDelay;
    }

    // Begins recording data
    public void StartRecording()
    {
        recordingStatus = desiredRecordingStatus;
        recordingFileName = gestureName + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".json";
        startRecordingTime = Time.time;
        
        // For dynamic gestures, calculate the snapshot interval and reset the sequence
        if (gestureType == GestureType.Dynamic)
        {
            snapshotInterval = dynamicGestureDuration / snapshotsPerGesture;
            lastSnapshotTime = Time.time;
            currentGestureSequence.Clear();
        }
    }

    // Stops recording data
    public void StopRecording()
    {
        recordingStatus = RecordingStatus.NotRecording;
    }

    public void SetRecordingHandModeOneHand()
    {
        SetRecordingHandModeRightHand();
    }

    public void SetRecordingHandModeLeftHand()
    {
        recordingHandMode = HandMode.LeftHand;
        // clear paths
        writePaths = new();
    }

    public void SetRecordingHandModeRightHand()
    {
        recordingHandMode = HandMode.RightHand;
        writePaths = new();
    }

    // Sets recording hand mode to two hands
    public void SetRecordingHandModeTwoHands()
    {
        recordingHandMode = HandMode.TwoHands;
        writePaths = new();
    }

    // Sets gesture type to static
    public void SetGestureTypeStatic()
    {
        gestureType = GestureType.Static;
    }

    // Sets gesture type to dynamic
    public void SetGestureTypeDynamic()
    {
        gestureType = GestureType.Dynamic;
    }
}
