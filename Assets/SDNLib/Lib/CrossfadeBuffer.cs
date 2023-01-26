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

        //Inizializzo la DelayLine Stereo
        delayLine = new StereoVariableDelayLine();

    }

    public CrossfadeBuffer(int bufferSize) : this(bufferSize, false){}


    Complex[][] tempWindow;
    Complex[][] old_hrtfs;


    private Complex[][] Util_CopyArrayLinq(Complex[][] source) // jagged array copy
    {
        return source.Select(s => s.ToArray()).ToArray();
    }


    StereoVariableDelayLine delayLine;
    public float[][] getFromBuffer(Complex[] data, Complex[][] hrtfs, float[] delays)
    {
        //resetInput = true; //questa linea bypassa data
        
        if (resetInput)
        {
            for (int i = 0; i < buffSize; i++)
            {
                //data[i].Re = (Mathf.Sin(2 * Mathf.PI * 2 * i / (buffSize)) + Mathf.Sin(2 * Mathf.PI * i / (buffSize)))/2;
                data[i].Re = Mathf.Sin(2 * Mathf.PI * 2 * i / (buffSize)) * 0.5;
                //data[i].Re = 1;
            }
        }

        //Debug.Log(delays[1]);

        for (int i = 0; i < tempWindow.Length; i++)
        {
            //MODIFICATO!!
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
            try
            {
                Complex tmp1 = tempWindow[0][i];
                Complex tmp2 = tmp1 * hrtfs[0][i] * buffSize;
                tempWindow[1][i] = tmp2;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                Debug.Log("Buffsize = " + buffSize);
                Debug.Log(hrtfs[0].Length);
                
            }
            tempWindow[2][i] = tempWindow[0][i] * hrtfs[1][i] * buffSize;

                ////VECCHIO HRTF
                tempWindow[3][i] = tempWindow[0][i] * old_hrtfs[0][i] * buffSize;
                tempWindow[4][i] = tempWindow[0][i] * old_hrtfs[1][i] * buffSize;
    }
        }

        FourierTransform.FFT(tempWindow[0], FourierTransform.Direction.Backward);

        //Nuova HRTF
        FourierTransform.FFT(tempWindow[1], FourierTransform.Direction.Backward);
        FourierTransform.FFT(tempWindow[2], FourierTransform.Direction.Backward);
        //Vecchia HRTF
        FourierTransform.FFT(tempWindow[3], FourierTransform.Direction.Backward);
        FourierTransform.FFT(tempWindow[4], FourierTransform.Direction.Backward);


        //Salvo i dati elaborati
        //area Crossfade
        for (int i = 0; i < overlap; i++)
        {
          
            outData[0][i] = (outBuffer[0][i]) + (float)tempWindow[3][i].Re * window[overlap + i] + (float)tempWindow[1][i].Re * window[i];
            outData[1][i] = (outBuffer[1][i]) + (float)tempWindow[4][i].Re * window[overlap + i] + (float)tempWindow[2][i].Re * window[i];

            //Prova errata
            //outData[0][i] = (float)tempWindow[3][i].Re * window[overlap + i] + ((float)tempWindow[1][i].Re + outBuffer[0][i]) * window[i];
            //outData[1][i] = (float)tempWindow[4][i].Re * window[overlap + i] + ((float)tempWindow[2][i].Re + outBuffer[1][i]) * window[i];

            //Provo a mettere il buffer alla fine
            //outData[0][i] = (outBuffer[0][i]) + (float)tempWindow[1][i].Re * window[i];
            //outData[1][i] = (outBuffer[1][i]) + (float)tempWindow[2][i].Re * window[i];



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

        if (hrtfs.Length == 2 && hrtfs[0].Length == buffSize)
        {
            for (int i = 0; i < buffSize; i++)
            {
                //NUOVO HRTF
                tempWindow[1][i] = tempWindow[0][i] * hrtfs[0][i] * buffSize;
                tempWindow[2][i] = tempWindow[0][i] * hrtfs[1][i] * buffSize;

            }

        }

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

            //Bypasso temporaneamente
            //outBuffer[0][i] = (float)tempWindow[1][i + buffSize/2].Re;
            //outBuffer[1][i] = (float)tempWindow[2][i + buffSize/2].Re;
        }

        //effettuo il crossfade, spostato alla fine per poter applicare correttamente il delay
        for (int i = 0; i < overlap; i++)
        {

            //outData[0][i] = (outBuffer[0][i]) + (float)tempWindow[3][i].Re * window[overlap + i] + outData[0][i] * window[i];
            //outData[1][i] = (outBuffer[1][i]) + (float)tempWindow[4][i].Re * window[overlap + i] + outData[1][i] * window[i];

          
            //outData[0][i] = (float)tempWindow[3][i].Re * window[overlap + i] + outData[0][i] * window[i];
            //outData[1][i] = (float)tempWindow[4][i].Re * window[overlap + i] + outData[1][i] * window[i];
        }

        //Copio l'outbuffer corretto
        for (int i = 0; i < buffSize/2; i++)
        {
            /*Canale destro?*/
            outBuffer[0][i] = (float)tempWindow[1][i + buffSize/2].Re;
            /*Canale sinistro?*/
            outBuffer[1][i] = (float)tempWindow[2][i + buffSize/2].Re;
        }


        old_hrtfs = Util_CopyArrayLinq(hrtfs);

        //outData=delayLine.processDelay(outData, 3, 3);

        //Queste linee bypassano tutta la convoluzione con crossfade
        /*for (int i = 0; i < buffSize; i++)
        {
            outData[0][i] = (float)data[i].Re;
            outData[1][i] = (float)data[i].Re;
        }*/

        outData=delayLine.processDelay(outData, (int)delays[0], (int)delays[1]);
        
        return outData;
    }

    /*public float[][] getFromBuffer(Complex[] data, Complex[][] hrtfs)
    {
        return getFromBuffer(data, hrtfs, new float[]{0,0});
    }*/

    public float[][] getFromBuffer(Complex[] data, Complex[][] hrtfs, bool writeTestFile)
    {
        float[][] outData = getFromBuffer(data, hrtfs, new float[]{0,0});
        return outData;
    }

}