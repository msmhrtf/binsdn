using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WallFiltAndGain))]
public class Wall_class: Editor
{


    // Start is called before the first frame update
    void OnEnable()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    enum walltypes {concrete, carpet, glass, gypsum, vynil, wood, rockfon};

    public override void OnInspectorGUI()
    {
        WallFiltAndGain myTarget = (WallFiltAndGain)target;

        myTarget.wall_absorption_coeff = EditorGUILayout.FloatField("Wall Absorption Coefficient", myTarget.wall_absorption_coeff);

        string[] options = new string[]
         {
             "concrete", "carpet", "glass", "gypsum","vynil", "wood", "rockfon"
         };

        int index = 0;
        for(int i=0; i<options.Length; i++) {
            if (options[i].Equals(myTarget.wall_material)) {
                index = i;
            }
        }

        
        myTarget.wall_material = options[EditorGUILayout.Popup("Wall Material", index,options)];
        
    }
}
