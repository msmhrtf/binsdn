using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using UnityEngine;
using System;
using AForge.Math;
using UnityEngine.UI;

public class SDN : MonoBehaviour
{
    private SDNEnvConfig subjectInfo;

    public GameObject listener;
    private boundary boundary;
    private GameObject geoForNetwork;
    private GameObject rooms;

    public static int targetNumReflections = 35;
    public bool PlayOnAwake = true;
    public bool update = true;

    public bool enableListen = true;
    public bool doLateReflections = true; //Calculate late Reflections?
    public bool applyHrtfToDirectSound = true; //apply HRTF filter to reflections and directSound?

    public bool applyHrtfToReflections = true; //apply HRTF filter to reflections and directSound?

    public float volumeGain = 1.0f;

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
    private Complex[] AFout;

    private float chanScale = 0.0f;
    private int numSamps;

    private int buffSize;
    private int sampleRate;

    private List<reflectionPath> waitingReflections;
    private reflectionPath waitingDirect;

    private bool haveNewReflections = false;
    private bool doClear = false;

    public bool debugThisSource = false;

    //private Mutex sampleMX;
    private Mutex networkMX;
    private Thread audioProcessThread;
    private bool audioProcessKeepAlive = true;
    private bool scriptInit = false;

    public List<UnityEngine.Vector3> positionArray; // dynamic list of nodes position, used for azi and ele, shared to HRTFmanager

    // direct path HRTF convolution variables 
    Complex[][] Jffts = new Complex[3][];
    // OLA samples to add to next buffer
    private float[][] sampOLA = new float[2][];
    // zero padded hrtfs
    private Complex[][] hrtf = new Complex[2][];
    private float[] directItds = new float[2];
    // hrtf from HRTFmanager
    private Complex[][] hrtf_C = new Complex[2][];
    // convolution output buffers
    private float[][] result = new float[2][];

    // junctions HRTF convolution variables
    private List<Complex[]> junctionsSamps;
    private List<Complex[][]> Jhrtf_C;

    //Junction HRTFS
    private List<HRTFData> JhrtfData;
    //private List<Complex[][]> Jhrtf;
    //private List<float[]> JItds;
    
    //private List<float[][]> Jbig_result;
    private List<float[][]> Jresult;

    private int idx = 0; // index for storing junction samples
    private int fftLength;

    AudioSource _source;

    //private bool loaded = false; // trick to let know that the HRTFs are loaded from the text file.
    private bool snowman = false;

    void Awake() {
        SDNEnvConfig[] tmp = FindObjectsOfType<SDNEnvConfig>();

        switch (tmp.Length) {
            case 0:
                Debug.Log("SDN.cs ERROR!!!! SDNEnvConfig is missing. Please insert one!");
                break;
            case 1:
                subjectInfo = tmp[0];
                break;
            default:
                Debug.Log("There are too many SDNEnvConfig elements in the scene");
                subjectInfo = tmp[0];
                break;
        }
    }

    void Start()
    {
        if(PlayOnAwake){
            GetComponent<AudioSource>().Play();
        }
        
        _source = this.gameObject.GetComponent<AudioSource>();

        AudioConfiguration AC = AudioSettings.GetConfiguration();
        sampleRate = AC.sampleRate;
        buffSize = AC.dspBufferSize;
        inSamples = new Queue<float>();
        outSamples = new Queue<float>();

        delayLine.sampleRate = sampleRate;

        int initalDirectDelay = delayLine.distanceToDelayTime(UnityEngine.Vector3.Distance(gameObject.transform.position, listener.transform.position));
        directDelay = new delayLine(1.0f, initalDirectDelay);

        network = new List<SDNnode>();
        RF = gameObject.AddComponent<reflectionFinder>();
        RF.setListener(listener);

        geoForNetwork = GameObject.FindObjectOfType<RoomBuilder>().getActiveRoom();
        Debug.Assert(geoForNetwork != null, "Error!! Please Insert a RoomBuilder inside the project!");

        boundary[] tmp = FindObjectsOfType<boundary>();
        if (tmp.Length < 1) Debug.Log("SDN.cs is looking for boundary which is missing! Please insert one!");
        boundary = tmp[0];

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
        //sampleMX = new Mutex();
        networkMX = new Mutex();
        //audioProcessThread = new Thread(audioProcess);
        //audioProcessThread.Start();
        scriptInit = true;

        fftLength = buffSize;

        AFout = new Complex[fftLength];

        for (int i = 0; i < sampOLA.Length; i++)
        {
            sampOLA[i] = new float[buffSize*2]; //MODIFICATO!!!!
            hrtf[i] = new Complex[200];
            result[i] = new float[buffSize];
            hrtf_C[i] = new Complex[fftLength];
        }

        junctionsSamps = new List<Complex[]>();
        Jresult = new List<float[][]>();
        Jhrtf_C = new List<Complex[][]>();
        //Jhrtf = new List<Complex[][]>();
        JhrtfData = new List<HRTFData>();

        // junctions ffts
        for (int i = 0; i < Jffts.Length; i++)
        {
            Jffts[i] = new Complex[fftLength];
        }

        //ROBA MIA
        circBuffer = new CrossfadeBuffer(buffSize);

    }

