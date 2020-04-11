using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour {

    public GameObject room1;
    public GameObject room2;

    private GameObject sources;

    public int room_num;

    private bool updateBoundaryFlag = false; // flag to update boundary in SDN

    Scene m_Scene;
    string sceneName;

    // Use this for initialization
    void Start () {
        //if (room1.activeSelf)
        //    room_num = 1;
        //else
        //    room_num = 2;

        if (room_num == 1)
        {
            room1.SetActive(true);
            room2.SetActive(false);
        }
        else if (room_num == 2)
        {
            room2.SetActive(true);
            room1.SetActive(false);
        }
        else
            Debug.Log("SELECT ROOM !! ");

        sources = GameObject.Find("Sources");
	}
	
	void Update () {

        if (Input.GetKeyDown(KeyCode.O))
        {
            room1.SetActive(true);
            room2.SetActive(false);
            room_num = 1;
            setBoundaryInSDN(room1);
            updateBoundaryFlag = true;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            room2.SetActive(true);
            room1.SetActive(false);
            room_num = 2;
            setBoundaryInSDN(room2);
            updateBoundaryFlag = true;
        }

    }

    public void setBoundaryInSDN(GameObject R)
    {
        //for (int i = 0; i < 6; i++)
        //{
        //    sources.GetComponent<SourceManager>().getSourceN(i).GetComponent<SDN>().setBoundary(R);
        //}

        //sources = GameObject.Find("Sources");

        m_Scene = SceneManager.GetActiveScene();
        sceneName = m_Scene.name;

        if (sceneName == "LocalisationTest")
        {
            // update geometry
            sources.GetComponent<SourceManager>().getSourceN(0).GetComponent<SDN>().setBoundary(R);
            // update wall properties
            sources.GetComponent<SourceManager>().getSourceN(0).GetComponent<SDN>().updateWallMaterialFilter();
        }
        else
        {

            for (int i = 0; i < 3; i++)
            {
                if (sources.GetComponent<SourceManager2>().getSourceN(i).activeSelf)
                {
                    sources.GetComponent<SourceManager2>().getSourceN(i).GetComponent<SDN>().setBoundary(R);
                    sources.GetComponent<SourceManager2>().getSourceN(i).GetComponent<SDN>().updateWallMaterialFilter();
                }
                else
                {
                    sources.GetComponent<SourceManager2>().getSourceN(i).SetActive(true);
                    sources.GetComponent<SourceManager2>().getSourceN(i).GetComponent<SDN>().setBoundary(R);
                    sources.GetComponent<SourceManager2>().getSourceN(i).GetComponent<SDN>().updateWallMaterialFilter();
                    sources.GetComponent<SourceManager2>().getSourceN(i).SetActive(false);
                }
            }

            Debug.Log("yes !!!!! room change !!!! ");
        }



        //sources.GetComponent<SourceManager>().getSourceN(0).GetComponent<SDN>().setHaveNewRef();

        //sources.GetComponent<SourceManager>().getSourceN(0).GetComponent<reflectionFinder>().updateAllPaths();
    }

    public GameObject getActiveRoom()
    {
        if (room_num == 1)
            return room1;
        else
            return room2;
    }

    //public bool getFlag()
    //{
    //    return updateBoundaryFlag;
    //}

    //public void setFlag(bool bi)
    //{
    //    updateBoundaryFlag = bi;
    //} 
}
