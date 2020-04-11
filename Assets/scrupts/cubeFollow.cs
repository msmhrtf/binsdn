using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cubeFollow : MonoBehaviour
{

    private Vector3 headPos;
    private Vector3 cubePos;

    // Use this for initialization
    void Start()
    {
        cubePos = this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        headPos = GameObject.Find("CenterEyeAnchor").transform.position;
        cubePos.y = headPos.y;
        cubePos.x = headPos.x;
        cubePos.z = headPos.z + 4.0f;
        this.transform.position = cubePos;
    }
}
