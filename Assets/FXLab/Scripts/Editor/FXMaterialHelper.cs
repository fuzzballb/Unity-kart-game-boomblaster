using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text.RegularExpressions;

public class FXMaterialHelper
{
    public class FXMaterialSlotDescriptor
    {
        public string PropertyName;
        public System.Type Type;

        public FXMaterialSlotDescriptor(string name, System.Type type)
        {
            PropertyName = name;
            Type = type;
        }
    }

    private static FXCamera[] fxCameras;
    private static FXCaptureScreen[] fxCaptureScreens;

    private static System.Type[] fXTextureTypes;
    public static System.Type[] FXTextureTypes
    {
        get
        {
            if (fXTextureTypes != null)
                return fXTextureTypes;

            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            fXTextureTypes = assemblies.SelectMany(assembly =>
            {
                var types = assembly.GetExportedTypes();
                return types.Where(type => type != null && type.IsSubclassOf(typeof(FXTexture)) && !type.IsAbstract);
            }).ToArray();

            return fXTextureTypes;
        }
    }

    public static void ResetCache()
    {
        fxCameras = null;
        fxCaptureScreens = null;
        fXTextureTypes = null;
    }

    public static void DrawRequiredFXTextures(Material material)
    {
        if (Selection.activeGameObject == null)
            return;
		var gameObject = Selection.activeGameObject;
		
        if (fxCameras == null)
            fxCameras = (FXCamera[])GameObject.FindObjectsOfType(typeof(FXCamera));
		if (fxCaptureScreens == null)
			fxCaptureScreens = (FXCaptureScreen[])GameObject.FindObjectsOfType(typeof(FXCaptureScreen));
			
        var requiredTextures = RequiredFXTextures(material);
        var missingTextures = requiredTextures.Where(descriptor =>
            {
				var fxTextureAssigners = gameObject.GetComponents<FXTextureAssigner>();
                if (fxTextureAssigners.Any(ta => ta.Assignments.Any(assignment => 
					{
						if (assignment.Material != material)
							return false;
						if (assignment.TextureName != descriptor.PropertyName)
							return false;
						if (assignment.RenderTexture == null)
							return false;
							
						return true;
					})))
                    return false;

                var availableTextures = FXRenderTextureManager.Groups.SelectMany(group => group.Textures).ToArray();
                if (availableTextures.Length == 0)
                    return true;

                return !availableTextures.Any(texture => texture.DefaultName == descriptor.PropertyName);
               
            }).ToArray();
        
        if (missingTextures.Length > 0)
        {
            var strings = missingTextures.Select(missing => string.Format("{0} ({1})", missing.PropertyName, ObjectNames.NicifyVariableName(missing.Type.Name))).ToArray();
			
			EditorGUILayout.HelpBox("Missing FXTextures:\n" + string.Join("\n", strings), MessageType.Warning);
			if (fxCameras.Length == 0 && Camera.main == null)
				EditorGUILayout.HelpBox("No Main Camera found, create one or add the FXCamera component to any other camera.", MessageType.Error);
			else if (GUILayout.Button("Add FXTextures"))
            {
				if (fxCameras.Length == 0)
					fxCameras = new FXCamera[] { Camera.main.gameObject.AddComponent<FXCamera>() };
                foreach (var missing in missingTextures)
                {
					if (missing.Type == null)
						continue;
						
                    var fxTexture = (FXTexture)fxCameras[0].gameObject.AddComponent(missing.Type);
                    fxTexture.RenderTexture.DefaultName = missing.PropertyName;
                }
            }
        }
    }

    public static System.Type GetFXTextureType(string shaderSlotDescription)
    {
        var match = Regex.Match(shaderSlotDescription, @".*\((\w*)\).*", RegexOptions.IgnoreCase);
        if (match.Success)
            return FXTextureTypes.FirstOrDefault(type => type.Name.ToLower() == match.Groups[1].Value.Trim().ToLower());
        return null;
    }

    public static FXMaterialSlotDescriptor[] RequiredFXTextures(Material material)
    {
        if (material == null)
            return new FXMaterialSlotDescriptor[0];

        var shader = material.shader;

        var fxSlots = new List<FXMaterialSlotDescriptor>();
        for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
        {
            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                var desc = ShaderUtil.GetPropertyDescription(shader, i);
                var match = Regex.Match(desc, @".*\((\w*)\).*", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var typeName = match.Groups[1].Value.Trim();
                    var fxTextureType = FXTextureTypes.FirstOrDefault(type => type.Name.ToLower() == typeName.ToLower());
                    fxSlots.Add(new FXMaterialSlotDescriptor(ShaderUtil.GetPropertyName(shader, i), fxTextureType));
                }
            }
        }

        return fxSlots.ToArray();
    }

    public static void DumpStats(Material targetMat)
    {
        var propertyCount = ShaderUtil.GetPropertyCount(targetMat.shader);
        for (int i = 0; i < propertyCount; ++i)
        {
            var type = ShaderUtil.GetPropertyType(targetMat.shader, i);
            var name = ShaderUtil.GetPropertyName(targetMat.shader, i);
            switch (type)
            {
                case ShaderUtil.ShaderPropertyType.Color:
                    Debug.Log(name + ": " + targetMat.GetColor(name));
                    break;
                case ShaderUtil.ShaderPropertyType.Float:
                    Debug.Log(name + ": " + targetMat.GetFloat(name));
                    break;
                case ShaderUtil.ShaderPropertyType.Range:
                    Debug.Log(name + ": " + targetMat.GetFloat(name));
                    break;
                case ShaderUtil.ShaderPropertyType.Vector:
                    Debug.Log(name + ": " + targetMat.GetVector(name));
                    break;
            }
        }
    }
}