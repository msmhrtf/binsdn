using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class test : MonoBehaviour
{
    void Awake()
    {
        //AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        AudioConfiguration config = AudioSettings.GetConfiguration();
        config.dspBufferSize = 2048;
        AudioSettings.Reset(config);

    }

    float[] hanning;
    void createHanning(int buffSize)
    {
        hanning = new float[buffSize];
        for (int i = 0; i < buffSize; i++)
        {
            hanning[i] = 0.5f * (1f - (float)System.Math.Cos(2f * System.Math.PI * i / (buffSize - 1)));
            //hamming
            //hanning[i] = 0.54f - 0.46f*((float)System.Math.Cos(2f * System.Math.PI * i / buffSize-1));
        }

    }


    AForge.Math.Complex[] SampleSignal1;
    AForge.Math.Complex[] SampleSignal12;
    AForge.Math.Complex[] SampleSignal2;
    void createSampleSignal(int buffSize) {
        SampleSignal1 = new AForge.Math.Complex[buffSize*2];
        SampleSignal12 = new AForge.Math.Complex[buffSize*2];
        SampleSignal2 = new AForge.Math.Complex[buffSize*2];

        for (int i = 0; i < buffSize; i++) {
            SampleSignal1[i].Re = Mathf.Sin(2 * Mathf.PI * 2*i/(buffSize)) + Mathf.Sin(2 * Mathf.PI * i / (buffSize));
            SampleSignal2[i].Re = Mathf.Sin(2 * Mathf.PI* 2*i /(buffSize)) + Mathf.Sin(2 * Mathf.PI * i / (buffSize));
        }


        //OLA della seconda metà
        for (int i = 0; i < buffSize / 2; i++)
        {
            SampleSignal12[i] = SampleSignal1[i + buffSize / 2];
        }


        for (int i = 0; i < buffSize / 2; i++)
        {
            SampleSignal12[i + buffSize / 2] = SampleSignal2[i];
        }

    }

    private void Start()
    {
//        int buffSize = 1024;
        //CircularBuffer cb = new CircularBuffer(buffSize, buffSize/2, CircularBuffer.WindowType.hanning,true);


        //cb.getFromBuffer(new AForge.Math.Complex[buffSize],new AForge.Math.Complex[buffSize][]);
                //cb.WriteFile(cb.getFromBuffer(new AForge.Math.Complex[buffSize])[0]);
        //        cb.WriteFile(cb.getFromBuffer(new AForge.Math.Complex[buffSize])[0]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize]);
        //cb.WriteFile(cb.getFromBuffer(new AForge.Math.Complex[buffSize], new AForge.Math.Complex[buffSize][])[0]);
        //cb.getFromBuffer(new AForge.Math.Complex[buffSize], new AForge.Math.Complex[buffSize][]);
        //cb.WriteFile(cb.getFromBuffer(new AForge.Math.Complex[buffSize], new AForge.Math.Complex[buffSize][])[0]);
    }
    

    //private void Start()
    //{
    //    int buffSize = 1024;
    //    createHanning(buffSize);
    //    createSampleSignal(buffSize);

    //    for (int i = 0; i < hanning.Length; i++)
    //    {
    //        SampleSignal1[i].Re = SampleSignal1[i].Re * hanning[i];
    //        SampleSignal12[i].Re = SampleSignal12[i].Re * hanning[i];
    //        SampleSignal2[i].Re = SampleSignal2[i].Re * hanning[i];
    //    }

    //    AForge.Math.FourierTransform.FFT(SampleSignal1, AForge.Math.FourierTransform.Direction.Forward);
    //    AForge.Math.FourierTransform.FFT(SampleSignal12, AForge.Math.FourierTransform.Direction.Forward);
    //    AForge.Math.FourierTransform.FFT(SampleSignal2, AForge.Math.FourierTransform.Direction.Forward);

    //    for (int i = 0; i < SampleSignal1.Length; i++) {
    //        SampleSignal1[i].Re *= 2;
    //        SampleSignal12[i].Re *= 2;
    //        SampleSignal2[i].Re *= 2;
    //    }

    //    AForge.Math.FourierTransform.FFT(SampleSignal1, AForge.Math.FourierTransform.Direction.Backward);
    //    AForge.Math.FourierTransform.FFT(SampleSignal12, AForge.Math.FourierTransform.Direction.Backward);
    //    AForge.Math.FourierTransform.FFT(SampleSignal2, AForge.Math.FourierTransform.Direction.Backward);


    //    //OLA
    //    for (int i = 0; i < buffSize / 2; i++)
    //    {
    //        SampleSignal1[i + buffSize].Re += SampleSignal2[i].Re;
    //    }

    //    //OLA della seconda metà
    //    for (int i = 0; i < buffSize; i++)
    //    {
    //        SampleSignal1[i + buffSize / 2].Re += SampleSignal12[i].Re;
    //    }




    //    string path = "Assets/Resources/test.txt";
    //    //Write some text to the test.txt file
    //    StreamWriter writer = new StreamWriter(path, false);
    //    for (int i = 0; i < SampleSignal1.Length; i++)
    //    {
    //        writer.WriteLine(SampleSignal1[i].Re.ToString());
    //    }
    //    writer.Close();

    //}

    private void Update()
    {
        
    }

}
