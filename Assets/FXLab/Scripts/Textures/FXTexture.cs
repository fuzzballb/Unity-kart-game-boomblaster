using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(FXCamera))]
public abstract class FXTexture : MonoBehaviour
{
	public abstract string DefaultMaterialSlot { get; }

    [System.Serializable]
    public class FXTextureCamera
    {
        public Camera Camera;
        
        [LayerAttribute]
        public int CullingMask = ~(1 << 1);
    }

    [SerializeField]
    [HideInInspector]
    protected FXRenderTexture _renderTexture;

    [HideInInspector]
    public FXTextureCamera[] Cameras;

    public virtual FXRenderTexture RenderTexture
    {
        get
        {
            if (!_renderTexture)
            {
                var newRenderTexture = ScriptableObject.CreateInstance<FXRenderTexture>();
                newRenderTexture.name = "Auto Generated RenderTexture";
                newRenderTexture.DefaultName = DefaultMaterialSlot;
                //newRenderTexture.hideFlags = HideFlags.HideAndDontSave;
				
				RenderTexture = newRenderTexture;
            }
			
            return _renderTexture;
        }
        set
        {
            if (_renderTexture == value)
                return;
            
			FreeRenderTexture();
			
			_renderTexture = value;
			if (_renderTexture && enabled)
				_renderTexture.Register();
			if (_renderTexture)
                _renderTexture.IsFloatTexture = false;
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

    void Start()
    {
        // to display the enable checkbox, dont remove this
    }

    void Reset()
    {
        Cameras = new FXTextureCamera[]
        {
            new FXTextureCamera()
            {
                Camera = camera
            }
        };
    }
	
	public virtual void Render(Camera renderCamera)
	{
        renderCamera.Render();
	}

    void OnDisable()
    {
        if (_renderTexture)
            _renderTexture.Unregister();
    }
	
	void OnEnable()
    {
        if (_renderTexture)
            _renderTexture.Register();
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