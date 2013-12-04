using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(FXTexture), true)]
public class FXTextureEditor : Editor
{
    private bool displayTexture;
    private bool displayCameras;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        FXTexture fxTexture = (FXTexture)target;

        displayCameras = EditorGUILayout.Foldout(displayCameras, "Rendering Cameras");
        if (displayCameras)
        {
            EditorGUILayout.HelpBox("Cameras which will provide to the RenderTexture.", MessageType.Info);
            EditorGUI.indentLevel++;
            var hasNullCamera = false;
            foreach (var cam in fxTexture.Cameras)
            {
                cam.Camera = (Camera)EditorGUILayout.ObjectField("Camera", cam.Camera, typeof(Camera), true);
                if (cam.Camera == null)
                    hasNullCamera = true;
                EditorGUI.indentLevel++;
                LayerDrawer.HandleLayer("Culling Mask", null, ref cam.CullingMask);
                EditorGUI.indentLevel--;
            }
            if (hasNullCamera)
                fxTexture.Cameras = fxTexture.Cameras.Where(cam => cam.Camera != null).ToArray();
            var newCamera = (Camera)EditorGUILayout.ObjectField("Add New Camera", null, typeof(Camera), true);
            if (newCamera)
            {
                fxTexture.Cameras = fxTexture.Cameras.Concat(new FXTexture.FXTextureCamera[]
                {
                    new FXTexture.FXTextureCamera()
                    {
                        Camera = newCamera
                    }
                }).ToArray();
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        fxTexture.RenderTexture = (FXRenderTexture)EditorGUILayout.ObjectField("Target Texture", fxTexture.RenderTexture, typeof(FXRenderTexture), false);
        displayTexture = EditorGUILayout.Foldout(displayTexture, "RenderTexture Details");
        if (displayTexture)
        {
            EditorGUI.indentLevel++;
            var editor = Editor.CreateEditor(fxTexture.RenderTexture);
            editor.OnInspectorGUI();
            Object.DestroyImmediate(editor);
            EditorGUI.indentLevel--;
        }
    }

    void OnEnable()
    {
        FXRenderTextureData.OnApply += OnApply;
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