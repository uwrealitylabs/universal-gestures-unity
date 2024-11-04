using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.PoseDetection;
using Oculus.Interaction.Input;

public class TestingSkeleton : MonoBehaviour
{
    public GameObject rightHand;
    public GameObject rightHandFeature;
    private FingerFeatureStateProvider rightFingersFeatureProvider;
    public const int ONE_HAND_NUM_FEATURES = 17;
    public static float[] handData;

    // Start is called before the first frame update
    void Start()
    {
        // OVRSkeleton skeleton = rightHand.GetComponent<OVRSkeleton>();
        // int currentBones = skeleton.GetCurrentNumBones();
        // Debug.Log("Current number of bones: " + currentBones);

        rightFingersFeatureProvider = rightHandFeature.GetComponent<FingerFeatureStateProvider>();
    }

    // Update is called once per frame
    void Update()
    {
        // Function used to normalize bounded data
        float Normalize(float value, float min, float max)
        {
            return (value - min) / (max - min);
        }

        float indexFingerCurl = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Curl) ?? 0.0f, 0.0f, 360.0f);
        float indexFingerAbduction = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Abduction) ?? 0.0f, 0.0f, 360.0f);
        float indexFingerFlexion = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Flexion) ?? 0.0f, 0.0f, 360.0f);
        float indexFingerOpposition = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Opposition) ?? 0.0f, 0.0f, 1.0f);

        float thumbFingerCurl = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Curl) ?? 0.0f, 0.0f, 360.0f);
        float thumbFingerAbduction = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Abduction) ?? 0.0f, 0.0f, 360.0f);
        // Flexion, Opposition not available on thumb
        
        float middleFingerCurl = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Curl) ?? 0.0f, 0.0f, 360.0f);
        float middleFingerAbduction = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Abduction) ?? 0.0f, 0.0f, 360.0f);
        float middleFingerFlexion = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Flexion) ?? 0.0f, 0.0f, 360.0f);
        float middleFingerOpposition = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Opposition) ?? 0.0f, 0.0f, 1.0f);

        float ringFingerCurl = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Curl) ?? 0.0f, 0.0f, 360.0f);
        float ringFingerAbduction = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Abduction) ?? 0.0f, 0.0f, 360.0f);
        float ringFingerFlexion = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Flexion) ?? 0.0f, 0.0f, 360.0f);
        float ringFingerOpposition = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Opposition) ?? 0.0f, 0.0f, 1.0f);

        float pinkyFingerCurl = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Curl) ?? 0.0f, 0.0f, 360.0f);
        // Pinky does not support abduction
        float pinkyFingerFlexion = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Flexion) ?? 0.0f, 0.0f, 360.0f);
        float pinkyFingerOpposition = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Opposition) ?? 0.0f, 0.0f, 1.0f);

        // Debug.Log("Index finger values: " + indexFingerCurl + ", " + indexFingerAbduction + ", " + indexFingerFlexion + ", " + indexFingerOpposition);

        // Add values to JsonWriter.GestureData.handData
        handData = new [] {thumbFingerCurl, thumbFingerAbduction, indexFingerCurl, indexFingerAbduction, indexFingerFlexion, indexFingerOpposition, middleFingerCurl, middleFingerAbduction, middleFingerFlexion, middleFingerOpposition, ringFingerCurl, ringFingerAbduction, ringFingerFlexion, ringFingerOpposition, pinkyFingerCurl, pinkyFingerFlexion, pinkyFingerOpposition};
    }   
}
