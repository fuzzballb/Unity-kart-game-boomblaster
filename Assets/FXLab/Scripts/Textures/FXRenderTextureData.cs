using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class FXRenderTextureData
{
    public static System.Action<FXRenderTextureData> OnApply;

	public List<FXRenderTextureChart> Charts = new List<FXRenderTextureChart>();
	public Rect Area;
	public Texture2D Texture;
    public Vector2 TexelSize;

    public FXRenderTextureGroup Group;

    public FXRenderTextureData(FXRenderTextureGroup group)
    {
        this.Group = group;
    }

	public bool IsDirty { get; private set; }

	public void SetDirty()
	{
		if (IsDirty)
			return;
		IsDirty = true;

		foreach (var chart in Charts)
			chart.Destroy();
		Charts.Clear();
	}

	public bool PackTextures(Vector2 chartSize, Vector2 maxSize, List<FXRenderTexture> textures)
	{
		IsDirty = false;

		var root = new Node<FXRenderTextureChart>(new Rect(0, 0, maxSize.x, maxSize.y));
		while (textures.Count > 0)
		{
			var chart = new FXRenderTextureChart();
			chart.Data = this;
           
			var node = root.Insert(chart, chartSize);
			if (node == null)
			{
				var freeLeaf = root.Leafs.FirstOrDefault(leaf => leaf.Element == null);
				if (freeLeaf == null)
				{
                    CalculateArea();
					return Charts.Count > 0;
				}
				chartSize = new Vector2(freeLeaf.Rectangle.width, freeLeaf.Rectangle.height);
				continue;
			}

			chart.Area = node.Rectangle;
			if (chart.PackTextures(textures))
				Charts.Add(chart);
		}

        CalculateArea();
		return Charts.Count > 0;
	}

    private void CalculateArea()
    {
        var width = 0.0f;
        var height = 0.0f;
        foreach (var chart in Charts)
        {
            width = Mathf.Max(width, chart.Area.xMax);
            height = Mathf.Max(height, chart.Area.yMax);
        }
        Area = new Rect(0, 0, width, height);
    }
	
	public void UpdateTexture()
    {
        var newWidthPOT = Mathf.NextPowerOfTwo((int)Area.width);
        var newHeightPOT = Mathf.NextPowerOfTwo((int)Area.height);

        TexelSize = new Vector2(1.0f / newWidthPOT, 1.0f / newHeightPOT);

        var needNewtexture = Texture == null || Texture.width != newWidthPOT || Texture.height != newHeightPOT;
        if (needNewtexture)
        {
            if (Texture == null)
            {
                Texture = new Texture2D(newWidthPOT, newHeightPOT, TextureFormat.RGB24, false);
                Texture.anisoLevel = 1;
                Texture.filterMode = FilterMode.Bilinear;
                Texture.wrapMode = TextureWrapMode.Repeat;
                Texture.hideFlags = HideFlags.HideAndDontSave;
            }
            else
                Texture.Resize(newWidthPOT, newHeightPOT);
        }
    }
	
	public void Apply()
	{
		Texture.Apply(false, false);
        if (OnApply != null)
            OnApply(this);
	}

	public void Destroy()
	{
		SetDirty();
		
		if (Texture != null)
			Object.DestroyImmediate(Texture);	
		Texture = null;
	}
}