    int oldnetworkcount = 0;
    void Update()
    {
        RF.doUpdate = update;

        if (update && haveNewReflections)
        {
            networkMX.WaitOne();
            processReflections();
            directDelay.setDelay(delayLine.distanceToDelayTime(directSound.totalDistance()));
            directAtt = 1.0f / (UnityEngine.Vector3.Distance(listener.transform.position, gameObject.transform.position) + 1.0f); //Corretta
            networkInScale = 1.0f / network.Count;
            if(network.Count != oldnetworkcount){
                //Debug.Log("Network count changed: " + network.Count);
            }
            oldnetworkcount = network.Count;
            haveNewReflections = false;
            networkMX.ReleaseMutex();
        }
        checkDelayClear();

        // get HRTFs from HRTFmanager
            hrtf = this.gameObject.GetComponent<HRTFmanager>().getHrftDirect();   // TO DO: update only if move
            directItds = this.gameObject.GetComponent<HRTFmanager>().getItdsDirect();
        //Debug.Log(junctionsSamps.Count);
        if(junctionsSamps.Count == this.gameObject.GetComponent<HRTFmanager>().hrtf_nodes.Count){
            JhrtfData = this.gameObject.GetComponent<HRTFmanager>().hrtf_nodes;
            //Jhrtf = this.gameObject.GetComponent<HRTFmanager>().hrtf_nodes;
            //JItds = this.gameObject.GetComponent<HRTFmanager>().itds_nodes;
        }else{
            Debug.Log("Warning: Wrong number of walls");
            Debug.Log("Junction Samples : " + junctionsSamps.Count);
            Debug.Log("HRTF Nodes : " + this.gameObject.GetComponent<HRTFmanager>().hrtf_nodes.Count);
        }
    }





    void OnApplicationQuit()
    {
        audioProcessKeepAlive = false;
    }

