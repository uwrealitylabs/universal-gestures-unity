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
    [Header ("Left UI")]
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI targetFileText;
    [SerializeField] private TextMeshProUGUI gestureNameText;
    [SerializeField] private TextMeshProUGUI handednessText;
    [SerializeField] private TextMeshProUGUI intentText;

    private string gestureName;
    private string targetFile;
    private HandMode handedness;

    [Header("Right UI")]
    [SerializeField] private GameObject startRecordingBtn;
    [SerializeField] private GameObject recordingPreview;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI countdownRecordStatusText;
    private RecordingStatus recordingStatus;

    [SerializeField] private UnityEvent setHandModeOneHand;
    [SerializeField] private UnityEvent setHandModeTwoHands;
    [SerializeField] private UnityEvent startRecordingDelay;
    private Intent intent;

    private void Start()
    {
        intent = Intent.Positive;
        intentText.text = "Positive";
        recordingPreview.SetActive(false);
        countdownText.text = "";
        startRecordingBtn.SetActive(true);
        countdownRecordStatusText.text = "";
    }

    void Update()
    {
        labelText.text = recordingStatus.ToString();
        targetFileText.text = targetFile;
        gestureNameText.text = gestureName;
    }
    public void StartRecording()
    {
        startRecordingDelay.Invoke();
        StartCoroutine(StartThreeTwoOneCountdown());
    }

    public IEnumerator StartThreeTwoOneCountdown(float countdownValue = 3f)
    {
        startRecordingBtn.SetActive(false);

        float timeLeft = countdownValue;
        while (timeLeft > 0)
        {
            countdownText.text = ((int)timeLeft).ToString();
            yield return new WaitForSeconds(1f);
            timeLeft--;
        }

        countdownText.text = "";
        recordingPreview.SetActive(true);
    }

    public IEnumerator StartRecordingCountdown(float countdownValue)
    {
        float timeLeft = countdownValue;
        while (timeLeft > 0)
        {
            countdownRecordStatusText.text = ((int)timeLeft).ToString();
            yield return new WaitForSeconds(1f);
            timeLeft--;
        }
        countdownRecordStatusText.text = "";
    }

    public void SetHandedness(HandMode handedness)
    {
        this.handedness = handedness;
        handednessText.text = handedness.ToString();
    }

    public void SwapHandedness()
    {
        Debug.Log("Swapping handedness...");
        if(this.handedness == HandMode.OneHand)
        {
            SetHandedness(HandMode.TwoHands);
            setHandModeTwoHands.Invoke();
        }
        else
        {
            SetHandedness(HandMode.OneHand);
            setHandModeOneHand.Invoke();
        }
    }
    public void SetRecordingStatus(RecordingStatus recordingStatus)
    {
        this.recordingStatus = recordingStatus;
        if(recordingStatus == RecordingStatus.NotRecording)
        {
            startRecordingBtn.SetActive(true);
            recordingPreview.SetActive(false);
        }
    }

    public RecordingStatus GetRecordingStatus()
    {
        return recordingStatus;
    }

    public void SetGestureName(string name)
    {
        this.gestureName = name;
    }

    public void SetTargetFile(string targetFile)
    {
        this.targetFile = targetFile;
    }

    public void ChangeIntent()
    {
        Debug.Log("Changing intent...");
        if (intent == Intent.Positive)
        {
            intent = Intent.Negative;
            intentText.text = "Negative";
        }
        else if(intent == Intent.Negative)
        {
            intent = Intent.Positive;
            intentText.text = "Positive";
        }
    }

    public Intent GetIntent()
    {
        return intent;
    }
}
