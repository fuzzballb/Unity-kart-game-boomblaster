using UnityEngine;
using System.Collections;

public class Rotate : MonoBehaviour
{

    public bool Up = false;
    public bool Forward = true;
    public bool Right = false;
    public float Speed = 0.025f;
	
	void FixedUpdate()
    {
        if (Up)
            transform.Rotate(Vector3.up, Speed);
        if (Forward)
            transform.Rotate(Vector3.forward, Speed);
        if (Right)
            transform.Rotate(Vector3.right, Speed);
	}
}
