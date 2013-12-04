using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class FXRenderTextureChart
{
	public List<FXRenderTexture> Textures = new List<FXRenderTexture>();
	public Rect Area;

	public Rect GrabArea
	{
		get
		{
			return new Rect(0, 0, Area.width, Area.height);
		}
	}

	public Vector2 GrabTarget
	{
		get
		{
			return new Vector2(Area.xMin, Area.yMin);
		}
	}

	public FXRenderTextureData Data;

	public void Destroy()
	{
		foreach (var texture in Textures)
		{
			texture.Registration.Chart = null;
			texture.Registration.Area = new Rect(0, 0, 0, 0);
		}
		Textures.Clear();
	}

	public bool PackTextures(List<FXRenderTexture> textures)
	{
		var root = new Node<FXRenderTexture>(Area);
		var chartSize = new Vector2(Area.width, Area.height);
		while (textures.Count > 0)
		{
			var texture = textures[0];

			var size = texture.CalculateSize(chartSize);
			var node = root.Insert(texture, size);
			if (node == null)
			{
				Area = root.PackedRectangle;
				return Textures.Count > 0;
			}

			texture.Registration.Area = node.Rectangle;
			texture.Registration.Chart = this;

            Textures.Add(texture);

			textures.RemoveAt(0);
		}

		Area = root.PackedRectangle;
		return Textures.Count > 0;
	}
	
	public void Grab(bool apply)
    {
		Data.Texture.ReadPixels(GrabArea, (int)GrabTarget.x, (int)GrabTarget.y, false);
		if (apply)
			Data.Apply();
    }	
}