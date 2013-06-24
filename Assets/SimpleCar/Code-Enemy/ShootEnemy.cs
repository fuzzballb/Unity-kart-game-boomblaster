using UnityEngine;
using System.Collections;

public class ShootEnemy : MonoBehaviour {
	
	//public GameObject bullet;
	public float spawnDistanceForward = 2.3f; // don't want the bullet spawn in centre
	public float spawnDistanceUp = 1.0f;
	public float reloadTime = 100.0f;
	private float tempReloadTime = 0.0f;
	// Use this for initialization
	void Start () {
		tempReloadTime	=	reloadTime;
	}
	
	// Update is called once per frame
	void Update () {
		
		tempReloadTime -= 10.0f * Time.deltaTime;
		
		if(tempReloadTime < 0.0f)
		{
			// Get all human players
			GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

			foreach(GameObject player in players)
			{
				var targetDir = player.transform.position - transform.position;
		        var forward = transform.forward;
		        var angle = Vector3.Angle(targetDir, forward);
		        if (angle < 5.0)
				{
					GameObject bullet = PhotonNetwork.Instantiate("BomfabEnemy", transform.position + (spawnDistanceForward * transform.forward)+ (spawnDistanceUp * transform.up),transform.rotation, 0);
					BulletAi controller = bullet.GetComponent<BulletAi>();
					controller.enabled = true;
					tempReloadTime = reloadTime;
				}
			}
		}
	}
}
