using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using UnityEngine;
using System;
using AForge.Math;

public class CrossfadeBuffer
{
    float[][] outBuffer;

    int overlap;

    int buffSize;

    /*
     * overlap-> in samples: deve essere un divisore di buffsize


     * WindowType: 
     * 1-hanning
     * 2-hamming
     * 3-blackmann
     */

    public enum WindowType
    {
        hanning, hamming, blackmann, square, triangle, tukey
    };

    float[] window;
    private void createWindow(WindowType type)
    {
        int winS = overlap * 2;
        window = new float[winS];

        for (int n = 0; n < winS; n++)
        {
            switch (type)
            {
                //hanning
                case WindowType.hanning:
                    window[n] = 0.5f * (1f - (float)System.Math.Cos(2f * System.Math.PI * n / (winS - 1)));
                    break;
                //hamming
                case WindowType.hamming:
                    window[n] = 0.54f - 0.46f * ((float)System.Math.Cos(2f * System.Math.PI * n / winS - 1));
                    break;
                case WindowType.blackmann:
                    window[n] = 0.42f - 0.5f * ((float)System.Math.Cos(2f * System.Math.PI * n / winS)) + 0.08f * ((float)System.Math.Cos(4f * System.Math.PI * n / winS));
                    break;
                case WindowType.square:
                    window[n] = 1;
                    break;
                case WindowType.triangle:
                    window[n] = 1 - Math.Abs((n - (winS / 2.0f)) / (winS / 2.0f));
                    break;
                case WindowType.tukey:
                    float alpha = 0.5f;
                    if (n > winS / 2)
                    {
                        window[n] = window[winS - n];
                    }
                    else
                    {
                        if (n < (alpha * winS / 2.0f))
                        {
                            window[n] = (float)(0.5f * (1 - Math.Cos(2 * Math.PI * n / (alpha * winS))));
                        }
                        else
                        {
                            window[n] = 1;
                        }
                    }
                    break;
            }
        }

    }

    bool resetInput;

    public CrossfadeBuffer(int bufferSize, bool bypassWaveform)
    {

        if (bypassWaveform) { Debug.Log("Disattivato reset forma d'onda"); }
        resetInput = bypassWaveform;

        if (bufferSize >= 1024) {
            overlap = 256;  //può essere la lunghezza del crossfade
        }else{
            overlap = bufferSize / 2;
        }

        buffSize = bufferSize;
        
        //Creo i due buffer di uscita
        outBuffer = new float[2][];
        for (int i = 0; i < outBuffer.Length; i++)
        {
            outBuffer[i] = new float[buffSize/2];
        }

        //Due con nuovo hrtf due con vecchio hrtf
        tempWindow = new Complex[5][];
        for (int i = 0; i < tempWindow.Length; i++)
        {
            //MODIFICATO!!
            //            tempWindow[i] = new Complex[buffSize * 2];
            tempWindow[i] = new Complex[buffSize];
        }

        //inizializzo old_hrtf
        old_hrtfs = new Complex[2][];
        for (int i = 0; i < outBuffer.Length; i++)
        {
            //MODIFICATO!!!
            //            old_hrtfs[i] = new Complex[buffSize * 2];
            old_hrtfs[i] = new Complex[buffSize];
        }

        //Creo la finestra
        createWindow(WindowType.hanning);

//        Debug.Log("Buffer size: " + bufferSize + " - " + overlap);

    }

    public CrossfadeBuffer(int bufferSize) : this(bufferSize, false){}


    Complex[][] tempWindow;
    Complex[][] old_hrtfs;


    private Complex[][] Util_CopyArrayLinq(Complex[][] source) // jagged array copy
    {
        return source.Select(s => s.ToArray()).ToArray();
    }
















