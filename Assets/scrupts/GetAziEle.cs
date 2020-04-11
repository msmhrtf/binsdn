using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetAziEle : MonoBehaviour {

    public GameObject source; // the drone is the sound source. Drag and drop in Unity GUI.
    private Vector3[] positionArray = new Vector3[7]; // 0 = direct path; 1-6 = wall nodes

    private Vector3 sourcePosition;

    // direct sound and junctions azimuth and elevation
    private float[] azi = new float[7] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    private float[] ele = new float[7] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    private Vector3 vectorLS;
    private float distanceLSz;
    private float theta;
    private float phi;
    private float headRotation_y;
    private float headRotation_x;

    private float[] test = new float[3];
    private float[] test2 = new float[3];

    void Start() {
        
        
    }


    void Update() {

        positionArray[0] = source.transform.position;
        
        sourcePosition = source.transform.position;
        test = getAzEl(sourcePosition);
        test2 = getAzElInteraural(sourcePosition);
        // Debug.Log("azimuth vertical: " + test[0]);

    }


    public float[] getAzEl(Vector3 sourcePos) // returns array of azimuth, elevation with heading and elevation without heading in vertical-polar coordinate system
    {
        float[] azEl = new float[3]; // [azimuth, elevation_heading, elevation_no_heading]

        float xL = this.transform.position.x;
        float yL = this.transform.position.y;
        float zL = this.transform.position.z;

        //float rX = this.transform.eulerAngles.x;
        //float rY = this.transform.eulerAngles.y;
        //float rZ = this.transform.eulerAngles.z;

        float xS = sourcePos.x;
        float yS = sourcePos.y;
        float zS = sourcePos.z;

        // ----- AZIMUTH -----

        float azimuth;

        // vector LS
        vectorLS = sourcePos - this.transform.position;
        // distance LS on xz-plane
        distanceLSz = Mathf.Sqrt(Mathf.Pow(vectorLS.z, 2) + Mathf.Pow(vectorLS.x, 2));
        // theta angle between head z-axis and vector LS. theta between [0, 360] clockwise
        if (sourcePos.x - this.transform.position.x < 0) // if source on left of z-axis
        {
            theta = 360 - (Mathf.Acos(vectorLS.z / distanceLSz)) * Mathf.Rad2Deg;
        }
        else // if source on right
        {
            theta = (Mathf.Acos(vectorLS.z / distanceLSz)) * Mathf.Rad2Deg;
        }
        // head rotation around head y-axis. head z-axis is 0. clockwise [0, 360]
        headRotation_y = this.transform.eulerAngles.y;
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
        elevation_static = Mathf.Acos(Mathf.Sqrt(Mathf.Pow(projectionPoint.x - xL, 2) + Mathf.Pow(projectionPoint.z - zL, 2)) / Vector3.Distance(this.transform.position, sourcePos)) * Mathf.Rad2Deg;
        if (yL > yS)
        {
            elevation_static = -elevation_static;
        }
        azEl[2] = elevation_static;

        // heading elevation
        headRotation_x = this.transform.eulerAngles.x;

        //float yo = headRotation_x;
        //if (headRotation_x <= 180)
        //{
        //    yo = -headRotation_x;
        //}
        //else
        //{
        //    yo = 360 - headRotation_x;
        //}

        if (headRotation_x <= 180)
        {
            elevation_heading = elevation_static + headRotation_x;
        }
        else
        {
            elevation_heading = elevation_static - (360 - headRotation_x);
        }
        azEl[1] = elevation_heading;
        // note: not sure about elevation with heading. considering here only head rotation around glabal x-axis and static elevation. their are not on the same plane.
        //       should i make a projection of the vector forming the head rotation with origin head onto the plane defined by head origin, source pos and source pos projection;
        //       and calculate the difference of angle between the projected vector and the LS vector?
        //       It confuses me...

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
        azEl[0] = -spherical1[1]; // = -spherical2[0]

        //Debug.Log("azi1 " + -spherical1[1] + " azi2 " + -spherical2[1] + " ele1 " + azEl[1] + " ele2 " + azEl[2]);

        return azEl;
    }

    public float[] cart2sph(float x, float y, float z) // cartesian to spherical. based on matlab function.
                                                       // returns angles in degrees.
    {
        float[] azElr = new float[3]; // [az, el, r]

        float hypotxz = Mathf.Sqrt(Mathf.Pow(x,2) + Mathf.Pow(z, 2));
        azElr[2] = Mathf.Sqrt(Mathf.Pow(hypotxz, 2) + Mathf.Pow(y, 2));
        azElr[1] = Mathf.Atan2(y, hypotxz) * Mathf.Rad2Deg;
        azElr[0] = Mathf.Atan2(z, x) * Mathf.Rad2Deg;

        return azElr;
    }

    public float[] sph2cart(float az, float elev, float r) // spherical to cartesian. based on matlab function.
                                                           // az, elev in radians
    {

        float[] cart = new float[3]; // [x, y, z]

        cart[1] = r * Mathf.Sin(elev);
        float rcoselev = r * Mathf.Cos(elev);
        cart[0] = rcoselev * Mathf.Cos(az);
        cart[2] = rcoselev * Mathf.Sin(az);

        return cart;
    }


}
