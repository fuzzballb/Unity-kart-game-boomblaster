using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class FXRenderTextureGroup
{
	public List<FXRenderTextureData> Datas = new List<FXRenderTextureData>();
	public List<FXRenderTexture> Textures = new List<FXRenderTexture>();
	
    public string Name;
    public int Priority;

    public int UpdateInterval;

    private float _elapsedUpdateTime = 0;
    public float ElapsedUpdateTime
    {
        get
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
                return _elapsedUpdateTime;
#if UNITY_EDITOR
            else
                return 0;
#endif
        }
        set
        {
            _elapsedUpdateTime = value;
        }
    }

	public FXRenderTextureGroup(string name)
	{
		Name = name;
	}

	public bool IsDirty { get; private set; }

	public void SetDirty()
	{
        UpdateInterval = Textures.Count == 0 ? 0 : Textures.Min(tex => tex.UpdateIntervalInMilliseconds);
        _elapsedUpdateTime = 0;

		if (IsDirty)
			return;
		IsDirty = true;

		foreach (var data in Datas)
			data.SetDirty();
	}

    public void Destroy()
    {
        foreach (var data in Datas)
            data.Destroy();
    }

	public bool PackTextures(Vector2 chartSize, Vector2 maxSize)
	{
		IsDirty = false;

		var textures = Textures.OrderByDescending(tex => tex.CalculateSize(chartSize).x)
								.ThenByDescending(tex => tex.CalculateSize(chartSize).y).ToList();
		var i = 0;

		while (textures.Count > 0)
		{
            if (i == Datas.Count)
            {
                Datas.Add(new FXRenderTextureData(this));
            }

			if (!Datas[i].PackTextures(chartSize, maxSize, textures))
				break;
			++i;
		}

		while (i < Datas.Count)
		{
			Datas[i].Destroy();
			Datas.RemoveAt(i);
		}

		return Datas.Count > 0;
	}
	
	public void UpdateTextures()
	{
		foreach (var data in Datas)
			data.UpdateTexture();
	}
}