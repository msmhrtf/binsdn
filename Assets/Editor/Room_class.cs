using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoomBuilder))]
public class Room_class: Editor
{

    // Start is called before the first frame update
    void OnEnable()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnInspectorGUI()
    {
        RoomBuilder myTarget = (RoomBuilder)target;

        myTarget.width = EditorGUILayout.FloatField("Width", myTarget.width);
        myTarget.height = EditorGUILayout.FloatField("Height", myTarget.height);
        myTarget.depth = EditorGUILayout.FloatField("Depth", myTarget.depth);
        myTarget.showWalls = EditorGUILayout.Toggle("Show Walls", myTarget.showWalls);
        if (GUILayout.Button("Build Room"))
        {
            myTarget.CreatePlane();
        }

//        SourceManager sourceManager = (SourceManager)EditorGUILayout.ObjectField("Source Manager:",myTarget.sourceManager, typeof(SourceManager), true);
//        myTarget.sourceManager = sourceManager;
    }
}
