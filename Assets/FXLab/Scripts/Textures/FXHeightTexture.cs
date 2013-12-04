using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("FXLab/Textures/Height")]
public class FXHeightTexture : FXTexture
{
	private Shader heightShader;

	public GameObject HeightPlane;
    public bool ClearZero = false;

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
			return "_FXHeightTexture";
		}
	}
	
	private void Awake()
	{
        heightShader = Shader.Find("Hidden/FXLab/PreciseGrabHeight");
	}
	
	public override void Render(Camera renderCamera)
	{
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = ClearZero ? Color.black : Color.white;

        if (!HeightPlane)
        {
            renderCamera.cullingMask = 0;
            renderCamera.Render();
            return;
        }

		var oldShadowDistance = QualitySettings.shadowDistance;
		QualitySettings.shadowDistance = 0;
		
		var pos = HeightPlane.transform.position;
		var normal = HeightPlane.transform.up;
		var heightPlane = new Plane(normal, pos);
		Shader.SetGlobalVector("_HeightPlaneEquation", new Vector4(heightPlane.normal.x, heightPlane.normal.y, heightPlane.normal.z, heightPlane.distance));
        
		renderCamera.SetReplacementShader(heightShader, "RenderType");
        base.Render(renderCamera);
        renderCamera.ResetReplacementShader();
		
		QualitySettings.shadowDistance = oldShadowDistance;
	}
}
