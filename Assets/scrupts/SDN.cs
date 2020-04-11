using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using UnityEngine;
//using B83.MathHelpers;
using System;

public class SDN : MonoBehaviour
{
    public GameObject listener;
    public boundary boundary;
    public GameObject geoForNetwork;
    private GameObject FPcam;
    private GameObject rooms;

    public static int targetNumReflections = 35;
    public bool update = true;

    public bool enableListen = true;
    public bool doLateReflections = true;
    public bool doHrtfReflections = true;

    private reflectionFinder RF;

    private reflectionPath directSound;
    private delayLine directDelay;

    private const float airSpeed = 343.0f;

    private float directAtt = 0.0f;
    private float networkInScale = 0.0f;

    private List<reflectionPath> reflections;
    private List<SDNnode> network;

    private Queue<float> inSamples;
    private Queue<float> outSamples;

    private float inVal = 0.0f;
    private float outVal = 0.0f;
    private float directVal = 0.0f;
    private float AFin = 0.0f;
    private AForge.Math.Complex[] AFout;

    private float chanScale = 0.0f;
    private int numSamps;

    private int buffSize;
    private int sampleRate;

    private List<reflectionPath> waitingReflections;
    private reflectionPath waitingDirect;

    private bool haveNewReflections = false;
    private bool doClear = false;

    private Mutex sampleMX;
    private Mutex networkMX;
    private Thread audioProcessThread;
    private bool audioProcessKeepAlive = true;
    private bool scriptInit = false;

    public List<Vector3> positionArray; // dynamic list of nodes position, used for azi and ele, shared to HRTFmanager

    // direct path HRTF convolution variables 
    AForge.Math.Complex[][] Jffts = new AForge.Math.Complex[3][];
    // OLA samples to add to next buffer
    private float[][] sampOLA = new float[2][];
    // zero padded hrtfs
    private AForge.Math.Complex[][] hrtf = new AForge.Math.Complex[2][];
    // hrtf from HRTFmanager
    private AForge.Math.Complex[][] hrtf_C = new AForge.Math.Complex[2][];
    // convolution output buffers
    private float[][] result = new float[2][];

    // junctions HRTF convolution variables
    private List<AForge.Math.Complex[]> junctionsSamps;
    private List<float[][]> JsampOLA;
    private List<AForge.Math.Complex[][]> Jhrtf_C;
    private List<AForge.Math.Complex[][]> Jhrtf;
    //private List<float[][]> Jbig_result;
    private List<float[][]> Jresult;

    private int idx = 0; // index for storing junction samples
    private int fftLength;

    AudioSource _source;

    private bool loaded = false; // trick to let know that the HRTFs are loaded from the text file.
    private bool snowman = false;

    // ITD fractional delay lines
    //private FDelay left_channel;
    //private FDelay right_channel;
    //private List<FDelay> Jleft_channel;
    //private List<FDelay> Jright_channel;

    void Start()
    {
        //listener = GameObject.Find("RigidBodyFPSController");
        //boundary = GameObject.Find("Boundary").GetComponent<boundary>();
        //geoForNetwork = GameObject.Find("room");
        //rooms = GameObject.Find("Rooms");

        //geoForNetwork = GameObject.Find("room");

        //if (rooms.GetComponent<RoomManager>().room_num == 1)
        //    geoForNetwork = GameObject.Find("room");
        //else if (rooms.GetComponent<RoomManager>().room_num == 2)
        //    geoForNetwork = GameObject.Find("room2");
        //else
        //    print("PROBLEM WITH ROOM MANAGER !");

        //left_channel = new FDelay(50, 30);
        //right_channel = new FDelay(50, 30);

        _source = this.gameObject.GetComponent<AudioSource>();

        AudioConfiguration AC = AudioSettings.GetConfiguration();
        sampleRate = AC.sampleRate;
        buffSize = AC.dspBufferSize;

        inSamples = new Queue<float>();
        outSamples = new Queue<float>();

        delayLine.sampleRate = sampleRate;

        int initalDirectDelay = delayLine.distanceToDelayTime(Vector3.Distance(gameObject.transform.position, listener.transform.position));
        directDelay = new delayLine(1.0f, initalDirectDelay);

        network = new List<SDNnode>();
        RF = gameObject.AddComponent<reflectionFinder>();
        RF.setListener(listener);

        geoForNetwork = GameObject.Find("Rooms").GetComponent<RoomManager>().getActiveRoom();
        boundary.setBoundaryBounds(GetMaxBounds(geoForNetwork));
        RF.setboundary(boundary);

        RF.setNumInitalDirections(targetNumReflections);
        RF.doUpdate = update;
        RF.onNewReflections += handleNewReflections;

        SDNDraw draw = GetComponent<SDNDraw>();
        if (draw != null)
        {
            RF.onNewReflections += draw.updateVisualNetwork;
        }
        sampleMX = new Mutex();
        networkMX = new Mutex();
        audioProcessThread = new Thread(audioProcess);
        audioProcessThread.Start();
        scriptInit = true;

        FPcam = GameObject.Find("CenterEyeAnchor");

        fftLength = 2 * buffSize;

        // convolution results in M+N-1 samples (M = buffSize, N = HRTFsize)
        // so need a power of 2 which can contain conv result => 2*buffSize (= 2048)
        AFout = new AForge.Math.Complex[fftLength]; // buffSize + FPcam.GetComponent<HRTFmanager>().getHrtfLength() - 1

        for (int i = 0; i < sampOLA.Length; i++)
        {
            sampOLA[i] = new float[buffSize];
            hrtf[i] = new AForge.Math.Complex[200];
            result[i] = new float[buffSize];
            hrtf_C[i] = new AForge.Math.Complex[fftLength];
        }

        junctionsSamps = new List<AForge.Math.Complex[]>();
        JsampOLA = new List<float[][]>();
        Jresult = new List<float[][]>();
        Jhrtf_C = new List<AForge.Math.Complex[][]>();
        Jhrtf = new List<AForge.Math.Complex[][]>();

        // junctions ffts
        for (int i = 0; i < Jffts.Length; i++)
        {
            Jffts[i] = new AForge.Math.Complex[fftLength];
        }
    }


