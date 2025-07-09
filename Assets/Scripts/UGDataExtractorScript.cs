using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.PoseDetection;
using Oculus.Interaction.Input;
using System.Linq;


public class UGDataExtractorScript : MonoBehaviour
{

    // Data sources
    [Header("Specify Data Sources")]
    public Hand leftHand;
    public Hand rightHand;
    public OVRHand leftOVRHand;
    public OVRHand rightOVRHand;
    public OVRSkeleton leftOVRSkeleton;
    public OVRSkeleton rightOVRSkeleton;
    private FingerFeatureStateProvider leftFingerFeatureStateProvider;
    private FingerFeatureStateProvider rightFingerFeatureStateProvider;
    private TransformFeatureStateProvider leftTransformFeatureProvider;
    private TransformFeatureStateProvider rightTransformFeatureProvider;

    // Data gathering settings - determines which features are analyzed
    [Header("Enable/Disable Data Gathering")]
    public bool leftHandDataEnabled = false;
    public bool rightHandDataEnabled = false;
    public bool twoHandDataEnabled = false;
    public bool positionalDataEnabled = false;

    // Config for transform features
    private TransformConfig transformConfig;

    // Hand data output arrays - used by other scripts
    [HideInInspector]
    public float[] leftHandData;
    [HideInInspector]
    public float[] rightHandData;
    [HideInInspector]
    public float[] leftHandTransformData;
    [HideInInspector]
    public float[] rightHandTransformData;
    [HideInInspector]
    public float[] leftHandMovementData;
    [HideInInspector]
    public float[] rightHandMovementData;
    [HideInInspector]
    public float[] leftFingerPositionData;
    [HideInInspector]
    public float[] rightFingerPositionData;
    [HideInInspector]
    public float[] twoHandsData;

    // Hand data for position and velocity calculations - not used by other scripts
    private float leftXprev, leftYprev, leftZprev;
    private float rightXprev, rightYprev, rightZprev;
    private float leftVelXprev, leftVelYprev, leftVelZprev;
    private float rightVelXprev, rightVelYprev, rightVelZprev;


    // Constants
    public const int ONE_HAND_NUM_FEATURES = 17;
    public const int ONE_HAND_TRANSFORM_NUM_FEATURES = 4;
    public const int TWO_HAND_NUM_FEATURES = 44;


    void Start()
    {
        // Set up data sources
        bool setupSuccess = SetupAndValidateConfiguration();
        if (!setupSuccess)
        {
            Debug.LogError("UGDataExtractorScript: Data source setup failed. Disabling script.");
            gameObject.SetActive(false);
            return;
        }
        // Initialize transform config
        transformConfig = new();
    }

    void Update()
    {
        // Update data from enabled sources
        if (leftHandDataEnabled)
        {
            leftHandData = GetOneHandData(leftFingerFeatureStateProvider);
            leftHandTransformData = GetOneHandTransformData(leftTransformFeatureProvider);
            if (positionalDataEnabled)
            {
                leftHandMovementData = GetHandMovementData(leftOVRHand, ref leftXprev, ref leftYprev, ref leftZprev, ref leftVelXprev, ref leftVelYprev, ref leftVelZprev);
                leftFingerPositionData = GetHandJointPositionData(leftOVRSkeleton);
            }

        }
        if (rightHandDataEnabled)
        {
            rightHandData = GetOneHandData(rightFingerFeatureStateProvider);
            rightHandTransformData = GetOneHandTransformData(rightTransformFeatureProvider);
            if (positionalDataEnabled)
            {
                rightHandMovementData = GetHandMovementData(rightOVRHand, ref rightXprev, ref rightYprev, ref rightZprev, ref rightVelXprev, ref rightVelYprev, ref rightVelZprev);
                rightFingerPositionData = GetHandJointPositionData(rightOVRSkeleton);
            }
        }
        if (twoHandDataEnabled)
        {
            twoHandsData = GetTwoHandsData();
            if (positionalDataEnabled)
            {
                leftHandMovementData = GetHandMovementData(leftOVRHand, ref leftXprev, ref leftYprev, ref leftZprev, ref leftVelXprev, ref leftVelYprev, ref leftVelZprev);
                rightHandMovementData = GetHandMovementData(rightOVRHand, ref rightXprev, ref rightYprev, ref rightZprev, ref rightVelXprev, ref rightVelYprev, ref rightVelZprev);
                leftFingerPositionData = GetHandJointPositionData(leftOVRSkeleton);
                rightFingerPositionData = GetHandJointPositionData(rightOVRSkeleton);
            }
        }
    }