    public float[][] getFromBuffer(Complex[] data, Complex[][] hrtfs)
    {

        if (resetInput)
        {
            for (int i = 0; i < buffSize; i++)
            {
                //data[i].Re = (Mathf.Sin(2 * Mathf.PI * 2 * i / (buffSize)) + Mathf.Sin(2 * Mathf.PI * i / (buffSize)))/2;
                data[i].Re = Mathf.Sin(2 * Mathf.PI * 2 * i / (buffSize));
                //data[i].Re = 1;
            }
        }

        for (int i = 0; i < tempWindow.Length; i++)
        {
            //MODIFICATO!!
            //tempWindow[i] = new Complex[buffSize * 2];
            tempWindow[i] = new Complex[buffSize];
        }


        //Creo array per i dati in uscita
        float[][] outData = new float[2][];
        for (int i = 0; i < 2; i++)
        {
            outData[i] = new float[buffSize];
        }


        //-------------
        //PRIMA PARTE!
        //-------------
        //Copio la pima metà dei dati su tempWindow[0]
        for (int i = 0; i < buffSize/2; i++)
        {
            tempWindow[0][i] = data[i];
        }

        //FFT

        FourierTransform.FFT(tempWindow[0], FourierTransform.Direction.Forward);
        //MODIFICATO!!!
        //        for (int i = 0; i < buffSize * 2; i++)
        if (hrtfs.Length == 2 && hrtfs[0].Length == buffSize && old_hrtfs[0].Length == buffSize) {
            for (int i = 0; i < buffSize; i++)
            {
                ////NUOVO HRTF
                Complex tmp1 = tempWindow[0][i];
                Complex tmp2 = tmp1 * hrtfs[0][i] * buffSize;
                tempWindow[1][i] = tmp2;

                tempWindow[2][i] = tempWindow[0][i] * hrtfs[1][i] * buffSize;

                ////VECCHIO HRTF
                tempWindow[3][i] = tempWindow[0][i] * old_hrtfs[0][i] * buffSize;
                tempWindow[4][i] = tempWindow[0][i] * old_hrtfs[1][i] * buffSize;
            }
        }
        //else
        //{
        //    Debug.Log("Buffer length non pronta " + hrtfs[0].Length);
        //}

        FourierTransform.FFT(tempWindow[0], FourierTransform.Direction.Backward);

        FourierTransform.FFT(tempWindow[1], FourierTransform.Direction.Backward);
        FourierTransform.FFT(tempWindow[2], FourierTransform.Direction.Backward);

        FourierTransform.FFT(tempWindow[3], FourierTransform.Direction.Backward);
        FourierTransform.FFT(tempWindow[4], FourierTransform.Direction.Backward);


        //Salvo i dati elaborati
        //area Crossfade
        for (int i = 0; i < overlap; i++)
        {
            //outData[0][i] = (outBuffer[0][i] + (float)tempWindow[3][i].Re) * window[overlap + i] + (float)tempWindow[1][i].Re * window[i];
            //outData[1][i] = (outBuffer[1][i] + (float)tempWindow[4][i].Re) * window[overlap + i] + (float)tempWindow[2][i].Re * window[i];

            outData[0][i] = (outBuffer[0][i]) + (float)tempWindow[3][i].Re * window[overlap + i] + (float)tempWindow[1][i].Re * window[i];
            outData[1][i] = (outBuffer[1][i]) + (float)tempWindow[4][i].Re * window[overlap + i] + (float)tempWindow[2][i].Re * window[i];


        }
        //Resto
        for (int i = overlap; i < buffSize; i++)
        {
            outData[0][i] = (float)tempWindow[1][i].Re;
            outData[1][i] = (float)tempWindow[2][i].Re;
        }

        //RIFACCIO LA STESSA COSA CON LA SECONDA PARTE -------------------------

        //Copio la pima metà dei dati su tempWindow[0]
        for (int i = 0; i < buffSize / 2; i++)
        {
            tempWindow[0][i] = data[i+ buffSize/2];
        }

        FourierTransform.FFT(tempWindow[0], FourierTransform.Direction.Forward);


        //Calcolo la FFT e applico le elaborazioni
        //MODIFICATO!!!
        //        for (int i = 0; i < buffSize * 2; i++)
        if (hrtfs.Length == 2 && hrtfs[0].Length == buffSize)
        {
            for (int i = 0; i < buffSize; i++)
            {
                //NUOVO HRTF
                tempWindow[1][i] = tempWindow[0][i] * hrtfs[0][i] * buffSize;
                tempWindow[2][i] = tempWindow[0][i] * hrtfs[1][i] * buffSize;

                //tempWindow[1][i] = tempWindow[0][i];
                //tempWindow[2][i] = tempWindow[0][i];
            }

        }
        //else {
        //    Debug.Log("Errore su HRTF length " + hrtfs[0].Length);
        //}
        FourierTransform.FFT(tempWindow[0], FourierTransform.Direction.Backward);


        FourierTransform.FFT(tempWindow[1], FourierTransform.Direction.Backward);
        FourierTransform.FFT(tempWindow[2], FourierTransform.Direction.Backward);

        //Salvo i dati elaborati
        //area Crossfade
        for (int i = 0; i < buffSize/2; i++)
        {
            float a = outData[0][i + buffSize / 2] + (float)tempWindow[1][i].Re;
            float b = outData[1][i + buffSize / 2] + (float)tempWindow[2][i].Re;
            outData[0][i + buffSize / 2] = a;
            outData[1][i + buffSize / 2] = b;

            /*Canale destro?*/
            outBuffer[0][i] = (float)tempWindow[1][i + buffSize/2].Re;
            /*Canale sinistro?*/
            outBuffer[1][i] = (float)tempWindow[2][i + buffSize/2].Re;
        }
        ////Resto
        //for (int i = overlap; i < buffSize/2; i++)
        //{
        //    float a = outData[0][i + buffSize / 2] + (float)tempWindow[1][i].Re;
        //    float b = outData[1][i + buffSize / 2] + (float)tempWindow[2][i].Re;
        //    outData[0][i + buffSize / 2] = a;
        //    outData[1][i + buffSize / 2] = b;
        //}






        //copio la vecchia hrtf
        old_hrtfs = Util_CopyArrayLinq(hrtfs);

        return outData;
    }
















