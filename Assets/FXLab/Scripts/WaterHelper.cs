using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class WaterHelper
{
    public static void CopyWaterProperties(Material sourceMaterial, Material targetMaterial)
    {
        var oldBump = targetMaterial.GetTexture("_BumpMap");
        var oldBumpScale = targetMaterial.GetTextureScale("_BumpMap");
        var oldBumpOffset = targetMaterial.GetTextureOffset("_BumpMap");

        var oldMask = targetMaterial.GetTexture("_MaskMap");
        var oldMaskScale = targetMaterial.GetTextureScale("_MaskMap");
        var oldMaskOffset = targetMaterial.GetTextureOffset("_MaskMap");

        var oldTransparency = targetMaterial.GetFloat("_Transparency");
        var oldBumpUpInfluence = targetMaterial.GetFloat("_BumpUpInfluence");
        var oldShader = targetMaterial.shader;
        targetMaterial.CopyPropertiesFromMaterial(sourceMaterial);
        targetMaterial.shader = oldShader;

        targetMaterial.SetTexture("_BumpMap", oldBump);
        targetMaterial.SetTextureScale("_BumpMap", oldBumpScale);
        targetMaterial.SetTextureOffset("_BumpMap", oldBumpOffset);

        targetMaterial.SetTexture("_MaskMap", oldMask);
        targetMaterial.SetTextureScale("_MaskMap", oldMaskScale);
        targetMaterial.SetTextureOffset("_MaskMap", oldMaskOffset);

        targetMaterial.SetFloat("_Transparency", oldTransparency);
        targetMaterial.SetFloat("oldBumpUpInfluence", oldBumpUpInfluence);
    }
}