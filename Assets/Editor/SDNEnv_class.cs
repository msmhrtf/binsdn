using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(SDNEnvConfig))]
[System.Serializable]
public class SDNEnv_class: Editor
{

    // Start is called before the first frame update
    void OnEnable()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    string[] buffers = {"64","128","256","512","1024","2048","4096"};
    string[] sampleRates = {"44100","48000"};
    string[] idSource = {"Resources", "ApplicationDataPath"};

    public override void OnInspectorGUI()
    {
        SDNEnvConfig myTarget = (SDNEnvConfig)target;

        //myTarget.BuffSize = EditorGUILayout.FloatField("Buff", myTarget.BufferSize);

        int indexbs = 0;
        for(int i=0; i<buffers.Length; i++) {
            if (int.Parse(buffers[i]) == myTarget.BufferSize) {
                indexbs = i;
            }
        }

        int indexsr = 0;
        for(int i=0; i<sampleRates.Length; i++) {
            if (int.Parse(sampleRates[i]) == myTarget.SystemSampleRate) {
                indexsr = i;
            }
        }

        myTarget.BufferSize = int.Parse(buffers[EditorGUILayout.Popup("Buffer Size", indexbs,buffers)]);
        myTarget.SystemSampleRate = int.Parse(sampleRates[EditorGUILayout.Popup("Sample Rate", indexsr,sampleRates)]);

        myTarget.UsePersonalizedSDN = EditorGUILayout.Toggle("Use Personalised SDN", myTarget.UsePersonalizedSDN);

        if(myTarget.UsePersonalizedSDN){
            int indexTF = myTarget.UsePersistentDataPath?1:0;
            indexTF = EditorGUILayout.Popup("HRTF location:", indexTF,idSource);
            myTarget.UsePersistentDataPath = (indexTF==1?true:false);

            if(myTarget.UsePersistentDataPath){
                myTarget.UseNewestSubject = EditorGUILayout.Toggle("Use Newest Subject", myTarget.UseNewestSubject);
            }else{
                myTarget.UseNewestSubject = false;
            }
            if(!myTarget.UseNewestSubject){
                myTarget.CIPIC = EditorGUILayout.TextField("Subject ID (CIPIC): ", myTarget.CIPIC);
            }
        }
        if (GUI.changed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

    }
}