    public void audioProcess()
    {

        while (audioProcessKeepAlive)
        {
            //propagateNetwork();
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
        Bounds b = new Bounds(g.transform.position, UnityEngine.Vector3.zero);
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

        // update boundary
        boundary.setBoundaryBounds(GetMaxBounds(geoForNetwork));
        Debug.Log(RF);
        RF.setboundary(boundary);




    }

    public void updateWallMaterialFilter()
    // Function to update wall absortption filters and wall absorbption coefficients according to wall properties set in the Unity user GUI for each wall of the room.
    // See RoomManager and SDNNode.
    {

        for (int i = 0; i < network.Count; i++)
        {
            network[i].updateWallFilt();
        }
    }

    public void setHaveNewRef()
    {
        haveNewReflections = true;
    }

    public void clearAllDelays()
    {
        doClear = true;
    }

    public void setNodeLoss(float loss)
    {
        SDNnode.setLoss(loss);
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

    public List<UnityEngine.Vector3> getPositionArray()
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

    public Complex[][] CopyArrayLinq(Complex[][] source) // jagged array copy
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

    public static void interleaveDataNoReflection(float[][] data_in, List<Complex[]> Jdata_in, float[] data_out, int num_ch)   // <T>(T[][] data_in, List<T[][]> Jdata_in, T[] data_out, int num_ch)
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
        Debug.Log("Channels " + num_ch);
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

        //        Debug.Log(numSamps);

        if (scriptInit)
        {

            //sampleMX.WaitOne();

            for (i = 0; i < numSamps; i++)
            {
                AFin = 0.0f;
                for (c = 0; c < channels; c++)
                {
                    AFin += data[(i * channels) + c]; //MONOIZZA GIA' IL SAMPLE: Prende i due dati (interleaved) e li fa diventare un solo canale, incaso siano doppi
                }
                inSamples.Enqueue(AFin * chanScale); //Se fosse, ad es, due canali, divide per due per rendere ok il volume
            }  //Questa parte carica i valori su una coda che poi li passa al "propagate network" (v. sotto), per l'elaborazione del rimbombo

            if (!(outSamples.Count <= numSamps)) // sound output when buffer is full -- pesca i sample dalla coda dello scattering delay
            {
                //Se bypassato il resto non ci sono clicks, questa parte è OK
                if (!enableListen)
                {
                    for (i = 0; i < numSamps; i++)
                    {
                        for (c = 0; c < channels; c++)
                        {
                            data[(i * channels) + c] *= directAtt; //Attenua un base alla distanza (calcolata in una altra void) senza applicare altro
                        }
                    }
                }
                else
                {
                    idx = 0; // put buffer index to zero
                    for (i = 0; i < numSamps; i++)
                    {
                        Jffts[0][i] = new Complex(outSamples.Dequeue(), 0);  //Carico i samples
                    }

                    convHRTF_Crossfade(applyHrtfToDirectSound, applyHrtfToReflections);

                    interleaveData(result, Jresult, data, channels);
                    //interleaveSimple(result, data, channels);
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

            propagateNetwork();

            //Applico un gain del volume
            for (int j = 0; j < data.Length; j++)
            {
                data[j] = data[j] * volumeGain;
            }

            //sampleMX.ReleaseMutex();

            

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

    public void propagateNetwork()
    {

        //sampleMX.WaitOne();
        networkMX.WaitOne();



        int numSampsToConsume = inSamples.Count;
        // horrible hack to solve latency @ startup - the queue quickly fills up from
        // the audio thread before the main thread can catch up. I am a bad person for doing this.
        if (inSamples.Count > 10000)
        {
            Debug.Log("Terrible hack incoming!!!");
            inSamples.Clear();
            //sampleMX.ReleaseMutex();
            networkMX.ReleaseMutex(); //Riga aggiunta da me... è corretta?
            return;
        }

        int i, j;

        for (i = 0; i < numSampsToConsume; i++)
        {
            outVal = 0.0f;
            inVal = inSamples.Dequeue() * networkInScale; //networkInScale = 1/network.Count;
                                                          //CREDO sia il numero di muri del network, altrimenti è il numero di nodi (cioè 36 - nodi "i-j" diagonali)

            // direct path processing
            directDelay.write(inVal);
            directVal = directDelay.read(); //leggo il sample con il delay indicato già corretto
            directVal *= directAtt; //calcolata come 1/distanza (approssimata?). Distanza è sempre positiva, quindi
            //non inverte la fase
            outSamples.Enqueue(directVal);

            // Prendo lo stesso sample e lo butto su tutti i nodi, con il delay corretto
            for (j = 0; j < network.Count; j++) //Per tutti i nodi presenti (6?)
            {
                try
                {
                    //junctionsSamps[j][idx] = new Complex(network[j].getOutgoing(), 0); //leggo il sample con il delay
                                                                                       //corretto. Gestisco un buffer intero con idx, che è definito globale 
                    junctionsSamps[j][i] = new Complex(network[j].getOutgoing(), 0); //leggo il sample con il delay
                                                                                       //corretto. Gestisco un buffer intero con idx, che è definito globale 

                }
                catch (Exception e)
                {
                    Debug.Log("ERRORE!!!");
                    Debug.Log("CCX junctionsSamps Length = " + junctionsSamps.Count);
                    Debug.Log("CCX junctionsSamps[j] Length = " + junctionsSamps[j].Length);
                    Debug.Log("CCX idx: " + idx);
                    Debug.Log("CCX j: " + j);

                    //sampleMX.ReleaseMutex();
                    networkMX.ReleaseMutex();
                    return;
                }
                network[j].inputIncoming(inVal);
                network[j].doScattering(doLateReflections);
            }
            //idx++;
        }


        //sampleMX.ReleaseMutex();
        networkMX.ReleaseMutex();
    }



    CrossfadeBuffer circBuffer;
    List<CrossfadeBuffer> juncCircBuffer = new List<CrossfadeBuffer>();
    
    private void convHRTF_Crossfade(bool doDirectHRTF,bool doReflectionHRTF)
    {
        //Copia la funziona hrtf corretta
        hrtf_C = CopyArrayLinq(hrtf);       // TO DO: copy only if moving

        if (doDirectHRTF)
        {
            result = circBuffer.getFromBuffer(Jffts[0], hrtf_C, directItds);
        }
        else
        { //Copy sample as-is without HRTF calculation
            for (int j = 0; j < result[0].Length; j++)
            {
                result[0][j] = (float)Jffts[0][j].Re;
                result[1][j] = (float)Jffts[0][j].Re;

            }
        }

        //QUI FACCIO LE HRTF con le 6 riflessioni sui muri

        //Non sempre i juncSampls coincidono, poiche' il numero di muri viene aggiunto "on the fly"
        //bisognerebbe aggiungere un semaphore quando vengono ricalcolate le riflessioni sui muri
        
        for (int i = 0; i < junctionsSamps.Count; i++)
        {
            Jhrtf_C[i] = CopyArrayLinq(JhrtfData[i].HRTFs);   //Giusto?
        //}
        // same thing for junctions --- qui vengono fatte le altre 6 convoluzioni
                // reinitialize for next junction
                for (int j = 0; j < Jffts.Length; j++)
                {
                    Jffts[j] = new Complex[fftLength];
                }

                // copy
                Array.Copy(junctionsSamps[i], Jffts[0], buffSize);


            if (doReflectionHRTF)
            {
                try{
                    //Jresult[i] = juncCircBuffer[i].getFromBuffer(Jffts[0], Jhrtf_C[i], JItds[i]);
                    Jresult[i] = juncCircBuffer[i].getFromBuffer(Jffts[0], Jhrtf_C[i], JhrtfData[i].Delays);
                }
                catch (Exception e)
                {
                    Debug.Log("Warning: some junctions is lost in threads. Do Not Worry.");
                    Debug.Log("void convHRTF_Crossfade");
                    Debug.Log("Lost Reflection?");
                    
                }
            }
            else { //Copy sample as-is without HRTF calculation
                for (int j = 0; j < Jresult[i][0].Length; j++) {
                    Jresult[i][0][j] = (float) Jffts[0][j].Re;
                    Jresult[i][1][j] = (float) Jffts[0][j].Re;
                    
                }
            }
        }
        // reinitialize for next buffer
        for (int j = 0; j < Jffts.Length; j++)
        {
            Jffts[j] = new Complex[fftLength];
        }

    }



    Complex[] OldBQueue;
    Complex[] OldABQueue;

    Complex[] sampleBuffer;

    int step = 0;


    void handleNewReflections(reflectionPath newDirectSound, List<reflectionPath> newReflections)
    {
        directSound = newDirectSound;
        reflections = newReflections;
        haveNewReflections = true;
    }

    private void removeHRTFmanager(int index) // updates (remove) list of azi/ele, database indices, and hrtfs array
    {

        //Debug.Log("Chiamato RemoveHRTFManager. Eliminata una Juction?");
        // hrtf manager
        positionArray.RemoveAt(index);
        this.gameObject.GetComponent<HRTFmanager>().azEl.RemoveAt(index);
        this.gameObject.GetComponent<HRTFmanager>().hrtf_nodes.RemoveAt(index);
        //this.gameObject.GetComponent<HRTFmanager>().itds_nodes.RemoveAt(index);

        // junction manager
        junctionsSamps.RemoveAt(index);
        Jhrtf_C.RemoveAt(index);
        Jresult.RemoveAt(index);

        //ROBA MIA
        juncCircBuffer.RemoveAt(index);
    }


    private void addHRTFmanager(UnityEngine.Vector3 nodePos) // updates (add) list of azi/ele, database indices, and initialize hrtfs jagged array
    {

        //Debug.Log("Chiamato addHRTFManager. Aggiunta una Juction?");
        float[][] floatEmptyBuff = new float[2][];
        Complex[][] complexEmptyBuff = new Complex[2][];
        Complex[][] complexEmptyBuff2 = new Complex[2][];
        for (int i = 0; i < 2; i++) //Creo il buffer per lo stereo
        {
            complexEmptyBuff[i] = new Complex[fftLength];
            complexEmptyBuff2[i] = new Complex[fftLength];
            floatEmptyBuff[i] = new float[buffSize];
        }

        

        // hrtf manager
        positionArray.Add(nodePos);
        float[] aziEle = this.gameObject.GetComponent<HRTFmanager>().getAzElInteraural(nodePos); // interaural !!!
        int[] ind = this.gameObject.GetComponent<HRTFmanager>().getIndices(aziEle[0], aziEle[1]);

        this.gameObject.GetComponent<HRTFmanager>().azEl.Add(aziEle);

        HRTFData tmp = new HRTFData();
        tmp.HRTFs = complexEmptyBuff;
        tmp.Delays = new float[] {0,0};

        this.gameObject.GetComponent<HRTFmanager>().hrtf_nodes.Add(tmp);

        // junction manager
        junctionsSamps.Add(new Complex[fftLength]);
        Jresult.Add(floatEmptyBuff);
        Jhrtf_C.Add(complexEmptyBuff2);

        //ROBA MIA
        juncCircBuffer.Add(new CrossfadeBuffer(buffSize));
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

                    float dist = UnityEngine.Vector3.Distance(reflections[j].segments[1].origin, network[i].getPosition());

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
            //END DEBUG
            for (int i = 0; i < reflections.Count; i++)
            {
                if (GameObject.Find(reflections[i].wallName).GetComponent<WallFiltAndGain>() != null)
                {

                    network.Add(new SDNnode(reflections[i]));

                    addHRTFmanager(reflections[i].segments[1].origin); // update HRTF manager's lists
                }
                else {
                    //Debug.Log("Incorrect Reflection!");
                }
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
            return Mathf.CeilToInt((distance * sampleRate) / airSpeed);//inverso di attenuation?
        }
    }

    public class SDNnode
    {
        //public static float specularity;
        public static float wallAbsCoeff;
        public static float nodeLoss;

        private UnityEngine.Vector3 position;
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
            incomingAttFactor = 1.0f / (nodePath.lengths[0] + 1.0f); //lengths[0]--> distanza source-refl.point
            outgoingAttFactor = 1.0f / (nodePath.lengths[1] + 1.0f); //lengths[1]--> distanza refl.point-listener
            wallName = thePath.wallName;
            if (!wallName.Equals("Sphere"))
            {
                wallFilter = wallFilt();
                wallAbsCoeff = GameObject.Find(wallName).GetComponent<WallFiltAndGain>().wall_absorption_coeff;
                nodeLoss = 1.0f - wallAbsCoeff; //quanto suono perde? Cioè tutto tranne quello assorbito dal muro
            }

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

            filt = AudioMaterials.getMaterial(GameObject.Find(wallName).GetComponent<WallFiltAndGain>().wall_material);
            return filt;
        }

        public void updateWallFilt()
        {
            wallFilter = AudioMaterials.getMaterial(GameObject.Find(wallName).GetComponent<WallFiltAndGain>().wall_material);

            // update wall absorption coefficient and node loss
            setWallAbs(GameObject.Find(wallName).GetComponent<WallFiltAndGain>().wall_absorption_coeff);
            setLoss(1.0f - wallAbsCoeff);

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
            FOSample *= incomingAttFactor; // divido per il numero di muri, perchè l'energia viene divisa in N pezzi
            //Se RIGA successiva viene commentata posso bypassare il materiale
            FOSample = wallFilter.Transform(FOSample);

            //Calcolo il coefficiente simile al reale

            FOSample *= nodeLoss; //Il sample lo moltiplico per l'assorbimento del muro
            outgoingSample = FOSample;
            HalfFOSample = 0.5f * FOSample;

            int i, j;

            for (i = 0; i < numConnections; i++)
            {
                connections[i].posSamp = connections[i].getSampleFromReverseConnection(); //Legge dalla delay_Line in entrata
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
            //scatteringFactor = (2.0f / minCon) - nodeLoss;
            scatteringFactor = ((2.0f / minCon));
            //scatteringFactorDiag = ((2.0f - minCon) / minCon) - -nodeLoss;
            scatteringFactorDiag = ((2.0f / minCon) - 1);
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

        public UnityEngine.Vector3 getPosition()
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
            length = UnityEngine.Vector3.Distance(parent.getPosition(), target.getPosition());
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
            length = UnityEngine.Vector3.Distance(parent.getPosition(), target.getPosition());
            delay.setDelay(delayLine.distanceToDelayTime(length));
        }
    }
}


