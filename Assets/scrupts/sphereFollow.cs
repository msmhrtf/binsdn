using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sphereFollow : MonoBehaviour
{

    // private Vector3 contactPointGlobal;
    // private Vector3 localContactPoint;
    // private Vector3 sphereGlobal;


    // Update is called once per frame
    void Update()
    {
        // always follow main camera
        this.transform.position = GameObject.Find("CenterEyeAnchor").transform.position;

        // get global contact point
        // contactPointGlobal = GameObject.Find("PointerRight").GetComponent<storePointwithLazer>().myPoint;

        // transform in local point
        // localContactPoint = transform.InverseTransformPoint(contactPointGlobal);

        //sphereGlobal = transform.TransformPoint(this.transform.position);
        // sphereGlobal = this.transform.position;

        // print
        //print("glabal contact: " + contactPointGlobal);
        //print("local contact: " + localContactPoint);
        //print("sphere: " + sphereGlobal);
    }
}