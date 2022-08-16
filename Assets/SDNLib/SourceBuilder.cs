using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SourceBuilder : MonoBehaviour
{
    public bool wantSphere = true;
    public AudioClip audioClip;
    public GameObject listener;
    public bool EnableDraw = true;

    public Material directSMat;
    public Material reflectionSMat;
    public Material junctionMat;


    private int i = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void CreateSource() {
        i++;
        //Floor;
        GameObject src;
        if (wantSphere)
        {
            src = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            src.name = "Source" + i;
            SphereCollider sc = src.GetComponent<SphereCollider>();
            DestroyImmediate(sc);
        }
        else
        {
            src = new GameObject("Source" + i);
        }
        src.transform.localPosition = new Vector3(0, 0.1f, 0);
        src.transform.parent = transform;
        src.AddComponent<AudioSource>();
        src.GetComponent<AudioSource>().clip = audioClip;
        src.GetComponent<AudioSource>().loop = true;
        //ADD SDN Stuff
        src.AddComponent<SDN>();
        src.GetComponent<SDN>().listener = listener;
        if (EnableDraw)
        {
            src.AddComponent<SDNDraw>();
            SDNDraw s = src.GetComponent<SDNDraw>();
            s.directSoundMat = directSMat;
            s.reflectionMat = reflectionSMat;
            s.junctionMat = junctionMat;
            s.drawNetwork = true;
            s.showDistances = true;
        }
        src.AddComponent<HRTFmanager>();
        src.GetComponent<HRTFmanager>().listener = listener;
        //src.AddComponent<Snowman>();
        //src.GetComponent<Snowman>().listener = listener;
        //src.GetComponent<Snowman>().enabled = false;


    }
}
