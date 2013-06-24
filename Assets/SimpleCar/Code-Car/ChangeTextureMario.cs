using UnityEngine;
using System.Collections;

public class ChangeTextureMario : MonoBehaviour {
	
		// Use this for initialization
	void Start () {
		
		// change material

	
	}
	
	// Use this for initialization
	public void makeTransparant(bool transparant)
	{	
		// change material
		//Material mat = Resources.Load("Models/Mario/Materials/" + "mario_mime", typeof(Material)) as Material;
		
		
		// Assets/SimpleCar/Resources/Models/Mario/Materials/mario_mime.mat
		
		/*
		string materialPath = AssetDatabase.GetAssetPath(material);
		materialPath = materialPath.TrimEnd(".mat");
		materialPath = materialPath.TrimStart("Assets/SimpleCar/Resources/");
		*/	

		string materialPath = "";
		if(transparant)
		{
			materialPath = "Models/Mario/Materials/mario_mime_transparent";
		}
		else
		{
			materialPath = "Models/Mario/Materials/mario_mime";
		}
		
		Material mat = Resources.Load(materialPath, typeof(Material)) as Material;
		
		if (mat != null)
		{
	//		Debug.Log("this.renderer.material " + this.renderer.material );
		    this.renderer.material = mat;
		}
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
