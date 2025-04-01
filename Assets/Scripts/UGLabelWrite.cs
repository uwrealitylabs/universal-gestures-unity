using System;
using System.IO;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Reflection.Emit;

/*public enum RecordingStatus
{
    NotRecording,
    RecordingNegative,
    RecordingPositive
}*/

public class UGLabelWrite : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject dataExtractorObject;
    public HandMode recordingHandMode;

    public float recordingDuration = 10.0f; // Duration of recording in seconds
    public float recordingStartDelay = 3.0f; // Delay before recording starts
    public string gestureLabel;

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
    class GestureData
    {
        public string label; // gesture (label)
        public float[] handData; // float array of hand position data (features)
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

    // Update is called once per frame
    void LateUpdate()
    {
        if (timeToStartRecording > 0 && Time.time >= timeToStartRecording)
        {
            StartRecording();
            timeToStartRecording = -1;
        }

        // Record data if recordingStatus is not NotRecording
        if (recordingStatus != RecordingStatus.NotRecording)
        {
            GestureData gestureData = new GestureData();
            // select hand data based on recordingHandMode
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
            gestureData.handData = handData;

            // Set confidence based on recordingStatus (positive or negative data)
            if ((recordingStatus == RecordingStatus.RecordingPositive) || (recordingStatus == RecordingStatus.RecordingNegative))
            {
                gestureData.label = gestureLabel;

                JsonWrite(gestureData);

                // If time since recording started is greater than duration, stop recording
                if (Time.time - startRecordingTime >= recordingDuration)
                {
                    StopRecording();
                }
            }
        }

        void JsonWrite(GestureData gestureData)
        {
            string prefix = ",\n    "; // Prefix & Suffix for each entry for proper json formatting
            string suffix = "\n]";
            string jsonDir = Application.dataPath + "/../JsonData/";

            if (Application.platform == RuntimePlatform.Android)
            {
                // Save to persistent data path on Android to avoid permission issues and persist data
                jsonDir = Application.persistentDataPath + "/JsonData/";
            }

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
    }

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
        recordingFileName = gestureLabel + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".json";
        startRecordingTime = Time.time;
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
}
