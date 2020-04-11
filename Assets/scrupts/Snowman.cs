using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Snowman HAT model (Algazi et al.) 
// Attach this script to each sources. Dependencies: HRTFmanager, HRTFcontainer, SDN

public class Snowman : MonoBehaviour
{

    // externals
    GameObject listener;
    private List<Vector3> positionArray; // junction positions

    // outputs
    private AForge.Math.Complex[] outL;
    private AForge.Math.Complex[] outR;
    public List<AForge.Math.Complex[]> JoutL = new List<AForge.Math.Complex[]>();
    public List<AForge.Math.Complex[]> JoutR = new List<AForge.Math.Complex[]>();

    // constants
    private float sampleRate;
    private float samplePeriod;
    private float[] impulse; // impulse signal
    private const float c = 343.0f; // speed of sound
    private const float alfa_min = 0.1f; // sphere shadow constant
    private const float theta_min = 5 * Mathf.PI / 6; // sphere shadow constant (= 150 degrees)
    private float theta_flat; // interpolation variable
    private float tau_head, tau_torso; // head and torso shadow constant
    private float a0_head, a1_head, a0_torso, a1_torso; // head and torso shadow filter "a" coefficients (dependent only on head radius)
    private float ac; // head ITD constant

    // anthropometric parmeters [meters]
    //public float X1; // head large (cf cipic)
    //public float X3; // head height (cf cipic)
    //private float a_opt; // optimal head radius
    public float a; // head radius
    public float b; // torso radius
    public float h; // neck height
    public float rho; // shoulder reflection coefficient
    public float e_b; // ear down shift
    public float e_d; // ear back shift
    public bool shift = false; // flag shiftted ears

    // positions
    private Vector3 S; // position of source
    private Vector3 M; // position of head center
    private Vector3 B; // position of torso center
    private Vector3 D_l; // postion of left ear
    private Vector3 D_r; // postion of right ear

    // vectors
    private Vector3 d_l; // from torso center to left ear
    private Vector3 d_r; // from torso center to right ear
    private Vector3 e_l; // from head center to left ear
    private Vector3 e_r; // from head center to right ear
    private Vector3 s; // unit vector from torso center to source

    // other parameters
    private float shadow_limit_l; // shadow cone limit left ear
    private float shadow_limit_r; // shadow cone limit right ear
    private float d_l_norm; // norm of d_l
    private float d_r_norm; // norm of d_r
    private float itd_l; // direct path ITD based on spherical model (in samples)
    private float itd_r;
    public List<float> Jitd_l = new List<float>(); // junctions ITDs (in samples)
    public List<float> Jitd_r = new List<float>();
    // torso shadow sub-model global variables
    private float zeta_min_l;
    private float zeta_min_r;
    private float w1_l;
    private float w1_r;
    // torso reflection sub-model gobal variables
    private float A_l;
    private float A_r;
    private float alfa0_l;
    private float alfa0_r;
    private float alfa_max_l;
    private float alfa_max_r;

    // filters
    private BiQuadFilter headShadowFilter;
    //private BiQuadFilter torsoShadowFilter;

    // ear position rotation parameters
    private Vector3 left, right;
    // az and el of ears relative to sphere center, in rad
    private float az;
    private float el;
    private float[] cart_l, cart_r;

    private float delay_left;
    private 

