using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(FXPostProcess), true)]
public class FXPostProcessEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var fxPostProcess = (FXPostProcess)target;

        var newMaterial = (Material)EditorGUILayout.ObjectField("Material", fxPostProcess.Material, typeof(Material), false);
        if (newMaterial != fxPostProcess.Material)
        {
            fxPostProcess.Assignments.Clear();
            fxPostProcess.Material = newMaterial;
        }
        if (!fxPostProcess.Material)
            return;

        var matEditor = Editor.CreateEditor(fxPostProcess.Material);
        var fxMatEditor = matEditor as FXMaterialEditor;
        if (fxMatEditor != null)
            fxMatEditor.DisplayHelpTexts = false;
        matEditor.OnInspectorGUI();
        Object.DestroyImmediate(matEditor);
    }
}