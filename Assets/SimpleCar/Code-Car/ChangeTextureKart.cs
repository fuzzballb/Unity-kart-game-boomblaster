using UnityEngine;
using System.Collections;

public class ChangeTextureKart : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
		// change material

	
	}
	
	public void makeTransparant(bool transparant)
	{
		string materialPath = "";
		if(transparant)
		{
			materialPath = "Models/Kart/Materials/kart_transperant";
		}
		else
		{
			materialPath = "Models/Kart/Materials/kart_III_uv_grid_ps";
		}
		
		Material mat = Resources.Load(materialPath, typeof(Material)) as Material;
		
		if (mat != null)
		{
			Material[] mats = renderer.materials;
			for(int i=0; i< this.renderer.materials.Length; i++)
			{
				mats[i] = mat;
			}
			this.renderer.materials = mats;
		}		
	}
	
	
	
	// Update is called once per frame
	void Update () {
		
	}
}