    void Update()
    {
        //if (rooms.GetComponent<RoomManager>().getFlag())
        //{
        //    if (rooms.GetComponent<RoomManager>().room_num == 1)
        //    {
        //        geoForNetwork = GameObject.Find("room");
        //    }
        //    else if (rooms.GetComponent<RoomManager>().room_num == 2)
        //    {
        //        geoForNetwork = GameObject.Find("room2");
        //    }
        //    else
        //        print("PROBLEM WITH ROOM MANAGER !");

        //    // update boundary
        //    boundary.setBoundaryBounds(GetMaxBounds(geoForNetwork));
        //    RF.setboundary(boundary);
        //    rooms.GetComponent<RoomManager>().setFlag(false);
        //}

        RF.doUpdate = update;

        if (update && haveNewReflections)
        {
            networkMX.WaitOne();
            processReflections();
            directDelay.setDelay(delayLine.distanceToDelayTime(directSound.totalDistance()));
            directAtt = 1.0f / (Vector3.Distance(listener.transform.position, gameObject.transform.position) + 1.0f);
            networkInScale = 1.0f / network.Count;
            haveNewReflections = false;
            networkMX.ReleaseMutex();
        }
        checkDelayClear();

        // get HRTFs from HRTFmanager
        hrtf = this.gameObject.GetComponent<HRTFmanager>().getHrftDirect();   // TO DO: update only if move
        Jhrtf = this.gameObject.GetComponent<HRTFmanager>().hrtf_nodes;

        loaded = GameObject.Find("CenterEyeAnchor").GetComponent<HRTFcontainer>().getLoaded();
        //snowman = this.gameObject.GetComponent<HRTFmanager>().getBoolSnowman();

        // update delay line
        //left_channel.setDelay(this.gameObject.GetComponent<Snowman>().getItd_l());
        //right_channel.setDelay(this.gameObject.GetComponent<Snowman>().getItd_r());
        //Debug.Log("l " + this.gameObject.GetComponent<Snowman>().getItd_l());
        //Debug.Log("r " + this.gameObject.GetComponent<Snowman>().getItd_r());

    }

    void OnApplicationQuit()
    {
        audioProcessKeepAlive = false;
    }

    public void audioProcess()
    {

        while (audioProcessKeepAlive)
        {
            propagateNetwork();
        }

    }

    public void propagateNetwork()
    {

        sampleMX.WaitOne();
        networkMX.WaitOne();
        int numSampsToConsume = inSamples.Count;
        // horrible hack to solve latency @ startup - the queue quickly fills up from
        // the audio thread before the main thread can catch up. I am a bad person for doing this.
        if (inSamples.Count > 10000)
        {
            inSamples.Clear();
            sampleMX.ReleaseMutex();
            return;
        }

        int i, j;

        for (i = 0; i < numSampsToConsume; i++)
        {
            outVal = 0.0f;
            inVal = inSamples.Dequeue() * networkInScale;

            // direct path processing
            directDelay.write(inVal);
            directVal = directDelay.read();
            directVal *= directAtt;
            outSamples.Enqueue(directVal);

            // junctions processing
            for (j = 0; j < network.Count; j++)
            {
                junctionsSamps[j][idx] = new AForge.Math.Complex(network[j].getOutgoing(), 0);
                // outVal += network[j].getOutgoing();
                network[j].inputIncoming(inVal);
                network[j].doScattering(doLateReflections);
            }
            idx++;

            // outVal += directVal;
            // outSamples.Enqueue(outVal*10);

        }
        sampleMX.ReleaseMutex();
        networkMX.ReleaseMutex();
    }

    public void doScatteringForNodeRange(int min, int max)
    {
        for (int i = min; i < max; i++)
        {
            network[i].doScattering(doLateReflections);
        }
    }

    public void checkDelayClear()
    {

        if (network != null && doClear)
        {
            directDelay.clear();
            for (int i = 0; i < network.Count; i++)
            {
                SDNnode n = network[i];
                List<SDNConnection> connections = n.getConnections();
                n.clearDelay();
                for (int j = 0; j < connections.Count; j++)
                {
                    connections[i].clearDelay();
                }
            }
            doClear = false;
        }

    }

    public Bounds GetMaxBounds(GameObject g)
    {
        Bounds b = new Bounds(g.transform.position, Vector3.zero);
        foreach (MeshRenderer r in g.GetComponentsInChildren<Renderer>())
        {
            b.Encapsulate(r.bounds);
        }
        return b;
    }

    public void setBoundary(GameObject room)
        // Function to set new room boundaries. 
        // See RoomManager script.
    {
        geoForNetwork = room;

        //doHrtfReflections = false;
        //doLateReflections = false;

        //network = new List<SDNnode>();

        // update boundary
        boundary.setBoundaryBounds(GetMaxBounds(geoForNetwork));
        RF.setboundary(boundary);

        //doHrtfReflections = true;
        //doLateReflections = true;

       

    }

    public void updateWallMaterialFilter()
        // Function to update wall absortption filters and wall absorbption coefficients according to wall properties set in the Unity user GUI for each wall of the room.
        // See RoomManager and SDNNode.
    {
        //clearNetwork();

        for (int i = 0; i < network.Count; i++)
        {
            network[i].updateWallFilt();
        }
    }

    //public void clearNetwork()
    //{
    //    doHrtfReflections = false;
    //    doLateReflections = false;

    //    network = new List<SDNnode>();
    //}

    //public void onNetwork()
    //{

    //    doHrtfReflections = true;
    //    doLateReflections = true;
    //}

    public void setHaveNewRef()
    {
        haveNewReflections = true;
        //Debug.Log(haveNewReflections);
    }

    public void clearAllDelays()
    {
        doClear = true;
    }

    public void setNodeLoss(float loss)
    {
        SDNnode.setLoss(loss);
    }

    public void setNodeSpecularity(float spec)
    {
        SDNnode.setSpecularity(spec);
    }

    public void setNodeWallAbs(float abs)
    {
        SDNnode.setWallAbs(abs);
    }

    public void enableAll()
    {
        enableListen = true;
        doLateReflections = true;
    }

    public void enableER()
    {
        enableListen = true;
        doLateReflections = false;
    }

    public void disableAll()
    {
        enableListen = false;
        doLateReflections = false;
    }

    public List<SDNnode> getNetwork()
    {
        return network;
    }

    public List<Vector3> getPositionArray()
    {
        return positionArray;
    }

    public void setNumRef(int numRef)
    {
        targetNumReflections = numRef;
        RF.setNumInitalDirections(numRef);
    }

    public void setUpdateNetwork(bool val)
    {
        update = val;
        RF.doUpdate = val;
    }

    public AForge.Math.Complex[][] CopyArrayLinq(AForge.Math.Complex[][] source) // jagged array copy
    {
        return source.Select(s => s.ToArray()).ToArray();
    }

