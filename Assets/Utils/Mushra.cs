using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Mushra : MonoBehaviour
{
    /*

        Real : mando messaggio via HTML
        Use SDN
        Sample Volume Calibration/Normalised
        Personal HRTF vs Kemar
        Wall Absorption with RT60
        Headphone Calibration: cambio di sample
     
     */

    // Start is called before the first frame update
    void Start()
    {
        actualSound = gameObject.GetComponent<SDN>();
        actualClip = gameObject.GetComponent<AudioSource>();
    }

    public void pressButton(int num) {
        Debug.Log("Selezionata opzione: " + num);
    }

    public void StartMode(bool real) {
        if (real)
        {
            StartCoroutine(GetRequest());
        }
    }

    SDN actualSound;
    AudioSource actualClip;

    public AudioClip normalSample;
    public AudioClip headphoneSample;
    public RoomBuilder room;
    public float audioVolume = 1.0f;
    public SDNEnvConfig sdnEnvConfig;

    public float room_width, room_height, room_depth, room_absorption;

    public void StartMode(bool real, bool useSDN, bool calibratedVolume,
        bool personalHRTF, bool correctWallAbsorption, bool headPhoneCalibration) {

        if (!real)
        {
            if (calibratedVolume)
            {
                actualClip.volume = audioVolume;
            }
            else
            {
                actualClip.volume = 1.0f;
            }

            if (personalHRTF)
            {
                sdnEnvConfig.SwitchHRTF(sdnEnvConfig.CIPIC);
            }
            else {
                sdnEnvConfig.useKemarHRTF();
            }


            //DECIDERE SE USARE L'ALGORITMO SDN oppure NO
                actualSound.doLateReflections = useSDN;

            if (headPhoneCalibration)
            {
                actualClip.clip = headphoneSample;
            }
            else {
                actualClip.clip = normalSample;
            }
        }

        actualClip.Play();
    }

    IEnumerator GetRequest()
    {
        string uri = "https://www.alessandroprivitera.it/CHITEST/StartMusic.php";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            yield return webRequest.SendWebRequest();
        }
    }


    }


/*
 * 
 * using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SendToPrivi : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
       StartCoroutine(GetRequest());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject[] objs1;
    public GameObject[] objs2;

    int state = 0;
    public static bool resetInit = true;

        IEnumerator GetRequest()
    {
        while (true)
        {
            if (resetInit) {
                string uri = "https://www.alessandroprivitera.it/CHITEST/clear.php";
                using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
                {
                    yield return webRequest.SendWebRequest();
                }
                resetInit = false;
                state++;
            }

            if (state == 2)
            {
                foreach (GameObject go in objs2)
                {
                    string uri = "https://www.alessandroprivitera.it/CHITEST/send.php?nome=" + go.name + "&x=" +
                        go.transform.position.x + "&y=" + go.transform.position.y + "&z=" + go.transform.position.z;
                    using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
                    {
                        yield return webRequest.SendWebRequest();
                    }
                }
            }
            else {
                foreach (GameObject go in objs1)
                {
                    string uri = "https://www.alessandroprivitera.it/CHITEST/send.php?nome=" + go.name + "&x=" +
                        go.transform.position.x + "&y=" + go.transform.position.y + "&z=" + go.transform.position.z;
                    using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
                    {
                        yield return webRequest.SendWebRequest();
                        string[] pages = uri.Split('/');
                        int page = pages.Length - 1;
                        switch (webRequest.result)
                        {
                            case UnityWebRequest.Result.ConnectionError:
                            case UnityWebRequest.Result.DataProcessingError:
                                Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                                break;
                            case UnityWebRequest.Result.ProtocolError:
                                Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                                break;
                            case UnityWebRequest.Result.Success:
//                                Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                                break;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
 * 
 */