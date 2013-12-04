using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("FXLab/FX Post Process")]
public class FXPostProcess : MonoBehaviour
{
    private static Texture2D dummyTexture;
	
	public Material Material;
    public List<FXTextureAssigner.RenderTextureAssignment> Assignments = new List<FXTextureAssigner.RenderTextureAssignment>();
   
    void OnPostRender()
    {
        if (!enabled || Material == null)
            return;

        if (!dummyTexture)
        {
            dummyTexture = new Texture2D(1, 1);
            dummyTexture.hideFlags = HideFlags.HideAndDontSave;
        }

        Shader.SetGlobalVector("_CameraViewDirection", camera.transform.forward);
        Shader.SetGlobalVector("_CameraRotation", camera.transform.eulerAngles);

        foreach (var assignment in Assignments)
            assignment.Apply(gameObject);

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, camera.pixelRect.width, camera.pixelRect.height, 0);
        var area = camera.pixelRect;
        Graphics.DrawTexture(area, dummyTexture, Material);
        GL.PopMatrix();
    }

    void Start()
    {
		// to display the enable checkbox, dont remove this
    }
}