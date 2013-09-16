using UnityEngine;
using System.Collections;

public class CarCam : MonoBehaviour {
	//public Camera useCamera;
	public Transform trackObject;
	private Vector3 camDistance;
	private Quaternion camRotation;
	private Transform _transform;

	
	public float height;
	public float angleX;

	
	// Use this for initialization
	void Start () {
		_transform = Camera.mainCamera.transform;
	}
	
	// Update is called once per frame
	void Update () {
		camDistance = new Vector3(0.0f,height,-angleX);
		
		// Matrix Rotation done before position
		var newPosition = Quaternion.Euler(-65.0f,trackObject.rotation.eulerAngles.y,0.0f) * camDistance + trackObject.position;
		_transform.position = Vector3.Lerp(_transform.position, newPosition, 4.0f*Time.deltaTime);
		
		// Always look at the target
    	_transform.LookAt(trackObject);
	}
}
