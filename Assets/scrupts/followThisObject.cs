using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followThisObject : MonoBehaviour {

    public GameObject followThis;
	
	void Update () {
        this.transform.position = followThis.transform.position;
    }
}
