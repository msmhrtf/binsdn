using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class storePointWithLazer_hoa : MonoBehaviour {

    // type of rendering
    private string type;
    // ID of the tested subject
    private string ID;

    private GameObject sphere;
    private float lastshoot;
    private float lastshoot2;
    private Vector3 myPoint = new Vector3(0.0f, 0.0f, 0.0f);
    RaycastHit hit = default(RaycastHit);
    private float r = 0.0f;
    private float az = 0.0f;
    private float el = 0.0f;
    private float[] azElr;
    private List<float[]> trueAzEl;
    private Vector3 spherePos;
    private Vector3 centeredPoint;
    private int idx;
    private int[] posExp_big;
    private int[] posExp_small;
    private string[] condi_big;
    private string[] condi_small;

    private bool flag = true;


    void Start () {

        sphere = GameObject.Find("Sphere");
        idx = 1;
        ID = GameObject.Find("subject_info").GetComponent<SubjectInfo>().ID;
        type = GameObject.Find("Type").GetComponent<TypeForHOA>().getTypeHoa();

        posExp_big = new int[] { 1, 5, 1, 2, 3, 2, 4, 5, 6 };
        posExp_small = new int[] { 5, 3, 1, 3, 6, 2, 4, 6, 4 };

        condi_big = new string[] { "d", "b", "b", "d", "b", "b", "b", "d", "b"};
        condi_small = new string[] { "s", "d", "s", "s", "d", "s", "s", "s", "d" };


        trueAzEl = new List<float[]>
        {
            new float[] { 0.0f, 0.9f },
            new float[] { 30.0f, 17.9f },
            new float[] { 120.0f, -36.8f },
            new float[] { 180.0f, 43.7f },
            new float[] { -150.0f, -27, 8f },
            new float[] { -60.0f, 28.9f }
        };
    }
	
	void Update () {

        type = GameObject.Find("Type").GetComponent<TypeForHOA>().getTypeHoa();

        if (OVRInput.Get(OVRInput.Button.One) == true && Time.time - lastshoot > 0.2f && flag)
        {

            //print("Point of contact: " + myPoint);

            spherePos = sphere.transform.position;
            centeredPoint.x = (myPoint.x - spherePos.x);
            centeredPoint.y = (myPoint.y - spherePos.y);
            centeredPoint.z = (myPoint.z - spherePos.z);
            azElr = cart2sph(centeredPoint.x, centeredPoint.y, centeredPoint.z);
            //CartesianToSpherical(centeredPoint, out r, out az, out el);
            //print("r: " + azElr[2] + "   az: " + azElr[0] + "   el: " + azElr[1]);

            SaveData(azElr[0], azElr[1]);

            lastshoot = Time.time;

            flag = false;
        }

        if (OVRInput.Get(OVRInput.Button.SecondaryThumbstick) == true && Time.time - lastshoot2 > 0.2f && flag == false)
        {
            SaveExterna();
            lastshoot2 = Time.time;
            flag = true;
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            CancelHOA();
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            CancelEXT();
        }
    }


    private void OnTriggerStay(Collider other)
    {
        Ray raycast = new Ray(this.transform.position, this.transform.forward);

        int layerMask = 1 << 8;

        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, layerMask))
        {
            myPoint = hit.point;
        }
    }

    private void SaveData(float az, float el)
    {
        string path = @"C:\Users\SMSEL\Desktop\Jason SMC10\scatAR-master\CODE_BASE\UNITY\findingFirstReflections\binauralsdn\TextFiles\" + ID + "_" + type + ".txt";
        using (StreamWriter sw = File.AppendText(path))
        {
            if (type == "hoa_g_big" || type == "hoa_p_big")
                sw.WriteLine(type.Substring(0,3) + "," + type.Substring(4,1) + "," + condi_big[idx - 1] + "," + ID + "," + idx + "," + posExp_big[idx-1] +  "," + az + "," + el + "," + trueAzEl[posExp_big[idx - 1]-1][0] + "," + trueAzEl[posExp_big[idx - 1]-1][1]);
            else
                sw.WriteLine(type.Substring(0, 3) + "," + type.Substring(4, 1) + "," + condi_small[idx - 1] + "," + ID + "," + idx + "," + posExp_small[idx - 1] + "," + az + "," + el + "," + trueAzEl[posExp_small[idx - 1]-1][0] + "," + trueAzEl[posExp_small[idx - 1]-1][1]);
        }
        Debug.Log("HOA " + idx + " type " + type);
        idx = (idx + 1) % 10;
        if (idx < 1)
            idx++;
    }

    public void SaveExterna()
    {

        string path = @"C:\Users\SMSEL\Desktop\Jason SMC10\scatAR-master\CODE_BASE\UNITY\findingFirstReflections\binauralsdn\TextFiles\" + ID + "_" + type + "_externa.txt";
        using (StreamWriter sw = File.AppendText(path))
        {
            if (idx == 1)
            {
                if (type == "hoa_g_big" || type == "hoa_p_big")
                    sw.WriteLine(type.Substring(0, 3) + "," + type.Substring(4, 1) + "," + condi_big[8] + "," + ID + ",9," + posExp_big[8] + "," + GameObject.Find("Slider").GetComponent<SliderControl>().getSliderVal());
                else
                    sw.WriteLine(type.Substring(0, 3) + "," + type.Substring(4, 1) + "," + condi_small[8] + "," + ID + ",9," + posExp_small[8] + "," + GameObject.Find("Slider").GetComponent<SliderControl>().getSliderVal());

                Debug.Log("EXT 9 type " + type);
            }
            else
            {
                if (type == "hoa_g_big" || type == "hoa_p_big")
                    sw.WriteLine(type.Substring(0, 3) + "," + type.Substring(4, 1) + "," + condi_big[idx - 2] + "," + ID + "," + (idx - 1) + "," + posExp_big[idx - 2] + "," + GameObject.Find("Slider").GetComponent<SliderControl>().getSliderVal());
                else
                    sw.WriteLine(type.Substring(0, 3) + "," + type.Substring(4, 1) + "," + condi_small[idx - 2] + "," + ID + "," + (idx - 1) + "," + posExp_small[idx - 2] + "," + GameObject.Find("Slider").GetComponent<SliderControl>().getSliderVal());

                Debug.Log("EXT " + (idx - 1) + " type " + type);
            }
        }
    }

    private void CancelHOA()
    // cancel stimuli
    {

        idx--;
        if (idx < 1)
            idx++;

        if (idx == 1)
            idx = 9;

        Debug.Log("Cancelling HOA " + idx);

        // erase last line in text file

        string path = @"C:\Users\SMSEL\Desktop\Jason SMC10\scatAR-master\CODE_BASE\UNITY\findingFirstReflections\binauralsdn\TextFiles\" + ID + "_" + type + ".txt";

        List<string> lines = File.ReadAllLines(path).ToList();
        File.WriteAllLines(path, lines.GetRange(0, lines.Count - 1).ToArray());

        flag = true;
    }

    private void CancelEXT()
    // cancel stimuli
    {
        Debug.Log("Cancelling EXT " + (idx-1));

        // erase last line in text file

        string path = @"C:\Users\SMSEL\Desktop\Jason SMC10\scatAR-master\CODE_BASE\UNITY\findingFirstReflections\binauralsdn\TextFiles\" + ID + "_" + type + "_externa.txt";

        List<string> lines = File.ReadAllLines(path).ToList();
        File.WriteAllLines(path, lines.GetRange(0, lines.Count - 1).ToArray());

        flag = false;
    }

    public float[] cart2sph(float x, float y, float z) // cartesian to spherical. based on matlab function.
                                                       // returns angles in degrees.
    {
        float[] azElr = new float[3]; // [az, el, r]
        x = -x;

        float hypotxz = Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(z, 2));
        azElr[2] = Mathf.Sqrt(Mathf.Pow(hypotxz, 2) + Mathf.Pow(y, 2));
        azElr[1] = Mathf.Atan2(y, hypotxz) * Mathf.Rad2Deg;
        azElr[0] = Mathf.Atan2(z, x) * Mathf.Rad2Deg - 90;

        if (azElr[0] < -180)
            azElr[0] += 360;

        return azElr;
    }

    public static void CartesianToSpherical(Vector3 cartCoords, out float outRadius, out float outPolar, out float outElevation)
    {
        if (cartCoords.x == 0)
            cartCoords.x = Mathf.Epsilon;
        outRadius = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                        + (cartCoords.y * cartCoords.y)
                        + (cartCoords.z * cartCoords.z));
        outPolar = Mathf.Atan(cartCoords.z / cartCoords.x);
        if (cartCoords.x < 0)
            outPolar += Mathf.PI;
        outElevation = Mathf.Asin(cartCoords.y / outRadius);

        outPolar *= Mathf.Rad2Deg;
        outElevation *= Mathf.Rad2Deg;
    }

    public Vector3 getPoint()
    {
        return myPoint;
    }
}