    bool SetupAndValidateConfiguration()
    {
        // ensure all required data sources are provided
        if (leftHand == null || rightHand == null || leftOVRHand == null || rightOVRHand == null || leftOVRSkeleton == null || rightOVRSkeleton == null)
        {
            Debug.LogError("UGDataExtractorScript: Data source setup failed. Ensure left/right hand, left/right OVR hand, and left/right OVR skeleton are provided.");
            return false;
        }

        // get finger feature state providers
        leftFingerFeatureStateProvider = leftHand.GetComponentInChildren<FingerFeatureStateProvider>();
        rightFingerFeatureStateProvider = rightHand.GetComponentInChildren<FingerFeatureStateProvider>();
        leftTransformFeatureProvider = leftHand.GetComponentInChildren<TransformFeatureStateProvider>();
        rightTransformFeatureProvider = rightHand.GetComponentInChildren<TransformFeatureStateProvider>();
        if (leftFingerFeatureStateProvider == null || rightFingerFeatureStateProvider == null)
        {
            Debug.LogError("UGDataExtractorScript: Data source setup failed. Ensure left hand and right hand have children with FingerFeatureStateProvider components.");
            return false;
        }
        return true;
    }

    private float[] GetOneHandData(FingerFeatureStateProvider fingersFeatureProvider)
    {

        float indexFingerCurl = fingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Curl) ?? 0.0f;
        float indexFingerAbduction = fingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Abduction) ?? 0.0f;
        float indexFingerFlexion = fingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Flexion) ?? 0.0f;
        float indexFingerOpposition = fingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Opposition) ?? 0.0f;

        float thumbFingerCurl = fingersFeatureProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Curl) ?? 0.0f;
        float thumbFingerAbduction = fingersFeatureProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Abduction) ?? 0.0f;
        // Flexion, Opposition not available on thumb

        float middleFingerCurl = fingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Curl) ?? 0.0f;
        float middleFingerAbduction = fingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Abduction) ?? 0.0f;
        float middleFingerFlexion = fingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Flexion) ?? 0.0f;
        float middleFingerOpposition = fingersFeatureProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Opposition) ?? 0.0f;

        float ringFingerCurl = fingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Curl) ?? 0.0f;
        float ringFingerAbduction = fingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Abduction) ?? 0.0f;
        float ringFingerFlexion = fingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Flexion) ?? 0.0f;
        float ringFingerOpposition = fingersFeatureProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Opposition) ?? 0.0f;

        float pinkyFingerCurl = fingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Curl) ?? 0.0f;
        // Pinky does not support abduction
        float pinkyFingerFlexion = fingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Flexion) ?? 0.0f;
        float pinkyFingerOpposition = fingersFeatureProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Opposition) ?? 0.0f;

        float[] handData = new[] {
            thumbFingerCurl,
            thumbFingerAbduction,
            indexFingerCurl,
            indexFingerAbduction,
            indexFingerFlexion,
            indexFingerOpposition,
            middleFingerCurl,
            middleFingerAbduction,
            middleFingerFlexion,
            middleFingerOpposition,
            ringFingerCurl,
            ringFingerAbduction,
            ringFingerFlexion,
            ringFingerOpposition,
            pinkyFingerCurl,
            pinkyFingerFlexion,
            pinkyFingerOpposition
        };

        return handData;
    }

    private float[] GetOneHandTransformData(TransformFeatureStateProvider transformFeatureProvider)
    {
        float wristUp = transformFeatureProvider.GetFeatureValue(transformConfig, TransformFeature.WristUp) ?? 0.0f;
        float palmUp = transformFeatureProvider.GetFeatureValue(transformConfig, TransformFeature.PalmUp) ?? 0.0f;
        float palmTowardsFace = transformFeatureProvider.GetFeatureValue(transformConfig, TransformFeature.PalmTowardsFace) ?? 0.0f;
        float fingersUp = transformFeatureProvider.GetFeatureValue(transformConfig, TransformFeature.FingersUp) ?? 0.0f;

        float[] transformData =
        {
            wristUp,
            palmUp,
            palmTowardsFace,
            fingersUp
        };

        return transformData;
    }

    private float[] GetTwoHandsData()
    {
        // ========================================
        // LEFT HAND FEATURES
        // ========================================
        float leftIndexFingerCurl = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Curl) ?? 0.0f;
        float leftIndexFingerAbduction = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Abduction) ?? 0.0f;
        float leftIndexFingerFlexion = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Flexion) ?? 0.0f;
        float leftIndexFingerOpposition = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Opposition) ?? 0.0f;

        float leftThumbFingerCurl = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Curl) ?? 0.0f;
        float leftThumbFingerAbduction = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Abduction) ?? 0.0f;
        // Flexion, Opposition not available on thumb

        float leftMiddleFingerCurl = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Curl) ?? 0.0f;
        float leftMiddleFingerAbduction = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Abduction) ?? 0.0f;
        float leftMiddleFingerFlexion = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Flexion) ?? 0.0f;
        float leftMiddleFingerOpposition = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Opposition) ?? 0.0f;

        float leftRingFingerCurl = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Curl) ?? 0.0f;
        float leftRingFingerAbduction = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Abduction) ?? 0.0f;
        float leftRingFingerFlexion = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Flexion) ?? 0.0f;
        float leftRingFingerOpposition = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Opposition) ?? 0.0f;

        float leftPinkyFingerCurl = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Curl) ?? 0.0f;
        // Pinky does not support abduction
        float leftPinkyFingerFlexion = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Flexion) ?? 0.0f;
        float leftPinkyFingerOpposition = leftFingerFeatureStateProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Opposition) ?? 0.0f;


        // ========================================
        // RIGHT HAND FEATURES
        // ========================================
        float rightIndexFingerCurl = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Curl) ?? 0.0f;
        float rightIndexFingerAbduction = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Abduction) ?? 0.0f;
        float rightIndexFingerFlexion = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Flexion) ?? 0.0f;
        float rightIndexFingerOpposition = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Opposition) ?? 0.0f;

        float rightThumbFingerCurl = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Curl) ?? 0.0f;
        float rightThumbFingerAbduction = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Thumb, FingerFeature.Abduction) ?? 0.0f;
        // Flexion, Opposition not available on thumb

        float rightMiddleFingerCurl = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Curl) ?? 0.0f;
        float rightMiddleFingerAbduction = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Abduction) ?? 0.0f;
        float rightMiddleFingerFlexion = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Flexion) ?? 0.0f;
        float rightMiddleFingerOpposition = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Middle, FingerFeature.Opposition) ?? 0.0f;

        float rightRingFingerCurl = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Curl) ?? 0.0f;
        float rightRingFingerAbduction = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Abduction) ?? 0.0f;
        float rightRingFingerFlexion = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Flexion) ?? 0.0f;
        float rightRingFingerOpposition = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Ring, FingerFeature.Opposition) ?? 0.0f;

        float rightPinkyFingerCurl = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Curl) ?? 0.0f;
        // Pinky does not support abduction
        float rightPinkyFingerFlexion = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Flexion) ?? 0.0f;
        float rightPinkyFingerOpposition = rightFingerFeatureStateProvider.GetFeatureValue(HandFinger.Pinky, FingerFeature.Opposition) ?? 0.0f;


        // ========================================
        // TWO-HAND RELATIVE FEATURES
        // ========================================

        // Position 
        float leftX = leftOVRHand.transform.position[0];
        float leftY = leftOVRHand.transform.position[1];
        float leftZ = leftOVRHand.transform.position[2];
        float rightX = rightOVRHand.transform.position[0];
        float rightY = rightOVRHand.transform.position[1];
        float rightZ = rightOVRHand.transform.position[2];

        float xDiff = rightX - leftX;
        float yDiff = rightY - leftY;
        float zDiff = rightZ - leftZ;
        float distance = Mathf.Sqrt(xDiff * xDiff + yDiff * yDiff + zDiff * zDiff);

        // Rotation
        float leftRotationX = leftOVRHand.transform.rotation.eulerAngles[0];
        float leftRotationY = leftOVRHand.transform.rotation.eulerAngles[1];
        float leftRotationZ = leftOVRHand.transform.rotation.eulerAngles[2];
        float rightRotationX = rightOVRHand.transform.rotation.eulerAngles[0];
        float rightRotationY = rightOVRHand.transform.rotation.eulerAngles[1];
        float rightRotationZ = rightOVRHand.transform.rotation.eulerAngles[2];

        float rotationXDiff = rightRotationX - leftRotationX;
        float rotationYDiff = rightRotationY - leftRotationY;
        float rotationZDiff = rightRotationZ - leftRotationZ;
        float rotationXSin = Mathf.Sin(rotationXDiff);
        float rotationXCos = Mathf.Cos(rotationXDiff);
        float rotationYSin = Mathf.Sin(rotationYDiff);
        float rotationYCos = Mathf.Cos(rotationYDiff);
        float rotationZSin = Mathf.Sin(rotationZDiff);
        float rotationZCos = Mathf.Cos(rotationZDiff);

        Debug.Log("Rotation X Diff: " + rotationXDiff + ", Rotation Y Diff: " + rotationYDiff + ", Rotation Z Diff: " + rotationZDiff);

        // Debug.Log("Index finger values: " + rightIndexFingerCurl + ", " + rightIndexFingerAbduction + ", " + rightIndexFingerFlexion + ", " + rightIndexFingerOpposition);

        float[] handData = new[] { rightThumbFingerCurl, rightThumbFingerAbduction, rightIndexFingerCurl, rightIndexFingerAbduction, rightIndexFingerFlexion, rightIndexFingerOpposition, rightMiddleFingerCurl, rightMiddleFingerAbduction, rightMiddleFingerFlexion, rightMiddleFingerOpposition, rightRingFingerCurl, rightRingFingerAbduction, rightRingFingerFlexion, rightRingFingerOpposition, rightPinkyFingerCurl, rightPinkyFingerFlexion, rightPinkyFingerOpposition,
                   leftThumbFingerCurl, leftThumbFingerAbduction, leftIndexFingerCurl, leftIndexFingerAbduction, leftIndexFingerFlexion, leftIndexFingerOpposition, leftMiddleFingerCurl, leftMiddleFingerAbduction, leftMiddleFingerFlexion, leftMiddleFingerOpposition, leftRingFingerCurl, leftRingFingerAbduction, leftRingFingerFlexion, leftRingFingerOpposition, leftPinkyFingerCurl, leftPinkyFingerFlexion, leftPinkyFingerOpposition,
                   xDiff, yDiff, zDiff, distance, rotationXSin, rotationXCos, rotationYSin, rotationYCos, rotationZSin, rotationZCos };
        return handData;
    }

    private float[] GetHandMovementData(OVRHand ovrHand, ref float Xprev, ref float Yprev, ref float Zprev, ref float VelXprev, ref float VelYprev, ref float VelZprev)
    {
        float X = ovrHand.transform.position[0];
        float Y = ovrHand.transform.position[1];
        float Z = ovrHand.transform.position[2];
        float RotationX = ovrHand.transform.rotation.eulerAngles[0];
        float RotationY = ovrHand.transform.rotation.eulerAngles[1];
        float RotationZ = ovrHand.transform.rotation.eulerAngles[2];

        float dt = Time.deltaTime;

        float VelX = (X - Xprev) / dt;
        float VelY = (Y - Yprev) / dt;
        float VelZ = (Z - Zprev) / dt;
        float AccelX = (VelX - VelXprev) / dt;
        float AccelY = (VelY - VelYprev) / dt;
        float AccelZ = (VelZ - VelZprev) / dt;

        Xprev = X;
        Yprev = Y;
        Zprev = Z;
        VelXprev = VelX;
        VelYprev = VelY;
        VelZprev = VelZ;

        float ConfidenceVal = ovrHand.HandConfidence == OVRHand.TrackingConfidence.High ? 1.0f : 0.0f;

        float[] HandMovement = new[] {
            X, Y, Z, RotationX, RotationY, RotationZ, VelX, VelY, VelZ, AccelX, AccelY, AccelZ, ConfidenceVal
        };
        return HandMovement;
    }


    private float[] GetHandJointPositionData(OVRSkeleton ovrSkeleton)
    {
        Transform thumbTip = ovrSkeleton.Bones.FirstOrDefault(b => b.Id == OVRSkeleton.BoneId.Hand_ThumbTip).Transform;
        Transform indexTip = ovrSkeleton.Bones.FirstOrDefault(b => b.Id == OVRSkeleton.BoneId.Hand_IndexTip).Transform;
        Transform middleTip = ovrSkeleton.Bones.FirstOrDefault(b => b.Id == OVRSkeleton.BoneId.Hand_MiddleTip).Transform;
        Transform ringTip = ovrSkeleton.Bones.FirstOrDefault(b => b.Id == OVRSkeleton.BoneId.Hand_RingTip).Transform;
        Transform pinkyTip = ovrSkeleton.Bones.FirstOrDefault(b => b.Id == OVRSkeleton.BoneId.Hand_PinkyTip).Transform;

        float thumbX = thumbTip.position[0];
        float thumbY = thumbTip.position[1];
        float thumbZ = thumbTip.position[2];
        float indexX = indexTip.position[0];
        float indexY = indexTip.position[1];
        float indexZ = indexTip.position[2];
        float middleX = middleTip.position[0];
        float middleY = middleTip.position[1];
        float middleZ = middleTip.position[2];
        float ringX = ringTip.position[0];
        float ringY = ringTip.position[1];
        float ringZ = ringTip.position[2];
        float pinkyX = pinkyTip.position[0];
        float pinkyY = pinkyTip.position[1];
        float pinkyZ = pinkyTip.position[2];

        float[] JointPosition = new[] {
            thumbX, thumbY, thumbZ, indexX, indexY, indexZ, middleX, middleY, middleZ, ringX, ringY, ringZ, pinkyX, pinkyY, pinkyZ
        };
        return JointPosition;

    }





}

