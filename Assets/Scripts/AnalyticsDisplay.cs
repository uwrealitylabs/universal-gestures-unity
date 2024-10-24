using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Profiling;

public class AnalyticsDisplay : MonoBehaviour
{
    [SerializeField] private GameObject scripts;
    [SerializeField] private TextMeshProUGUI inferenceOutputText;
    [SerializeField] private TextMeshProUGUI thresholdConfidenceText;
    [SerializeField] private TextMeshProUGUI latencyText;
    [SerializeField] private TextMeshProUGUI CPUText;
    [SerializeField] private TextMeshProUGUI RAMUsageText;

    private UniversalGesturesInference inference;
    private URWLHandPoseDetection confidence;

    private ProfilerRecorder mainThreadTimeRecorder;
    private ProfilerRecorder cpuTotalTimeRecorder;

    private int timer;
    private static int cpuUpdateInterval = 100;

    private void Start()
    {
        inference = scripts.GetComponent<UniversalGesturesInference>();
        confidence = scripts.GetComponent<URWLHandPoseDetection>();
        timer = 0;
    }
    private void Update()
    {
        timer--;

        inferenceOutputText.text = string.Format("Inference Output: {0:0.00000}", inference.inferenceOutput);
        thresholdConfidenceText.text = string.Format("Threshold Confidence: {0:0.00000}", confidence.getThresholdConfidence());
        latencyText.text = string.Format("{0:0.00000}ms", inference.latency);

        //CPU
        if (timer <= 0)
        {
            timer = cpuUpdateInterval;
            CPUText.text = string.Format("{0:0.00}%", GetCpuPercent());
        }

        RAMUsageText.text =  GetRamUsage() + "MB";
        
    }
    private float GetCpuPercent()
    {
        // CPU calculations
        FrameTimingManager.CaptureFrameTimings();
        long cpuMainThreadFrameTime = mainThreadTimeRecorder.LastValue;
        long cpuTotalTime = cpuTotalTimeRecorder.LastValue;

        return (float) cpuMainThreadFrameTime / cpuTotalTime * 100.0f;
    } 
    private long GetRamUsage()
    {
        return Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
    }
    private void OnEnable()
    {
        mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Main Thread Frame Time");
        cpuTotalTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Total Frame Time");
    }
    private void OnDisable()
    {
        mainThreadTimeRecorder.Dispose();
        cpuTotalTimeRecorder.Dispose();
    }
}