    void Start()
    {

        listener = GameObject.Find("CenterEyeAnchor");

        AudioConfiguration AC = AudioSettings.GetConfiguration();
        sampleRate = AC.sampleRate;
        samplePeriod = 1 / sampleRate;

        impulse = new float[200];
        impulse[0] = 1.0f;

        // compute best radius (linear regression formula from Bahu, 2018)
        // a_opt = 0.41f * X1 + 0.22f * X3 + 3.7f;

        theta_flat = (float)(theta_min * (0.5 + 1 / Mathf.PI * Mathf.Asin(alfa_min / (2 - alfa_min)))); // rad
        tau_head = 2 * a / c;
        a0_head = 2 * tau_head + samplePeriod;
        a1_head = samplePeriod - 2 * tau_head;
        ac = a / c;

        headShadowFilter = new BiQuadFilter(1, 1, 1, 1, 1, 1); // instantiate filter
        //torsoShadowFilter = new BiQuadFilter(1, 1, 1, 1, 1, 1);

        // +- 90 ear pos around origin
        left.x = a * Mathf.Cos(Mathf.PI);
        left.y = 0.0f;
        left.z = a * Mathf.Sin(Mathf.PI);
        right.x = a * Mathf.Cos(2 * Mathf.PI);
        right.y = 0.0f;
        right.z = a * Mathf.Sin(2 * Mathf.PI);
        // shifted ears
        az = Mathf.Atan(a / e_b);
        el = Mathf.Atan(a / e_d) - Mathf.PI / 2;
        cart_r = this.gameObject.GetComponent<HRTFmanager>().sph2cart(-(az + listener.transform.eulerAngles.y * Mathf.Deg2Rad) + Mathf.PI / 2, el + listener.transform.eulerAngles.x * Mathf.Deg2Rad, a);
        cart_l = this.gameObject.GetComponent<HRTFmanager>().sph2cart(-(-az + listener.transform.eulerAngles.y * Mathf.Deg2Rad) + Mathf.PI / 2, el + listener.transform.eulerAngles.x * Mathf.Deg2Rad, a);

        //Vector3 X = new Vector3 ( 2.0f, 0.0f, 0.0f );
        //Vector3 Y;
        //float[][] Z = new float[3][];

        //X[0] = new float[3] { 1.0f, 0.0f, 0.0f };
        //X[1] = new float[3] { 0.0f, 1.0f, 0.0f };
        //X[2] = new float[3] { 0.0f, 0.0f, 1.0f };

        //float[][] R = euler2rotationMatrix(0.0f * Mathf.Deg2Rad, 90.0f * Mathf.Deg2Rad, 90.0f * Mathf.Deg2Rad);

        //Y[0] = new float[3] { 4, 5, 6 };
        //Y[1] = new float[3] { 7, 8, 9 };
        //Y[2] = new float[3] { 10, 11, 12 };

        //for (int i = 0; i < Z.Length; i++)
        //    Z[i] = new float[3];

        //MultiplyMatricesParallel(X, Y, Z);

        //Y.x = R[0][0] * X.x + R[0][1] * X.z + R[0][2] * X.y + M.x;
        //Y.z = R[1][0] * X.x + R[1][1] * X.z + R[1][2] * X.y + M.z;
        //Y.y = R[2][0] * X.x + R[2][1] * X.z + R[2][2] * X.y + M.y;

        //for (int i = 0; i < R.Length; i++)
        //{
        //Debug.Log(Y.x + " " + Y.z + " " + Y.y);
        //}
    }