    public static T[][] deinterleaveData<T>(T[] data, int num_ch)
    {
        T[][] deinterleaved = new T[num_ch][];
        int channel_length = data.Length / num_ch;
        for (int ch = 0; ch < num_ch; ++ch)
        {
            deinterleaved[ch] = new T[channel_length];
            for (int i = 0; i < channel_length; ++i)
                deinterleaved[ch][i] = data[i * num_ch];
        }

        return deinterleaved;
    }

    public static void interleaveData(float[][] data_in, List<float[][]> Jdata_in, float[] data_out, int num_ch)   // <T>(T[][] data_in, List<T[][]> Jdata_in, T[] data_out, int num_ch)
    {
        // int num_ch = data_in.Length;
        for (int i = 0; i < data_in[0].Length; ++i)
        {
            for (int ch = 0; ch < num_ch; ++ch)
            {
                data_out[i * num_ch + ch] = data_in[ch][i];
                for (int j = 0; j < Jdata_in.Count; ++j)
                    data_out[i * num_ch + ch] += Jdata_in[j][ch][i] * 5;
            }
        }
    }

    public static void interleaveDataNoReflection(float[][] data_in, List<AForge.Math.Complex[]> Jdata_in, float[] data_out, int num_ch)   // <T>(T[][] data_in, List<T[][]> Jdata_in, T[] data_out, int num_ch)
    {
        // int num_ch = data_in.Length;
        for (int i = 0; i < data_in[0].Length; ++i)
        {
            for (int ch = 0; ch < num_ch; ++ch)
            {
                data_out[i * num_ch + ch] = data_in[ch][i];
                for (int j = 0; j < Jdata_in.Count; ++j)
                    data_out[i * num_ch + ch] += (float)Jdata_in[j][i].Re * 5;
            }
        }
    }

    public static void interleaveSimple(float[][] data_in, float[] data_out, int num_ch)  
    {
        for (int i = 0; i < data_in[0].Length; ++i)
        {
            for (int ch = 0; ch < num_ch; ++ch)
            {
                data_out[i * num_ch + ch] = data_in[ch][i] * 5;
            }
        }
    }

    void OnAudioFilterRead(float[] data, int channels) // audio processing by buffer
    {
        numSamps = data.Length / channels;
        chanScale = 1.0f / channels;
        int i, c;

        if (scriptInit)
        {

            sampleMX.WaitOne();

            for (i = 0; i < numSamps; i++)
            {
                AFin = 0.0f;
                for (c = 0; c < channels; c++)
                {
                    AFin += data[(i * channels) + c];
                }
                inSamples.Enqueue(AFin * chanScale);
            }

            if (!(outSamples.Count < numSamps)) // sound output when buffer is full
            {
                if (!enableListen)
                {
                    for (i = 0; i < numSamps; i++)
                    {
                        // AFout = outSamples.Dequeue ();
                        for (c = 0; c < channels; c++)
                        {
                            data[(i * channels) + c] *= directAtt;
                        }
                    }
                }
                else
                {
                    idx = 0; // put buffer index to zero
                    for (i = 0; i < numSamps; i++)
                    {
                        Jffts[0][i] = new AForge.Math.Complex(outSamples.Dequeue(), 0);
                    }

                    if (loaded)
                        convHRTF(doHrtfReflections);

                    //float[][] test = new float[2][];
                    //for (i = 0; i < test.Length; i++)
                    //    test[i] = new float[numSamps];

                    if (doHrtfReflections)
                    {
                        interleaveData(result, Jresult, data, channels);
                    }
                    else
                    {
                        //if (doLateReflections)
                           //interleaveDataNoReflection(result, junctionsSamps, data, channels);
                        //else
                           interleaveSimple(result, data, channels);
                    }
                        

                    //for (i = 0; i < numSamps; i++)
                    //{
                    //    for (c = 0; c < channels; c++)
                    //    {
                    //        //data[(i * channels) + c] = (float)junctionsSamps[0][i].Re;
                    //        data[(i * channels) + c] = ((c + 1) % 2) * (result[0][i]) + (c % 2) * (result[1][i]);
                    //    }
                    //}
                }
            }
            else
            {
                for (i = 0; i < numSamps; i++)
                {
                    for (c = 0; c < channels; c++)
                    {
                        data[(i * channels) + c] = 0.0f;
                    }
                }
            }
            sampleMX.ReleaseMutex();

        }
        else
        {
            for (i = 0; i < numSamps; i++)
            {
                for (c = 0; c < channels; c++)
                {
                    data[(i * channels) + c] = 0.0f;
                }
            }
        }
    }

