using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//script for tracking controller left

public class TouchControllerLeft: MonoBehaviour {

    void Update()
    {
        transform.localPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        transform.localRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
    }
}
