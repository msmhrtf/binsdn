using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoomBuilder))]
[System.Serializable]
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

    string[] options = new string[]
         {
             "concrete", "carpet", "glass", "gypsum","vynil", "wood", "rockfon"
         };

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

        myTarget.wallabs = EditorGUILayout.FloatField("Wall Absorption Coefficient", myTarget.wallabs);

        if (GUILayout.Button("Update Wall Absorption Coeff"))
        {
            myTarget.ModifyAbsorptionCoefficient();
        }

        //myTarget.wall_material = options[EditorGUILayout.Popup("Wall Material", myTarget.index, options)];
        //        SourceManager sourceManager = (SourceManager)EditorGUILayout.ObjectField("Source Manager:",myTarget.sourceManager, typeof(SourceManager), true);
        //        myTarget.sourceManager = sourceManager;
    }
}
