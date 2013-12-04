using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class WaterMaterialEditor : FXMaterialEditor
{
    public override void OnEnable()
    {
        base.OnEnable();

        FXMaterialHelper.ResetCache();
    }

    public override void OnInspectorGUI()
    {
        if (!isVisible)
        {
            base.OnInspectorGUI();
            return;
        }

        Material targetMat = target as Material;
        var name = targetMat.shader.name;
        var lastSlash = name.LastIndexOf('/');
        var path = name;
        if (lastSlash >= 0)
            path = name.Substring(0, lastSlash);
        name = name.Substring(lastSlash + 1);

        var usedKeywords = name.Split('_').ToArray();
        var newKeywords = new List<string>();

        var reflectionState = usedKeywords.Contains("Reflection");
        var refractionState = usedKeywords.Contains("Refraction");
        var dispersionState = usedKeywords.Contains("Dispersion") && (reflectionState || refractionState);
        var depthState = usedKeywords.Contains("Depth") && (reflectionState || refractionState);
        var decalState = usedKeywords.Contains("Decal");
		var flowMapState = usedKeywords.Contains("FlowMap");

        reflectionState = EditorGUILayout.Toggle("Use Real Reflection", reflectionState);
        refractionState = EditorGUILayout.Toggle("Use Real Refraction", refractionState);
        GUI.enabled = reflectionState || refractionState;
        dispersionState = EditorGUILayout.Toggle("Use Color Dispersion", dispersionState) && (reflectionState || refractionState);
        depthState = EditorGUILayout.Toggle("Use Depth Data", depthState) && (reflectionState || refractionState);
        GUI.enabled = true;
        decalState = EditorGUILayout.Toggle("Is Decal (Splash)", decalState);
		GUI.enabled = !decalState;
        flowMapState = EditorGUILayout.Toggle("Use Flow Map", flowMapState) && !decalState;
        GUI.enabled = true;

        if (depthState)
            newKeywords.Add("Depth");
		if (flowMapState)
            newKeywords.Add("FlowMap");
        if (reflectionState)
            newKeywords.Add("Reflection");
        if (refractionState)
            newKeywords.Add("Refraction");
        if (dispersionState)
            newKeywords.Add("Dispersion");
        if (decalState)
            newKeywords.Add("Decal");
        if (newKeywords.Count == 0)
            newKeywords.Add("Simple");

        var newName = string.Join("_", newKeywords.ToArray());

        if (newName != name)
        {
            var shader = Shader.Find(path + "/" + newName);
            SetShader(shader);
            return;
        }

        base.OnInspectorGUI();
    }
}