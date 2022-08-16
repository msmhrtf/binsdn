using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Script that transforms hrtfs from text files in a specific folder (Resources) 
// into a matrix of the same structure as in CIPIC for Matlab database.

// Scpipts also updates azimuth and elevation for direct path and each junctions dynamically (list)

public class HRTFmanager : MonoBehaviour
{
    private bool limiter = false;
    private float limit = 45;

    private int buffSize;
    private int sampleRate;
    private int fftLength;

    private float[] azimuths = new float[27] { -90, -80, -65, -55, -45, -40, -35, -30, -25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 55, 65, 80, 90 };
    private float[] elevations = new float[52] { -90, -45, -39.374f, -33.748f, -28.122f, -22.496f, -16.87f, -11.244f, -5.618f, 0.00800000000000267f, 5.634f, 11.26f, 16.886f, 22.512f, 28.138f, 33.764f, 39.39f, 45.016f, 50.642f, 56.268f, 61.894f, 67.52f, 73.146f, 78.772f, 84.398f, 90.024f, 95.65f, 101.276f, 106.902f, 112.528f, 118.154f, 123.78f, 129.406f, 135.032f, 140.658f, 146.284f, 151.91f, 157.536f, 163.162f, 168.788f, 174.414f, 180.04f, 185.666f, 191.292f, 196.918f, 202.544f, 208.17f, 213.796f, 219.422f, 225.048f, 230.674f, 270 };

    private int hrtfLength = 200;

    // ---- get HRTF variables ----

    public GameObject listener; // the drone is the sound source. Drag and drop in Unity GUI.
    private Vector3 listenerPos;
    private List<Vector3> positionArray; // dynamic list of nodes position

    private Vector3 prevListenerPos;
    private Vector3 prevSourcePos;
    private Vector3 prevAngles;

    // HRTF pairs for direct sound and junctions. updating if position of source, listener and listener's head rotation is changing.
    private AForge.Math.Complex[][] hrtf_direct = new AForge.Math.Complex[2][];
    public List<AForge.Math.Complex[][]> hrtf_nodes = new List<AForge.Math.Complex[][]>(); // dynamic list of hrtf, following "network" in SDN script
                                   
    private float[] f_axis; // frequency axis

    private float[] azEl_direct = new float[2]; // interaural coordinate system
    public List<float[]> azEl = new List<float[]>(); // interaural coordinate system

    private Vector3 vectorLS;
    private float distanceLSz;
    private float theta;
    private float headRotation_y;
    private float headRotation_x;

    AForge.Math.Complex a;
    AForge.Math.Complex b;

    void Start()
    {

        AudioConfiguration AC = AudioSettings.GetConfiguration();
        sampleRate = AC.sampleRate;
        buffSize = AC.dspBufferSize;

        fftLength = buffSize;

        prevListenerPos = new Vector3(listener.transform.position.x, listener.transform.position.y, listener.transform.position.y);
        prevSourcePos = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z);
        prevAngles = new Vector3(this.transform.eulerAngles.x, this.transform.eulerAngles.y, this.transform.eulerAngles.z);

        // hrtf pairs for a specific source location
        for (int i = 0; i < hrtf_direct.Length; i++)
        {
            hrtf_direct[i] = new AForge.Math.Complex[fftLength];
        }

