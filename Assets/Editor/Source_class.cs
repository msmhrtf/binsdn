using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SourceBuilder))]
public class Source_class: Editor
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
        SourceBuilder myTarget = (SourceBuilder)target;

        myTarget.wantSphere = EditorGUILayout.Toggle("Create Sphere", myTarget.wantSphere);
        myTarget.audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip:", myTarget.audioClip, typeof(AudioClip), true);
        myTarget.EnableDraw = EditorGUILayout.Toggle("Insert SDN Draw", myTarget.EnableDraw);
        bool okdraw = true;
        if (myTarget.EnableDraw) {
            myTarget.directSMat = (Material)EditorGUILayout.ObjectField("Direct Sound Mat:", myTarget.directSMat, typeof(Material), true);
            myTarget.reflectionSMat = (Material)EditorGUILayout.ObjectField("Reflection Mat:", myTarget.reflectionSMat, typeof(Material), true);
            myTarget.junctionMat = (Material)EditorGUILayout.ObjectField("Junction Mat:", myTarget.junctionMat, typeof(Material), true);
            if (myTarget.directSMat == null || myTarget.reflectionSMat==null || myTarget.junctionMat==null) {
                EditorGUILayout.HelpBox("Please Insert All the Materials", MessageType.Error);
                okdraw = false;
            }
        }


        myTarget.listener= (GameObject)EditorGUILayout.ObjectField("Object With Listener:", myTarget.listener, typeof(GameObject), true);
        
        bool haveListener = (myTarget.listener != null && myTarget.listener.GetComponent<AudioListener>() != null);
        bool haveSDNEnvConfig = (myTarget.listener != null && myTarget.listener.GetComponent<SDNEnvConfig>() != null);

        if (haveListener && haveSDNEnvConfig)
        {
            if (okdraw) { if (GUILayout.Button("Build Source")) myTarget.CreateSource(); }
        }
        else
        {
            EditorGUILayout.HelpBox("Listener Object MUST contain audio Listener AND SDNEnvConfig!", MessageType.Error);
        }

    }
}
