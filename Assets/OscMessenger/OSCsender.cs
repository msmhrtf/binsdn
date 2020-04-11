using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OSCsender : MonoBehaviour
{
    private float frames;

    public float rotX;
    public float rotY;
    public float rotZ;

    void Start()
    {
        OSCHandler.Instance.Init();
    }

    // Update is called once per frame
    void Update()
    {
        frames++;

        if (frames % 3 == 0)
        {
            FrameTwoUpdate();
        }
    }

    void FrameTwoUpdate()
    {
        rotX = this.transform.eulerAngles.x;
        rotY = this.transform.eulerAngles.y;
        rotZ = this.transform.eulerAngles.z;

        // Yaw
        if (rotY > 180)
            rotY = -(360 - rotY);

        // Pitch
        if (rotX >= 180)
            rotX = 360 - rotX;
        else if (rotX < 180)
            rotX = -rotX;

        // Roll
        if (rotZ >= 180)
            rotZ = 360 - rotZ;
        else if (rotZ < 180)
            rotZ = -rotZ;

        List<object> headRot = new List<object>();
        headRot.AddRange(new object[] { rotY, rotX, rotZ });

        //string str = " \\ypr[0] " + rotY.ToString() + " \\ypr[1] " + rotX.ToString() + " \\ypr[2] " + rotZ.ToString();

        OSCHandler.Instance.SendMessageToClient("UnityToSparta", "/ypr", headRot);

        //OSCHandler.Instance.SendMessageToClient("UnityToSparta2", "\\ypr", headRot);

        //OSCHandler.Instance.SendMessageToClient("UnityToSparta", "\\ypr[0]", rotY);
        //OSCHandler.Instance.SendMessageToClient("UnityToSparta", "\\ypr[1]", rotX);
        //OSCHandler.Instance.SendMessageToClient("UnityToSparta", "\\ypr[2]", rotZ);
    }
}