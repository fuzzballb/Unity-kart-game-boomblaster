using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[AddComponentMenu("FXLab/FX Texture Assigner")]
[ExecuteInEditMode]
public class FXTextureAssigner : MonoBehaviour
{
    [Serializable]
    public class RenderTextureAssignment
    {
        public Material Material;
        public FXRenderTexture RenderTexture;
        public string TextureName;
        public string TextureDescription;

        public void Apply(GameObject go)
        {
            if (!Material)
                return;

            if (!RenderTexture || !RenderTexture.IsRegistered)
            {
                FXRenderTexture renderTexture;
                if (FXRenderTexture.LastSetRenderTextures.TryGetValue(TextureName, out renderTexture) && renderTexture != null && renderTexture.IsRegistered)
                    renderTexture.SetShaderData(Material, TextureName);
                else
                    FXRenderTexture.SetShaderDataValues(Material, TextureName, FXRenderTexture.EmptyTexture, Vector2.zero, Vector4.zero, Vector4.zero);
                return;
            }

            RenderTexture.SetShaderData(Material, TextureName);
        }

        public static void SetAssignment(List<RenderTextureAssignment> list, Material material, string name, string description, FXRenderTexture newFxRenderTexture)
        {
            var existing = list.FirstOrDefault(a => a.Material == material && a.TextureName == name);
            if (existing == null)
            {
                var newAssignment = new RenderTextureAssignment()
                {
                    Material = material,
                    RenderTexture = null,
                    TextureDescription = description,
                    TextureName = name
                };
                list.Add(newAssignment);
                return;
            }
            existing.TextureDescription = description;
            existing.RenderTexture = newFxRenderTexture;
        }

        public static FXRenderTexture GetAssignment(List<RenderTextureAssignment> list, Material material, string name)
        {
            var existing = list.FirstOrDefault(a => a.Material == material && a.TextureName == name);
            if (existing == null)
                return null;
            return existing.RenderTexture;
        }

        public bool IsValid(Renderer renderer)
        {
            if (!renderer)
                return false;
            return renderer.sharedMaterials.Any(m => m == this.Material);
        }

        internal bool IsValid(Renderer cachedRenderer, FXPostProcess[] postEffects)
        {
            var valid = cachedRenderer && cachedRenderer.sharedMaterials.Any(m => m == this.Material);
            valid = valid || postEffects.Any(m => m == this.Material);

            return valid;
        }
    }

    public List<RenderTextureAssignment> Assignments = new List<RenderTextureAssignment>();
    private Renderer cachedRenderer;

    void OnWillRenderObject()
    {
        CheckAssignmentValidity();

        if (Assignments.Count == 0)
        {
            DestroyImmediate(this);
            return;
        }

        foreach (var assignment in Assignments)
            assignment.Apply(gameObject);
    }

    private void CheckAssignmentValidity()
    {
        cachedRenderer = cachedRenderer ? cachedRenderer : renderer;
        var postEffects = GetComponents<FXPostProcess>();
        foreach (var assignment in Assignments.ToArray())
        {
            if (!assignment.IsValid(cachedRenderer, postEffects))
                Assignments.Remove(assignment);
        }
    }
}