    private void convHRTF(bool doIt)
    {

        hrtf_C = CopyArrayLinq(hrtf);       // TO DO: copy only if moving
        for (int j = 0; j < junctionsSamps.Count; j++)
        {
            Jhrtf_C[j] = CopyArrayLinq(Jhrtf[j]);
        }

        // direct fft of buffer 2048
        AForge.Math.FourierTransform.FFT(Jffts[0], AForge.Math.FourierTransform.Direction.Forward);

        // fft of hrtfs
        //AForge.Math.FourierTransform.FFT(Jffts[1], AForge.Math.FourierTransform.Direction.Forward);
        //AForge.Math.FourierTransform.FFT(Jffts[2], AForge.Math.FourierTransform.Direction.Forward);

        //if (snowman)
        //{
        //    AForge.Math.Complex[][] h_lr = new AForge.Math.Complex[2][];                    // TEST with hrtf of 150 samples. Cutting the artfifacts of reconstruction
        //    for (int j = 0; j < h_lr.Length; j++)
        //    {
        //        h_lr[j] = new AForge.Math.Complex[fftLength];
        //    }
        //    AForge.Math.FourierTransform.FFT(hrtf_C[0], AForge.Math.FourierTransform.Direction.Backward);
        //    AForge.Math.FourierTransform.FFT(hrtf_C[1], AForge.Math.FourierTransform.Direction.Backward);
        //    for (int j = 0; j < 150; j++)
        //    {
        //        h_lr[0][j] = hrtf_C[0][j];
        //        h_lr[1][j] = hrtf_C[1][j];
        //    }
        //    AForge.Math.FourierTransform.FFT(h_lr[0], AForge.Math.FourierTransform.Direction.Forward);
        //    AForge.Math.FourierTransform.FFT(h_lr[1], AForge.Math.FourierTransform.Direction.Forward);

        //    // direct freq domain convolution 2048
        //    for (int i = 0; i < fftLength; i++)
        //    {
        //        Jffts[1][i] = Jffts[0][i] * h_lr[0][i] * fftLength;
        //        Jffts[2][i] = Jffts[0][i] * h_lr[1][i] * fftLength;
        //    }
        //}
        //else
        //{
            for (int i = 0; i < fftLength; i++)
            {
                Jffts[1][i] = Jffts[0][i] * hrtf_C[0][i] * fftLength;
                Jffts[2][i] = Jffts[0][i] * hrtf_C[1][i] * fftLength;
            }
        //}

        // inverse fft 2048
        AForge.Math.FourierTransform.FFT(Jffts[1], AForge.Math.FourierTransform.Direction.Backward);
        AForge.Math.FourierTransform.FFT(Jffts[2], AForge.Math.FourierTransform.Direction.Backward);

        // seperate and store results 1024 and OLA samples 1024
        int k = 0;
        while (k < fftLength)
        {
            while (k < buffSize)
            {
                //result[0][k] = left_channel.process((float)Jffts[1][k].Re + sampOLA[0][k]);
                //result[1][k] = right_channel.process((float)Jffts[2][k].Re + sampOLA[1][k]);

                result[0][k] = ((float)Jffts[1][k].Re + sampOLA[0][k]);
                result[1][k] = ((float)Jffts[2][k].Re + sampOLA[1][k]);
                k++;
            }
            sampOLA[0][k - buffSize] = (float)Jffts[1][k].Re;
            sampOLA[1][k - buffSize] = (float)Jffts[2][k].Re;
            k++;
        }

        if (doIt)
        {
            // same thing for junctions
            for (int i = 0; i < junctionsSamps.Count; i++)
            {

                // reinitialize for next junction
                for (int j = 0; j < Jffts.Length; j++)
                {
                    Jffts[j] = new AForge.Math.Complex[fftLength];
                }

                // copy
                Array.Copy(junctionsSamps[i], Jffts[0], buffSize);

                // fft
                AForge.Math.FourierTransform.FFT(Jffts[0], AForge.Math.FourierTransform.Direction.Forward);
                //AForge.Math.FourierTransform.FFT(Jffts[1], AForge.Math.FourierTransform.Direction.Forward);
                //AForge.Math.FourierTransform.FFT(Jffts[2], AForge.Math.FourierTransform.Direction.Forward);

                //if (snowman)
                //{
                //    // TEST with hrtf of 150 samples. Cutting the artfifacts of reconstruction
                //    AForge.Math.Complex[][] h_lr = new AForge.Math.Complex[2][];
                //    for (int j = 0; j < h_lr.Length; j++)
                //    {
                //        h_lr[j] = new AForge.Math.Complex[fftLength];
                //    }
                //    AForge.Math.FourierTransform.FFT(Jhrtf_C[i][0], AForge.Math.FourierTransform.Direction.Backward);
                //    AForge.Math.FourierTransform.FFT(Jhrtf_C[i][1], AForge.Math.FourierTransform.Direction.Backward);
                //    for (int j = 0; j < 150; j++)
                //    {
                //        h_lr[0][j] = Jhrtf_C[i][0][j];
                //        h_lr[1][j] = Jhrtf_C[i][1][j];
                //    }
                //    AForge.Math.FourierTransform.FFT(h_lr[0], AForge.Math.FourierTransform.Direction.Forward);
                //    AForge.Math.FourierTransform.FFT(h_lr[1], AForge.Math.FourierTransform.Direction.Forward);

                //    // covolution
                //    for (int j = 0; j < fftLength; j++)
                //    {
                //        Jffts[1][j] = Jffts[0][j] * h_lr[0][j] * fftLength;
                //        Jffts[2][j] = Jffts[0][j] * h_lr[1][j] * fftLength; // Jhrtf_C[i][1][j]
                //    }
                //}
                //else
                //{
                    for (int j = 0; j < fftLength; j++)
                    {
                        Jffts[1][j] = Jffts[0][j] * Jhrtf_C[i][0][j] * fftLength;
                        Jffts[2][j] = Jffts[0][j] * Jhrtf_C[i][1][j] * fftLength; // Jhrtf_C[i][1][j]
                    }
                //}

                // ifft
                AForge.Math.FourierTransform.FFT(Jffts[1], AForge.Math.FourierTransform.Direction.Backward);
                AForge.Math.FourierTransform.FFT(Jffts[2], AForge.Math.FourierTransform.Direction.Backward);
                // output and OLA
                k = 0;
                while (k < fftLength)
                {
                    while (k < buffSize)
                    {
                        Jresult[i][0][k] = ((float)Jffts[1][k].Re + JsampOLA[i][0][k]);    // sometimes crushes here if reflection is removed abrubtly.
                        Jresult[i][1][k] = ((float)Jffts[2][k].Re + JsampOLA[i][1][k]);
                        k++;
                    }
                    JsampOLA[i][0][k - buffSize] = (float)Jffts[1][k].Re;
                    JsampOLA[i][1][k - buffSize] = (float)Jffts[2][k].Re;
                    k++;
                }

            }
        }
        // reinitialize for next buffer
        for (int j = 0; j < Jffts.Length; j++)
        {
            Jffts[j] = new AForge.Math.Complex[fftLength];
        }
    }

    void handleNewReflections(reflectionPath newDirectSound, List<reflectionPath> newReflections)
    {
        directSound = newDirectSound;
        reflections = newReflections;
        haveNewReflections = true;
    }

    private void removeHRTFmanager(int index) // updates (remove) list of azi/ele, database indices, and hrtfs array
    {
        // hrtf manager
        positionArray.RemoveAt(index);
        this.gameObject.GetComponent<HRTFmanager>().azEl.RemoveAt(index);
        //this.gameObject.GetComponent<HRTFmanager>().indices.RemoveAt(index);
        this.gameObject.GetComponent<HRTFmanager>().hrtf_nodes.RemoveAt(index);

        // junction manager
        junctionsSamps.RemoveAt(index);
        Jhrtf_C.RemoveAt(index);
        JsampOLA.RemoveAt(index);
        Jresult.RemoveAt(index);

        // snowman manager
        this.gameObject.GetComponent<Snowman>().JoutL.RemoveAt(index);
        this.gameObject.GetComponent<Snowman>().JoutR.RemoveAt(index);
        this.gameObject.GetComponent<Snowman>().Jitd_l.RemoveAt(index);
        this.gameObject.GetComponent<Snowman>().Jitd_r.RemoveAt(index);
    }

