using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class CopyWaterMaterialEditor : WaterMaterialEditor
{
    public override void OnInspectorGUI()
    {
        if (!isVisible)
        {
            base.OnInspectorGUI();
            return;
        }

        Material targetMat = target as Material;

        var sourceMaterial = (Material)EditorGUILayout.ObjectField("Copy From", null, typeof(Material), false);
        if (sourceMaterial)
        {
            WaterHelper.CopyWaterProperties(sourceMaterial, targetMat);
        }

        base.OnInspectorGUI();
    }
}