using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("FXLab/Textures/ScreenBuffer")]
public class FXScreenBufferTexture : FXTexture
{
	public bool CaptureShadows = false;

	public override string DefaultMaterialSlot
	{
		get
		{
			return "_FXScreenTexture";
		}
	}
	
	public override void Render(Camera renderCamera)
	{
		var oldShadowDistance = QualitySettings.shadowDistance;
		if (!CaptureShadows)
			QualitySettings.shadowDistance = 0;
			
		base.Render(renderCamera);
		
		QualitySettings.shadowDistance = oldShadowDistance;
	}
}