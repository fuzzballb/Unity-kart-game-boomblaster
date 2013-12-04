using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(FXCamera))]
public class FXCameraEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //FXCamera fxCamera = (FXCamera)target;

        //if (fxCamera.gameObject.GetComponents<FXTexture>().Length == 0)
        //    EditorGUILayout.HelpBox("No FX Textures found, please add at least one FX Texture to this GameObject.", MessageType.Warning);

        //if (GUI.changed)
        //{
        //    EditorUtility.SetDirty(fxCamera);
        //    System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
        //    System.Type type = assembly.GetType("UnityEditor.GameView");
        //    EditorWindow gameview = EditorWindow.GetWindow(type);
        //    gameview.Repaint();
        //}
    }
}