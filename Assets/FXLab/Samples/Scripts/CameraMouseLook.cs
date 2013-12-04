using UnityEngine;
using System.Collections;

public class CameraMouseLook : MonoBehaviour {

public Vector2 Sensitivity = Vector2.one;
public Vector2 MinimumAngle = new Vector2(-360, -60);
public Vector2 MaximumAngle = new Vector2(360, 60);
 
private Vector2 rotation;
private Quaternion originalRotation;
 
void Start ()
{
originalRotation = transform.localRotation;
}
 
void Update ()
{
rotation += new Vector2(Input.GetAxis("Mouse X") * Sensitivity.x, Input.GetAxis("Mouse Y") * Sensitivity.y);
rotation = ClampAngle(rotation, MinimumAngle, MaximumAngle);
transform.localRotation = originalRotation * Quaternion.AngleAxis(rotation.x, Vector3.up) * Quaternion.AngleAxis(rotation.y, Vector3.left);
}
 
private Vector2 ClampAngle(Vector2 angle, Vector2 minAngle, Vector2 maxAngle)
{
return new Vector2(ClampAngle(angle.x, minAngle.x, maxAngle.x), ClampAngle(angle.y, minAngle.y, maxAngle.y));
}
 
private float ClampAngle(float angle, float minAngle, float maxAngle)
{
if (angle < -360)
angle += 360;
if (angle > 360)
angle -= 360;
return Mathf.Clamp(angle, minAngle, maxAngle);
}
}
