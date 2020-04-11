using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followEar : MonoBehaviour {

    public bool right_ear = false;
    private GameObject snowmanObj;

    // Use this for initialization
    void Start () {
        snowmanObj = GameObject.Find("PA_Drone_1");
	}
	
	// Update is called once per frame
	void Update () {
        if (right_ear)
            this.transform.position = snowmanObj.GetComponent<Snowman>().getEarPos("right");
        else
            this.transform.position = snowmanObj.GetComponent<Snowman>().getEarPos("left");
    }
}
