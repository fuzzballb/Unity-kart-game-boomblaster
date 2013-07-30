using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Collections;
using UnityEditor;
using Debug = UnityEngine.Debug;

[CustomEditor(typeof(ServerSettings))]
public class ServerSettingsInspector : Editor 
{
	// Use this for initialization
	void Start () {
	     
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public override void OnInspectorGUI()
    {
        EditorGUIUtility.LookLikeInspector();
        DrawDefaultInspector();

        GUILayout.BeginHorizontal();
        GUILayout.Space(30);
        if (GUILayout.Button("Refresh RPCs"))
        {
            PhotonEditor.UpdateRpcList();
            Repaint();
        }
        if (GUILayout.Button("Clear RPCs"))
        {
            PhotonEditor.ClearRpcList();
        }
        GUILayout.EndHorizontal();

        //SerializedProperty sp = serializedObject.FindProperty("RpcList");
        //EditorGUILayout.PropertyField(sp, true);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

}
