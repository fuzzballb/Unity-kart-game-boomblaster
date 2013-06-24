// ----------------------------------------------------------------------------
// <copyright file="PhotonViewInspector.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   Custom inspector for the PhotonView component.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhotonView))]
public class PhotonViewInspector : Editor
{
    private static GameObject GetPrefabParent(GameObject mp)
    {
        #if UNITY_2_6_1 || UNITY_2_6 || UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4
        // Unity 3.4 and older use EditorUtility
        return (EditorUtility.GetPrefabParent(mp) as GameObject);
        #else
        // Unity 3.5 uses PrefabUtility
        return PrefabUtility.GetPrefabParent(mp) as GameObject;
        #endif
    }

    public override void OnInspectorGUI()
    {
        EditorGUIUtility.LookLikeInspector();
        EditorGUI.indentLevel = 1;

        PhotonView mp = (PhotonView)this.target;
        bool isProjectPrefab = EditorUtility.IsPersistent(mp.gameObject);


        // OWNER
        if (isProjectPrefab)
        {
            EditorGUILayout.LabelField("Owner:", "Set at runtime");
        }
        else if (mp.isSceneView)
        {
            EditorGUILayout.LabelField("Owner:", "Scene");
        }
        else if (mp.owner == null)
        {
            EditorGUILayout.LabelField("Owner:", "null, disconnected?");
        }
        else
        {
            EditorGUILayout.LabelField("Owner:", "[" + mp.ownerId + "] " + mp.owner);
        }


        // View ID
        if (isProjectPrefab)
        {
            EditorGUILayout.LabelField("View ID", "Set at runtime");
        }
        else if (EditorApplication.isPlaying)
        {
            if (mp.ownerId != 0 && mp.owner != null)
            {
                EditorGUILayout.LabelField("View ID", "[" + mp.ownerId + "] " + mp.viewID);
            }
            else
            {
                EditorGUILayout.LabelField("View ID", mp.viewID + string.Empty);
            }
        }
        else
        {
            int newId = EditorGUILayout.IntField("View ID [0.."+(PhotonNetwork.MAX_VIEW_IDS-1)+"]", mp.viewID);
            if (newId != mp.viewID)
            {
                mp.viewID = newId;
                EditorUtility.SetDirty(mp);
                PhotonViewHandler.HierarchyChange();
            }
        }


        // OBSERVING    
        EditorGUILayout.BeginHorizontal();

        // Using a lower version then 3.4? Remove the TRUE in the next line to fix an compile error
        string title = string.Empty;
        int firstOpen = 0;
        if (mp.observed != null)
        {
            firstOpen = mp.observed.ToString().IndexOf('(');
        }

        if (firstOpen > 0)
        {
            title = mp.observed.ToString().Substring(firstOpen - 1);
        }

        mp.observed = (Component)EditorGUILayout.ObjectField("Observe: " + title, mp.observed, typeof(Component), true);
        if (GUI.changed)
        {
            PhotonViewHandler.HierarchyChange();  // TODO: check if needed
            if (mp.observed != null)
            {
                mp.synchronization = ViewSynchronization.ReliableDeltaCompressed;
            }
            else
            {
                mp.synchronization = ViewSynchronization.Off;
            }
        }

        EditorGUILayout.EndHorizontal();

        if (mp.synchronization == ViewSynchronization.Off)
        {
            GUI.color = Color.grey;
        }

        mp.synchronization = (ViewSynchronization)EditorGUILayout.EnumPopup("Observe option:", mp.synchronization);
        if (GUI.changed)
        {
            PhotonViewHandler.HierarchyChange();  // TODO: check if needed
            if (mp.synchronization != ViewSynchronization.Off && mp.observed == null)
            {
                EditorUtility.DisplayDialog("Warning", "Setting the synchronization option only makes sense if you observe something.", "OK, I will fix it.");
            }
        }

        if (mp.observed != null)
        {
            Type type = mp.observed.GetType();
            if (type == typeof(Transform))
            {
                mp.onSerializeTransformOption = (OnSerializeTransform)EditorGUILayout.EnumPopup("Serialization:", mp.onSerializeTransformOption);
            }
            else if (type == typeof(Rigidbody))
            {
                mp.onSerializeRigidBodyOption = (OnSerializeRigidBody)EditorGUILayout.EnumPopup("Serialization:", mp.onSerializeRigidBodyOption);
            }
        }

        GUI.color = Color.white;
        EditorGUIUtility.LookLikeControls();
    }
}
