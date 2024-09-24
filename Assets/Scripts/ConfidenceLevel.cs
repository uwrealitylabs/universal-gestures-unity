using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class ConfidenceLevel : MonoBehaviour
{
    [SerializeField] private UnityEvent confidenceLevelReached;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float confidenceThreshold;

    [SerializeField] private TextMeshProUGUI confidenceText;

    private Boolean eventTriggered = false;

    private UniversalGesturesInference inference;

    private void Start()
    {
        inference = GetComponent<UniversalGesturesInference>();
    }
    private void Update()
    {
        if(!eventTriggered && inference.inferenceOutput >= confidenceThreshold)
        {
            confidenceLevelReached.Invoke();
            eventTriggered = true;
        }
        confidenceText.text = "Confidence Threshold: " + confidenceThreshold;
    }

}
