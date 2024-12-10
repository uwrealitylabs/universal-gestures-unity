using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System;

public enum RecordingStatus
{
    NotRecording,
    RecordingNegative,
    RecordingPositive
}

public enum Intent
{
    Positive,
    Negative
}

public class RecordingStatusUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetFileText;
    [SerializeField] private TextMeshProUGUI gestureNameText;
    [SerializeField] private TextMeshProUGUI handednessText;
    [SerializeField] private UIButton positive;
    [SerializeField] private UIButton negative;

    private HandMode handedness;

    [SerializeField] private GameObject recordingPreview;
    [SerializeField] private UIButton recordingIcon;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI countdownRecordStatusText;
    private RecordingStatus recordingStatus;


    [SerializeField] private JsonWriter jsonWriter;
    [SerializeField] Transform recordingsDestination;
    [SerializeField] GameObject recordingItemPrefab;

    private Intent intent;

    public void Start()
    {   
        intent = Intent.Positive;
        countdownText.text = "";
        countdownRecordStatusText.text = "";
        gestureNameText.text = jsonWriter.GetGestureName();
    }

    public void StartRecording()
    {
        jsonWriter.StartRecordDelay();
        StartCoroutine(StartThreeTwoOneCountdown());
    }

    public IEnumerator StartThreeTwoOneCountdown(float countdownValue = 3f)
    {
        float timeLeft = countdownValue;
        while (timeLeft > 0)
        {
            countdownText.text = ((int)timeLeft).ToString();
            yield return new WaitForSeconds(1f);
            timeLeft--;
        }

        countdownText.text = "";
        recordingPreview.SetActive(true);
        recordingIcon.Activate();
    }

    public IEnumerator StartRecordingCountdown(float countdownValue)
    {
        float timeLeft = countdownValue;
        while (timeLeft > 0)
        {
            countdownRecordStatusText.text = string.Format("{00}",  ((int)timeLeft).ToString());
            yield return new WaitForSeconds(1f);
            timeLeft--;
        }
        countdownRecordStatusText.text = "";
        recordingIcon.Deactivate();
    }

    public void SetHandedness(HandMode handedness)
    {
        this.handedness = handedness;
        if (handedness == HandMode.OneHand)
        {
            handednessText.text = "One-handed*";
        }
        else if (handedness == HandMode.TwoHands)
        {
            handednessText.text = "Two-handed*";
        }
    }

    public void SwapHandedness()
    {
        if (this.handedness == HandMode.OneHand)
        {
            SetHandedness(HandMode.TwoHands);
            jsonWriter.SetRecordingHandModeTwoHands();
        }
        else
        {
            SetHandedness(HandMode.OneHand);
            jsonWriter.SetRecordingHandModeOneHand();
        }
    }

    public void SetRecordingStatus(RecordingStatus recordingStatus)
    {
        this.recordingStatus = recordingStatus;
        if (recordingStatus == RecordingStatus.NotRecording)
        {
            recordingPreview.SetActive(false);
            positive.Deactivate();
            negative.Deactivate();
        }
    }

    public RecordingStatus GetRecordingStatus()
    {
        return recordingStatus;
    }

    public void SetTargetFile(string targetFile)
    {
        targetFileText.text = targetFile;
    }

    public void StartRecordingPositive()
    {
        intent = Intent.Positive;
        StartRecording();
    }

    public void StartRecordingNegative()
    {
        intent = Intent.Negative;
        StartRecording();
    }

    public Intent GetIntent()
    {
        return intent;
    }

    public void AddRecording(string name)
    {
        GameObject item = Instantiate(recordingItemPrefab, recordingsDestination);
        RecordingItem recordingItem = item.GetComponent<RecordingItem>();
        recordingItem.Initialize(name, intent);
    }
}
