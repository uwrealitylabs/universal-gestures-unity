using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Google.Protobuf;
using TMPro;
using UnityEngine;
using System.IO;

public class RecordingItem : MonoBehaviour
{
    private int recordingNum; // not zero-indexed
    private string recordingName;
    private string filePath;

    [SerializeField]
    private TextMeshProUGUI labelText; // index number + recording name

    [SerializeField]
    private TextMeshProUGUI intentText; // "+" or "-"

    private static List<RecordingItem> recordings;
    private static string folderDir;

    // initializes a recordingItem with appropriate name, intent

    public void Initialize(string name, Intent? intent)
    {
        if (recordings == null)
        {
            recordings = new List<RecordingItem>();
        }
        recordings.Add(this);

        recordingName = name;
        recordingNum = recordings.Count;
        filePath = folderDir + recordingName;

        Debug.Log("Initializing with " + name + " " + recordingNum);

        UpdateLabel();

        Intent? myintent = intent;

        if (intent == null)
        {
            StreamReader reader = new StreamReader(filePath);
            reader.ReadLine();
            string myline = reader.ReadLine();
            char intentIndicator = myline[18];
            reader.Close();

            Debug.Log("Intent Indicator: " + intentIndicator);

            if (intentIndicator == '0')
            {
                myintent = Intent.Negative;
            }
            else if (intentIndicator == '1')
            {
                myintent = Intent.Positive;
            }
        }
        if (myintent == Intent.Positive)
        {
            intentText.text = "+";
        }
        else if (myintent == Intent.Negative)
        {
            intentText.text = "-";
        }
    }

    // sets the recordingNum of a recording item while updating the label
    //     increments the index by 1 because indices are 0 indexed and we don't want that
    public RecordingItem SetRecordingNum(int index)
    {
        recordingNum = index + 1;
        UpdateLabel();
        return this;
    }

    // updates the label text
    public void UpdateLabel()
    {
        labelText.text = string.Format("{00}", recordingNum) + " - " + recordingName;
    }

    // deletes the recording from the visual list while also removing it from the static list of recordings,
    //    which keeps track of the indices
    public void DeleteRecording()
    {
        Debug.Log("Deleting Recording");

        // remove from the list
        recordings.RemoveAt(recordingNum - 1);
        UpdateRecordingsList(recordingNum - 1);

        // delete the actual file
        if (!File.Exists(filePath))
        {
            Debug.Log("File does not exist (file path: " + filePath);
        }
        else
        {
            File.Delete(filePath);
            Debug.Log("Deleting File");
        }

        // destroy the gameobject that this script is attached to (the recordingitem)
        Destroy(gameObject);
    }

    // manage indices for the recordings when a recording is deleted
    public static void UpdateRecordingsList(int startIndex)
    {
        for (int i = startIndex; i < recordings.Count; i++)
        {
            recordings[i] = recordings[i].SetRecordingNum(i);
        }
    }

    public static void SetFolderDir(string folder)
    {
        folderDir = folder;
    }
}
