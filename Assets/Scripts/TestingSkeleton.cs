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
        float indexFingerCurl = rightFingersFeatureProvider.GetFeatureValue(HandFinger.Index, FingerFeature.Curl) ?? 0.0f;
        Debug.Log("Index finger curl: " + indexFingerCurl);
    }   
}
