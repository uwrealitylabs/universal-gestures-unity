using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using UnityEngine;

public class RecordingItem : MonoBehaviour
{
    private int recordingNum; // not zero-indexed
    private string recordingName;

    [SerializeField]
    private TextMeshProUGUI labelText; // index number + recording name

    [SerializeField]
    private TextMeshProUGUI intentText; // "+" or "-"

    private static List<RecordingItem> recordings;

    // initializes a recordingItem with appropriate name, intent
    public void Initialize(string name, Intent intent)
    {
        if(recordings == null)
        {
            recordings = new List<RecordingItem>();
        }

        recordingName = name;
        recordingNum = recordings.Count + 1;

        Debug.Log("Initializing with " + name + " " + recordingNum);

        UpdateLabel();
        
        if(intent == Intent.Positive)
        {
            intentText.text = "+";
        }else if (intent == Intent.Negative)
        {
            intentText.text = "-";
        }

        recordings.Add(this);
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
        recordings.Remove(this);
        UpdateRecordingsList(recordingNum - 1);
        GameObject.Destroy(this);
    }

    // manage indices for the recordings when a recording is deleted
    public static void UpdateRecordingsList(int startIndex)
    {
        for (int i = startIndex; i < recordings.Count; i++)
        {
            recordings[i] = recordings[i].SetRecordingNum(i);
        }
    }

    // must change this later using the file path
    public void OnDestroy()
    {
        // some code to delete the file
    }
}