    void Update()
    {

        // TO DO: 1) update only when movement
        //        2) create 1 snowman attached to the listener updating head, ears, torso positions and shadow limit, ear vecotrs, ... 
        //        3) create another script that computes the rest for each sources.

        //Debug.Log(listener.transform.eulerAngles.x);

        // update parameters
        // general
        S = this.transform.position;
        M = listener.transform.position;
        positionArray = this.gameObject.GetComponent<SDN>().positionArray;
        //B.x = M.x;
        //B.y = M.y - (h + b);
        //B.z = M.z;
        computeEarsPos(ref D_l, ref D_r, shift);
        //d_l = D_l - B;
        //d_r = D_r - B;
        e_l = D_l - M;
        e_r = D_r - M;
        //d_l_norm = d_l.magnitude;
        //d_r_norm = d_r.magnitude;
        //shadow_limit_l = -Mathf.Sqrt(Mathf.Pow(d_l_norm, 2) - Mathf.Pow(b, 2));
        //shadow_limit_r = -Mathf.Sqrt(Mathf.Pow(d_r_norm, 2) - Mathf.Pow(b, 2));
        // torso shadow
        //zeta_min_l = Mathf.PI / 2 + Mathf.Acos(b / d_l_norm);
        //zeta_min_r = Mathf.PI / 2 + Mathf.Acos(b / d_r_norm);
        //w1_l = Mathf.Pow(b / d_l_norm, 2);
        //w1_r = Mathf.Pow(b / d_r_norm, 2);
        // torso reflection
        //A_l = d_l_norm / b;
        //A_r = d_r_norm / b;

        //////////////////////////////////////////////////////////////////////////// TESTING SECTION

        // DIRECT
        s = (S - M).normalized; // unit vector from head center to source (direct)
        // left ear
        float theta_D = Mathf.Acos(Vector3.Dot(s, e_l) / a);
        int indexMax_l = 0;
        float delay = 0;
        outL = headShadow(impulse, theta_D);
        outL = headITD(outL, theta_D, ref indexMax_l, false, ref delay);
        itd_l = delay*sampleRate;
        // right ear
        theta_D = Mathf.Acos(Vector3.Dot(s, e_r) / a);
        int indexMax_r = 0;
        outR = headShadow(impulse, theta_D);
        outR = headITD(outR, theta_D, ref indexMax_r, false, ref delay);
        itd_r = delay*sampleRate;

        //Debug.Log("l " + itd_l + " r " + itd_r + " ITD: " + Mathf.Abs(itd_l-itd_r));

        //itd = Mathf.Abs(indexMax_r - indexMax_l);

        AForge.Math.FourierTransform.FFT(outL, AForge.Math.FourierTransform.Direction.Forward);
        AForge.Math.FourierTransform.FFT(outR, AForge.Math.FourierTransform.Direction.Forward);

        // JUNCTIONS
        if (positionArray.Count > 0)
        {
            for (int i = 0; i < positionArray.Count; i++)
            {
                s = (positionArray[i] - M).normalized;
                // left
                indexMax_l = 0;
                theta_D = Mathf.Acos(Vector3.Dot(s, e_l) / a);
                JoutL[i] = headShadow(impulse, theta_D);
                JoutL[i] = headITD(JoutL[i], theta_D, ref indexMax_l, false, ref delay);
                Jitd_l[i] = delay*sampleRate;
                // right
                indexMax_r = 0;
                theta_D = Mathf.Acos(Vector3.Dot(s, e_r) / a);
                JoutR[i] = headShadow(impulse, theta_D);
                JoutR[i] = headITD(JoutR[i], theta_D, ref indexMax_r, false, ref delay);
                Jitd_r[i] = delay*sampleRate;

                AForge.Math.FourierTransform.FFT(JoutL[i], AForge.Math.FourierTransform.Direction.Forward);
                AForge.Math.FourierTransform.FFT(JoutR[i], AForge.Math.FourierTransform.Direction.Forward);
            }
        }

        ////////////////////////////////////////////////////////////////////////////


        //// update snowman for direct path left ear
        //if (Vector3.Dot(d_l, s) < shadow_limit_l)
        //{
        //    torsoShadowSub();
        //    //Debug.Log("shadow");
        //}
        //else
        //{
        //    torsoReflectionSub();
        //    //Debug.Log("reflection");
        //}

        //// update snowman for direct path right ear
        //if (Vector3.Dot(d_r, s) < shadow_limit_r)
        //    torsoShadowSub();
        //else
        //    torsoReflectionSub();

        //// update snowman for each junctions
        //for (int i = 1; i < positionArray.Count; i++)
        //{
        //    // left ear
        //    if (Vector3.Dot(d_l, s) < shadow_limit_l)
        //        torsoShadowSub();
        //    else
        //        torsoReflectionSub();

        //    // right ear
        //    if (Vector3.Dot(d_r, s) < shadow_limit_r)
        //        torsoShadowSub();
        //    else
        //        torsoReflectionSub();
        //}

        // f = Mathf.Sqrt(Mathf.Pow(b, 2) + Mathf.Pow(d_l_norm, 2) - 2 * b * d_l_norm * Mathf.Cos(alfa));     // depends on alfa which depends on changing param

    }

    public void torsoReflectionSub()
    {

    }

    public void torsoShadowSub()
    {

    }

    public AForge.Math.Complex[] headITD(AForge.Math.Complex[] sig, float theta, ref int indexMax , bool frac , ref float delay)
    {
        AForge.Math.Complex[] output = new AForge.Math.Complex[2048]; // 2 * buffer size
        int delay_samp;
        double maxVal = 0.0;

        if (Mathf.Abs(theta) < Mathf.PI / 2)
            delay = -ac * Mathf.Cos(theta) + ac; // adding ac in order to have positive delays, the ITD stays the same.
        else
            delay = ac * (Mathf.Abs(theta) - Mathf.PI / 2) + ac;
        
        if (!frac) // rounded delay
        {
            delay_samp = Mathf.RoundToInt(delay * sampleRate);
            int i = delay_samp;
            while (i < sig.Length)
            {
                output[i].Re = sig[i - (int)delay_samp].Re;
                // detect peak for ITD calculation
                if (output[i].Re > maxVal)
                {
                    maxVal = output[i].Re;
                    indexMax = i;
                }
                i++;
            }
        }
        else // fractional delay -> linear interploation
        {
            //int delay_base = Mathf.FloorToInt(delay * sampleRate);
            //float delay_frac = delay - delay_base;
            //int i = delay_base;
            //while (i < sig.Length)
            //{
            //    output[i].Re = sig[i - delay_base].Re;
            //    // detect peak for ITD calculation
            //    if (output[i].Re > maxVal)
            //    {
            //        maxVal = output[i].Re;
            //        indexMax = i;
            //    } 
            //    i++;
            //}

            // then add the frac delay by interpolation
        }

        return output;
    }

