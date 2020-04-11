using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//script for tracking the conroller right

public class TouchControllerRight : MonoBehaviour {
	
	void Update () {
        transform.localPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        transform.localRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
	}
}
