using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LazerScriptRight: MonoBehaviour {

    float pressurevalue = 0f;
    RaycastHit hit = new RaycastHit();

    void Start () {
        this.GetComponent<Renderer>().enabled = true;
	}

    //this section down there was to show the lazer only when you grab te secondary trigger (if you wanna get it back you have to set in void start: this.GetComponent<Renderer>().enabled = false);

    //	void Update () {
    //        pressurevalue = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger, OVRInput.Controller.Touch);

    //        if (pressurevalue != 0)
    //        {
    //            this.GetComponent<Renderer>().enabled = true;
    //        }
    //        else
    //        {
    //            this.GetComponent<Renderer>().enabled = false;
    //        }
    //}
}
