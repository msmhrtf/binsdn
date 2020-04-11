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

    private GameObject listener; // the drone is the sound source. Drag and drop in Unity GUI.
    private bool snowman = false;
    private Vector3 listenerPos;
    private List<Vector3> positionArray; // dynamic list of nodes position

    private Vector3 prevListenerPos;
    private Vector3 prevSourcePos;
    private Vector3 prevAngles;

    // HRTF pairs for direct sound and junctions. updating if position of source, listener and listener's head rotation is changing.
    private AForge.Math.Complex[][] hrtf_direct = new AForge.Math.Complex[2][];
    public List<AForge.Math.Complex[][]> hrtf_nodes = new List<AForge.Math.Complex[][]>(); // dynamic list of hrtf, following "network" in SDN script

    // Snowman parameters
    //private AForge.Math.Complex[][] hrtf_snowman_direct = new AForge.Math.Complex[2][];
    private int nfreq; // half spectrum size
    private float[] omega; // frequency fft points
    private float omega_low = 500; // snowman cutoff freq low
    private float omega_high = 3000; // snowman cutoff freq high
    private int idx_low, idx_high; // fft indexes for omega low and high
                                   
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
        fftLength = 2 * buffSize;

        listener = GameObject.Find("CenterEyeAnchor");

        prevListenerPos = new Vector3(listener.transform.position.x, listener.transform.position.y, listener.transform.position.y);
        prevSourcePos = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z);
        prevAngles = new Vector3(this.transform.eulerAngles.x, this.transform.eulerAngles.y, this.transform.eulerAngles.z);

        // hrtf pairs for a specific source location
        for (int i = 0; i < hrtf_direct.Length; i++)
        {
            hrtf_direct[i] = new AForge.Math.Complex[fftLength];
            // hrtf_snowman_direct[i] = new AForge.Math.Complex[fftLength];
        }

        // snowman param
        nfreq = Mathf.FloorToInt(fftLength / 2);
        omega = new float[nfreq + 1];
        float[] omega2_low = new float[nfreq + 1];
        float[] omega2_high = new float[nfreq + 1];
        for (int i = 0; i <= nfreq; i++)
        {
            omega[i] = i * sampleRate / (2 * nfreq);  // [0, ... , Nyquist = 22050]
            omega2_low[i] = Mathf.Abs(omega[i] - omega_low);
            omega2_high[i] = Mathf.Abs(omega[i] - omega_high);
        }
        idx_low = Enumerable.Range(0, omega2_low.Length).Aggregate((a, b) => (omega2_low[a] < omega2_low[b]) ? a : b); // find index
        idx_high = Enumerable.Range(0, omega2_high.Length).Aggregate((a, b) => (omega2_high[a] < omega2_high[b]) ? a : b);

        f_axis = new float[fftLength];
        for (int j = 0; j < fftLength; j++)
        {
            f_axis[j] = j * sampleRate / fftLength;
        }

        //a = new AForge.Math.Complex(2, 3);
        //b = new AForge.Math.Complex(7, 8);
        //Debug.Log(a.Re + " " + a.Im +  " " + a.Magnitude + " " + a.Phase);
        //a.Re = 4;
        //a.Im = 5;
        //Debug.Log(a.Re + " " + a.Im + " " + a.Magnitude + " " + a.Phase);
        //b.Re = 9;
        //b.Im = 10;
        //Debug.Log(b.Re + " " + b.Im + " " + b.Magnitude + " " + b.Phase);
        //b = a;
        //Debug.Log(b.Re + " " + b.Im + " " + b.Magnitude + " " + b.Phase);
        //a.Re = 1;
        //a.Im = 2;
        //Debug.Log(b.Re + " " + b.Im + " " + b.Magnitude + " " + b.Phase);
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
            hrtf_direct = listener.GetComponent<HRTFcontainer>().getInterpolated_HRTF(azEl_direct);


            //if ( limiter == true && (headRotation_y < limit || headRotation_y > 360-limit) )
            //    this.GetComponent<AudioSource>().mute = false;
            //else
            //    this.GetComponent<AudioSource>().mute = true;

            //hrtf_direct[0] = this.gameObject.GetComponent<Snowman>().getSnowmanFFT_left();
            //hrtf_direct[1] = this.gameObject.GetComponent<Snowman>().getSnowmanFFT_right();

            if (snowman) // Hybrid between snowman and measured hrtf
            {
                hybridSnowman(hrtf_direct, this.gameObject.GetComponent<Snowman>().getSnowmanFFT_left(), this.gameObject.GetComponent<Snowman>().getSnowmanFFT_right());
                Debug.Log("SNOWMAN !");
            }

            // display
            //Debug.Log("S pos: " + source.transform.position + " az: " + azEl_direct[0] + " el: " + azEl_direct[1] + " i_az: " + index_direct[0] + " i_el: " + index_direct[1] + " (" + getAzEl(source.transform.position)[0] + "; " + getAzEl(source.transform.position)[1] + "; " + getAzEl(source.transform.position)[2] + ")");
            //Debug.Log("az: " + getAzEl(this.gameObject.transform.position)[0] + " el: " + getAzEl(this.gameObject.transform.position)[1] + " azI: " + azEl_direct[0] + " elI: " + azEl_direct[1]); // + " el2:  " + getAzEl(this.gameObject.transform.position)[2] + " elI:  " + azEl_direct[2]);

            // JUNCTIONS azi/ele update
            positionArray = this.gameObject.GetComponent<SDN>().positionArray;
            if (positionArray.Count > 0)
            {
                // constantly go through the list and update azi and ele
                for (int i = 0; i < 2; i++) // Parallel.For(0, positionArray.Count, i => //
                {
                    azEl[i] = getAzElInteraural(positionArray[i]);

                    // get hrtfs from database
                    hrtf_nodes[i] = listener.GetComponent<HRTFcontainer>().getInterpolated_HRTF(azEl[i]);

                    if (snowman)
                        JhybridSnowman(hrtf_nodes[i], this.gameObject.GetComponent<Snowman>().J_getSnowmanFFT_left(i), this.gameObject.GetComponent<Snowman>().J_getSnowmanFFT_right(i), i);
                }

                //// display

                //for (int i = 0; i < positionArray.Count; i++)
                //{
                //    // Debug.Log("pos " + i + ": " + positionArray[i] + " az: " + azEl[i][0] + " el: " + azEl[i][1] + " i_az: " + indices[i][0] + " i_el: " + indices[i][1]);
                //    Debug.Log("pos " + i + ": " + positionArray[i] + " az: " + azEl[i][0] + " el: " + azEl[i][1] + " i_az: " + indices[i][0] + " i_el: " + indices[i][1] + " (" + getAzEl(positionArray[i])[0] + "; " + getAzEl(positionArray[i])[1] + "; " + getAzEl(positionArray[i])[2] + ")");
                //}

            }
        }
    }

    //public void hybridSnowman(AForge.Math.Complex[][] hrtf, AForge.Math.Complex[] snow_l, AForge.Math.Complex[] snow_r)
    //{

    //    int i, j;

    //    // construct new fft log10 magnitude
    //    double[] As_l = new double[nfreq + 1];
    //    double[] As_r = new double[nfreq + 1];
    //    j = 1;
    //    for (i = 0; i <= nfreq; i++)
    //    {
    //        if (i <= idx_low) // snowman
    //        {
    //            As_l[i] = snow_l[i].Magnitude;
    //            As_r[i] = snow_r[i].Magnitude;
    //        }
    //        else if (i >= idx_high) // measured
    //        {
    //            As_l[i] = hrtf[0][i].Magnitude;
    //            As_r[i] = hrtf[1][i].Magnitude;
    //        }
    //        else // cross-fade
    //        {
    //            As_l[i] = snow_l[i].Magnitude + (hrtf[0][i].Magnitude - snow_l[i].Magnitude) / (omega[idx_high] - omega[idx_low]);
    //            As_r[i] = snow_r[i].Magnitude + (hrtf[1][i].Magnitude - snow_r[i].Magnitude) / (omega[idx_high] - omega[idx_low]);
    //        }
    //        //else // mirror first half of spectrum to second half
    //        //{
    //        //    As_l[i] = As_l[i - 2 * j];
    //        //    As_r[i] = As_r[i - 2 * j];
    //        //    j++;
    //        //}
    //    }

    //    // reconstruct full symetric spectrum
    //    i = 0;
    //    j = 1;
    //    while (i < fftLength)
    //    {
    //        // first half of spectrum
    //        while (i <= nfreq)
    //        {
    //            //hrtf_snowman_direct[0][i].Re = hrtf[0][i].Re;
    //            //hrtf_snowman_direct[0][i].Im = hrtf[0][i].Magnitude * Mathf.Sin((float)snow_l[i].Phase);
    //            //hrtf_snowman_direct[1][i].Re = hrtf[1][i].Re;
    //            //hrtf_snowman_direct[1][i].Im = hrtf[1][i].Magnitude * Mathf.Sin((float)snow_r[i].Phase);

    //            // Re = magnitude * cos(phase)
    //            // Im = magnitude * sin(phase)
    //            hrtf_direct[0][i].Re = As_l[i] * Mathf.Cos((float)snow_l[i].Phase);
    //            hrtf_direct[0][i].Im = As_l[i] * Mathf.Sin((float)snow_l[i].Phase);
    //            hrtf_direct[1][i].Re = As_r[i] * Mathf.Cos((float)snow_r[i].Phase);
    //            hrtf_direct[1][i].Im = As_r[i] * Mathf.Sin((float)snow_r[i].Phase);
    //            i++;
    //        }
    //        //hrtf_snowman_direct[0][i].Re = hrtf[0][i].Re;
    //        //hrtf_snowman_direct[0][i].Im = hrtf[0][i].Magnitude * Mathf.Sin((float)snow_l[i].Phase);
    //        //hrtf_snowman_direct[1][i].Re = hrtf[1][i].Re;
    //        //hrtf_snowman_direct[1][i].Im = hrtf[1][i].Magnitude * Mathf.Sin((float)snow_r[i].Phase);

    //        // after Nyquist freq -> symetric of first half
    //        hrtf_direct[0][i].Re = As_l[As_l.Length - j] * Mathf.Cos((float)snow_l[i].Phase);
    //        hrtf_direct[0][i].Im = As_l[As_l.Length - j] * Mathf.Sin((float)snow_l[i].Phase);
    //        hrtf_direct[1][i].Re = As_r[As_l.Length - j] * Mathf.Cos((float)snow_r[i].Phase);
    //        hrtf_direct[1][i].Im = As_r[As_l.Length - j] * Mathf.Sin((float)snow_r[i].Phase);
    //        i++;
    //        j++;
    //    }

    //}

    //public void JhybridSnowman(AForge.Math.Complex[][] hrtf, AForge.Math.Complex[] snow_l, AForge.Math.Complex[] snow_r, int index) // sorry for duplicating code
    //{

    //    int i, j;

    //    // construct new fft log10 magnitude
    //    double[] As_l = new double[nfreq + 1];
    //    double[] As_r = new double[nfreq + 1];
    //    j = 1;
    //    for (i = 0; i <= nfreq; i++)
    //    {
    //        if (i <= idx_low) // snowman
    //        {
    //            As_l[i] = snow_l[i].Magnitude;
    //            As_r[i] = snow_r[i].Magnitude;
    //        }
    //        else if (i >= idx_high) // measured
    //        {
    //            As_l[i] = hrtf[0][i].Magnitude;
    //            As_r[i] = hrtf[1][i].Magnitude;
    //        }
    //        else // cross-fade
    //        {
    //            As_l[i] = snow_l[i].Magnitude + (hrtf[0][i].Magnitude - snow_l[i].Magnitude) / (omega[idx_high] - omega[idx_low]);
    //            As_r[i] = snow_r[i].Magnitude + (hrtf[1][i].Magnitude - snow_r[i].Magnitude) / (omega[idx_high] - omega[idx_low]);
    //        }
    //        //else // mirror first half of spectrum to second half
    //        //{
    //        //    As_l[i] = As_l[i - 2 * j];
    //        //    As_r[i] = As_r[i - 2 * j];
    //        //    j++;
    //        //}
    //    }

    //    // reconstruct full symetric spectrum
    //    i = 0;
    //    j = 1;
    //    while (i < fftLength)
    //    {
    //        // first half of spectrum
    //        while (i <= nfreq)
    //        {
    //            //hrtf_snowman_direct[0][i].Re = hrtf[0][i].Re;
    //            //hrtf_snowman_direct[0][i].Im = hrtf[0][i].Magnitude * Mathf.Sin((float)snow_l[i].Phase);
    //            //hrtf_snowman_direct[1][i].Re = hrtf[1][i].Re;
    //            //hrtf_snowman_direct[1][i].Im = hrtf[1][i].Magnitude * Mathf.Sin((float)snow_r[i].Phase);

    //            // Re = magnitude * cos(phase)
    //            // Im = magnitude * sin(phase)
    //            hrtf_nodes[index][0][i].Re = As_l[i] * Mathf.Cos((float)snow_l[i].Phase);
    //            hrtf_nodes[index][0][i].Im = As_l[i] * Mathf.Sin((float)snow_l[i].Phase);
    //            hrtf_nodes[index][1][i].Re = As_r[i] * Mathf.Cos((float)snow_r[i].Phase);
    //            hrtf_nodes[index][1][i].Im = As_r[i] * Mathf.Sin((float)snow_r[i].Phase);
    //            i++;
    //        }
    //        //hrtf_snowman_direct[0][i].Re = hrtf[0][i].Re;
    //        //hrtf_snowman_direct[0][i].Im = hrtf[0][i].Magnitude * Mathf.Sin((float)snow_l[i].Phase);
    //        //hrtf_snowman_direct[1][i].Re = hrtf[1][i].Re;
    //        //hrtf_snowman_direct[1][i].Im = hrtf[1][i].Magnitude * Mathf.Sin((float)snow_r[i].Phase);

    //        // after Nyquist freq -> symetric of first half
    //        hrtf_nodes[index][0][i].Re = As_l[As_l.Length - j] * Mathf.Cos((float)snow_l[i].Phase);
    //        hrtf_nodes[index][0][i].Im = As_l[As_l.Length - j] * Mathf.Sin((float)snow_l[i].Phase);
    //        hrtf_nodes[index][1][i].Re = As_r[As_l.Length - j] * Mathf.Cos((float)snow_r[i].Phase);
    //        hrtf_nodes[index][1][i].Im = As_r[As_l.Length - j] * Mathf.Sin((float)snow_r[i].Phase);
    //        i++;
    //        j++;
    //    }

    //}


    public void hybridSnowman(AForge.Math.Complex[][] hrtf, AForge.Math.Complex[] snow_l, AForge.Math.Complex[] snow_r)
    {

        int i, j;

        // construct new fft log10 magnitude
        AForge.Math.Complex[] As_l = new AForge.Math.Complex[fftLength];
        AForge.Math.Complex[] As_r = new AForge.Math.Complex[fftLength];
        //j = 1;
        //for (i = 0; i < fftLength; i++)
        //{
        //    if (i <= idx_low) // snowman
        //    {
        //        As_l[i].Re = (float)snow_l[i].Magnitude;
        //        As_r[i].Re = (float)snow_r[i].Magnitude;
        //    }
        //    else if (i > idx_low && i < idx_high) // cross-fade
        //    {
        //        As_l[i].Re = (float)snow_l[i].Magnitude + ((float)hrtf[0][i].Magnitude - (float)snow_l[i].Magnitude) / (omega[idx_high] - omega[idx_low]);
        //        As_r[i].Re = (float)snow_r[i].Magnitude + ((float)hrtf[1][i].Magnitude - (float)snow_r[i].Magnitude) / (omega[idx_high] - omega[idx_low]);
        //    }
        //    else if (i >= idx_high && i <= nfreq) // measured
        //    {
        //        As_l[i].Re = (float)hrtf[0][i].Magnitude;
        //        As_r[i].Re = (float)hrtf[1][i].Magnitude;
        //    }
        //    else // mirror first half of spectrum to second half
        //    {
        //        As_l[i].Re = As_l[i - 2 * j].Re;
        //        As_r[i].Re = As_r[i - 2 * j].Re;
        //        j++;
        //    }
        //}

        for (i = 0; i < fftLength; i++)
        {
            As_l[i] = hrtf[0][i];
            As_r[i] = hrtf[1][i];
        }


        // minimum phase reconstruction

        //float delay_t = this.gameObject.GetComponent<Snowman>().getItd_l() / sampleRate;
        ////delay_t = 0.002f;
        //float phase_delay;

        //AForge.Math.Complex[] temp = new AForge.Math.Complex[fftLength];
        mps(ref As_l); // convert to minimum phase
        mps(ref As_r);

        for (i = 0; i < fftLength; i++)
        {
            hrtf_direct[0][i] = As_l[i];
            hrtf_direct[1][i] = As_r[i];
        }
        //if (azEl_direct[0] < 0) // apply delay to right ear
        //{
        //    //AForge.Math.FourierTransform.FFT(As_r, AForge.Math.FourierTransform.Direction.Backward);  // make a frac delay function
        //    //for (i = (int)this.gameObject.GetComponent<Snowman>().getItd(); i < fftLength; i++)
        //    //{
        //    //    temp[i] = As_r[i - (int)this.gameObject.GetComponent<Snowman>().getItd()];
        //    //}
        //    //AForge.Math.FourierTransform.FFT(temp, AForge.Math.FourierTransform.Direction.Forward);

            //    //phase_delay = PhaseUnwrap(As_r);

            //    for (i = 0; i < fftLength; i++)
            //    {
            //        ////phase_delay[i] = (float)As_r[i].Phase + (-2) * Mathf.PI * delay_t * f_axis[i];
            //        phase_delay = (float)As_l[i].Phase + (-2) * Mathf.PI * delay_t * f_axis[i];
            //        hrtf_direct[0][i] = As_l[i];
            //        ////hrtf_direct[1][i] = temp[i];
            //        hrtf_direct[1][i].Re = As_r[i].Magnitude * Mathf.Cos(phase_delay);
            //        hrtf_direct[1][i].Im = As_r[i].Magnitude * Mathf.Sin(phase_delay);
            //        ////hrtf_direct[1][i] = As_r[i];

            //        //hrtf_direct[0][i] = hrtf[0][i];
            //        //hrtf_direct[1][i] = hrtf[1][i];

            //        //hrtf_direct[0][i] = snow_l[i];
            //        //hrtf_direct[1][i] = snow_r[i];

            //    }
            //}
            //else // apply delay to left ear
            //{
            //    //AForge.Math.FourierTransform.FFT(As_l, AForge.Math.FourierTransform.Direction.Backward);
            //    //for (i = (int)this.gameObject.GetComponent<Snowman>().getItd(); i < fftLength; i++)
            //    //{
            //    //    temp[i] = As_l[i - (int)this.gameObject.GetComponent<Snowman>().getItd()];
            //    //}
            //    //AForge.Math.FourierTransform.FFT(temp, AForge.Math.FourierTransform.Direction.Forward);

            //    //phase_delay = PhaseUnwrap(As_l);

            //    for (i = 0; i < fftLength; i++)
            //    {

            //        ////Debug.Log(As_l[i].Phase);
            //        phase_delay = (float)As_l[i].Phase + (-2) * Mathf.PI * delay_t * f_axis[i];
            //        hrtf_direct[0][i].Re = As_l[i].Magnitude * Mathf.Cos(phase_delay);
            //        hrtf_direct[0][i].Im = As_l[i].Magnitude * Mathf.Sin(phase_delay);
            //        ////hrtf_direct[0][i] = temp[i];

            //        hrtf_direct[1][i] = As_r[i];
            //        ////hrtf_direct[0][i] = As_l[i];

            //        //hrtf_direct[0][i] = hrtf[0][i];
            //        //hrtf_direct[1][i] = hrtf[1][i];

            //        //hrtf_direct[0][i] = snow_l[i];
            //        //hrtf_direct[1][i] = snow_r[i];
            //    }
            //}

            //for (i = 0; i < fftLength; i++)
            //{
            //    hrtf_direct[0][i].Re = As_l[i].Re * Mathf.Cos((float)hrtf[0][i].Phase);
            //    hrtf_direct[0][i].Im = As_l[i].Re * Mathf.Sin((float)hrtf[0][i].Phase);
            //    hrtf_direct[1][i].Re = As_r[i].Re * Mathf.Cos((float)hrtf[1][i].Phase);
            //    hrtf_direct[1][i].Im = As_r[i].Re * Mathf.Sin((float)hrtf[1][i].Phase);
            //}

            //for (i = 0; i < fftLength; i++)         // snowman pure
            //{
            //    hrtf_direct[0][i] = snow_l[i];
            //    hrtf_direct[1][i] = snow_r[i];
            //}

            //// reconstruct full symetric spectrum
            //i = 0;
            //j = 1;
            //while (i < fftLength)
            //{
            //    // first half of spectrum
            //    while (i <= nfreq)
            //    {
            //        //hrtf_snowman_direct[0][i].Re = hrtf[0][i].Re;
            //        //hrtf_snowman_direct[0][i].Im = hrtf[0][i].Magnitude * Mathf.Sin((float)snow_l[i].Phase);
            //        //hrtf_snowman_direct[1][i].Re = hrtf[1][i].Re;
            //        //hrtf_snowman_direct[1][i].Im = hrtf[1][i].Magnitude * Mathf.Sin((float)snow_r[i].Phase);

            //        // Re = magnitude * cos(phase)
            //        // Im = magnitude * sin(phase)
            //        hrtf_direct[0][i].Re = As_l[i].Re * Mathf.Cos((float)snow_l[i].Phase);
            //        hrtf_direct[0][i].Im = As_l[i].Re * Mathf.Sin((float)snow_l[i].Phase);
            //        hrtf_direct[1][i].Re = As_r[i].Re * Mathf.Cos((float)snow_r[i].Phase);
            //        hrtf_direct[1][i].Im = As_r[i].Re * Mathf.Sin((float)snow_r[i].Phase);
            //        i++;
            //    }
            //    //hrtf_snowman_direct[0][i].Re = hrtf[0][i].Re;
            //    //hrtf_snowman_direct[0][i].Im = hrtf[0][i].Magnitude * Mathf.Sin((float)snow_l[i].Phase);
            //    //hrtf_snowman_direct[1][i].Re = hrtf[1][i].Re;
            //    //hrtf_snowman_direct[1][i].Im = hrtf[1][i].Magnitude * Mathf.Sin((float)snow_r[i].Phase);

            //    // after Nyquist freq -> symetric of first half
            //    hrtf_direct[0][i].Re = As_l[As_l.Length - j].Re * Mathf.Cos((float)snow_l[i].Phase);
            //    hrtf_direct[0][i].Im = As_l[As_l.Length - j].Re * Mathf.Sin((float)snow_l[i].Phase);
            //    hrtf_direct[1][i].Re = As_r[As_l.Length - j].Re * Mathf.Cos((float)snow_r[i].Phase);
            //    hrtf_direct[1][i].Im = As_r[As_l.Length - j].Re * Mathf.Sin((float)snow_r[i].Phase);
            //    i++;
            //    j++;
            //}

    }

    public void JhybridSnowman(AForge.Math.Complex[][] hrtf, AForge.Math.Complex[] snow_l, AForge.Math.Complex[] snow_r, int index) // sorry for duplicating code
    {

        int i, j;

        // construct new fft log10 magnitude
        AForge.Math.Complex[] As_l = new AForge.Math.Complex[fftLength];
        AForge.Math.Complex[] As_r = new AForge.Math.Complex[fftLength];
        j = 1;
        for (i = 0; i < fftLength; i++)
        {
            if (i <= idx_low) // snowman
            {
                As_l[i].Re = (float)snow_l[i].Magnitude;
                As_r[i].Re = (float)snow_r[i].Magnitude;
            }
            else if (i > idx_low && i < idx_high) // cross-fade
            {
                As_l[i].Re = (float)snow_l[i].Magnitude + ((float)hrtf[0][i].Magnitude - (float)snow_l[i].Magnitude) / (omega[idx_high] - omega[idx_low]);
                As_r[i].Re = (float)snow_r[i].Magnitude + ((float)hrtf[1][i].Magnitude - (float)snow_r[i].Magnitude) / (omega[idx_high] - omega[idx_low]);
            }
            else if (i >= idx_high && i <= nfreq) // measured
            {
                As_l[i].Re = (float)hrtf[0][i].Magnitude;
                As_r[i].Re = (float)hrtf[1][i].Magnitude;
            }
            else // mirror first half of spectrum to second half
            {
                As_l[i].Re = As_l[i - 2 * j].Re;
                As_r[i].Re = As_r[i - 2 * j].Re;
                j++;
            }
        }

        // minimum phase reconstruction

        float delay_t = this.gameObject.GetComponent<Snowman>().Jitd_l[index] / sampleRate;
        float[] phase_delay = new float[fftLength];
        //AForge.Math.Complex[] temp = new AForge.Math.Complex[fftLength];
        mps(ref As_l); // convert to minimum phase
        mps(ref As_r);
        if (azEl[index][0] < 0) // apply delay to right ear
        {
            //AForge.Math.FourierTransform.FFT(As_r, AForge.Math.FourierTransform.Direction.Backward);  // make a frac delay function
            //for (i = (int)this.gameObject.GetComponent<Snowman>().Jitd[index]; i < fftLength; i++)
            //{
            //    temp[i] = As_r[i - (int)this.gameObject.GetComponent<Snowman>().Jitd[index]];
            //}
            //AForge.Math.FourierTransform.FFT(temp, AForge.Math.FourierTransform.Direction.Forward);
            for (i = 0; i < fftLength; i++)
            {
                phase_delay[i] = (float)As_r[i].Phase + (-2) * Mathf.PI * delay_t * f_axis[i];
                //hrtf_nodes[index][0][i] = As_l[i];
                ////hrtf_nodes[index][1][i] = temp[i];
                ////hrtf_nodes[index][1][i].Re = As_r[i].Magnitude * Mathf.Cos(phase_delay[i]);
                ////hrtf_nodes[index][1][i].Im = As_r[i].Magnitude * Mathf.Sin(phase_delay[i]);
                ////hrtf_nodes[index][1][i] = As_r[i];

                hrtf_nodes[index][0][i] = hrtf[0][i];
                hrtf_nodes[index][1][i] = hrtf[1][i];

                //hrtf_nodes[index][0][i] = snow_l[i];
                //hrtf_nodes[index][1][i] = snow_r[i];
            }

        }
        else // apply delay to left ear
        {
            //AForge.Math.FourierTransform.FFT(As_l, AForge.Math.FourierTransform.Direction.Backward);
            //for (i = (int)this.gameObject.GetComponent<Snowman>().Jitd[index]; i < fftLength; i++)
            //{
            //    temp[i] = As_l[i - (int)this.gameObject.GetComponent<Snowman>().Jitd[index]];
            //}
            //AForge.Math.FourierTransform.FFT(temp, AForge.Math.FourierTransform.Direction.Forward);
            for (i = 0; i < fftLength; i++)
            {
                phase_delay[i] = (float)As_l[i].Phase + (-2) * Mathf.PI * delay_t * f_axis[i];
                ////hrtf_nodes[index][0][i].Re = As_l[i].Magnitude * Mathf.Cos(phase_delay[i]);
                ////hrtf_nodes[index][0][i].Im = As_l[i].Magnitude * Mathf.Sin(phase_delay[i]);
                ////hrtf_nodes[index][0][i] = As_l[i];
                ////hrtf_nodes[index][0][i] = temp[i];
                //hrtf_nodes[index][1][i] = As_r[i];

                hrtf_nodes[index][0][i] = hrtf[0][i];
                hrtf_nodes[index][1][i] = hrtf[1][i];

                //hrtf_nodes[index][0][i] = snow_l[i];
                //hrtf_nodes[index][1][i] = snow_r[i];
            }
        }

        //// reconstruct full symetric spectrum
        //i = 0;
        //j = 1;
        //while (i < fftLength)
        //{
        //    // first half of spectrum
        //    while (i <= nfreq)
        //    {
        //        //hrtf_snowman_direct[0][i].Re = hrtf[0][i].Re;
        //        //hrtf_snowman_direct[0][i].Im = hrtf[0][i].Magnitude * Mathf.Sin((float)snow_l[i].Phase);
        //        //hrtf_snowman_direct[1][i].Re = hrtf[1][i].Re;
        //        //hrtf_snowman_direct[1][i].Im = hrtf[1][i].Magnitude * Mathf.Sin((float)snow_r[i].Phase);

        //        // Re = magnitude * cos(phase)
        //        // Im = magnitude * sin(phase)
        //        hrtf_nodes[index][0][i].Re = As_l[i] * Mathf.Cos((float)snow_l[i].Phase);
        //        hrtf_nodes[index][0][i].Im = As_l[i] * Mathf.Sin((float)snow_l[i].Phase);
        //        hrtf_nodes[index][1][i].Re = As_r[i] * Mathf.Cos((float)snow_r[i].Phase);
        //        hrtf_nodes[index][1][i].Im = As_r[i] * Mathf.Sin((float)snow_r[i].Phase);
        //        i++;
        //    }
        //    //hrtf_snowman_direct[0][i].Re = hrtf[0][i].Re;
        //    //hrtf_snowman_direct[0][i].Im = hrtf[0][i].Magnitude * Mathf.Sin((float)snow_l[i].Phase);
        //    //hrtf_snowman_direct[1][i].Re = hrtf[1][i].Re;
        //    //hrtf_snowman_direct[1][i].Im = hrtf[1][i].Magnitude * Mathf.Sin((float)snow_r[i].Phase);

        //    // after Nyquist freq -> symetric of first half
        //    hrtf_nodes[index][0][i].Re = As_l[As_l.Length - j] * Mathf.Cos((float)snow_l[i].Phase);
        //    hrtf_nodes[index][0][i].Im = As_l[As_l.Length - j] * Mathf.Sin((float)snow_l[i].Phase);
        //    hrtf_nodes[index][1][i].Re = As_r[As_l.Length - j] * Mathf.Cos((float)snow_r[i].Phase);
        //    hrtf_nodes[index][1][i].Im = As_r[As_l.Length - j] * Mathf.Sin((float)snow_r[i].Phase);
        //    i++;
        //    j++;
        //}

    }

    public void mps(ref AForge.Math.Complex[] s)
    {
        int i;
        //clipdb(ref s, -100);
        for (i = 0; i < s.Length; i++)
        {
            s[i] = AForge.Math.Complex.Log(s[i]);
            //s[i].Re = Mathf.Log((float)s[i].Magnitude);
            //s[i].Im = s[i].Phase;
        }

        AForge.Math.FourierTransform.FFT(s, AForge.Math.FourierTransform.Direction.Backward);
        AForge.Math.Complex[] folded = fold(s);
        AForge.Math.FourierTransform.FFT( folded, AForge.Math.FourierTransform.Direction.Forward);

        for (i = 0; i < s.Length; i++)
            s[i] = AForge.Math.Complex.Exp(folded[i]);
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

        // [el_err, el_idx] = min(abs(elevations-rad2deg(elevation))); --- Matlab
        // [az_err, az_idx] = min(abs(azimuths-rad2deg(azimuth)));
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
        //Debug.Log(headRotation_y);
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

    public bool getBoolSnowman()
    {
        return snowman;
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
