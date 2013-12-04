using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("FXLab/Textures/Reflection")]
public class FXReflectionTexture : FXTexture
{
	public bool CaptureShadows = false;
	public GameObject ReflectionPlane;
	public float ClipPlaneOffset = 0.07f;
	
	public override string DefaultMaterialSlot
	{
		get
		{
			return "_FXReflectionTexture";
		}
	}
	
	public override void Render(Camera renderCamera)
	{
		var oldShadowDistance = QualitySettings.shadowDistance;
		if (!CaptureShadows)
			QualitySettings.shadowDistance = 0;
			
        if (!ReflectionPlane)
        {
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.black;
            renderCamera.Render();
			
			QualitySettings.shadowDistance = oldShadowDistance;
            return;
        }

		var pos = ReflectionPlane.transform.position;
		var normal = ReflectionPlane.transform.up;
		var reflectionPlane = new Plane(normal, pos);
		Shader.SetGlobalVector("_ReflectionPlaneEquation", new Vector4(reflectionPlane.normal.x, reflectionPlane.normal.y, reflectionPlane.normal.z, reflectionPlane.distance));

        var d = -Vector3.Dot(normal, pos) - ClipPlaneOffset;
        var reflectionVector = new Vector4(normal.x, normal.y, normal.z, d);

        var reflectionMatrix = Matrix4x4.zero;
        CalculateReflectionMatrix(ref reflectionMatrix, reflectionVector);
        var oldPos = camera.transform.position;
        var newPos = reflectionMatrix.MultiplyPoint(oldPos);
        renderCamera.worldToCameraMatrix = camera.worldToCameraMatrix * reflectionMatrix;

        var clipPlane = CameraSpacePlane(renderCamera, pos, normal, 1.0f);
        var projectionMatrix = camera.projectionMatrix;
        CalculateObliqueMatrix(ref projectionMatrix, clipPlane);
        renderCamera.projectionMatrix = projectionMatrix;

        GL.SetRevertBackfacing(true);
        renderCamera.transform.position = newPos;
        var euler = camera.transform.eulerAngles;
        renderCamera.transform.eulerAngles = new Vector3(0, euler.y, euler.z);
        base.Render(renderCamera);
        renderCamera.transform.position = oldPos;
        GL.SetRevertBackfacing(false);
		
		QualitySettings.shadowDistance = oldShadowDistance;
	}
	
	private Vector4 CameraSpacePlane(Camera camera, Vector3 position, Vector3 normal, float sideSign)
    {
        var offsetPos = position + normal * ClipPlaneOffset;
        var viewMatrix = camera.worldToCameraMatrix;
        var cameraPosition = viewMatrix.MultiplyPoint(offsetPos);
        var cameraDirection = viewMatrix.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cameraDirection.x, cameraDirection.y, cameraDirection.z, -Vector3.Dot(cameraPosition, cameraDirection));
    }

    private void CalculateObliqueMatrix(ref Matrix4x4 projection, Vector4 clipPlane)
    {
        Vector4 obliquePlane = projection.inverse * new Vector4(
            Mathf.Sign(clipPlane.x),
            Mathf.Sign(clipPlane.y),
            1.0f,
            1.0f
        );
        clipPlane = clipPlane * (2.0f / (Vector4.Dot(clipPlane, obliquePlane)));
        projection[2] = clipPlane.x - projection[3];
        projection[6] = clipPlane.y - projection[7];
        projection[10] = clipPlane.z - projection[11];
        projection[14] = clipPlane.w - projection[15];
    }

    private void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1 - 2 * plane[0] * plane[0]);
        reflectionMat.m01 = (-2 * plane[0] * plane[1]);
        reflectionMat.m02 = (-2 * plane[0] * plane[2]);
        reflectionMat.m03 = (-2 * plane[3] * plane[0]);

        reflectionMat.m10 = (-2 * plane[1] * plane[0]);
        reflectionMat.m11 = (1 - 2 * plane[1] * plane[1]);
        reflectionMat.m12 = (-2 * plane[1] * plane[2]);
        reflectionMat.m13 = (-2 * plane[3] * plane[1]);

        reflectionMat.m20 = (-2 * plane[2] * plane[0]);
        reflectionMat.m21 = (-2 * plane[2] * plane[1]);
        reflectionMat.m22 = (1 - 2 * plane[2] * plane[2]);
        reflectionMat.m23 = (-2 * plane[3] * plane[2]);

        reflectionMat.m30 = 0;
        reflectionMat.m31 = 0;
        reflectionMat.m32 = 0;
        reflectionMat.m33 = 1;
    }
}