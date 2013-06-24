using UnityEngine;
using System.Collections;

public class ChangeTexture : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
		// change material
		Material mat = Resources.Load("Models/Mario/Materials/" + "mario_mime", typeof(Material)) as Material;
		if (mat != null)
		{
		    this.renderer.material = mat;
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
