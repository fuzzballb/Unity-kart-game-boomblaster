using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("FXLab/FX Camera")]
public class FXCamera : MonoBehaviour
{
    private Camera renderCamera;
    private float lastTime;

    public void Awake()
    {
		CreateRenderObjects();
    }

    public void Start()
    {
        lastTime = Time.realtimeSinceStartup;
    }
	
    public void OnPreCull()
    {
        if (!enabled)
            return;

        if (renderCamera == null)
            CreateRenderObjects();

		FXRenderTextureManager.Update();
			
		var fxTextures = this.gameObject.GetComponents<FXTexture>().Where(f => f.enabled && f.RenderTexture).ToArray();
        if (fxTextures.Length == 0)
            return;

        RenderToTextures(fxTextures);
    }

    private void RenderToTextures(FXTexture[] fxTextures)
    {
        var time = Time.realtimeSinceStartup;
        var deltaTime = (time - lastTime) * 1000;
        lastTime = time;

		foreach (var group in FXRenderTextureManager.OrderedGroups)
		{
            var allowRender = true;
            group.ElapsedUpdateTime -= deltaTime;
            if (group.ElapsedUpdateTime <= 0)
            {
                group.ElapsedUpdateTime = group.UpdateInterval;
            }
            else
            {
                allowRender = false;
            }

			foreach (var data in group.Datas)
			{
                if (allowRender)
                {
                    foreach (var chart in data.Charts)
                    {
                        foreach (var texture in chart.Textures)
                        {
                            var fxTexture = fxTextures.FirstOrDefault(t => t.RenderTexture == texture);
                            if (fxTexture == null || fxTexture.Cameras == null)
                                continue;

                            foreach (var subCamera in fxTexture.Cameras)
                            {
                                if (!subCamera.Camera || !subCamera.Camera.gameObject.activeSelf)
                                    continue;

                                TransferCameraSettings(subCamera.Camera);

                                renderCamera.cullingMask &= subCamera.CullingMask;
                                renderCamera.pixelRect = texture.Registration.RenderArea;

                                fxTexture.Render(renderCamera);
                            }
                        }

                        chart.Grab(false);
                    }
                    data.Apply();
                }
				
				foreach (var chart in data.Charts)
					foreach (var tex in chart.Textures)
						tex.SetShaderData(null, tex.DefaultName);
			}
		}            
    }

    
    private void OnDestroy()
    {
        if (renderCamera)
        {
            if (Application.isPlaying)
                Destroy(renderCamera.gameObject);
            else
                DestroyImmediate(renderCamera.gameObject);
			renderCamera = null;
        }
    }

    private void TransferCameraSettings(Camera camera)
    {			
		renderCamera.CopyFrom(camera);
        
        if (camera.clearFlags == CameraClearFlags.Skybox)
        {
            var sky = camera.GetComponent<Skybox>();
            var destSky = renderCamera.GetComponent<Skybox>();
            if (!sky || !sky.material)
                destSky.enabled = false;
            else
            {
                destSky.enabled = true;
                destSky.material = sky.material;
            }
        }
    }
	
	private void CreateRenderObjects()
    {
        if (!renderCamera)
        {
            var go = new GameObject("FXLab Camera", typeof(Camera), typeof(Skybox));
            go.hideFlags = HideFlags.HideAndDontSave;
			
			renderCamera = go.camera;
            renderCamera.enabled = false;
        }
    }
}