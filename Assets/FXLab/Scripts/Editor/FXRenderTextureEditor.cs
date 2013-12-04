using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

[CustomEditor(typeof(FXRenderTexture))]
public class FXRenderTextureEditor : Editor
{
	private static Material displayTextureMaterial;
    private static Material displayFloatTextureMaterial;
	
    [MenuItem("Assets/Create/FX RenderTexture")]
    public static void CreateAsset()
    {
        var asset = ScriptableObject.CreateInstance<FXRenderTexture>();

        CreatePhysicalAsset(asset, "New RenderTexture");
    }

    public static void CreatePhysicalAsset(FXRenderTexture asset, string name)
    {
        asset.hideFlags = HideFlags.None;

        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + name + ".asset");

        AssetDatabase.CreateAsset(asset, assetPathAndName);

        AssetDatabase.SaveAssets();
        //EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    public override void OnInspectorGUI()
    {
        FXRenderTexture data = (FXRenderTexture)target;

        if (data == null)
            return;

        if (data.IsRegistered)
        {
            EnsureDisplayMaterial();

            if (data.IsFloatTexture)
            {
                var current = displayFloatTextureMaterial.GetFloat("_MaxFloat");
                current = Mathf.Max(float.Epsilon, EditorGUILayout.FloatField("Preview Range", current));
                displayFloatTextureMaterial.SetFloat("_MaxFloat", current);
            }

            var maxSize = 256.0f;
            var previewArea = GUILayoutUtility.GetRect(maxSize, maxSize);

            EditorGUI.DrawRect(previewArea, Color.gray);

            if (data.Registration.Chart != null)
            {
                var area = data.Registration.Area;
                var width = area.width;
                var height = area.height;
                var ratio = Mathf.Min(previewArea.width / width, previewArea.height / height);

                width *= ratio;
                height *= ratio;

                var texArea = new Rect(previewArea.xMin + previewArea.width / 2 - width / 2, previewArea.yMin + previewArea.height / 2 - height / 2, width, height);

                data.SetShaderData(data.IsFloatTexture ? displayFloatTextureMaterial : displayTextureMaterial, "_MainTex");
                EditorGUI.DrawPreviewTexture(texArea, data.Registration.Chart.Data.Texture, data.IsFloatTexture ? displayFloatTextureMaterial : displayTextureMaterial);

                //EditorGUI.LabelField(previewArea, "Current RenderTargets: " + Resources.FindObjectsOfTypeAll(typeof(FXRenderTexture)).Length.ToString());
            }
        }

        if (!AssetDatabase.Contains(data))
        {
            EditorGUILayout.HelpBox("This RenderTexture is automatic generated and is not an asset which is assignable and will only be usable for global shader properties, click the Export Button to create an asset from it.", MessageType.Info);
            if (GUILayout.Button("Export"))
            {
                CreatePhysicalAsset(data, data.DefaultName.Replace("_", string.Empty) + " RenderTexture");
            }
            EditorGUILayout.Space();
        }

        if (data.SizeMode != FXRenderTexture.FXRenderTextureSizeMode.ImageEffect)
        {
            EditorGUILayout.HelpBox("RenderTextures are grouped for faster scene captures by there Priority, change the Priority to allow one group to be rendered before an other.", MessageType.Info);
            var priority = EditorGUILayout.IntField("Priority", data.Priority);
            data.GroupName = priority.ToString();
        }

        EditorGUILayout.HelpBox("Only the lowest Update Interval per priority group will be used.", MessageType.Info);
        EditorGUILayout.BeginHorizontal();
        data.UpdateIntervalInMilliseconds = EditorGUILayout.IntField("Update Interval (ms)", data.UpdateIntervalInMilliseconds);
        if (data.Registration.Chart != null && GUILayout.Button("For Group"))
        {
            var allTexturesInGroup = FXRenderTextureManager.Groups.First(group => group.Name == data.GroupName).Textures.ToArray();
            foreach (var tex in allTexturesInGroup)
            {
                tex.UpdateIntervalInMilliseconds = data.UpdateIntervalInMilliseconds;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("The Shader Property Name will be used to set a global texture with the given name for all materials. This will allow to set the default texture for objects without a set material property.", MessageType.Info);
        data.DefaultName = EditorGUILayout.TextField("Shader Property Name", data.DefaultName);

		var newSizeMode = (FXRenderTexture.FXRenderTextureSizeMode)EditorGUILayout.EnumPopup("Size Mode: ", data.SizeMode);
        if (newSizeMode != data.SizeMode && data.SizeMode == FXRenderTexture.FXRenderTextureSizeMode.ImageEffect)
        {
            if (EditorUtility.DisplayDialog("Changing Texture Size Mode", "Changing this mode will make the usage of this RenderTexture for PostProcessing Grabs invalid, are you sure?", "Yes", "No"))
            {
                data.SizeMode = newSizeMode;
            }
        }
		else
			data.SizeMode = newSizeMode;
			
        if (data.SizeMode == FXRenderTexture.FXRenderTextureSizeMode.Factor)
		{
            data.SizeFactor = EditorGUILayout.Slider("Size Factor", data.SizeFactor, 0.01f, 1.0f);
		}
		else if (data.SizeMode == FXRenderTexture.FXRenderTextureSizeMode.MaxSize)
		{
            data.MaximumSize = EditorGUILayout.Vector2Field("Max Size: ", data.MaximumSize);
		}
    }

    private static void EnsureDisplayMaterial()
    {
        if (!displayFloatTextureMaterial)
        {
            displayFloatTextureMaterial = new Material(Shader.Find("Hidden/FXLab/DisplayFloatTexture"));
            displayFloatTextureMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        if (!displayTextureMaterial)
        {
            displayTextureMaterial = new Material(Shader.Find("Hidden/FXLab/DisplayTexture"));
            displayTextureMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    void OnEnable()
    {
        FXRenderTextureData.OnApply += OnApply;
    }

    void OnDisable()
    {
        FXRenderTextureData.OnApply -= OnApply;
    }

    void OnApply(FXRenderTextureData data)
    {
        Repaint();
    }
}