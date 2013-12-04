using UnityEngine;
using System.Collections;
 
public class CameraMouseRotate : MonoBehaviour {

    public Transform target;
    public Vector3 targetOffset;
    public float distance = 100.0f;
    public float maxDistance = 100.0f;
    public float minDistance = 25.0f;
    public float xSpeed = 200.0f;
    public float ySpeed = 200.0f;
	public float xMinLimit = -45;
    public float xMaxLimit = 45;
    public float yMinLimit = -45;
    public float yMaxLimit = 45;
    public int zoomRate = 40;
    public float panSpeed = 0.3f;
    public float zoomDampening = 5.0f;
 
    private float xDeg = 0.0f;
    private float yDeg = 0.0f;
    private float currentDistance;
    private float desiredDistance;
    private Quaternion currentRotation;
    private Quaternion desiredRotation;
    private Quaternion rotation;
    private Vector3 position;
 
    public void Start()
    {
        distance = Vector3.Distance(transform.position, target.position);
        currentDistance = distance;
        desiredDistance = distance;

        position = transform.position;
        rotation = transform.rotation;
        currentRotation = transform.rotation;
        desiredRotation = transform.rotation;
		
		xMinLimit = transform.eulerAngles.y - Mathf.Abs(xMinLimit);
		xMaxLimit = transform.eulerAngles.y + xMaxLimit;
 
        xDeg = Vector3.Angle(Vector3.right, transform.right );
        yDeg = Vector3.Angle(Vector3.up, transform.up );
    }
 
    void LateUpdate()
    {
        if (Input.GetMouseButton(0))
            desiredDistance -= Input.GetAxis("Mouse Y") * Time.deltaTime * zoomRate*0.125f * Mathf.Abs(desiredDistance);
		
		if (!Input.GetMouseButton(0)) {
        xDeg += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
        yDeg -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

		xDeg = ClampAngle(xDeg, xMinLimit, xMaxLimit);
        yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);

        desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);
        currentRotation = transform.rotation;
 
        rotation = Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime * zoomDampening);
        transform.rotation = rotation;
		}

        desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * zoomRate * Mathf.Abs(desiredDistance);
        desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * zoomDampening);
 
        position = target.position - (rotation * Vector3.forward * currentDistance + targetOffset);
        transform.position = position;
    }
 
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}