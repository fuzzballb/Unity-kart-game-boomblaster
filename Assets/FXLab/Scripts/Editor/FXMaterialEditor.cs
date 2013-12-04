using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text.RegularExpressions;

public class FXMaterialEditor : MaterialEditor
{
    public bool DisplayHelpTexts = true;

    public override void OnEnable()
    {
        serializedObject.Update();
        var theShader = serializedObject.FindProperty("m_Shader");
        if (!theShader.hasMultipleDifferentValues && theShader.objectReferenceValue != null)
        {
            Shader shader = theShader.objectReferenceValue as Shader;
            var material = target as Material;
            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    var fxTextureType = FXMaterialHelper.GetFXTextureType(ShaderUtil.GetPropertyDescription(shader, i));
                    if (fxTextureType != null)
                    {
                        material.SetTexture(name, null);
                    }
                }
            }
        }

        base.OnEnable();

        FXMaterialHelper.ResetCache();
    }

    private void ShaderPropertyImpl(Material owner, Shader shader, int propertyIndex)
    {
        int i = propertyIndex;
        string label = ShaderUtil.GetPropertyDescription(shader, i);
        string propertyName = ShaderUtil.GetPropertyName(shader, i);
        switch (ShaderUtil.GetPropertyType(shader, i))
        {
            case ShaderUtil.ShaderPropertyType.Range: // float ranges
                {
                    GUILayout.BeginHorizontal();
                    float v2 = ShaderUtil.GetRangeLimits(shader, i, 1);
                    float v3 = ShaderUtil.GetRangeLimits(shader, i, 2);
                    RangeProperty(propertyName, label, v2, v3);
                    GUILayout.EndHorizontal();

                    break;
                }
            case ShaderUtil.ShaderPropertyType.Float: // floats
                {
                    FloatProperty(propertyName, label);
                    break;
                }
            case ShaderUtil.ShaderPropertyType.Color: // colors
                {
                    ColorProperty(propertyName, label);
                    break;
                }
            case ShaderUtil.ShaderPropertyType.TexEnv: // textures
                {
                    var fxTextureType = FXMaterialHelper.GetFXTextureType(ShaderUtil.GetPropertyDescription(shader, i));
                    if (fxTextureType != null)
                    {
                        if (!(Selection.activeObject is GameObject))
                        {
                            if (DisplayHelpTexts)
                            {
                                GUILayout.Label(label);
                                EditorGUILayout.HelpBox(label + " is a FXTexture, you can use a FXTextureAssigner or a FXPostProcess component to set this Texture.", MessageType.Info);
                            }
                            else
                                break;
                        }
                        else
                        {
                            var match = Regex.Match(label, @"(.*)\(\w*\).*", RegexOptions.IgnoreCase);
                            var description = match.Groups[1].Value.Trim();
                            var name = ShaderUtil.GetPropertyName(shader, i);
                            FXRenderTexture oldFxRenderTexture = GetRenderTextureForProperty(owner, shader, name);
                            var newFxRenderTexture = (FXRenderTexture)EditorGUILayout.ObjectField(description + " (FXRenderTexture)", oldFxRenderTexture, typeof(FXRenderTexture), false);
                            SetRenderTextureForProperty(owner, shader, name, description, newFxRenderTexture);
                        }
                    }
                    else
                    {
                        ShaderUtil.ShaderPropertyTexDim desiredTexdim = ShaderUtil.GetTexDim(shader, i);
                        TextureProperty(propertyName, label, desiredTexdim);
                    }

                    GUILayout.Space(6);
                    break;
                }
            case ShaderUtil.ShaderPropertyType.Vector: // vectors
                {
                    VectorProperty(propertyName, label);
                    break;
                }
            default:
                {
                    GUILayout.Label("Unknown " + label + " : " + ShaderUtil.GetPropertyType(shader, i));
                    break;
                }
        }
    }

    private void SetRenderTextureForProperty(Material material, Shader shader, string name, string description, FXRenderTexture newFxRenderTexture)
    {
        var gameObject = (GameObject)Selection.activeObject;
        var renderer = gameObject.GetComponents<Renderer>().FirstOrDefault(r => r.sharedMaterials.Any(m => m == material));
        var postProcess = gameObject.GetComponents<FXPostProcess>().FirstOrDefault(pp => pp.Material == material);

        if (renderer != null)
        {
            var assigner = gameObject.GetComponent<FXTextureAssigner>() ?? gameObject.AddComponent<FXTextureAssigner>();
            FXTextureAssigner.RenderTextureAssignment.SetAssignment(assigner.Assignments, material, name, description, newFxRenderTexture);
        }
        else if (postProcess != null)
        {
            FXTextureAssigner.RenderTextureAssignment.SetAssignment(postProcess.Assignments, material, name, description, newFxRenderTexture);
        }
    }

    private FXRenderTexture GetRenderTextureForProperty(Material material, Shader shader, string name)
    {
        var gameObject = (GameObject)Selection.activeObject;
        var renderer = gameObject.GetComponents<Renderer>().FirstOrDefault(r => r.sharedMaterials.Any(m => m == material));
        var postProcess = gameObject.GetComponents<FXPostProcess>().FirstOrDefault(pp => pp.Material == material);

        if (renderer != null)
        {
            var assigner = gameObject.GetComponent<FXTextureAssigner>() ?? gameObject.AddComponent<FXTextureAssigner>();
            return FXTextureAssigner.RenderTextureAssignment.GetAssignment(assigner.Assignments, material, name);
        }
        else if (postProcess != null)
        {
            return FXTextureAssigner.RenderTextureAssignment.GetAssignment(postProcess.Assignments, material, name);
        }

        return null;
    }

    public void DrawMaterialProperties()
    {
        serializedObject.Update();
        var theShader = serializedObject.FindProperty("m_Shader");
        if (isVisible && !theShader.hasMultipleDifferentValues && theShader.objectReferenceValue != null)
        {
            float controlSize = 128;

            EditorGUIUtility.LookLikeControls(Screen.width - controlSize - 20);

            EditorGUI.BeginChangeCheck();
            Shader shader = theShader.objectReferenceValue as Shader;

            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                ShaderPropertyImpl(target as Material, shader, i);
            }

            if (EditorGUI.EndChangeCheck())
                PropertiesChanged();
        }
    }

    public override void OnInspectorGUI()
    {
        FXMaterialHelper.DrawRequiredFXTextures(target as Material);
        DrawMaterialProperties();
    }
}