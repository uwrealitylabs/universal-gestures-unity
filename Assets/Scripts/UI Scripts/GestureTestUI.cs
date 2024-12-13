using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureTestUI : MonoBehaviour
{
    [SerializeField] private GestureItem heartHands;
    [SerializeField] private GestureItem fingerGun;
    [SerializeField] private GestureItem peace;
    [SerializeField] private GestureItem thumbUp;
    [SerializeField] private GestureItem scissors;

    [Header("Testing")]
    [Range(0f, 1f)] [SerializeField] private float heartHandsConfidence;
    [Range(0f, 1f)][SerializeField] private float fingerGunConfidence;
    [Range(0f, 1f)][SerializeField] private float peaceConfidence;
    [Range(0f, 1f)][SerializeField] private float thumbUpConfidence;
    [Range(0f, 1f)][SerializeField] private float scissorsConfidence;

    [SerializeField] URWLHandPoseDetection handPoseDetection;

    public void Start()
    {
        GestureItem.SetConfidenceThreshold(handPoseDetection.getThresholdConfidence());
    }

    public void Update()
    {
        // change these out for some getter methods
        heartHands.SetConfidence(heartHandsConfidence);
        fingerGun.SetConfidence(fingerGunConfidence);
        peace.SetConfidence(peaceConfidence);
        thumbUp.SetConfidence(thumbUpConfidence);
        scissors.SetConfidence(scissorsConfidence);
    }
}
