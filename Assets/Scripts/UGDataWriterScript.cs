using System;
using System.IO;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;


// -- JSON File Writer --
// Press the Rec. Pos. or Rec. Neg. buttons when scene is running to
// start recording positive or negative data.
//
// Upon pressing the buttons, recording will start after a delay of recordingStartDelay seconds
// and will continue for recordingDuration seconds.
//
// Each recording is saved to its own json file with name "{gestureName}_{timestamp}.json"
// in JsonData directory.
//
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

// NEW — specify whether the gesture is a single static pose or a time‑varying sequence
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
    // recordingHandMode = OneHand to record data for one hand, TwoHands to record data for two hands

    // Toggle between static and dynamic recordings
    public GestureType gestureType = GestureType.Dynamic;

    public float recordingDuration   = 10.0f; // Duration of recording in seconds
    public float recordingStartDelay = 3.0f;  // Delay before recording starts

    // Dynamic‑gesture‑specific parameters
    public float dynamicGestureDuration = 1.0f; // Target length of each dynamic gesture (seconds)
    public int snapshotsPerGesture    = 15;   // Frames per dynamic gesture

    public string gestureName;
    public bool recordTransformData;

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


    private readonly List<float[]> currentGestureSequence = new();
    private float lastSnapshotTime = 0f;
    private float snapshotInterval = 0f;
    [Serializable]
    private class GestureData
    {
        public int     confidence; // confidence of gesture (label)
        public float[] handData;   // float array of hand position data (features)
    }

    [Serializable]
    private class DynamicGestureData
    {
        public int           confidence;    // confidence label
        public List<float[]> sequenceData;  // list of snapshot feature vectors

        public DynamicGestureData() => sequenceData = new List<float[]>();
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

    void LateUpdate()
    {
        // Start recording if timeToStartRecording is set and current time is greater than timeToStartRecording
        if (timeToStartRecording > 0 && Time.time >= timeToStartRecording)
        {
            StartRecording();
            timeToStartRecording = -1;
        }

        // If we are not currently recording, exit early
        if (recordingStatus == RecordingStatus.NotRecording) return;

        // select hand data based on recordingHandMode
        float[] handData;
        if (recordingHandMode == HandMode.LeftHand)
        {
            handData = dataExtractor.leftHandData;
            if (recordTransformData)
            {
                handData = handData.Concat(dataExtractor.leftHandTransformData).ToArray();
            }
        }
        else if (recordingHandMode == HandMode.RightHand)
        {
            handData = dataExtractor.rightHandData;
            if (recordTransformData)
            {
                handData = handData.Concat(dataExtractor.rightHandTransformData).ToArray();
            }
        }
        else // TwoHands
        {
            handData = dataExtractor.twoHandsData;
        }

        // Handle static versus dynamic recording types
        if (gestureType == GestureType.Static)
        {
            GestureData gestureData = new GestureData();
            gestureData.handData = handData;

            // Set confidence based on recordingStatus (positive or negative data)
            if (recordingStatus == RecordingStatus.RecordingPositive)
            {
                gestureData.confidence = 1;
            }
            else if (recordingStatus == RecordingStatus.RecordingNegative)
            {
                gestureData.confidence = 0;
            }

            JsonWrite(gestureData);

            // If time since recording started is greater than duration, stop recording
            if (Time.time - startRecordingTime >= recordingDuration)
            {
                StopRecording();
            }
        }
        else // GestureType.Dynamic
        {
            // Capture snapshots at fixed intervals
            if (Time.time >= lastSnapshotTime + snapshotInterval)
            {
                lastSnapshotTime = Time.time;

                float[] snapshotCopy = new float[handData.Length];
                Array.Copy(handData, snapshotCopy, handData.Length);
                currentGestureSequence.Add(snapshotCopy);

                // Once we have the full sequence, persist it
                if (currentGestureSequence.Count >= snapshotsPerGesture)
                {
                    SaveDynamicGesture();
                    currentGestureSequence.Clear();
                }
            }

            // If time since recording started is greater than duration, stop recording
            if (Time.time - startRecordingTime >= recordingDuration)
            {
                // Persist any partial sequence (ensures we keep at least a minimal gesture)
                if (currentGestureSequence.Count >= 3)
                {
                    SaveDynamicGesture();
                }
                StopRecording();
            }
        }
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

        if (gestureType == GestureType.Dynamic)
        {
            snapshotInterval = dynamicGestureDuration / Mathf.Max(1, snapshotsPerGesture);
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

    public void SetGestureTypeStatic()  => gestureType = GestureType.Static;
    public void SetGestureTypeDynamic() => gestureType = GestureType.Dynamic;

    // JsonWrite(gestureData) writes gestureData to json file with name "{gestureName}.json" in JsonData directory. If file doesn't exist, creates it.
    void JsonWrite(GestureData gestureData)
    {
        string prefix  = ",\n    "; // Prefix & Suffix for each entry for proper json formatting
        string suffix  = "\n]";
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
            using var s = File.Create(path);
            writePaths.Add(path);
        }

        using var stream = new FileStream(path, FileMode.Open);
        if (stream.Length == 0)
        {
            prefix = "[\n    ";
        }
        stream.Position = Math.Max(stream.Length - 2, 0);
        string jsonString = prefix + JsonUtility.ToJson(gestureData) + suffix;
        byte[] insertBytes = Encoding.ASCII.GetBytes(jsonString);
        stream.Write(insertBytes);
    }

    private void JsonWriteDynamic(DynamicGestureData gestureData)
    {
        string prefix  = ",\n    ";
        string suffix  = "\n]";
        string jsonDir = Application.dataPath + "/../JsonData/";
        if (Application.platform == RuntimePlatform.Android)
            jsonDir = Application.persistentDataPath + "/JsonData/";
        if (!Directory.Exists(jsonDir)) Directory.CreateDirectory(jsonDir);

        string path = jsonDir + "dynamic_" + recordingFileName;
        writePath = path;
        if (!File.Exists(path))
        {
            using var s = File.Create(path);
            writePaths.Add(path);
        }

        using var stream = new FileStream(path, FileMode.Open);
        if (stream.Length == 0) prefix = "[\n    ";
        stream.Position = Math.Max(stream.Length - 2, 0);

        // Manual serialisation for nested list (JsonUtility can’t handle jagged arrays)
        var builder = new StringBuilder();
        builder.Append(prefix);
        builder.Append("{\"confidence\":").Append(gestureData.confidence).Append(",\"sequenceData\":[");
        for (int i = 0; i < gestureData.sequenceData.Count; i++)
        {
            float[] snap = gestureData.sequenceData[i];
            builder.Append("[");
            for (int j = 0; j < snap.Length; j++)
            {
                builder.Append(snap[j].ToString("F6"));
                if (j < snap.Length - 1) builder.Append(',');
            }
            builder.Append("]");
            if (i < gestureData.sequenceData.Count - 1) builder.Append(',');
        }
        builder.Append("]}").Append(suffix);

        byte[] insert = Encoding.ASCII.GetBytes(builder.ToString());
        stream.Write(insert);
    }

    private void SaveDynamicGesture()
    {
        if (currentGestureSequence.Count == 0) return;
        var dg = new DynamicGestureData
        {
            confidence = recordingStatus == RecordingStatus.RecordingPositive ? 1 : 0,
            sequenceData = new List<float[]>(currentGestureSequence)
        };
        JsonWriteDynamic(dg);
    }
}
