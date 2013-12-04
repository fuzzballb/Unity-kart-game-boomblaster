using UnityEngine;
using System.Collections;

public class RotateAroundScript : MonoBehaviour {
	
	public GameObject RotOriginObj;
	public float speed;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		transform.RotateAround (RotOriginObj.transform.position, Vector3.up, speed * Time.deltaTime);
	}
}
