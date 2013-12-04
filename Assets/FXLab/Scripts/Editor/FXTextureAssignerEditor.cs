using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[CustomEditor(typeof(FXTextureAssigner))]
public class FXTextureAssignerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        return;
        /*
        FXTextureAssigner fxObject = (FXTextureAssigner)target;
        fxObject.Material = (Material)EditorGUILayout.ObjectField("Material", fxObject.Material, typeof(Material), false);
        if (!fxObject.Material)
        {
            var renderer = fxObject.renderer;
            if (renderer)
                fxObject.Material = renderer.sharedMaterial; 
        }

        var material = fxObject.Material;
        var assignments = fxObject.Assignments;

        EditorGUILayout.Space();
        HandleAssignments(material, assignments);
        */
    }

    public class FXTextureSlot
    {
        public string Name;
        public string Description;
    }

    public static void HandleAssignments(Material material, List<FXTextureAssigner.RenderTextureAssignment> assignments)
    {
        var fxTextureSlots = new List<FXTextureSlot>();

        if (material)
        {
            var shader = material.shader;

            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var isFxTexture = ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv &&
                                  FXMaterialHelper.GetFXTextureType(ShaderUtil.GetPropertyDescription(shader, i)) != null;

                if (isFxTexture)
                {
                    var match = Regex.Match(ShaderUtil.GetPropertyDescription(shader, i), @"(.*)\(\w*\).*", RegexOptions.IgnoreCase);

                    fxTextureSlots.Add(new FXTextureSlot()
                    {
                        Name = ShaderUtil.GetPropertyName(shader, i),
                        Description = match.Groups[1].Value.Trim()
                    });
                }
            }
        }

        var currentSlots = assignments.Select(a => a.TextureName);
        var slotsToRemove = currentSlots.Except(fxTextureSlots.Select(slot => slot.Name)).ToArray();
        var slotsToAdd = fxTextureSlots.Where(slot => !currentSlots.Any(s => s == slot.Name)).ToArray();

        foreach (var toRemove in slotsToRemove)
            assignments.Remove(assignments.First(a => a.TextureName == toRemove));

        foreach (var toAdd in slotsToAdd)
        {
            assignments.Add(new FXTextureAssigner.RenderTextureAssignment()
            {
                TextureName = toAdd.Name,
                TextureDescription = toAdd.Description
            });
        }

        foreach (var assignment in assignments)
        {
            assignment.RenderTexture = (FXRenderTexture)EditorGUILayout.ObjectField(assignment.TextureDescription, assignment.RenderTexture, typeof(FXRenderTexture), false);
        }
    }
}