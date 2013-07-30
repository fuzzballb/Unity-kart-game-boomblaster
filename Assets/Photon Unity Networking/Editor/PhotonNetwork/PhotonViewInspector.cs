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
    public override void OnInspectorGUI()
    {
        EditorGUIUtility.LookLikeInspector();
        EditorGUI.indentLevel = 1;

        PhotonView mp = (PhotonView)this.target;
        bool isProjectPrefab = EditorUtility.IsPersistent(mp.gameObject);



        // Owner
        if (isProjectPrefab)
        {
            EditorGUILayout.LabelField("Owner:", "Set at runtime");
        }
        else if (mp.isSceneView)
        {
            EditorGUILayout.LabelField("Owner:", "Scene");
        }
        else
        {
            PhotonPlayer owner = mp.owner;
            string ownerInfo = (owner != null) ? owner.name : "<no PhotonPlayer found>";

            if (string.IsNullOrEmpty(ownerInfo))
            {
                ownerInfo = "<no playername set>";
            }

            EditorGUILayout.LabelField("Owner:", "[" + mp.ownerId + "] " + ownerInfo);
        }



        // View ID
        if (isProjectPrefab)
        {
            EditorGUILayout.LabelField("View ID", "Set at runtime");
        }
        else if (EditorApplication.isPlaying)
        {
            EditorGUILayout.LabelField("View ID", mp.viewID.ToString());
        }
        else
        {
            int idValue = EditorGUILayout.IntField("View ID [0.."+(PhotonNetwork.MAX_VIEW_IDS-1)+"]", mp.viewID);
            mp.viewID = idValue;
        }



        // Locally Controlled
        if (EditorApplication.isPlaying)
        {
            string masterClientHint = PhotonNetwork.isMasterClient ? "(master)" : "";
            EditorGUILayout.Toggle("Controlled locally: " + masterClientHint, mp.isMine);
        }



        // Observed Item    
        EditorGUILayout.BeginHorizontal();

        // Using a lower version then 3.4? Remove the TRUE in the next line to fix an compile error
        string typeOfObserved = string.Empty;
        if (mp.observed != null)
        {
            int firstBracketPos = mp.observed.ToString().LastIndexOf('(');
            if (firstBracketPos > 0)
            {
                typeOfObserved = mp.observed.ToString().Substring(firstBracketPos);
            }
        }


        Component componenValue = (Component)EditorGUILayout.ObjectField("Observe: " + typeOfObserved, mp.observed, typeof(Component), true);
        if (mp.observed != componenValue)
        {
            if (mp.observed == null)
            {
                mp.synchronization = ViewSynchronization.Unreliable;    // if we didn't observe anything before, we could observe unreliably now
            }
            if (componenValue == null)
            {
                mp.synchronization = ViewSynchronization.Off;
            }

            mp.observed = componenValue;
        }

        EditorGUILayout.EndHorizontal();


        
        // ViewSynchronization (reliability)
        if (mp.synchronization == ViewSynchronization.Off)
        {
            GUI.color = Color.grey;
        }

        ViewSynchronization vsValue = (ViewSynchronization)EditorGUILayout.EnumPopup("Observe option:", mp.synchronization);
        if (vsValue != mp.synchronization)
        {
            mp.synchronization = vsValue;
            if (mp.synchronization != ViewSynchronization.Off && mp.observed == null)
            {
                EditorUtility.DisplayDialog("Warning", "Setting the synchronization option only makes sense if you observe something.", "OK, I will fix it.");
            }
        }



        // Serialization
        // show serialization options only if something is observed
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



        // Cleanup: save and fix look
        if (GUI.changed)
        {
            EditorUtility.SetDirty(mp);
            PhotonViewHandler.HierarchyChange();  // TODO: check if needed
        }

        GUI.color = Color.white;
        EditorGUIUtility.LookLikeControls();
    }

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
}
