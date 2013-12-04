using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class FXRenderTexture : ScriptableObject
{
	public static Dictionary<string, FXRenderTexture> LastSetRenderTextures = new Dictionary<string, FXRenderTexture>();

	public enum FXRenderTextureSizeMode
	{
		Factor,
		MaxSize,
		ImageEffect
	}

	public struct RegistrationData
	{
        public int RegistrationCount;
		public Rect Area;
        public FXRenderTextureChart Chart;
		
        public Rect RenderArea
		{
			get
			{
				return new Rect(Area.xMin - Chart.GrabTarget.x, Area.yMin - Chart.GrabTarget.y, Area.width, Area.height);
			}
		}
	}
	
	private static Texture2D _emptyTexture;
    
    public static Texture2D EmptyTexture
    {
        get
        {
            if (_emptyTexture == null)
            {
                _emptyTexture = new Texture2D(1, 1);
                _emptyTexture.hideFlags = HideFlags.HideAndDontSave;
                _emptyTexture.SetPixel(0, 0, new Color(0, 0, 0, 0));
                _emptyTexture.Apply();
            }
            return _emptyTexture;
        }
    }

    [System.NonSerialized]
	public RegistrationData Registration;
	
	public bool IsFloatTexture;
	
	[SerializeField]
	private string _defaultName;
	public string DefaultName 
	{
		get
		{
			return _defaultName;
		}
		set
		{
			if (_defaultName == value)
				return;
			_defaultName = value;
			MarkDirty();
		}
	}

	[SerializeField]	
	private string _groupName = "0";
	public string GroupName 
	{
		get
		{
			return _groupName;
		}
		set
		{
			if (_groupName == value)
				return;
			_groupName = value;
            int.TryParse(_groupName, out _priority);
			MarkDirty();
		}
	}

    [SerializeField]
    private int _priority = 0;
    public int Priority
    {
        get { return _priority; }
    }

    [SerializeField]
    private int _updateIntervalInMilliseconds = 0;
    public int UpdateIntervalInMilliseconds
    {
        get
        {
            return _updateIntervalInMilliseconds;
        }
        set
        {
            var newValue = Mathf.Max(0, value);
            if (newValue == _updateIntervalInMilliseconds)
                return;

            _updateIntervalInMilliseconds = newValue;
            MarkDirty();
        }
    }

	[SerializeField]
	private FXRenderTextureSizeMode _sizeMode = FXRenderTextureSizeMode.Factor;
	public FXRenderTextureSizeMode SizeMode 
	{
		get
		{
			return _sizeMode;
		}
		set
		{
			if (_sizeMode == value)
				return;
            if (_sizeMode == FXRenderTextureSizeMode.ImageEffect)
            {
                GroupName = "0";
            }
			_sizeMode = value;
            if (_sizeMode == FXRenderTextureSizeMode.ImageEffect)
            {
                GroupName = System.Guid.NewGuid().ToString();
            }
			MarkDirty();
		}
	}
	
	[SerializeField]
	private Vector2 _maximumSize = new Vector2(512, 512);
	public Vector2 MaximumSize 
	{
		get
		{
			return _maximumSize;
		}
		set
		{
			if (_maximumSize == value)
				return;
			_maximumSize = value;
			MarkDirty();
		}
	}
	
	[SerializeField]
	private float _sizeFactor = 0.5f;
	public float SizeFactor 
	{
		get
		{
			return _sizeFactor;
		}
		set
		{
			if (_sizeFactor == value)
				return;
			_sizeFactor = value;
			MarkDirty();
		}
	}
	
	public void MarkDirty()
	{
		if (!IsRegistered)
			return;
			
		Unregister();
		Register();
	}
	
	public bool IsRegistered
	{
		get
		{
			return Registration.RegistrationCount != 0;
		}
	}

	public void Register()
	{
        Registration.RegistrationCount ++;
        if (Registration.RegistrationCount == 1)
		    FXRenderTextureManager.Register(this);

#if UNITY_EDITOR
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo repaintAll = T.GetMethod("RepaintAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        repaintAll.Invoke(null, null);
#endif
	}

	public void Unregister()
	{
        Registration.RegistrationCount --;
        if (Registration.RegistrationCount <= 0)
        {
            Registration.RegistrationCount = 0;
            FXRenderTextureManager.Unregister(this);
        }
	}

	public Vector2 CalculateSize(Vector2 chartSize)
	{
		var size = chartSize;
        if (SizeMode == FXRenderTexture.FXRenderTextureSizeMode.Factor)
            size *= SizeFactor;
        else if (SizeMode == FXRenderTexture.FXRenderTextureSizeMode.MaxSize)
            size = Vector2.Min(size, MaximumSize);
        else
            size = FXRenderTextureManager.GetRenderSize();
		size = Vector2.Max(Vector2.one, size);
        
		return new Vector2((int)size.x, (int)size.y);
	}
	
	public void SetShaderData(Material target, string name)
    {
		if (!IsRegistered || Registration.Chart == null)
			return;
		
        if (string.IsNullOrEmpty(name))
            name = DefaultName;

        if (string.IsNullOrEmpty(name))
            return;

        var area = Registration.Area;
        var fastArea = area;
        fastArea.xMin += 0.5f;
        fastArea.yMin += 0.5f;
        fastArea.xMax -= 0.5f;
        fastArea.yMax -= 0.5f;
        
        var normalizedArea = new Vector4(
            area.xMin / Registration.Chart.Data.Texture.width, area.yMin / Registration.Chart.Data.Texture.height,
            area.xMax / Registration.Chart.Data.Texture.width, area.yMax / Registration.Chart.Data.Texture.height);
        var normalizedFastArea = new Vector4(
            fastArea.xMin / Registration.Chart.Data.Texture.width, fastArea.yMin / Registration.Chart.Data.Texture.height,
            fastArea.xMax / Registration.Chart.Data.Texture.width, fastArea.yMax / Registration.Chart.Data.Texture.height);

        SetShaderDataValues(target, name, Registration.Chart.Data.Texture, Registration.Chart.Data.TexelSize, normalizedArea, normalizedFastArea);

        if (!target)
            LastSetRenderTextures[name] = this;
    }

    public static void SetShaderDataValues(Material target, string name, Texture texture, Vector2 texelSize, Vector4 normalizedArea, Vector4 normalizedFastArea)
    {
        var areaName = name + "_Area";
        var fastAreaName = name + "_FastArea";
        var texelSizeName = name + "_TexelSize";

        if (target)
        {
            target.SetTexture(name, texture);
            target.SetVector(areaName, normalizedArea);
            target.SetVector(fastAreaName, normalizedFastArea);
            target.SetVector(texelSizeName, texelSize);
        }
        else
        {
            Shader.SetGlobalTexture(name, texture);
            Shader.SetGlobalVector(areaName, normalizedArea);
            Shader.SetGlobalVector(fastAreaName, normalizedFastArea);
            Shader.SetGlobalVector(texelSizeName, texelSize);
        }
    }
	
	private void OnDestroy()
	{
		if (IsRegistered)
			Unregister();
	}
}