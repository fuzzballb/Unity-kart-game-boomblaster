using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("FXLab/FX Capture Screen")]
public class FXCaptureScreen : MonoBehaviour
{
    private static Texture2D dummyTexture;

    private float lastTime;

    [SerializeField]
    private FXRenderTexture _renderTexture;
	
    public FXRenderTexture RenderTexture
    {
        get
        {
            EnsureRenderTexture();
			
            return _renderTexture;
        }
        set
        {
			if (value != null && value.SizeMode != FXRenderTexture.FXRenderTextureSizeMode.ImageEffect)
			{
				Debug.LogError("Input RenderTexture must have the SizeMode set to 'Full'.");
				return;
			}
			
            if (_renderTexture == value)
                return;
				
            if (value == null)
                FreeRenderTexture();

            _renderTexture = value;
			if (_renderTexture && enabled)
				_renderTexture.Register();
        }
    }

    private void EnsureRenderTexture()
    {
        if (!_renderTexture)
        {
            var newRenderTexture = ScriptableObject.CreateInstance<FXRenderTexture>();
            newRenderTexture.name = "Auto Generated RenderTexture";
            newRenderTexture.DefaultName = "_FXScreenTexture";
            newRenderTexture.SizeMode = FXRenderTexture.FXRenderTextureSizeMode.ImageEffect;

            RenderTexture = newRenderTexture;
        }
    }

	private void FreeRenderTexture()
	{
		if (_renderTexture)
			_renderTexture.Unregister();
		if (_renderTexture && _renderTexture.name == "Auto Generated RenderTexture")
			DestroyImmediate(_renderTexture);
		_renderTexture = null;
	}

    void OnPostRender()
    {
        if (!enabled)
            return;

        if (!dummyTexture)
        {
            dummyTexture = new Texture2D(1, 1);
            dummyTexture.hideFlags = HideFlags.HideAndDontSave;
        }

        EnsureRenderTexture();
        FXRenderTextureManager.Update();

        CaptureScreen();
    }

    private void CaptureScreen()
    {
        var time = Time.realtimeSinceStartup;
        var group = RenderTexture.Registration.Chart.Data.Group;

        group.ElapsedUpdateTime -= (time - lastTime) * 1000;
        lastTime = time;
        if (group.ElapsedUpdateTime <= 0)
        {
            group.ElapsedUpdateTime = group.UpdateInterval;
            RenderTexture.Registration.Chart.Grab(true);
        }

        RenderTexture.SetShaderData(null, string.Empty);
    }

    void Start()
    {
        lastTime = Time.realtimeSinceStartup;
    }
	
	void OnEnable()
    {
        if (_renderTexture)
            _renderTexture.Register();
    }

    void OnDisable()
    {
        if (_renderTexture)
            _renderTexture.Unregister();
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlaying || !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            FreeRenderTexture();
        }
#else
        FreeRenderTexture();
#endif
    }
}