    public AForge.Math.Complex[] headShadow(float[] sig, float theta)
    {
        AForge.Math.Complex[] output = new AForge.Math.Complex[sig.Length];

        float alfa = (1 + alfa_min / 2) + (1 - alfa_min / 2) * Mathf.Cos(theta / theta_min * Mathf.PI);
        // filter coefficients ( coefficients a0 and a1 are constants defined in Start() )
        float b0 = 2 * alfa * tau_head + samplePeriod;
        float b1 = samplePeriod - 2 * alfa * tau_head;

        // set biquad
        headShadowFilter.SetCoefficients(a0_head, a1_head, 0, b0, b1, 0); // TO DO: create a 1st order IIR in BiQuadFilter class, would save 2 multiplications and 2 memory slots per sample!

        // filter
        for (int i = 0; i < sig.Length; i++)
            output[i].Re = headShadowFilter.Transform(sig[i]);    //output[i].Re = sig[i];

        return output;
    }

    public float[] torsoShadow(float[] sig, float theta)
    {
        float[] output = new float[sig.Length];

        float alfa = (1 + alfa_min / 2) + (1 - alfa_min / 2) * Mathf.Cos(theta / theta_min * Mathf.PI);
        // filter coefficients ( coefficients a0 and a1 are constants defined in Start() )
        float b0 = 2 * alfa * tau_torso + samplePeriod;
        float b1 = samplePeriod - 2 * alfa * tau_torso;

        // set biquad
        headShadowFilter.SetCoefficients(a0_torso, a1_torso, 0, b0, b1, 0);

        // filter
        for (int i = 0; i < sig.Length; i++)
            output[i] = headShadowFilter.Transform(sig[i]);

        return output;
    }

    

    public void computeEarsPos(ref Vector3 left_ear, ref Vector3 right_ear, bool shift)
    {   // modifies ear positions relative to head center, head radius, and head "roll"
        // rotates coordinates with yaw ptich roll rotation matrix convention

        float[][] R = euler2rotationMatrix(listener.transform.eulerAngles.y * Mathf.Deg2Rad, listener.transform.eulerAngles.z * Mathf.Deg2Rad, listener.transform.eulerAngles.x * Mathf.Deg2Rad);

        // +/- 90 degrees ears
        if (!shift)
        {
            left_ear.x = R[0][0] * left.x + R[0][1] * left.z + R[0][2] * left.y + M.x;
            left_ear.z = R[1][0] * left.x + R[1][1] * left.z + R[1][2] * left.y + M.z;
            left_ear.y = R[2][0] * left.x + R[2][1] * left.z + R[2][2] * left.y + M.y;

            right_ear.x = R[0][0] * right.x + R[0][1] * right.z + R[0][2] * right.y + M.x;
            right_ear.z = R[1][0] * right.x + R[1][1] * right.z + R[1][2] * right.y + M.z;
            right_ear.y = R[2][0] * right.x + R[2][1] * right.z + R[2][2] * right.y + M.y;
        }
        // down back shifted ears
        else
        {
            left_ear.x = R[0][0] * cart_l[0] + R[0][1] * cart_l[2] + R[0][2] * cart_l[1] + M.x;
            left_ear.z = R[1][0] * cart_l[0] + R[1][1] * cart_l[2] + R[1][2] * cart_l[1] + M.z;
            left_ear.y = R[2][0] * cart_l[0] + R[2][1] * cart_l[2] + R[2][2] * cart_l[1] + M.y;

            right_ear.x = R[0][0] * cart_r[0] + R[0][1] * cart_r[2] + R[0][2] * cart_r[1] + M.x;
            right_ear.z = R[1][0] * cart_r[0] + R[1][1] * cart_r[2] + R[1][2] * cart_r[1] + M.z;
            right_ear.y = R[2][0] * cart_r[0] + R[2][1] * cart_r[2] + R[2][2] * cart_r[1] + M.y;
        }
    }

