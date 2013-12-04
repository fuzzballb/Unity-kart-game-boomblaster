using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class GlassMaterialEditor : FXMaterialEditor
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

        var refractionState = usedKeywords.Contains("Refraction");
        var dispersionState = usedKeywords.Contains("Dispersion") && (refractionState);
        var blurredState = usedKeywords.Contains("Blurred") && (refractionState);

        refractionState = EditorGUILayout.Toggle("Use Real Refraction", refractionState);
        GUI.enabled = refractionState;
        dispersionState = EditorGUILayout.Toggle("Use Color Dispersion", dispersionState) && (refractionState);
        blurredState = EditorGUILayout.Toggle("Use Blur", blurredState) && (refractionState);
        GUI.enabled = true;

        if (refractionState)
            newKeywords.Add("Refraction");
        if (dispersionState)
            newKeywords.Add("Dispersion");
        if (blurredState)
            newKeywords.Add("Blurred");
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