    private void addHRTFmanager(Vector3 nodePos) // updates (add) list of azi/ele, database indices, and initialize hrtfs jagged array
    {

        float[][] tempBuff2 = new float[2][];
        float[][] tempBuff = new float[2][];
        float[][] tempBuffBuff = new float[2][];
        AForge.Math.Complex[][] tempBuff2_C = new AForge.Math.Complex[2][];
        AForge.Math.Complex[][] tempBuff2_CC = new AForge.Math.Complex[2][];
        for (int i = 0; i < tempBuff.Length; i++)
        {
            tempBuff2[i] = new float[fftLength];
            tempBuff2_C[i] = new AForge.Math.Complex[fftLength];
            tempBuff2_CC[i] = new AForge.Math.Complex[fftLength];
            tempBuff[i] = new float[buffSize];
            tempBuffBuff[i] = new float[buffSize];
        }

        // hrtf manager
        positionArray.Add(nodePos);
        float[] aziEle = this.gameObject.GetComponent<HRTFmanager>().getAzElInteraural(nodePos); // interaural !!!
        int[] ind = this.gameObject.GetComponent<HRTFmanager>().getIndices(aziEle[0], aziEle[1]);

        this.gameObject.GetComponent<HRTFmanager>().azEl.Add(aziEle);
        //this.gameObject.GetComponent<HRTFmanager>().indices.Add(ind);
        this.gameObject.GetComponent<HRTFmanager>().hrtf_nodes.Add(tempBuff2_C);

        // junction manager
        junctionsSamps.Add(new AForge.Math.Complex[fftLength]);
        JsampOLA.Add(tempBuff);
        Jresult.Add(tempBuffBuff);
        Jhrtf_C.Add(tempBuff2_CC);

        // snowman manager
        this.gameObject.GetComponent<Snowman>().JoutL.Add(new AForge.Math.Complex[fftLength]);
        this.gameObject.GetComponent<Snowman>().JoutR.Add(new AForge.Math.Complex[fftLength]);
        this.gameObject.GetComponent<Snowman>().Jitd_l.Add(0.0f);
        this.gameObject.GetComponent<Snowman>().Jitd_r.Add(0.0f);
    }

    private void processReflections()
    {
        for (int i = 0; i < network.Count; i++)
        {

            if (reflections.Count > 0)
            {

                int minDistIdx = 0;
                float minDist = float.MaxValue;

                for (int j = 0; j < reflections.Count; j++)
                {

                    float dist = Vector3.Distance(reflections[j].segments[1].origin, network[i].getPosition());

                    if (dist < minDist)
                    {
                        minDist = dist;
                        minDistIdx = j;
                    }
                }

                network[i].updatePath(reflections[minDistIdx]);
                positionArray[i] = network[i].getPosition(); // update position array
                reflections.RemoveAt(minDistIdx);
            }
            else
            {
                // removes node from network
                network[i].informConnectionDelete();
                network.Remove(network[i]);
                removeHRTFmanager(i); // update HRTF manager's lists
            }
        }

        if (reflections.Count > 0)
        {
            // adds node to network
            for (int i = 0; i < reflections.Count; i++)
            {
                network.Add(new SDNnode(reflections[i]));
                addHRTFmanager(reflections[i].segments[1].origin); // update HRTF manager's lists
            }
        }

        for (int i = 0; i < network.Count; i++)
        {
            for (int j = 1; j < network.Count; j++)
            {
                int idx = (j + i) % network.Count;
                if (!network[i].containsConnection(network[idx]))
                {
                    network[i].addConnection(network[idx]);
                    //network[i].getConnections[i].setFilter()
                }
            }
        }

        for (int i = 0; i < network.Count; i++)
        {
            SDNnode n = network[i];
            n.findReverseConnections();
            n.updateConnectionDelay();
        }
    }

    public class delayLine
    {
        public static int sampleRate;
        public static float interpVal = 0.1f;
        private int delayTime;
        private int readPtr;
        private int interpReadPtr;
        private float interpFactor;
        private bool doInterp;

        private bool newInterpWaiting;
        private int newInterpReadPtr;

        private int writePtr;

        private int capacity;
        private float[] buffer;

        private float outSamp;
        public delayLine(float maxTimeInSeconds, int newDelayTime)
        {
            capacity = (int)(sampleRate * maxTimeInSeconds);
            buffer = new float[capacity];

            writePtr = 0;
            interpFactor = 0.0f;
            doInterp = false;
            newInterpWaiting = false;

            if (newDelayTime > capacity)
            {
                print(string.Format("delay time of {0} for buffer size of {1} is not possible", newDelayTime, capacity));
                newDelayTime = capacity - 1;
            }
            if (newDelayTime < 0)
            {
                newDelayTime = 1;
            }
            readPtr = writePtr - newDelayTime;

            if (readPtr < 0)
            {
                readPtr += capacity;
            }

            delayTime = newDelayTime;
        }

        public void clear()
        {
            System.Array.Clear(buffer, 0, buffer.Length);
        }

        public int getDelayTime()
        {
            return delayTime;
        }

        public void setDelay(int newDelayTime)
        {

            if (newDelayTime > capacity)
            {
                print(string.Format("delay time of {0} for buffer size of {1} is not possible", newDelayTime, capacity));
                newDelayTime = capacity - 1;
            }

            if (newDelayTime <= 2)
            {
                newDelayTime = 3;
            }

            if (doInterp)
            {
                newInterpWaiting = true;
                if (newDelayTime != delayTime)
                {
                    newInterpReadPtr = writePtr - newDelayTime;
                    if (newInterpReadPtr < 0)
                    {
                        newInterpReadPtr += capacity;
                    }
                    delayTime = newDelayTime;
                }

            }
            else
            {

                if (newDelayTime != delayTime)
                {
                    interpReadPtr = writePtr - newDelayTime;
                    if (interpReadPtr < 0)
                    {
                        interpReadPtr += capacity;
                    }
                    delayTime = newDelayTime;
                    doInterp = true;
                    interpFactor = 0.0f;
                }
            }
        }

        public void write(float sample)
        {
            buffer[writePtr] = sample;
            writePtr++;
            writePtr %= capacity;
        }