        f_axis = new float[fftLength];
        for (int j = 0; j < fftLength; j++)
        {
            f_axis[j] = j * sampleRate / fftLength;
        }

    }

    void Update()
    {

        listenerPos = listener.transform.position;

        // Check if listener, source or head rotation have moved substantially
        if (Vector3.Distance(prevSourcePos, this.transform.position) > 0.1f || Vector3.Distance(prevListenerPos, listenerPos) > 0.1f || Mathf.Abs(listener.transform.eulerAngles.x - prevAngles.x) > 0.2f || (Mathf.Abs(listener.transform.eulerAngles.y - prevAngles.y) > 0.2f) || (Mathf.Abs(listener.transform.eulerAngles.z - prevAngles.z) > 0.2f))
        {

            prevListenerPos.x = listenerPos.x; prevListenerPos.y = listenerPos.y; prevListenerPos.z = listenerPos.z;
            prevSourcePos.x = this.transform.position.x; prevSourcePos.y = this.transform.position.y; prevSourcePos.z = this.transform.position.z;
            prevAngles.x = listener.transform.eulerAngles.x; prevAngles.y = listener.transform.eulerAngles.y; prevAngles.z = listener.transform.eulerAngles.z;

            // DIRECT path azi/ele update
            azEl_direct = getAzElInteraural(this.gameObject.transform.position);
            hrtf_direct = listener.GetComponent<SDNEnvConfig>().getInterpolated_HRTF(azEl_direct);

            // JUNCTIONS azi/ele update
            positionArray = this.gameObject.GetComponent<SDN>().positionArray;
            if (positionArray.Count > 0)
            {
                // constantly go through the list and update azi and ele
                for (int i = 0; i < 2; i++) // Parallel.For(0, positionArray.Count, i => //
                {
                    azEl[i] = getAzElInteraural(positionArray[i]);

                    // get hrtfs from database
                    hrtf_nodes[i] = listener.GetComponent<SDNEnvConfig>().getInterpolated_HRTF(azEl[i]);

                }


            }
        }
    }

    public void clipdb(ref AForge.Math.Complex[] s, int cutoff)
    { // ccrma function
        int i;
        double[] ass = new double[s.Length];
        for (i = 0; i < ass.Length; i++)
            ass[i] = s[i].Magnitude;
        double mass = ass.Max();
        if (mass == 0.0)
            return;
        if (cutoff >= 0)
            return;
        double thresh = mass * Mathf.Pow(10, cutoff / 20);
        List<int> index = new List<int>();
        for (i = 0; i < ass.Length; i++)
        {
            if (ass[i] < thresh)
                s[i].Re = thresh;
        }
    }

    public AForge.Math.Complex[] fold(AForge.Math.Complex[] r)
    { // ccrma function
        AForge.Math.Complex[] rw = new AForge.Math.Complex[r.Length];

        double[] rf = new double[r.Length / 2];
        int nt = r.Length / 2;
        rf[rf.Length - 1] = 0.0;
        for (int i = 0; i < rf.Length - 1; i++)
        {
            rf[i] = r[i + 1].Re;
        }

        int j = r.Length - 1;
        rw[0].Re = r[0].Re;
        for (int i = 0; i < rf.Length - 1; i++)
        {
            rw[i + 1].Re = rf[i] + r[j].Re;
            j--;
        }
        return rw;
    }

    public int[] getIndices(float azimuth, float elevation) // get the matrix indices relative to azimuth (0) and elevation (1)
    {
        int[] indices = new int[2];

        float[] azimuths2 = new float[27];
        float[] elevations2 = new float[52];

        for (int i = 0; i < 27; i++) // Is there a way to do it more efficiently than a for loop? 
        {
            azimuths2[i] = Math.Abs(azimuths[i] - azimuth);
        }
        for (int i = 0; i < 52; i++)
        {
            elevations2[i] = Math.Abs(elevations[i] - elevation);
        }

        // Find index of minimum
        indices[0] = Enumerable.Range(0, azimuths2.Length).Aggregate((a, b) => (azimuths2[a] < azimuths2[b]) ? a : b);
        indices[1] = Enumerable.Range(0, elevations2.Length).Aggregate((a, b) => (elevations2[a] < elevations2[b]) ? a : b);

        return indices;
    }

    public float[] getAzEl(Vector3 sourcePos) // returns array of azimuth, elevation with heading and elevation without heading in vertical-polar coordinate system
    {
        float[] azEl = new float[3]; // [azimuth, elevation_heading, elevation_no_heading]

        float xL = listenerPos.x;
        float yL = listenerPos.y;
        float zL = listenerPos.z;

        float xS = sourcePos.x;
        float yS = sourcePos.y;
        float zS = sourcePos.z;

        // ----- AZIMUTH -----

        float azimuth;

        // vector LS
        vectorLS = sourcePos - listenerPos;
        // distance LS on xz-plane
        distanceLSz = Mathf.Sqrt(Mathf.Pow(vectorLS.z, 2) + Mathf.Pow(vectorLS.x, 2));
        // theta angle between head z-axis and vector LS. theta between [0, 360] clockwise
        if (sourcePos.x - listenerPos.x < 0) // if source on left of z-axis       // HERE mayebe inverse sub
        {
            theta = 360 - (Mathf.Acos(vectorLS.z / distanceLSz)) * Mathf.Rad2Deg;
        }
        else // if source on right
        {
            theta = (Mathf.Acos(vectorLS.z / distanceLSz)) * Mathf.Rad2Deg;
        }
        // head rotation around head y-axis. head z-axis is 0. clockwise [0, 360]
        headRotation_y = listener.transform.eulerAngles.y;
        // compute azimuth and scale it between [-180, 180] (vertical-polar coordinate system)
        azimuth = theta - headRotation_y;
        
        if (azimuth > 180)
        {
            azimuth = azimuth - 360;
        }
        else if (azimuth < -180)
        {
            azimuth = 360 + azimuth;
        }
        azEl[0] = azimuth;

        // ----- ELEVATION -----

        // note: 2 types of elevation. 1) static elevation, not cnsidering heading. 2) dynamic elevation, considering heading.
        //       Here I will compute both static and dynamic elevation. In practice, I will convolve hrtfs with the dynamic one, but will take the static one for the snowman model,
        //       in order to have correct shoulder reflextion. Tricky...

        float elevation_static;
        float elevation_heading;

        // static elevation
        Vector3 projectionPoint = new Vector3(xS, yS - (yS - yL), zS); // source 3d point projected onto the xz-plane
        elevation_static = Mathf.Acos(Mathf.Sqrt(Mathf.Pow(projectionPoint.x - xL, 2) + Mathf.Pow(projectionPoint.z - zL, 2)) / Vector3.Distance(listenerPos, sourcePos)) * Mathf.Rad2Deg;
        if (yL > yS)
        {
            elevation_static = -elevation_static;
        }
        azEl[2] = elevation_static;

        // heading elevation
        headRotation_x = listener.transform.eulerAngles.x;
        if (headRotation_x >= 180)
            headRotation_x = 360 - headRotation_x;
        else
            headRotation_x = -headRotation_x;
        // if source is behind
        if ((azEl[0] >= -180 && azEl[0] < -90) || (azEl[0] <= 180 && azEl[0] > 90))
            headRotation_x = -headRotation_x;

        if (headRotation_x >= 0)
        {
            elevation_heading = elevation_static - headRotation_x;
        }
        else
        {
            elevation_heading = elevation_static - headRotation_x;
        }
        // for [-90; 90]
        if (elevation_heading > 90)
        {
            elevation_heading = 180 - elevation_heading;
            // adjust azimuth
            azEl[0] = 180 - azEl[0];
            if (azEl[0] > 180)
                azEl[0] = -360+azEl[0];
        }
        if (elevation_heading < -90)
        {
            elevation_heading = -180 - elevation_heading;
            // adjust azimuth
            azEl[0] = 180 - azEl[0];
            if (azEl[0] > 180)
                azEl[0] = -360 + azEl[0];
        }

        azEl[1] = elevation_heading;
        // note: not sure about elevation with heading. considering here only head rotation around glabal x-axis and static elevation. their are not on the same plane.
        //       should i make a projection of the vector forming the head rotation with origin head onto the plane defined by head origin, source pos and source pos projection;
        //       and calculate the difference of angle between the projected vector and the LS vector?
        //       It is confusing...

        return azEl;
    }

    public float[] getAzElInteraural(Vector3 sourcePos) // returns array of azimuth, elevation with heading and elevation without heading in interaural-polar coordinate system
    {
        float[] azEl = new float[3]; // [azimuth, elevation_heading, elevation_no_heading]

        azEl = getAzEl(sourcePos);

        float[] spherical1 = new float[3];
        float[] spherical2 = new float[3];
        float[] cart1 = new float[3];
        float[] cart2 = new float[3];

        float pol1, pol2;

        if (azEl[0] < 0)
        {
            azEl[0] = 360 + azEl[0];
        }

        cart1 = sph2cart(azEl[0] * Mathf.Deg2Rad, azEl[1] * Mathf.Deg2Rad, 1.0f);
        cart2 = sph2cart(azEl[0] * Mathf.Deg2Rad, azEl[2] * Mathf.Deg2Rad, 1.0f);

        spherical1 = cart2sph(cart1[0], -cart1[2], cart1[1]);
        spherical2 = cart2sph(cart2[0], -cart2[2], cart2[1]);

        pol1 = spherical1[0];
        pol2 = spherical2[0];
        azEl[1] = Mathf.Repeat(pol1 + 90, 360) - 90;
        azEl[2] = Mathf.Repeat(pol2 + 90, 360) - 90;
        azEl[0] = -spherical1[1]; // = -spherical2[1]

        return azEl;
    }

    public float[] cart2sph(float x, float y, float z) // cartesian to spherical. based on matlab function.
                                                       // returns angles in degrees.
    {
        float[] azElr = new float[3]; // [az, el, r]

        float hypotxz = Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(z, 2));
        azElr[2] = Mathf.Sqrt(Mathf.Pow(hypotxz, 2) + Mathf.Pow(y, 2));
        azElr[1] = Mathf.Atan2(y, hypotxz) * Mathf.Rad2Deg;
        azElr[0] = Mathf.Atan2(z, x) * Mathf.Rad2Deg;

        return azElr;
    }

    public float[] sph2cart(float az, float elev, float r) // spherical to cartesian. based on matlab function.
                                                           // az, elev in radians.
    {

        float[] cart = new float[3]; // [x, y, z]

        cart[1] = r * Mathf.Sin(elev);
        float rcoselev = r * Mathf.Cos(elev);
        cart[0] = rcoselev * Mathf.Cos(az);
        cart[2] = rcoselev * Mathf.Sin(az);

        return cart;
    }

    public static float[] PhaseUnwrap(AForge.Math.Complex[] data)
    {
        float[] newdata;
        float offset = 0.0f;
        newdata = new float[data.Length];

        newdata[0] = (float)data[0].Phase;
        for (int i = 1; i < data.Length; i++)
        {
            if ((data[i - 1].Phase - data[i].Phase) > Mathf.PI)
            {
                offset += 2 * Mathf.PI;
            }
            else if ((data[i - 1].Phase - data[i].Phase) < -Mathf.PI)
            {
                offset -= 2 * Mathf.PI;
            }
            newdata[i] = (float)data[i].Phase + offset;
        }
        return newdata;
    }

    public int getHrtfLength()
    {
        return hrtfLength;
    }

    public AForge.Math.Complex[][] getHrftDirect()
    {
        return hrtf_direct;
    }

    public float[] getAzElDirect()
    {
        return azEl_direct;
    }

    public List<float[]> getJAzEl()
    {
        return azEl;
    }

}
