using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("FXLab/Textures/Depth")]
public class FXDepthTexture : FXTexture
{
	private Shader depthShader;

    public override FXRenderTexture RenderTexture
    {
        get { return base.RenderTexture; }
        set
        {
            base.RenderTexture = value;
            if (_renderTexture)
                _renderTexture.IsFloatTexture = true;
        }
    }

	public override string DefaultMaterialSlot
	{
		get
		{
			return "_FXDepthTexture";
		}
	}

    private void Awake()
    {
        depthShader = Shader.Find("Hidden/FXLab/PreciseGrabDepth");
    }
	
	public override void Render(Camera renderCamera)
	{
		var oldShadowDistance = QualitySettings.shadowDistance;
		QualitySettings.shadowDistance = 0;
		
		renderCamera.SetReplacementShader(depthShader, "RenderType");
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = Color.white;
        base.Render(renderCamera);
        renderCamera.ResetReplacementShader();
		
		QualitySettings.shadowDistance = oldShadowDistance;
	}
}