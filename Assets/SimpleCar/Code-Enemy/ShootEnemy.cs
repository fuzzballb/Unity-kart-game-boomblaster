using UnityEngine;
using System.Collections;
using RAIN.Core;

public class ShootEnemy : MonoBehaviour {
	
	//public GameObject bullet;
	public float spawnDistanceForward = 2.3f; // don't want the bullet spawn in centre
	public float spawnDistanceUp = 1.0f;
	public float reloadTime = 50.0f;
	public float enemyShootAngle = 10.0f;
	
	private float tempReloadTime = 0.0f;
	private Transform _transform;
	
	// Use this for initialization
	void Start () {
		reloadTime = SingleMatch.enemyReloadTime;
		enemyShootAngle = SingleMatch.enemyShootAngle;
		
		
		tempReloadTime = reloadTime;
		_transform = transform;
	}
	
	// Update is called once per frame
	void Update () {
		
		tempReloadTime -= 10.0f * Time.deltaTime;
		
		if(tempReloadTime < 0.0f)
		{
			// Get all human players
			GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
			
			//RAINAgent ai = gameObject.GetComponent<RAINAgent>();
			AIRig ai = gameObject.transform.Find("AI").GetComponent<AIRig>();
			
	
			foreach(GameObject player in players)
			{
				var targetDir = player.transform.position - _transform.position;
		        var forward = _transform.forward;
		        var angle = Vector3.Angle(targetDir, forward);
				
				// Get ammo count from AI
			//	int ammo = ai.Agent.actionContext.GetContextItem<int>("ammo");
				int ammo = ai.AI.WorkingMemory.GetItem<int>("ammo");
			
				
		        if (angle < enemyShootAngle && ammo > 0)
				{
					
					GameObject bullet = PhotonNetwork.Instantiate("BomfabEnemy", _transform.position + (spawnDistanceForward * _transform.forward)+ (spawnDistanceUp * _transform.up),_transform.rotation, 0);
					BulletAi controller = bullet.GetComponent<BulletAi>();
					controller.enabled = true;
					
					tempReloadTime = reloadTime;
					
				//	Debug.Log( "AI ammo " + ammo);
					// Set ammo count to AI
				//	ai.Agent.actionContext.SetContextItem<int>("ammo", ammo-1);
					ai.AI.WorkingMemory.SetItem<int>("ammo", ammo-1);
				}
			}
		}
	}
}
