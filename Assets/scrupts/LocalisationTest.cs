using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalisationTest : MonoBehaviour {

    AudioSource audioData;

    void Start () {
        audioData = GetComponent<AudioSource>();
    }
	
	void Update () {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
        }
    }
}
