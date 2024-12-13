using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlInEditor : MonoBehaviour
{
    [SerializeField]
    private string _gestureName;

    [SerializeField]
    private string _filePath;

    [SerializeField]
    [Range(1f, 99f)]
    private float _durationInSeconds;

    private static string gestureName;
    private static string filePath;
    private static float recordingDuration;

    private void Awake()
    {        
        gestureName = _gestureName;
        filePath = _filePath;
        recordingDuration = _durationInSeconds;
    }

    public static string GetFilePath()
    {
        return filePath;
    }

    public static string GetGestureName()
    {
        return gestureName;
    }

    public static float GetRecordingDuration()
    {
        return recordingDuration;
    }
}
