using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.PoseDetection;
using Oculus.Interaction.Input;


// TestingSkeletonTwoHands.cs
// This script is used to expose the finger features of the left and right hands,
// as well as the relative features between the two hands.


public class TestingSkeletonTwoHands : MonoBehaviour
{
    public GameObject leftHand;
    public GameObject leftHandFeature;
    public GameObject rightHand;
    public GameObject rightHandFeature;
    private FingerFeatureStateProvider leftFingersFeatureProvider;
    private FingerFeatureStateProvider rightFingersFeatureProvider;
    public const int TWO_HAND_NUM_FEATURES = 44;
    public static float[] handData;

    // Start is called before the first frame update
    void Start()
    {
        // OVRSkeleton skeleton = rightHand.GetComponent<OVRSkeleton>();
        // int currentBones = skeleton.GetCurrentNumBones();
        // Debug.Log("Current number of bones: " + currentBones);

        leftFingersFeatureProvider = leftHandFeature.GetComponent<FingerFeatureStateProvider>();
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

        // ========================================
        // LEFT HAND FEATURES
        // ========================================
        float leftIndexFingerCurl = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Curl) ?? 0.0f, 0, 360);
        float leftIndexFingerAbduction = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Abduction) ?? 0.0f, 0, 360);
        float leftIndexFingerFlexion = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Flexion) ?? 0.0f, 0, 360);
        float leftIndexFingerOpposition = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Opposition) ?? 0.0f, 0, 1);

        float leftThumbFingerCurl = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Curl) ?? 0.0f, 0, 360);
        float leftThumbFingerAbduction = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Abduction) ?? 0.0f, 0, 360);
        // Flexion, Opposition not available on thumb

        float leftMiddleFingerCurl = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Curl) ?? 0.0f, 0, 360);
        float leftMiddleFingerAbduction = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Abduction) ?? 0.0f, 0, 360);
        float leftMiddleFingerFlexion = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Flexion) ?? 0.0f, 0, 360);
        float leftMiddleFingerOpposition = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Opposition) ?? 0.0f, 0, 1);

        float leftRingFingerCurl = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Curl) ?? 0.0f, 0, 360);
        float leftRingFingerAbduction = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Abduction) ?? 0.0f, 0, 360);
        float leftRingFingerFlexion = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Flexion) ?? 0.0f, 0, 360);
        float leftRingFingerOpposition = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Opposition) ?? 0.0f, 0, 1);

        float leftPinkyFingerCurl = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Curl) ?? 0.0f, 0, 360);
        // Pinky does not support abduction
        float leftPinkyFingerFlexion = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Flexion) ?? 0.0f, 0, 360);
        float leftPinkyFingerOpposition = Normalize(leftFingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Opposition) ?? 0.0f, 0, 1);


        // ========================================
        // RIGHT HAND FEATURES
        // ========================================
        float rightIndexFingerCurl = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Curl) ?? 0.0f, 0, 360);
        float rightIndexFingerAbduction = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Abduction) ?? 0.0f, 0, 360);
        float rightIndexFingerFlexion = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Flexion) ?? 0.0f, 0, 360);
        float rightIndexFingerOpposition = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Opposition) ?? 0.0f, 0, 1);

        float rightThumbFingerCurl = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Curl) ?? 0.0f, 0, 360);
        float rightThumbFingerAbduction = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Abduction) ?? 0.0f, 0, 360);
        // Flexion, Opposition not available on thumb

        float rightMiddleFingerCurl = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Curl) ?? 0.0f, 0, 360);
        float rightMiddleFingerAbduction = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Abduction) ?? 0.0f, 0, 360);
        float rightMiddleFingerFlexion = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Flexion) ?? 0.0f, 0, 360);
        float rightMiddleFingerOpposition = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Opposition) ?? 0.0f, 0, 1);

        float rightRingFingerCurl = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Curl) ?? 0.0f, 0, 360);
        float rightRingFingerAbduction = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Abduction) ?? 0.0f, 0, 360);
        float rightRingFingerFlexion = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Flexion) ?? 0.0f, 0, 360);
        float rightRingFingerOpposition = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Opposition) ?? 0.0f, 0, 1);

        float rightPinkyFingerCurl = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Curl) ?? 0.0f, 0, 360);
        // Pinky does not support abduction
        float rightPinkyFingerFlexion = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Flexion) ?? 0.0f, 0, 360);
        float rightPinkyFingerOpposition = Normalize(rightFingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Opposition) ?? 0.0f, 0, 1);


        // ========================================
        // TWO-HAND RELATIVE FEATURES
        // ========================================
        float leftX = leftHand.transform.position[0];
        float leftY = leftHand.transform.position[1];
        float leftZ = leftHand.transform.position[2];
        float rightX = rightHand.transform.position[0];
        float rightY = rightHand.transform.position[1];
        float rightZ = rightHand.transform.position[2];
        float xDiff = rightX - leftX;
        float yDiff = rightY - leftY;
        float zDiff = rightZ - leftZ;
        float distance = Mathf.Sqrt(xDiff * xDiff + yDiff * yDiff + zDiff * zDiff);

        float leftRotationX = leftHand.transform.rotation.eulerAngles[0];
        float leftRotationY = leftHand.transform.rotation.eulerAngles[1];
        float leftRotationZ = leftHand.transform.rotation.eulerAngles[2];
        float rightRotationX = rightHand.transform.rotation.eulerAngles[0];
        float rightRotationY = rightHand.transform.rotation.eulerAngles[1];
        float rightRotationZ = rightHand.transform.rotation.eulerAngles[2];
        float rotationXDiff = rightRotationX - leftRotationX;
        float rotationYDiff = rightRotationY - leftRotationY;
        float rotationZDiff = rightRotationZ - leftRotationZ;
        float rotationXSin = Normalize(Mathf.Sin(rotationXDiff), -1, 1);
        float rotationXCos = Normalize(Mathf.Cos(rotationXDiff), -1, 1);
        float rotationYSin = Normalize(Mathf.Sin(rotationYDiff), -1, 1);
        float rotationYCos = Normalize(Mathf.Cos(rotationYDiff), -1, 1);
        float rotationZSin = Normalize(Mathf.Sin(rotationZDiff), -1, 1);
        float rotationZCos = Normalize(Mathf.Cos(rotationZDiff), -1, 1);
        Debug.Log("Rotation X Diff: " + rotationXDiff + ", Rotation Y Diff: " + rotationYDiff + ", Rotation Z Diff: " + rotationZDiff);

        // Debug.Log("Index finger values: " + rightIndexFingerCurl + ", " + rightIndexFingerAbduction + ", " + rightIndexFingerFlexion + ", " + rightIndexFingerOpposition);

        // Add values to JsonWriter.GestureData.handData
        handData = new[] { rightThumbFingerCurl, rightThumbFingerAbduction, rightIndexFingerCurl, rightIndexFingerAbduction, rightIndexFingerFlexion, rightIndexFingerOpposition, rightMiddleFingerCurl, rightMiddleFingerAbduction, rightMiddleFingerFlexion, rightMiddleFingerOpposition, rightRingFingerCurl, rightRingFingerAbduction, rightRingFingerFlexion, rightRingFingerOpposition, rightPinkyFingerCurl, rightPinkyFingerFlexion, rightPinkyFingerOpposition,
                   leftThumbFingerCurl, leftThumbFingerAbduction, leftIndexFingerCurl, leftIndexFingerAbduction, leftIndexFingerFlexion, leftIndexFingerOpposition, leftMiddleFingerCurl, leftMiddleFingerAbduction, leftMiddleFingerFlexion, leftMiddleFingerOpposition, leftRingFingerCurl, leftRingFingerAbduction, leftRingFingerFlexion, leftRingFingerOpposition, leftPinkyFingerCurl, leftPinkyFingerFlexion, leftPinkyFingerOpposition,
                   xDiff, yDiff, zDiff, distance, rotationXSin, rotationXCos, rotationYSin, rotationYCos, rotationZSin, rotationZCos };
    }
}
