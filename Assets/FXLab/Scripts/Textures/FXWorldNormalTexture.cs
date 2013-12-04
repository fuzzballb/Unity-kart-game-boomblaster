using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("FXLab/Textures/WorldNormal")]
public class FXWorldNormalTexture : FXTexture
{
	private Shader normalShader;

	public override string DefaultMaterialSlot
	{
		get
		{
			return "_FXWorldNormalTexture";
		}
	}

    private void Awake()
    {
        normalShader = Shader.Find("Hidden/FXLab/WorldNormal");
    }
	
	public override void Render(Camera renderCamera)
	{
		var oldShadowDistance = QualitySettings.shadowDistance;
		QualitySettings.shadowDistance = 0;

        renderCamera.SetReplacementShader(normalShader, "RenderType");
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        var clearColorVector = (-renderCamera.transform.forward * 0.5f + Vector3.one * 0.5f);
        renderCamera.backgroundColor = new Color(clearColorVector.x, clearColorVector.y, clearColorVector.z);
        base.Render(renderCamera);
        renderCamera.ResetReplacementShader();
		
		QualitySettings.shadowDistance = oldShadowDistance;
	}
}