        public float read()
        {

            if (newInterpWaiting && !doInterp)
            {
                doInterp = true;
                newInterpWaiting = false;
                interpReadPtr = newInterpReadPtr;
                interpVal = 0.0f;
            }

            if (doInterp)
            {

                outSamp = (buffer[readPtr] * (1.0f - interpFactor)) + (buffer[interpReadPtr] * interpFactor);

                interpReadPtr++;
                interpReadPtr %= capacity;
                interpFactor += interpVal;

                if (interpFactor > 1.0f)
                {
                    readPtr = interpReadPtr;
                    interpFactor = 0.0f;
                    doInterp = false;
                }

            }
            else
            {
                outSamp = buffer[readPtr];
            }

            readPtr++;
            readPtr %= capacity;
            return outSamp;
        }

        public static int distanceToDelayTime(float distance)
        {
            return Mathf.CeilToInt((distance * sampleRate) / airSpeed);
        }
    }

    public class FDelay
    {
        // A polynomial interploation fractional delay line
        // Based on Pelle Juul´s C++ implementation: https://github.com/PelleJuul/lyt/blob/master/library/fdelay.cpp

        private int index = 300;
        private int d0;
        private int d1;
        private int d2;
        private int d3;
        private float delay;
        private float frac;
        private float[] vec;

        public FDelay()
        {
            setMaxDelay(1);
            setDelay(1);
        }

        public FDelay(int maxDelay, float delay)
        {
            setMaxDelay(maxDelay);
            setDelay(delay);
        }

        public void setMaxDelay(int newMaxDelay)
        {
            Array.Resize(ref vec, newMaxDelay + 4);
        }

        public void setDelay(float newDelay)
        {
            this.delay = newDelay;
            d0 = Mathf.FloorToInt(delay);
            d1 = Mathf.CeilToInt(delay);
            d2 = d1 + 1;
            d3 = d1 + 2;

            //Debug.Log("d0 " + d0);
            //Debug.Log("d1 " + d1);
            //Debug.Log("d2 " + d2);
            //Debug.Log("d3 " + d3);

            frac = delay - d0;
        }

        public float process(float value)
        {
            //Debug.Log(vec.Length);
            //Debug.Log((index - d0) % vec.Length);
            //Debug.Log(d0);
            float y0 = vec[(index - d0) % vec.Length];
            float y1 = vec[(index - d1) % vec.Length];
            float y2 = vec[(index - d2) % vec.Length];
            float y3 = vec[(index - d3) % vec.Length];
            float x = frac;

            //Debug.Log("0 " + (index - d0) % vec.Length);
            //Debug.Log("1 " + (index - d1) % vec.Length);
            //Debug.Log("2 " + (index - d2) % vec.Length);
            //Debug.Log("3 " + (index - d3) % vec.Length);

            float y =
                ((x - 1) * (x - 2) * (x - 3)) / ((0 - 1) * (0 - 2) * (0 - 3)) * y0 +
                ((x - 0) * (x - 2) * (x - 3)) / ((1 - 0) * (1 - 2) * (1 - 3)) * y1 +
                ((x - 0) * (x - 1) * (x - 3)) / ((2 - 0) * (2 - 1) * (2 - 3)) * y2 +
                ((x - 0) * (x - 1) * (x - 2)) / ((3 - 0) * (3 - 1) * (3 - 2)) * y3;

            vec[index % vec.Length] = value;
            index += 1;

            return y;
        }
    }

    public class SDNnode
    {
        public static float specularity;
        public static float wallAbsCoeff;
        public static float nodeLoss;

        private Vector3 position;
        private List<SDNConnection> connections;
        private reflectionPath nodePath;

        private delayLine incoming;
        private delayLine outgoing;

        private float scatteringFactor;
        private float scatteringFactorDiag;

        private float FOSample = 0.0f;
        private float HalfFOSample = 0.0f;
        private float outgoingSample = 0.0f;
        private float incomingAttFactor = 0.0f;
        private float outgoingAttFactor = 0.0f;

        private int numConnections = 0;

        private int wallNumber; // 1=right, 2=front, 3=left, 4=back, 5=ceiling, 6=floor
        private string wallName;
        private FourthOrderFilter wallFilter;

        public SDNnode(reflectionPath thePath)
        {
            connections = new List<SDNConnection>();
            nodePath = thePath;
            position = nodePath.segments[1].origin;
            incoming = new delayLine(1.0f, delayLine.distanceToDelayTime(nodePath.lengths[0]));
            outgoing = new delayLine(1.0f, delayLine.distanceToDelayTime(nodePath.lengths[1]));
            incomingAttFactor = 1.0f / (nodePath.lengths[0] + 1.0f);
            outgoingAttFactor = 1.0f / (nodePath.lengths[1] + 1.0f);
            wallName = thePath.wallName;
            wallFilter = wallFilt();
            specularity = 0.5f;
            wallAbsCoeff = GameObject.Find(wallName).GetComponent<WallFiltAndGain>().wall_absorption_coeff;
            nodeLoss = 1.0f - wallAbsCoeff;
        }

        public void updatePath(reflectionPath thePath)
        {
            nodePath = thePath;
            position = thePath.segments[1].origin;
            incoming.setDelay(delayLine.distanceToDelayTime(nodePath.lengths[0]));
            outgoing.setDelay(delayLine.distanceToDelayTime(nodePath.lengths[1]));
            incomingAttFactor = 1.0f / (nodePath.lengths[0] + 1.0f);
            outgoingAttFactor = 1.0f / (nodePath.lengths[1] + 1.0f);
        }

