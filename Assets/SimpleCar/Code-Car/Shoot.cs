using UnityEngine;
using System.Collections;

public class Shoot : MonoBehaviour {
	
	//public GameObject bullet;
	public float spawnDistanceForward = 2.3f; // don't want the bullet spawn in centre
	public float spawnDistanceUp = 1.0f;
	public float reloadTime = 100.0f;
	private float tempReloadTime = 0.0f;
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		
		// Cool downs for Player weapon
		tempReloadTime -= 10.0f * Time.deltaTime;
		
		if(tempReloadTime < 0.0f)
		{
			if(Input.GetButton("Fire1"))
			{
				GameObject bullet = PhotonNetwork.Instantiate("Bomfab", transform.position + (spawnDistanceForward * transform.forward)+ (spawnDistanceUp * transform.up),transform.rotation, 0);
				BulletAi controller = bullet.GetComponent<BulletAi>();
				controller.enabled = true;

				tempReloadTime = reloadTime;
			}
		}
	}
}
