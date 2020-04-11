using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SourceManager : MonoBehaviour {

    public GameObject source1;
    public GameObject source2;
    public GameObject source3;
    public GameObject source4;
    public GameObject source5;
    public GameObject source6;

    public int activeSource;

    public int first;
    public bool yesFirst;

    private GameObject[] sources;
    private int idx;

    // experiment source positions
    private Vector3[] positions;

    private bool flag = false;
    
    void Start () {
        sources = new GameObject[] { source1, source2, source3, source4, source5, source6 }; // TO DO: order here for experiment
        idx = 0;
        activeSource = idx + 1;

        positions = new Vector3[6];                              // (azi, ele)
        positions[0] = new Vector3(0.0f, 1.25f, 1.1998f);        // (0, 0.9)
        positions[1] = new Vector3(0.5708f, 1.6f, 0.9886f);      // (30, 17.9)
        positions[2] = new Vector3(0.8314f, 0.51f, -0.48f);      // (120, -36.8)
        positions[3] = new Vector3(0.0f, 2.06f, -0.8667f);       // (180, 43.7)
        positions[4] = new Vector3(-0.5307f, 0.67f, -0.9191f);   // (-150, -27.8)
        positions[5] = new Vector3(-0.9098f, 1.81f, 0.5253f);    // (-60, 28.9)

        for (int i = 0; i < sources.Length; i++)
        {
            sources[i].transform.position = positions[i];
        }

        sources[0].transform.position = positions[4];

        if (yesFirst)
            activateSource(first);

        flag = true;
    }

    public void activateSource(int x)
    {
        // activate wanted source
        sources[x].SetActive(true);
        idx = x;
        activeSource = idx + 1;

        // deactivate others
        for (int i = 0; i < 6; i++)
        {
            if (i != x)
                sources[i].SetActive(false);
        }
    }

    public GameObject getActiveSource()
    {
        return sources[idx];
    }

    public void incrementIdx(int s)
    {
        idx = (idx + s) % 6;
        activeSource = idx + 1;
    }

    public int getIdx()
    {
        return idx;
    }

    public bool getFlag()
    {
        return flag;
    }

    public void setFlagDown()
    {
        flag = false;
    }

    public GameObject getSourceN(int n)
    {
        return sources[n];
    }

    public void setSourceToPos(int posX, int expIdx)
    {
        source1.transform.position = positions[posX];
        // deactivate reflection renderer if expIdx = 1, 3 or 7 -> see storePointWithLazer
        if (expIdx == 1 || expIdx == 3 || expIdx == 7)
        {
            sources[idx].GetComponent<SDN>().doHrtfReflections = false;
            sources[idx].GetComponent<SDN>().doLateReflections = false;
        }
        else
        {
            sources[idx].GetComponent<SDN>().doHrtfReflections = true;
            sources[idx].GetComponent<SDN>().doLateReflections = true;
        }
    }
}
