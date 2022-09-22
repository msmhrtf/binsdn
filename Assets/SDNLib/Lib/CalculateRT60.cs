using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CalculateRT60 : MonoBehaviour
{

    int buffersize = 0;
    int sampleRate = 0;

    // Start is called before the first frame update
    void Start()
    {
        //string path = "Assets/Resources/test.txt";
        //StreamWriter wr = new StreamWriter(path, false);
        //wr.Close();

        AudioConfiguration AC = AudioSettings.GetConfiguration();
        buffersize = AC.dspBufferSize;
        sampleRate = AC.sampleRate;

        source = GetComponent<AudioSource>();

    }

    AudioSource source;
    public AudioClip[] sounds;
    string clipname = "";

    bool startSimulation = false;
    public int WaitingTime = 5;
    float elapsed = 0;

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.A))
        //{
        //    startSimulation = true;
        //    elapsed = WaitingTime;
        //}

        if (Input.GetKeyDown(KeyCode.Q))
        {
            source.clip = sounds[0];
            source.Play();
            started = true;
            current = 0;
            clipname = source.clip.name;
 //           wr = new StreamWriter(path+clipname+".txt", false);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            source.clip = sounds[1];
            source.Play();
            started = true;
            current = 0;
            clipname = source.clip.name;
 //           wr = new StreamWriter(path + clipname + ".txt", false);

        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            source.clip = sounds[2];
            source.Play();
            started = true;
            current = 0;
            clipname = source.clip.name;
//            wr = new StreamWriter(path + clipname + ".txt", false);

        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            source.clip = sounds[3];
            source.Play();
            started = true;
            current = 0;
            clipname = source.clip.name;
 //           wr = new StreamWriter(path + clipname + ".txt", false);

        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            source.clip = sounds[4];
            source.Play();
            started = true;
            current = 0;
            clipname = source.clip.name;
//            wr = new StreamWriter(path + clipname + ".txt", false);

        }


        //if (startSimulation) {
        //    elapsed += Time.deltaTime;
        //    if (elapsed >= WaitingTime)
        //    {
        //        source.Play();
        //        started = true;
        //        current = 0;
        //        elapsed = 0;
                
        //    }
        //}
    }
    StreamWriter wr;
    string path = "Assets/Resources/";
    

    bool started = false;
    float current = 0;
    float maxDb = 0;
    void OnAudioFilterRead(float[] data, int channels) // audio processing by buffer
    {
        if (started) {
            //current = Time.time;
            //Debug.Log(data.Length);
            //Debug.Log("entrato!");


            float max = data[0];
            for (int i = 0; i < data.Length; i++)
            {
                //if (i % 2 == 0)
                //{
                    
                //    wr.WriteLine(data[i]);
                    
                //}

                if (data[i] > max)
                {
                    max = data[i];
                }
            }

            //to DB
            float dB;
            if (max != 0)
                dB = 20.0f * Mathf.Log10(max);
            else
                dB = -144.0f;


            if (current == 0)
            {
                maxDb = dB;
                //Debug.Log("Picco massimo " + maxDb);
            }
            else {
                if (dB > maxDb) {
                    maxDb = dB;
                    //Debug.Log("Cambiato picco massimo massimo " + maxDb  +  "--" + current);
                    current = 0;
                }
                if ((maxDb - 80) > dB)
                {
                    //Debug.Log("Picco Trovato a sample " + current);
                    Debug.Log("RT60 at " + clipname +" = " + (0.0f + (current * buffersize)/sampleRate) + "sec");
                    started = false;
                    current = 0;
                    //wr.Close();
                }
            }

            //Debug.Log(dB);
            current++;
        }
    }

}