        private FourthOrderFilter wallFilt()
        {
            FourthOrderFilter filt;
            //BiQuadFilter filt;

            switch (GameObject.Find(wallName).GetComponent<WallFiltAndGain>().wall_material)
            {

                case "concrete": // walls, hard surface average
                    return filt = new FourthOrderFilter(1, -3.52116135875940, 4.71769322757540, -2.85612549976053,   0.659726857770080, 0.975675045601632, -3.43354999748689,   4.59790176394788, -2.78225880232183,   0.642363879989748);
                case "carpet": // carpet tile
                    return filt = new FourthOrderFilter(1, -1.08262757037834, -0.409156873201483,  0.393177044223513, 0.115374144111459, 0.557133737058345, -0.595471680050212, -0.198869264528946, 0.202098010808594, 0.0519704029964675);
                case "glass": // double glass window
                    return filt = new FourthOrderFilter(1, -3.88157817484550,   5.67768298901651, -3.70995794379865,  0.913857561160056, 0.988950446602402, -3.84068106116436,   5.62070430601924, -3.67455649442745,   0.905586752101161);
                case "gypsum": // perforated gypsum
                    return filt = new FourthOrderFilter(1, -3.56627223138536,   4.86547502358377, -3.01433043560409,   0.716117186547968, 0.489588360222364, -1.76575810943288,   2.43564604718648, -1.52441182610865,   0.365712142392197);
                case "vinyl": // vinyl concrete
                    return filt = new FourthOrderFilter(1, -1.20321523440173, -0.152249555702279, 0.452752314993338, -0.0802737840229035, 0.951113234620835, -1.14521870903135, -0.126014399528383,  0.412593832519338, -0.0756816378410196);
                case "wood": // wooden door
                    return filt = new FourthOrderFilter(1, -3.80539017238370,   5.45234860126098, -3.48668369576338,   0.839765796280106, 0.949092279345441, -3.61085651980882,   5.17248568134037, -3.30701292959816, 0.796328272495264);
                case "rockfon": // rockfon panel
                    return filt = new FourthOrderFilter(1, -1.10082609915788, -0.197118900834112, 0.396353649853209, -0.0863017873494277, 0.604052413410784, -0.840106608467757, -0.0638796553407742, 0.478513769747507, -0.165417289996871);
                default:
                    System.Console.WriteLine("Wall " + wallName + ": Non existing wall material.");
                    return filt = null;

                    //case "concrete":
                    //    return filt = new BiQuadFilter(1, -1.672776409411440, 0.713278490262988, 0.975768631797481, -1.630106862433270, 0.694418597128906);
                    //case "wood":
                    //    return filt = new BiQuadFilter(1, -1.912927181801299, 0.913863324941008, 0.962667699415728, -1.844854689523742, 0.883011230349589);
                    //case "carpet":
                    //    return filt = new BiQuadFilter(1, -1.851010754838570, 0.857074229441280, 0.760467002249119, -1.383797223563548, 0.629324606679103);
                    //default:
                    //    System.Console.WriteLine("Wall " + wallName + ": Non existing wall material.");
                    //    return filt = null;
            }
        }

        public void updateWallFilt()
        {
            // update wall absoption filter coefficients
            switch (GameObject.Find(wallName).GetComponent<WallFiltAndGain>().wall_material)
            {
                //case "concrete": // walls, hard surface average
                //    wallFilter.SetCoefficients(1, -3.52116135875940, 4.71769322757540, -2.85612549976053, 0.659726857770080, 0.975675045601632, -3.43354999748689, 4.59790176394788, -2.78225880232183, 0.642363879989748);
                //    break;
                //case "carpet": // carpet tile
                //    wallFilter.SetCoefficients(1, -1.08262757037834, -0.409156873201483, 0.393177044223513, 0.115374144111459, 0.557133737058345, -0.595471680050212, -0.198869264528946, 0.202098010808594, 0.0519704029964675);
                //    break;
                //case "glass": // double glass window
                //    wallFilter.SetCoefficients(1, -3.88157817484550, 5.67768298901651, -3.70995794379865, 0.913857561160056, 0.988950446602402, -3.84068106116436, 5.62070430601924, -3.67455649442745, 0.905586752101161);
                //    break;
                //case "gypsum": // perforated gypsum
                //    wallFilter.SetCoefficients(1, -3.56627223138536, 4.86547502358377, -3.01433043560409, 0.716117186547968, 0.489588360222364, -1.76575810943288, 2.43564604718648, -1.52441182610865, 0.365712142392197);
                //    break;
                //case "vinyl": // vinyl concrete
                //    wallFilter.SetCoefficients(1, -1.20321523440173, -0.152249555702279, 0.452752314993338, -0.0802737840229035, 0.951113234620835, -1.14521870903135, -0.126014399528383, 0.412593832519338, -0.0756816378410196);
                //    break;
                //case "wood": // wooden door
                //    wallFilter.SetCoefficients(1, -3.80539017238370, 5.45234860126098, -3.48668369576338, 0.839765796280106, 0.949092279345441, -3.61085651980882, 5.17248568134037, -3.30701292959816, 0.796328272495264);
                //    break;
                //case "rockfon": // rockfon panel
                //    wallFilter.SetCoefficients(1, -1.10082609915788, -0.197118900834112, 0.396353649853209, -0.0863017873494277, 0.604052413410784, -0.840106608467757, -0.0638796553407742, 0.478513769747507, -0.165417289996871);
                //    break;
                //default:
                //    System.Console.WriteLine("Wall reset problem" + wallName + ": Non existing wall material.");
                //    break;

                case "concrete": // walls, hard surface average
                    wallFilter = new FourthOrderFilter(1, -3.52116135875940, 4.71769322757540, -2.85612549976053, 0.659726857770080, 0.975675045601632, -3.43354999748689, 4.59790176394788, -2.78225880232183, 0.642363879989748);
                    break;
                case "carpet": // carpet tile
                    wallFilter = new FourthOrderFilter(1, -1.08262757037834, -0.409156873201483, 0.393177044223513, 0.115374144111459, 0.557133737058345, -0.595471680050212, -0.198869264528946, 0.202098010808594, 0.0519704029964675);
                    break;
                case "glass": // double glass window
                    wallFilter = new FourthOrderFilter(1, -3.88157817484550, 5.67768298901651, -3.70995794379865, 0.913857561160056, 0.988950446602402, -3.84068106116436, 5.62070430601924, -3.67455649442745, 0.905586752101161);
                    break;
                case "gypsum": // perforated gypsum
                    wallFilter = new FourthOrderFilter(1, -3.56627223138536, 4.86547502358377, -3.01433043560409, 0.716117186547968, 0.489588360222364, -1.76575810943288, 2.43564604718648, -1.52441182610865, 0.365712142392197);
                    break;
                case "vinyl": // vinyl concrete
                    wallFilter = new FourthOrderFilter(1, -1.20321523440173, -0.152249555702279, 0.452752314993338, -0.0802737840229035, 0.951113234620835, -1.14521870903135, -0.126014399528383, 0.412593832519338, -0.0756816378410196);
                    break;
                case "wood": // wooden door
                    wallFilter = new FourthOrderFilter(1, -3.80539017238370, 5.45234860126098, -3.48668369576338, 0.839765796280106, 0.949092279345441, -3.61085651980882, 5.17248568134037, -3.30701292959816, 0.796328272495264);
                    break;
                case "rockfon": // rockfon panel
                    wallFilter = new FourthOrderFilter(1, -1.10082609915788, -0.197118900834112, 0.396353649853209, -0.0863017873494277, 0.604052413410784, -0.840106608467757, -0.0638796553407742, 0.478513769747507, -0.165417289996871);
                    break;
                default:
                    System.Console.WriteLine("Wall " + wallName + ": Non existing wall material.");
                    wallFilter = null;
                    break;
            }

            // update wall absorption coefficient and node loss
            setWallAbs(GameObject.Find(wallName).GetComponent<WallFiltAndGain>().wall_absorption_coeff);
            setLoss(1.0f - wallAbsCoeff);

            //Debug.Log("Updated wall " + wallName + " absCoeff " + wallAbsCoeff + " filter " + GameObject.Find(wallName).GetComponent<WallFiltAndGain>().wall_material);
        }

