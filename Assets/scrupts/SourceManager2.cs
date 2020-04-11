using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SourceManager2 : MonoBehaviour {

    public GameObject source1;
    public GameObject source2;
    public GameObject source3;

    // scenario manager
    public int scenario;
    private List<int[][]> scenarios; // 8x3 matrix containing the source position indexes to be rendered
    private int scene;

    private GameObject[] sources;

    // experiment source positions
    private Vector3[] positions;

    private AudioSource[] audioData;
    private ulong fs;
    private AudioClip[] audioClips;

    void Start () {

        sources = new GameObject[] { source1, source2, source3 }; // TO DO: order here for experiment
        scene = 0;

        audioData = new AudioSource[sources.Length];
        audioClips = new AudioClip[] {(AudioClip)Resources.Load("male101"), // speech
                                      (AudioClip)Resources.Load("female10"),
                                      (AudioClip)Resources.Load("male102"),
                                      (AudioClip)Resources.Load("noise1"), // noise
                                      (AudioClip)Resources.Load("noise2"),
                                      (AudioClip)Resources.Load("noise3")};

        positions = new Vector3[6];                              // (azi, ele)
        positions[0] = new Vector3(0.0f, 1.25f, 1.1998f);        // (0, 0.9)
        positions[1] = new Vector3(0.5708f, 1.6f, 0.9886f);      // (30, 17.9)
        positions[2] = new Vector3(0.8314f, 0.51f, -0.48f);      // (120, -36.8)
        positions[3] = new Vector3(0.0f, 2.06f, -0.8667f);       // (180, 43.7)
        positions[4] = new Vector3(-0.5307f, 0.67f, -0.9191f);   // (-150, -27.8)
        positions[5] = new Vector3(-0.9098f, 1.81f, 0.5253f);    // (-60, 28.9)

        // initiate scenarios
        scenarios = new List<int[][]>();
        for (int i = 0; i < 16; i++) {
            scenarios.Add(new int[4][]);
        }

        scenarios[0][0] = new int[] { 0, -1, -1}; //
        scenarios[0][1] = new int[] { 0, 1, 3 };
        scenarios[0][2] = new int[] { 0, 1, 4 };
        scenarios[0][3] = new int[] { 1, -1, -1 };

        scenarios[1][0] = new int[] { 5, -1, -1 }; //
        scenarios[1][1] = new int[] { 0, 3, 5 };
        scenarios[1][2] = new int[] { 0, 2, 5 };
        scenarios[1][3] = new int[] { 0, -1, -1 };

        scenarios[2][0] = new int[] { 1, -1, -1 };  //
        scenarios[2][1] = new int[] { 0, 1, 3 };
        scenarios[2][2] = new int[] { 0, 3 , 5 };
        scenarios[2][3] = new int[] { 5, -1, -1 };

        scenarios[3][0] = new int[] { 0, -1, -1 }; //
        scenarios[3][1] = new int[] { 0, 1, 3 };
        scenarios[3][2] = new int[] { 0, 3, 4 };
        scenarios[3][3] = new int[] { 1, -1, -1 };

        scenarios[4][0] = new int[] { 5, -1, -1 }; //
        scenarios[4][1] = new int[] { 0, 2 , 3 };
        scenarios[4][2] = new int[] { 0, 2 , 5 };
        scenarios[4][3] = new int[] { 0, -1, -1 };

        scenarios[5][0] = new int[] { 1, -1, -1 }; //
        scenarios[5][1] = new int[] { 0, 1, 3 };
        scenarios[5][2] = new int[] { 0, 3, 5 };
        scenarios[5][3] = new int[] { 5, -1, -1 };

        scenarios[6][0] = new int[] { 0, -1, -1 }; //
        scenarios[6][1] = new int[] { 0, 2 , 4 };
        scenarios[6][2] = new int[] { 0, 3, 4 };
        scenarios[6][3] = new int[] { 1, -1, -1 };

        scenarios[7][0] = new int[] { 5, -1, -1 }; //
        scenarios[7][1] = new int[] { 0, 4, 5 };
        scenarios[7][2] = new int[] { 0, 1, 4 };
        scenarios[7][3] = new int[] { 0, -1, -1 };

        scenarios[8][0] = new int[] { 1, -1, -1 }; //
        scenarios[8][1] = new int[] { 0, 1 , 4 };
        scenarios[8][2] = new int[] { 0, 2, 5 };
        scenarios[8][3] = new int[] { 2, -1, -1 };

        scenarios[9][0] = new int[] { 0, -1, -1 }; //
        scenarios[9][1] = new int[] { 0, 3, 4 };
        scenarios[9][2] = new int[] { 0, 1, 2 };
        scenarios[9][3] = new int[] { 1, -1, -1 };

        scenarios[10][0] = new int[] { 5, -1, -1 }; //
        scenarios[10][1] = new int[] { 0, 2 , 4};
        scenarios[10][2] = new int[] { 0, 1, 3};
        scenarios[10][3] = new int[] { 0, -1, -1 };

        scenarios[11][0] = new int[] { 1, -1, -1 }; //
        scenarios[11][1] = new int[] { 0, 2, 5 };
        scenarios[11][2] = new int[] { 0, 3, 4 };
        scenarios[11][3] = new int[] { 2, -1, -1 };

        scenarios[12][0] = new int[] { 0, -1, -1 }; //
        scenarios[12][1] = new int[] { 0, 3, 5 };
        scenarios[12][2] = new int[] { 0, 3, 4 };
        scenarios[12][3] = new int[] { 1, -1, -1 };

        scenarios[13][0] = new int[] { 5, -1, -1 }; //
        scenarios[13][1] = new int[] { 0, 1, 3 };
        scenarios[13][2] = new int[] { 0, 2, 4 };
        scenarios[13][3] = new int[] { 0, -1, -1 };

        scenarios[14][0] = new int[] { 1, -1, -1 }; //
        scenarios[14][1] = new int[] { 0, 2, 3 };
        scenarios[14][2] = new int[] { 0, 1, 5 };
        scenarios[14][3] = new int[] { 5, -1, -1 };

        scenarios[15][0] = new int[] { 0, -1, -1 }; //
        scenarios[15][1] = new int[] { 0, 2, 4 };
        scenarios[15][2] = new int[] { 0, 3, 5 };
        scenarios[15][3] = new int[] { 1, -1, -1 };

        // set first sound scene of selected scenario
        for (int i = 0; i < sources.Length; i++)
        {
            // get audio sources tchak tchak
            audioData[i] = sources[i].GetComponent<AudioSource>();

            if (scenarios[scenario][scene][i] >= 0)
            {
                // activate and position
                sources[i].SetActive(true);
                sources[i].transform.position = positions[scenarios[scenario][scene][i]];

                // load audio clip (2 speeches first, 1male 1female)
                audioData[i].clip = audioClips[i];

                //Debug.Log(audioData[i].clip.name);
            }
            else
            {
                sources[i].SetActive(false);
            }
        }
        Debug.Log("Scene number " + (scene + 1) + " - Scenario " + scenario);

        scene++;

    }
	

	void Update () {

        // next scene
        if (Input.GetKeyDown(KeyCode.RightArrow))
        { 
            setNextScene();
        }

        // play audio
        if (Input.GetKeyDown(KeyCode.Space))
        {
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].activeSelf)
                {
                    audioData[i].Play(fs); // delay of 1 sec
                }
            }
        }

    }

    private void setNextScene()
    {

        for (int i = 0; i < sources.Length; i++)
        {
            if (scenarios[scenario][scene][i] >= 0)
            {
                sources[i].SetActive(true);
                sources[i].transform.position = positions[scenarios[scenario][scene][i]];                
            }
            else
            {
                sources[i].SetActive(false);
            }
        }

        int randonFront = Random.Range(0, 2); // random speech selection for single source

        switch (scene)    // TO DO: randomize wisely 
        {
            case 0: // 1 speeches
                audioData[0].clip = audioClips[randonFront];
                break;
            case 1: // 3 noises
                audioData[0].clip = audioClips[3];
                audioData[1].clip = audioClips[4];
                audioData[2].clip = audioClips[5];
                break;
            case 2: // 3 speeches
                audioData[0].clip = audioClips[0];
                audioData[1].clip = audioClips[1];
                audioData[2].clip = audioClips[2];
                break;
            case 3: // 1 noises
                audioData[0].clip = audioClips[4];
                break;
            default:
                Debug.Log("Oops, problem in SourceManager2 scene selection.");
                break;
        }

        Debug.Log("Scene number " + (scene + 1) + " - Scenario " + scenario);

        scene = (scene + 1) % 4;

    }

    public GameObject getSourceN(int n)
    {
        return sources[n];
    }

}
