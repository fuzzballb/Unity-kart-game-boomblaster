using UnityEngine;
using System.Collections;

public class Billboard : MonoBehaviour
{
	void OnWillRenderObject()
	{
		transform.parent.LookAt(Camera.current.transform.position);
	}
}
