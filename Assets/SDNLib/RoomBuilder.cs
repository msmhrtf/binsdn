using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomBuilder : MonoBehaviour
{
    public float height = 2.4f, width = 3f, depth= 3f;
    public bool showWalls = true;

    public void CreatePlane()
    {
        while (gameObject.transform.childCount != 0)
        {
            //Debug.Log(child.name);
            GameObject.DestroyImmediate(gameObject.transform.GetChild(0).gameObject);
        }

        //Floor;
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.localPosition = new Vector3(0, 0, 0);
        plane.transform.localScale = new Vector3(width/10, 1, depth / 10);
        plane.GetComponent<MeshRenderer>().enabled = showWalls;
        plane.AddComponent<WallFiltAndGain>();
        plane.name = "Floor";
        plane.transform.parent = gameObject.transform;
        //Ceiling
        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.localPosition = new Vector3(0, height, 0);
        plane.transform.localScale = new Vector3(width / 10, 1, depth / 10);
        plane.transform.Rotate(180f, 0, 0);
        plane.GetComponent<MeshRenderer>().enabled = showWalls;
        plane.AddComponent<WallFiltAndGain>();
        plane.name = "Ceiling";
        plane.transform.parent = gameObject.transform;
        //Front
        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.localPosition = new Vector3(0, height/2, depth/2);
        plane.transform.localScale = new Vector3(width / 10, 1, height/ 10);
        plane.transform.Rotate(-90f, 0, 0);
        plane.GetComponent<MeshRenderer>().enabled = showWalls;
        plane.AddComponent<WallFiltAndGain>();
        plane.name = "Front";
        plane.transform.parent = gameObject.transform;
        //Back
        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.localPosition = new Vector3(0, height / 2, -depth / 2);
        plane.transform.localScale = new Vector3(width / 10, 1, height / 10);
        plane.transform.Rotate(90f, 0, 0);
        plane.GetComponent<MeshRenderer>().enabled = showWalls;
        plane.AddComponent<WallFiltAndGain>();
        plane.name = "Back";
        plane.transform.parent = gameObject.transform;
        //Left
        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.localPosition = new Vector3(-width / 2, height/ 2, 0);
        plane.transform.localScale = new Vector3(height / 10, 1, depth/ 10);
        plane.transform.Rotate(0, 0, -90f);
        plane.GetComponent<MeshRenderer>().enabled = showWalls;
        plane.AddComponent<WallFiltAndGain>();
        plane.name = "Left";
        plane.transform.parent = gameObject.transform;
        //Right
        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.localPosition = new Vector3(width/ 2, height / 2, 0);
        plane.transform.localScale = new Vector3(height / 10, 1, depth / 10);
        plane.transform.Rotate(0, 0, 90f);
        plane.GetComponent<MeshRenderer>().enabled = showWalls;
        plane.AddComponent<WallFiltAndGain>();
        plane.name = "Right";
        plane.transform.parent = gameObject.transform;

    }

    //public GameObject Room;

//    public SourceManager sourceManager;

    // Use this for initialization
    void Start()
    {
//        Debug.Log(this.gameObject.name);
//        setBoundaryInSDN(gameObject);
    }

    public void setBoundaryInSDN(GameObject R)
    {
        //NUOVA VERSIONE
        // update geometry
//        sourceManager.getSourceN(0).GetComponent<SDN>().setBoundary(R);
        // update wall properties
//        sourceManager.getSourceN(0).GetComponent<SDN>().updateWallMaterialFilter();

    }

    public GameObject getActiveRoom()
    {
        //Debug.Log("Richiesta funzione getActiveRoom!!!");
        return this.gameObject;
    }
}
