using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(WallFiltAndGain))]
[System.Serializable]
public class Wall_class: Editor
{
    string[] options;

    // Start is called before the first frame update
    void OnEnable()
    {
//        WallFiltAndGain myTarget = (WallFiltAndGain)target;
        options = AudioMaterials.getMaterialList();
//        Debug.Log("options: " + myTarget.wall_material);
    }


    //enum walltypes {concrete, carpet, glass, gypsum, vynil, wood, rockfon};

    public override void OnInspectorGUI()
    {
        WallFiltAndGain myTarget = (WallFiltAndGain)target;

        myTarget.wall_absorption_coeff = EditorGUILayout.FloatField("Wall Absorption Coefficient", myTarget.wall_absorption_coeff);

        //string[] options = new string[]
        // {
        //     "concrete", "carpet", "glass", "gypsum","vynil", "wood", "rockfon", "priviwood", "butterworthLow", "allpass"
        // };

        int index = 0;
        for(int i=0; i<options.Length; i++) {
            if (options[i].Equals(myTarget.wall_material)) {
                index = i;
            }
        }

        
        myTarget.wall_material = options[EditorGUILayout.Popup("Wall Material", index,options)];

        if (GUI.changed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        
    }
}