    public float[][] euler2rotationMatrix(float alpha, float beta, float gamma)
    { // Construct the rotation matrix from Euler angles, yaw, pitch roll

        int i;
        float[][] Rx = new float[3][];
        float[][] Ry = new float[3][];
        float[][] Rz = new float[3][];
        float[][] R1 = new float[3][];
        float[][] R2 = new float[3][];
        for (i = 0; i < R1.Length; i++)
        {
            R1[i] = new float[3];
            R2[i] = new float[3];
        }

        Rx[0] = new float[3] { 1.0f, 0.0f, 0.0f };
        Rx[1] = new float[3] { 0.0f, Mathf.Cos(gamma), Mathf.Sin(gamma) };
        Rx[2] = new float[3] { 0.0f, -Mathf.Sin(gamma), Mathf.Cos(gamma) };

        Ry[0] = new float[3] { Mathf.Cos(beta), 0.0f, -Mathf.Sin(beta) };
        Ry[1] = new float[3] { 0.0f, 1.0f, 0.0f };
        Ry[2] = new float[3] { Mathf.Sin(beta), 0.0f, Mathf.Cos(beta) };

        Rz[0] = new float[3] { Mathf.Cos(alpha), Mathf.Sin(alpha), 0.0f };
        Rz[1] = new float[3] { -Mathf.Sin(alpha), Mathf.Cos(alpha), 0.0f };
        Rz[2] = new float[3] { 0.0f, 0.0f, 1.0f };

        MultiplyMatricesParallel(Rx, Ry, R1);
        MultiplyMatricesParallel(R1, Rz, R2);

        return R2;
    }

    static void MultiplyMatricesParallel(float[][] matA, float[][] matB, float[][] result)
    {
        // A basic matrix multiplication.
        // Parallelize the outer loop to partition the source array by rows.
        Parallel.For(0, matA.Length, i =>
        {
            for (int j = 0; j < matB[0].Length; j++)
            {
                float temp = 0;
                for (int k = 0; k < matA[0].Length; k++)
                {
                    temp += matA[i][k] * matB[k][j];
                }
                result[i][j] += temp;
            }
        }); // Parallel.For
    }

    public Vector3 getEarPos(string LR)
    {
        if (LR == "left")
            return D_l;
        else if (LR == "right")
            return D_r;
        else
            return Vector3.zero;
    }

    public float[] getSnowmanLeft()
    {
        float[] snowMan = new float[200];
        for (int i = 0; i < snowMan.Length; i++)
            snowMan[i] = (float)outL[i].Re;
        return snowMan;
    }

    public float[] getSnowmanRight()
    {
        float[] snowMan = new float[200];
        for (int i = 0; i < snowMan.Length; i++)
            snowMan[i] = (float)outR[i].Re;
        return snowMan;
    }

    public AForge.Math.Complex[] getSnowmanFFT_left()
    {
        //float[] azEl = this.gameObject.GetComponent<HRTFmanager>().getAzElInteraural(S);
        //Debug.Log("you  " + azEl[0] + "   " + azEl[1]);
        //AForge.Math.FourierTransform.FFT(outL, AForge.Math.FourierTransform.Direction.Forward);
        return outL;
    }

    public AForge.Math.Complex[] getSnowmanFFT_right()
    {
        //AForge.Math.FourierTransform.FFT(outR, AForge.Math.FourierTransform.Direction.Forward);
        return outR;
    }

    public AForge.Math.Complex[] J_getSnowmanFFT_left(int index)
    {
        //AForge.Math.FourierTransform.FFT(JoutL[index], AForge.Math.FourierTransform.Direction.Forward);
        return JoutL[index];
    }

    public AForge.Math.Complex[] J_getSnowmanFFT_right(int index)
    {
        //AForge.Math.FourierTransform.FFT(JoutR[index], AForge.Math.FourierTransform.Direction.Forward);
        return JoutR[index];
    }

    public float getItd_l()
    {
        return itd_l;
    }

    public float getItd_r()
    {
        return itd_r;
    }

}
