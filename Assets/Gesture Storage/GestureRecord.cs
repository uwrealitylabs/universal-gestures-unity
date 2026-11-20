using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// The JSON record saved for each gesture as gestures/{id}/gesture.json.
// All file paths inside are RELATIVE to GestureLibrary.Root, so the data can be moved
// between machines / devices without breaking.

[Serializable]
public class GestureRecord
{
    public const int CurrentSchemaVersion = 1;

    public int schemaVersion = CurrentSchemaVersion;

    /// <summary>Stable ID. Never changes, even when the gesture is renamed. Also the folder name.</summary>
    public string id;

    /// <summary>What the user typed, e.g. "Rockstar". Can change freely.</summary>
    public string displayName;

    public string createdAt;
    public string updatedAt;

    /// <summary>Everything needed to interpret the recordings and run the model.</summary>
    public GestureSettings settings = new GestureSettings();

    public List<RecordingRef> recordings = new List<RecordingRef>();

    /// <summary>Null until the gesture has been trained.</summary>
    public ModelRef model;

    [JsonIgnore] public bool IsTrained => model != null && !string.IsNullOrEmpty(model.file);
    [JsonIgnore] public int PositiveCount => recordings.FindAll(r => r.label == RecordingLabel.Positive).Count;
    [JsonIgnore] public int NegativeCount => recordings.FindAll(r => r.label == RecordingLabel.Negative).Count;
}

[Serializable]
public class GestureSettings
{
    [JsonConverter(typeof(StringEnumConverter))]
    public HandMode handMode = HandMode.RightHand;

    /// <summary>One-hand only: whether the 4 transform features are appended (17 + 4 = 21).</summary>
    public bool useTransformData;

    /// <summary>Floats per frame: 17, 21 or 44. Must match the recordings and the model input.</summary>
    public int featureCount = UGDataExtractorScript.ONE_HAND_NUM_FEATURES;

    // Recognition settings (mirrors UGInferenceRunnerScript)
    public float thresholdConfidenceLevel = 0.8f;
    public bool loopFunctionWhilePoseIsHeld;
    public float inferenceInterval = 0.5f;

    public static int FeatureCountFor(HandMode mode, bool useTransform)
    {
        if (mode == HandMode.TwoHands) return UGDataExtractorScript.TWO_HAND_NUM_FEATURES;
        return UGDataExtractorScript.ONE_HAND_NUM_FEATURES +
               (useTransform ? UGDataExtractorScript.ONE_HAND_TRANSFORM_NUM_FEATURES : 0);
    }
}

public enum RecordingLabel { Positive, Negative }

[Serializable]
public class RecordingRef
{
    /// <summary>e.g. "gestures/7f3a9c21e4b0/recordings/001_neg.json" (existing JSON recording format).</summary>
    public string file;

    [JsonConverter(typeof(StringEnumConverter))]
    public RecordingLabel label;

    public int frames;
    public string recordedAt;
}

[Serializable]
public class ModelRef
{
    /// <summary>e.g. "models/m_20261029-201502_ab12cd.onnx". Models live in a shared folder so
    /// one model can serve several gestures; a model file is only deleted when nothing uses it.</summary>
    public string file;

    public string trainedAt;

    /// <summary>"binary" for today's models (one sigmoid output = confidence of THIS gesture).
    /// "multiclass" is reserved for future few-shot / multi-gesture models.</summary>
    public string outputType = "binary";

    /// <summary>Which output of the model is this gesture's confidence. 0 for binary models.</summary>
    public int outputIndex;

    /// <summary>Label mapping: classLabels[i] = gesture ID for model output i.
    /// Binary models: [thisGestureId]. Kept so multi-class models can be shared safely.</summary>
    public List<string> classLabels = new List<string>();

    /// <summary>Recordings the model was trained on, so the UI can show "needs retraining".</summary>
    public List<string> trainedOnRecordings = new List<string>();
}
