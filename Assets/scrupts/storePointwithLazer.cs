using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class storePointwithLazer : MonoBehaviour
    // Script that handles the localisation test. Places sources in specific positions in space, plays the sound and saves coordinates of user input.
{
    // type of rendering
    public string type;
    // ID of the tested subject
    private string ID;
    private string type2;

    private GameObject sourceManager;
    private GameObject roomManager;

    private GameObject sphere;
    private GameObject sdnSource;
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
    private int idx1;
    //private Vector2[] azEl_exp = new Vector2[25];

    AudioSource audioData;
    ulong fs;

    private int[] posExp_big;
    private int[] posExp_small;

    bool okayFlag = false;
    bool crossFlag = true;

    void Start()
    {
        sourceManager = GameObject.Find("Sources");
        roomManager = GameObject.Find("Rooms");

        sphere = GameObject.Find("Sphere");
        //sdnSource = GameObject.Find("PA_Drone_1");
        //sdnSource = sourceManager.GetComponent<SourceManager>().getActiveSource();
        //audioData = sdnSource.GetComponent<AudioSource>();
        AudioConfiguration AC = AudioSettings.GetConfiguration();
        fs = (ulong)AC.sampleRate;

        ID = GameObject.Find("subject_info").GetComponent<SubjectInfo>().ID;
        idx = 0;
        idx1 = 1;

        posExp_big = new int[] { 4, 1, 0, 4, 2, 1, 3, 5, 5 };   // 1 4 5 dry -> pos array 1 3 7 -> in storePointWithLazer script, I will deactivate reflections when those occur.
        posExp_small = new int[] { 4, 2, 0, 3, 2, 1, 3, 0, 5 }; // 0 2 3 dry -> pos array 1 3 7


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

    void Update()
    {

        //spherePos = sphere.transform.position;
        //centeredPoint.x = (myPoint.x - spherePos.x);
        //centeredPoint.y = (myPoint.y - spherePos.y);
        //centeredPoint.z = (myPoint.z - spherePos.z);
        //azElr = cart2sph(centeredPoint.x, centeredPoint.y, centeredPoint.z);
        ////CartesianToSpherical(centeredPoint, out r, out az, out el);
        //print("r: " + azElr[2] + "   az: " + azElr[0] + "   el: " + azElr[1]);

        if (sourceManager.GetComponent<SourceManager>().getFlag())
        {
            sdnSource = sourceManager.GetComponent<SourceManager>().getActiveSource();
            audioData = sdnSource.GetComponent<AudioSource>();
                                        //Debug.Log("OK");
            sourceManager.GetComponent<SourceManager>().setFlagDown();
        }

        // I start the sequence
        if (Input.GetKeyDown(KeyCode.Space)) //  && idxExperiment < 13
        {
            audioData.Play(fs*0); // delay of 1 sec
            okayFlag = true;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            // set next source position and play sound
            setNextPosition();
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            CancelSDN();
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            CancelEXT();
        }

        if (OVRInput.Get(OVRInput.Button.One) == true && Time.time - lastshoot > 0.2f && audioData.isPlaying == false && okayFlag && crossFlag)
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
            okayFlag = false;
            crossFlag = false;
        }

        if (OVRInput.Get(OVRInput.Button.SecondaryThumbstick) == true && Time.time - lastshoot2 > 0.2f && crossFlag == false)
        {
            SaveExterna();
            lastshoot2 = Time.time;
            crossFlag = true;
        }

       
    }

    private void setNextPosition()
    {
        //sourceManager.GetComponent<SourceManager>().incrementIdx(1); // increment index in source structure
        //sourceManager.GetComponent<SourceManager>().activateSource(sourceManager.GetComponent<SourceManager>().getIdx()); // activate this source and deactivate others
        idx = (idx+1) % 9;

        //sourceManager.GetComponent<SourceManager>().activateSource(posExp[idx]); // activate this source and deactivate others
        //sdnSource = sourceManager.GetComponent<SourceManager>().getActiveSource();
        //audioData = sdnSource.GetComponent<AudioSource>();

        if (roomManager.GetComponent<RoomManager>().room_num == 2)
        {
            sourceManager.GetComponent<SourceManager>().setSourceToPos(posExp_big[idx], idx); // here i activate(deactivate reflections
        }
        else if (roomManager.GetComponent<RoomManager>().room_num == 1)
        {
            sourceManager.GetComponent<SourceManager>().setSourceToPos(posExp_small[idx], idx);
        }
        else
            Debug.Log("PROBLEM IN ROOM MANAGER !");

        Debug.Log("pos " + (idx + 1));
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

        if (roomManager.GetComponent<RoomManager>().room_num == 1)
            type2 = type + "_small";
        else if (roomManager.GetComponent<RoomManager>().room_num == 2)
            type2 = type + "_big";
        else
            Debug.Log("PROBLEM IN ROOM MANAGER !");

        string path = @"C:\Users\SMSEL\Desktop\Jason SMC10\scatAR-master\CODE_BASE\UNITY\findingFirstReflections\binauralsdn\TextFiles\" + ID + "_" + type2 + ".txt";
        using (StreamWriter sw = File.AppendText(path))
        {
            // dry sound
            if (idx == 1 || idx == 3 || idx == 7)
            {
                if (roomManager.GetComponent<RoomManager>().room_num == 2)
                    sw.WriteLine(type2.Substring(0, 3) + "," + type2.Substring(4, 1) + ",d," + ID + "," + idx1 + "," + (posExp_big[idx]+1) + "," + az + "," + el + "," + trueAzEl[ posExp_big[idx] ][0] + "," + trueAzEl[ posExp_big[idx] ][1]);
                else if (roomManager.GetComponent<RoomManager>().room_num == 1)
                    sw.WriteLine(type2.Substring(0, 3) + "," + type2.Substring(4, 1) + ",d," + ID + "," + idx1 + "," + (posExp_small[idx]+1) + "," + az + "," + el + "," + trueAzEl[posExp_small[idx]][0] + "," + trueAzEl[posExp_small[idx]][1]);
                else
                    Debug.Log("PROBLEM IN ROOM MANAGER !");
            }
            // relfections
            else
            {
                if (roomManager.GetComponent<RoomManager>().room_num == 2)
                    sw.WriteLine(type2.Substring(0, 3) + "," + type2.Substring(4, 1) + ",b," + ID + "," + idx1 + "," + (posExp_big[idx]+1) + "," + az + "," + el + "," + trueAzEl[posExp_big[idx]][0] + "," + trueAzEl[posExp_big[idx]][1]);
                else if (roomManager.GetComponent<RoomManager>().room_num == 1)
                    sw.WriteLine(type2.Substring(0, 3) + "," + type2.Substring(4, 1) + ",s," + ID + "," + idx1 + "," + (posExp_small[idx]+1) + "," + az + "," + el + "," + trueAzEl[posExp_small[idx]][0] + "," + trueAzEl[posExp_small[idx]][1]);
                else
                    Debug.Log("PROBLEM IN ROOM MANAGER !");
            }

        }
        Debug.Log("Saved SDN " + idx1 + " room " + roomManager.GetComponent<RoomManager>().room_num);
        idx1 = (idx1 + 1) % 10;
        if (idx1 < 1)
            idx1++;
    }

    public void SaveExterna()
    {
        if (roomManager.GetComponent<RoomManager>().room_num == 1)
            type2 = type + "_small";
        else if (roomManager.GetComponent<RoomManager>().room_num == 2)
            type2 = type + "_big";
        else
            Debug.Log("PROBLEM IN ROOM MANAGER !");

        string path = @"C:\Users\SMSEL\Desktop\Jason SMC10\scatAR-master\CODE_BASE\UNITY\findingFirstReflections\binauralsdn\TextFiles\" + ID + "_" + type2 + "_externa.txt";
        using (StreamWriter sw = File.AppendText(path))
        {
            // dry sound
            if ( idx == 1 || idx == 3 || idx == 7)
            {
                if (roomManager.GetComponent<RoomManager>().room_num == 2)
                    sw.WriteLine(type2.Substring(0, 3) + "," + type2.Substring(4, 1) + ",d," + ID + "," + (idx1 - 1) + "," + (posExp_big[idx]+1) + "," + GameObject.Find("Slider").GetComponent<SliderControl>().getSliderVal());
                else if (roomManager.GetComponent<RoomManager>().room_num == 1)
                    sw.WriteLine(type2.Substring(0, 3) + "," + type2.Substring(4, 1) + ",d," + ID + "," + (idx1 - 1) + "," + (posExp_small[idx]+1) + "," + GameObject.Find("Slider").GetComponent<SliderControl>().getSliderVal());
                else
                    Debug.Log("PROBLEM IN ROOM MANAGER !");
            }
            // relfections
            else
            {
                if (roomManager.GetComponent<RoomManager>().room_num == 2)
                {
                    if (idx1 == 1)
                        sw.WriteLine(type2.Substring(0, 3) + "," + type2.Substring(4, 1) + ",b," + ID + ",9," + (posExp_big[8] + 1) + "," + GameObject.Find("Slider").GetComponent<SliderControl>().getSliderVal());
                    else
                        sw.WriteLine(type2.Substring(0, 3) + "," + type2.Substring(4, 1) + ",b," + ID + "," + (idx1 - 1) + "," + (posExp_big[idx] + 1) + "," + GameObject.Find("Slider").GetComponent<SliderControl>().getSliderVal());
                }
                else if (roomManager.GetComponent<RoomManager>().room_num == 1)
                {
                    if (idx1 == 1)
                        sw.WriteLine(type2.Substring(0, 3) + "," + type2.Substring(4, 1) + ",s," + ID + ",9," + (posExp_small[8] + 1) + "," + GameObject.Find("Slider").GetComponent<SliderControl>().getSliderVal());
                    else
                        sw.WriteLine(type2.Substring(0, 3) + "," + type2.Substring(4, 1) + ",s," + ID + "," + (idx1 - 1) + "," + (posExp_small[idx] + 1) + "," + GameObject.Find("Slider").GetComponent<SliderControl>().getSliderVal());
                }
                else
                    Debug.Log("PROBLEM IN ROOM MANAGER !");
            }

        }
        Debug.Log("Saved EXT " + (idx1-1) + " room " + roomManager.GetComponent<RoomManager>().room_num);
    }

    private void CancelSDN()
    // cancel stimuli
    {

        idx1--;
        if (idx1 < 1)
            idx1++;

        if (idx1 == 1)
            idx1 = 9;

        Debug.Log("Cancelling SDN " + idx1);

        // erase last line in text file

        if (roomManager.GetComponent<RoomManager>().room_num == 1)
            type2 = type + "_small";
        else if (roomManager.GetComponent<RoomManager>().room_num == 2)
            type2 = type + "_big";
        else
            Debug.Log("PROBLEM IN ROOM MANAGER !");

        string path = @"C:\Users\SMSEL\Desktop\Jason SMC10\scatAR-master\CODE_BASE\UNITY\findingFirstReflections\binauralsdn\TextFiles\" + ID + "_" + type2 + ".txt";

        List<string> lines = File.ReadAllLines(path).ToList();
        File.WriteAllLines(path, lines.GetRange(0, lines.Count - 1).ToArray());

        crossFlag = true;
        okayFlag = true;
    }

    private void CancelEXT()
    // cancel stimuli
    {
        Debug.Log("Cancelling EXT " + (idx1-1));

        // erase last line in text file

        if (roomManager.GetComponent<RoomManager>().room_num == 1)
            type2 = type + "_small";
        else if (roomManager.GetComponent<RoomManager>().room_num == 2)
            type2 = type + "_big";
        else
            Debug.Log("PROBLEM IN ROOM MANAGER !");

        string path = @"C:\Users\SMSEL\Desktop\Jason SMC10\scatAR-master\CODE_BASE\UNITY\findingFirstReflections\binauralsdn\TextFiles\" + ID + "_" + type2 + "_externa.txt";

        List<string> lines = File.ReadAllLines(path).ToList();
        File.WriteAllLines(path, lines.GetRange(0, lines.Count - 1).ToArray());

        crossFlag = false;
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

//azEl_exp[0] = new Vector2(120, -28); // continue on that: find coordinates from angles, then in setNextPos() change source pos in a loop.
//azEl_exp[1] = new Vector2(-120, 28);
//azEl_exp[2] = new Vector2(120, 39);
//azEl_exp[3] = new Vector2(120, 84);
//azEl_exp[4] = new Vector2(180, 84);
//azEl_exp[5] = new Vector2(0 , 39);
//azEl_exp[6] = new Vector2(-60, 28);
//azEl_exp[7] = new Vector2(180, -28);
//azEl_exp[8] = new Vector2(0, 90);
//azEl_exp[9] = new Vector2(-60, 39);
//azEl_exp[10] = new Vector2(0, 28);
//azEl_exp[11] = new Vector2(180, 28);
//azEl_exp[12] = new Vector2(-60, 84);
//azEl_exp[13] = new Vector2(-120, 84);
//azEl_exp[14] = new Vector2(180, 39);
//azEl_exp[15] = new Vector2(120, 28);
//azEl_exp[16] = new Vector2(60, 39);
//azEl_exp[17] = new Vector2(0 , -28);
//azEl_exp[18] = new Vector2(0 , 84);
//azEl_exp[19] = new Vector2(60, -28);
//azEl_exp[20] = new Vector2(-60, -28);
//azEl_exp[21] = new Vector2(60, 84);
//azEl_exp[22] = new Vector2(-120, -28);
//azEl_exp[23] = new Vector2(-120, 39);
//azEl_exp[24] = new Vector2(60, 28);