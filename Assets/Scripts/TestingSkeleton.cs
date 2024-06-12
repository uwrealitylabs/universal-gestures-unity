using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingSkeleton : MonoBehaviour
{
    public GameObject rightHand;

    // Start is called before the first frame update
    void Start()
    {
        OVRSkeleton skeleton = rightHand.GetComponent<OVRSkeleton>();
        int currentBones = skeleton.GetCurrentNumBones();
        Debug.Log("Current number of bones: " + currentBones);

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