        public int getWallNumber()
        {
            return wallNumber;
        }

        public void clearDelay()
        {
            incoming.clear();
            outgoing.clear();
        }

        public static void setLoss(float loss)
        {
            nodeLoss = loss;
        }

        public static void setSpecularity(float spec)
        {
            specularity = spec;
        }

        public static void setWallAbs(float abs)
        {
            wallAbsCoeff = abs;
        }

        public void inputIncoming(float sample)
        {
            incoming.write(sample);
        }

        public float getOutgoing()
        {
            return outgoing.read();
        }


        public void doScattering(bool outputLateReflections)
        {
            FOSample = incoming.read();
            FOSample *= incomingAttFactor;
            FOSample = wallFilter.Transform(FOSample);
            FOSample *= wallAbsCoeff;
            outgoingSample = FOSample;
            HalfFOSample = 0.5f * FOSample;

            //Debug.Log(wallAbsCoeff);

            int i, j;

            for (i = 0; i < numConnections; i++)
            {
                connections[i].posSamp = connections[i].getSampleFromReverseConnection();
            }
            for (i = 0; i < numConnections; i++)
            {

                outgoingSample += connections[i].posSamp;
                outgoingSample += connections[i].prevSample;
                connections[i].negSamp += (HalfFOSample);

                for (j = 0; j < numConnections; j++)
                {
                    if (i == j)
                    {
                        connections[i].negSamp += connections[j].posSamp * scatteringFactorDiag;
                    }
                    else
                    {
                        connections[i].negSamp += connections[j].posSamp * scatteringFactor;
                    }
                }

                //				connections [i].negSamp = connections [i].connectFilter.Transform (connections [i].negSamp);
                //				connections [i].negSamp = connections [i].junctionFilters[i].Transform (connections [i].negSamp);
                connections[i].negSamp -= connections[i].prevSample;
                connections[i].inputToDelay(connections[i].negSamp);
                connections[i].prevSample = connections[i].negSamp;

            }

            if (outputLateReflections)
            {
                outgoingSample *= outgoingAttFactor;
                //do listener filtering for each node
                outgoing.write(outgoingSample);
            }
            else
            {
                FOSample *= outgoingAttFactor;
                outgoing.write(FOSample);
            }
        }


        public void updateScatteringFactor()
        {
            int minCon = connections.Count + 1;

            scatteringFactor = (2.0f / minCon) - nodeLoss;
            scatteringFactorDiag = ((2.0f - minCon) / minCon) - -nodeLoss;
        }

        public void updateConnectionDelay()
        {
            for (int i = 0; i < connections.Count; i++)
            {
                connections[i].updateDelayLength();
            }
        }

        public List<SDNConnection> getConnections()
        {
            return connections;
        }

        public void addConnection(SDNnode n)
        {
            connections.Add(new SDNConnection(this, n));
            updateScatteringFactor();
            numConnections = connections.Count;
        }

        public void findReverseConnections()
        {
            for (int i = 0; i < connections.Count; i++)
            {
                SDNConnection theTarget = connections[i].getTarget().connections.Find(item => item.getTarget() == this);
                connections[i].setReverseConnection(ref theTarget);
            }
        }

        public void informConnectionDelete()
        {
            for (int i = 0; i < connections.Count; i++)
            {
                SDNnode nodeToInform = connections[i].getTarget();
                int idx = nodeToInform.connections.FindIndex(item => item.getTarget() == this);
                nodeToInform.connections.RemoveAt(idx);
                nodeToInform.numConnections = nodeToInform.connections.Count;
                nodeToInform.updateScatteringFactor();

            }
        }

        public bool containsConnection(SDNnode n)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].getTarget().Equals(n))
                {
                    return true;
                }
            }
            return false;
        }

        public Vector3 getPosition()
        {
            return position;
        }

        public int getIncomingDelayTime()
        {
            return incoming.getDelayTime();
        }

        public int getOutgoingDelayTime()
        {
            return outgoing.getDelayTime();
        }

        public int getTotalDelayTime()
        {
            return outgoing.getDelayTime() + incoming.getDelayTime();
        }

        public reflectionPath getPath()
        {
            return nodePath;
        }
    }

    public class SDNConnection
    {
        public float posSamp = 0.0f;
        public float negSamp = 0.0f;
        public float prevSample = 0.0f;

        private SDNnode parent;
        private SDNnode target;
        private float length;
        private delayLine delay;
        private SDNConnection reverseConnection;
        public BiQuadFilter connectFilter;

        public SDNConnection(SDNnode theParent, SDNnode theTarget)
        {
            connectFilter = BiQuadFilter.highPassAirFilter(delayLine.sampleRate);

            parent = theParent;
            target = theTarget;
            length = Vector3.Distance(parent.getPosition(), target.getPosition());
            delay = new delayLine(1.0f, delayLine.distanceToDelayTime(length));
        }

        public void clearDelay()
        {
            delay.clear();
        }

        public void setTarget(SDNnode n)
        {
            target = n;
            updateDelayLength();
        }

        public int getDelayTime()
        {
            return delay.getDelayTime();
        }

        public void inputToDelay(float sample)
        {
            delay.write(sample);
        }

        public float readFromDelay()
        {
            return delay.read();
        }

        public float getSampleFromReverseConnection()
        {
            if (reverseConnection != null)
            {
                return reverseConnection.readFromDelay();
            }
            else
            {
                return 0.0f;
            }
        }

        public SDNnode getTarget()
        {
            return target;
        }

        public SDNnode getParent()
        {
            return parent;
        }

        public float getLength()
        {
            return length;
        }

        public void setReverseConnection(ref SDNConnection c)
        {
            reverseConnection = c;
        }

        public void updateDelayLength()
        {
            length = Vector3.Distance(parent.getPosition(), target.getPosition());
            delay.setDelay(delayLine.distanceToDelayTime(length));
        }
    }
}


