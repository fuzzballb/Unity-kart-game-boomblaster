using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(FXCaptureScreen), true)]
public class FXCaptureScreenEditor : Editor
{
    private bool displayTexture;
    public void OnEnable()
    {
        FXMaterialHelper.ResetCache();
        FXRenderTextureData.OnApply += OnApply;
    }

    public override void OnInspectorGUI()
    {
        var fxScreenCapture = (FXCaptureScreen)target;

        EditorGUILayout.HelpBox("This component captures the content of the rendered image. Move this component up or down the hierarchy to specifiy the exact moment for your needs (For example: Before or after PostProcessing effects).", MessageType.Info);
        fxScreenCapture.RenderTexture = (FXRenderTexture)EditorGUILayout.ObjectField("Target Texture", fxScreenCapture.RenderTexture, typeof(FXRenderTexture), false);
        displayTexture = EditorGUILayout.Foldout(displayTexture, "RenderTexture Details");
        if (displayTexture)
        {
            EditorGUI.indentLevel++;
            var editor = Editor.CreateEditor(fxScreenCapture.RenderTexture);
            editor.OnInspectorGUI();
            Object.DestroyImmediate(editor);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        if (GUI.changed)
        {
            //var cameras = Camera.allCameras;
            //if (cameras.Length > 0)
            //    cameras[0].transform.Translate(Vector3.zero);
            //EditorUtility.SetDirty(fxScreenCapture);
            //SceneView.RepaintAll();
            //this.Repaint();
            System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            System.Reflection.MethodInfo repaintAll = T.GetMethod("RepaintAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            repaintAll.Invoke(null, null);
        }
    }

    void OnDisable()
    {
        FXRenderTextureData.OnApply -= OnApply;
    }

    void OnApply(FXRenderTextureData data)
    {
        Repaint();
    }
}