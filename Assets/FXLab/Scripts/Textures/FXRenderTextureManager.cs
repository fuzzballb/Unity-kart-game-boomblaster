using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class FXRenderTextureManager
{
	private static float lastScreenWidth;
	private static float lastScreenHeight;
    
    public static List<FXRenderTextureGroup> Groups = new List<FXRenderTextureGroup>();
    public static IEnumerable<FXRenderTextureGroup> OrderedGroups 
    {
        get
        {
            return Groups.OrderBy(group => group.Priority);
        }
    }

	public static void Register(FXRenderTexture texture)
	{
		var group = Groups.FirstOrDefault(g => g.Name == texture.GroupName);
		if (group == null)
		{
			group = new FXRenderTextureGroup(texture.GroupName);
            group.Priority = texture.Priority;

			Groups.Add(group);
			Groups = Groups.OrderBy(g => g.Name).ToList();
		}

        if (!group.Textures.Contains(texture))
		    group.Textures.Add(texture);

		group.SetDirty();

        PackTextures();
	}

	public static void Unregister(FXRenderTexture texture)
	{
		texture.Registration.Area = new Rect(0, 0, 0, 0);
		texture.Registration.Chart = null;
		
		var group = Groups.FirstOrDefault(g => g.Name == texture.GroupName);
		if (group == null)
			return; //throw new Exception("RenderTexture is was not registered.");

		if (!group.Textures.Remove(texture))
			return; //throw new Exception("RenderTexture is was not registered.");

        if (group.Textures.Count == 0)
        {
            group.Destroy();
            Groups.Remove(group);
        }
        else
        {
            group.SetDirty();
            PackTextures();
        }
	}

	public static void SetDirty()
	{
		foreach (var group in Groups)
			group.SetDirty();
	}

    private static Vector2? maxResolution;
	public static void PackTextures()
	{
        if (!maxResolution.HasValue)
        {
#if UNITY_EDITOR
            maxResolution = new Vector2(4096, 4096);
#else
            maxResolution = EstimateMaxTextureResolution();
#endif
        }
        var size = GetRenderSize();
        PackTextures(new Vector2(size.x, size.y), maxResolution.Value);
	}

    private static Vector2 EstimateMaxTextureResolution()
    {
        int resolution = 1024;
        for (int i = 0; i < 10; ++i)
        {
            try
            {
                var texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
                Object.DestroyImmediate(texture);
            }
            catch
            {
                Debug.Log("The exception above was expected, ignore it.");
                break;
            }
            resolution *= 2;
        }
        resolution /= 2;

        return new Vector2(resolution, resolution);
    }

	public static void Update()
	{
        var size = GetRenderSize();

		if (lastScreenWidth != size.x || lastScreenHeight != size.y)
		{
			lastScreenWidth = size.x;
			lastScreenHeight = size.y;
			FXRenderTextureManager.SetDirty();
		}
		FXRenderTextureManager.PackTextures();
	}

	private static void PackTextures(Vector2 chartSize, Vector2 maxSize)
	{
        chartSize = Vector2.Max(chartSize, Vector2.one);

		foreach (var group in Groups.ToArray())
		{
			if (!group.IsDirty)
				continue;

			if (group.PackTextures(chartSize, maxSize))
			{
				group.UpdateTextures();
			}
			else
				Groups.Remove(group);
		}
	}

    public static Vector2 GetRenderSize()
    {
#if UNITY_EDITOR
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
        return (Vector2)Res;
#else
        return new Vector2(Screen.width, Screen.height);
#endif
    }

}