    //public float[][] getFromBuffer(Complex[] data, Complex[][] hrtfs)
    //{

    //    if (resetInput)
    //    {
    //        for (int i = 0; i < buffSize; i++)
    //        {
    //            //data[i].Re = (Mathf.Sin(2 * Mathf.PI * 2 * i / (buffSize)) + Mathf.Sin(2 * Mathf.PI * i / (buffSize)))/2;
    //            data[i].Re = Mathf.Sin(2 * Mathf.PI * 2 * i / (buffSize));
    //            //data[i].Re = 1;
    //        }
    //    }

    //    for (int i = 0; i < tempWindow.Length; i++)
    //    {
    //        tempWindow[i] = new Complex[buffSize * 2];
    //    }

    //    //Copio la finestra di dati su tempWindow[0]
    //    for (int i = 0; i < buffSize; i++)
    //    {
    //        tempWindow[0][i] = data[i];
    //    }

    //    FourierTransform.FFT(tempWindow[0], FourierTransform.Direction.Forward);


    //    //Calcolo la FFT e applico le elaborazioni
    //    for (int i = 0; i < buffSize * 2; i++)
    //    {
    //        //NUOVO HRTF
    //        tempWindow[1][i] = tempWindow[0][i] * hrtfs[0][i] * buffSize * 2;
    //        tempWindow[2][i] = tempWindow[0][i] * hrtfs[1][i] * buffSize * 2;

    //        //VECCHIO HRTF
    //        tempWindow[3][i] = tempWindow[0][i] * old_hrtfs[0][i] * buffSize * 2;
    //        tempWindow[4][i] = tempWindow[0][i] * old_hrtfs[1][i] * buffSize * 2;
    //    }
    //    FourierTransform.FFT(tempWindow[1], FourierTransform.Direction.Backward);
    //    FourierTransform.FFT(tempWindow[2], FourierTransform.Direction.Backward);

    //    FourierTransform.FFT(tempWindow[3], FourierTransform.Direction.Backward);
    //    FourierTransform.FFT(tempWindow[4], FourierTransform.Direction.Backward);

    //    //Copio i dati in uscita
    //    float[][] outData = new float[2][];
    //    for (int i = 0; i < 2; i++) {
    //        outData[i] = new float[buffSize];
    //    }


    //    //Salvo i dati elaborati
    //    //area Crossfade
    //    for (int i = 0; i < overlap; i++)
    //    {
    //        outData[0][i] = (outBuffer[0][i] + (float)tempWindow[3][i].Re) * window[overlap+i] + (float)tempWindow[1][i].Re * window[i];
    //        outData[1][i] = (outBuffer[1][i] + (float)tempWindow[4][i].Re) * window[overlap+i] + (float)tempWindow[2][i].Re * window[i];

    //        /*Canale destro?*/
    //        outBuffer[0][i] = (float)tempWindow[1][i + buffSize].Re;
    //        /*Canale sinistro?*/
    //        outBuffer[1][i] = (float)tempWindow[2][i + buffSize].Re;
    //    }
    //    //Resto
    //    for (int i = overlap; i < buffSize; i++)
    //    {
    //        outData[0][i] = (float)tempWindow[1][i].Re;
    //        outData[1][i] = (float)tempWindow[2][i].Re;

    //        /*Canale destro?*/
    //        //outBuffer[0][i] = (float)tempWindow[1][i+buffSize].Re;
    //        /*Canale sinistro?*/
    //        //outBuffer[1][i] = (float)tempWindow[2][i + buffSize].Re;
    //    }


    //    //copio la vecchia hrtf
    //    old_hrtfs = Util_CopyArrayLinq(hrtfs);

    //    return outData;
    //}

    public float[][] getFromBuffer(Complex[] data, Complex[][] hrtfs, bool writeTestFile)
    {
        float[][] outData = getFromBuffer(data, hrtfs);
        return outData;
    }


}
