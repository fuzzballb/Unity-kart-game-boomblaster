using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Core;

public class SingleMatch : Photon.MonoBehaviour {
	
	public  int level = 3;
	public  int currentLevel = 1;
	public  int playerScore = 1;
	public  int enemyScore = 0;
	
	public static float enemyReloadTime = 100.0f;
	public static int enemyBulletStore = 2;
	public static float enemyShootAngle = 10.0f;
	
	public  int currentEnemyPlayers = 1;
	public  int desiredAmountOfEnemyPlayers = 1;
	
	public static bool connecting = false;
	private float amountOfTimeTillReAppear = 2.0f;
	
	// Added this list, because gameObject can't be found by tag if they are inactive
	public static List<GameObject> objectsToHide = new List<GameObject>();
	
	
	// Use this for initialization
	void Start () {
		PhotonNetwork.ConnectUsingSettings("0.3"); // make sure this ID is different form the multiplayer ID
	}
	
	// Update is called once per frame
	void OnGUI () {
	
		// This is the single player scene, so don't create a real room
		if(!connecting)
		{
			PhotonNetwork.offlineMode = true;
			PhotonNetwork.CreateRoom(null);
			connecting = true;
		}
		
		GUILayout.Label(PhotonNetwork.connectionStateDetailed.ToString());
	}
	
	void OnJoinedRoom()
	{
		// start adding players to the room
		StartCoroutine(WaitAndCreatePlayer(1.0f));
		StartCoroutine(hideLevelText(2.0f));
		StartCoroutine(WaitAndCreateAI(3.0f)); // make shure this is called after "amountOfTimeTillReAppear"
	
		
		GameObject.FindGameObjectWithTag("GUI_Level").guiText.text = "Level " + level;
		
	}

	IEnumerator WaitAndCreatePlayer(float waitTime) {
        yield return new WaitForSeconds(waitTime);
		
		var spawnpoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
		
		var pickup1 = PhotonNetwork.Instantiate("PickUpfab", spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position, Quaternion.identity, 0);
		objectsToHide.Add(pickup1);
		
		
		var car = PhotonNetwork.Instantiate("CarPrefab", spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position, Quaternion.identity, 0);
		objectsToHide.Add(car);
		
		CarCam camControler = car.GetComponent<CarCam>();
		camControler.enabled = true;	
		CarDriver controller = car.GetComponent<CarDriver>();
		controller.enabled = true;
		Shoot ShootControler = car.GetComponent<Shoot>();
		ShootControler.enabled = true;
	}
	
	IEnumerator WaitAndCreateAI(float waitTime) {
        yield return new WaitForSeconds(waitTime);
		
		Debug.Log("start create new AI");	
		var spawnpoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
		var player = GameObject.FindGameObjectWithTag("Player");
		//
		// choose a spawnpoint that is as far away from the player as possible
		//
		Vector3 emptyPosition = new Vector3();
		float distance = 0.0f;
		
		if(player != null)
		{
			Vector3 playerPosition = player.transform.position;
			foreach(GameObject spawnpoint in spawnpoints)			
			{
				//Debug.Log("spawnpoint.transform.position" + spawnpoint.transform.position + "player.transform.position" + playerPosition); 
				Vector3 spawnPosition = spawnpoint.transform.position;
				float distanceToPlayer = Vector3.Distance(spawnPosition, playerPosition);
				if(distanceToPlayer > distance)
				{
					distance = distanceToPlayer;
					emptyPosition = spawnPosition;
				}
			}
		}
		else	
		{
			// When the level switches, the player cant be found by tag, because the player is is not active
			emptyPosition = spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position;
		}
		
		
		
		GameObject SimpleAICharacter = PhotonNetwork.Instantiate("SimpleAICharacterfab", emptyPosition, Quaternion.identity, 0);		
		objectsToHide.Add(SimpleAICharacter);

		Debug.Log("done create new AI");
	}
	
	
	
	//
	//	During gameplay
	//  Gets called form Bullet AI
	public void CreateNewAI()
	{
		currentEnemyPlayers--;
		
		while(currentEnemyPlayers < desiredAmountOfEnemyPlayers)
		{
			currentEnemyPlayers++;
			StartCoroutine(WaitAndCreateAI(amountOfTimeTillReAppear));
		}
	}
	

	
	public void UpdateScore()
	{
		GameObject.FindGameObjectWithTag("GUI_score").guiText.text = playerScore + " - " + enemyScore;
	
		/* Start values
		level = 1;
		currentLevel = 1;
		playerScore = 0;
		enemyScore = 0;
		currentEnemyPlayers = 1;
		desiredAmountOfEnemyPlayers = 1;
		*/
		
		
		if(playerScore >= 3 || enemyScore >= 3)
		{
			if(enemyScore >= 3)
			{
				// player dead scene, 
				// return to menu
				Application.LoadLevel("Dead");
			}
			else
			{
				// Go to next level, set all objects to inactive
				var carObject = GameObject.FindGameObjectWithTag("Player");
				var Pickup = GameObject.FindGameObjectWithTag("PickUp");
				var Enemy = GameObject.FindGameObjectWithTag("Enemy");
				var Explosion = GameObject.FindGameObjectWithTag("Explosion");
				carObject.SetActive(false);
				Pickup.SetActive(false);
				Enemy.SetActive(false);
			
				
				Explosion.particleSystem.enableEmission = false;
				
				level++;
			}
		}
		
		//level = 4;
		
		if(level > currentLevel)
		{
			switch(level)
			{
				case 1: // first environment 1 enemy
				{
					desiredAmountOfEnemyPlayers = 1;
					break;
				}
				case 2: // first environment 1 enemy
				{
					// show level text
					GameObject.FindGameObjectWithTag("GUI_score").guiText.text = "0 - 0";
					GameObject.FindGameObjectWithTag("GUI_Level").guiText.text = "Level " + level;
					StartCoroutine(hideLevelText(2.0f));
		
		//
		//	Fog
		//		
		string materialPath = "Skyboxes/Overcast2 Skybox";
		Material mat = Resources.Load(materialPath, typeof(Material)) as Material;
		RenderSettings.skybox = mat;
		RenderSettings.ambientLight = new Color(0.8f, 0.8f, 0.8f, 1);
		
		// Edit level color
		GameObject levelThePit = GameObject.FindGameObjectWithTag("ThePit");
		Color clr = new Color(1.0f, 1.0f, 1.0f, 1);
		levelThePit.renderer.material.SetColor("_Color", clr);	
		
		materialPath = "Level/Materials/Default";
		mat = Resources.Load(materialPath, typeof(Material)) as Material;
		levelThePit.renderer.material = mat;
	
		// Add fog to scene
		RenderSettings.fog = true;
		RenderSettings.fogDensity = 0.02f;
		RenderSettings.fogColor = new Color(0.3f, 0.3f, 0.3f, 1);
		//
		//	End Fog
		//					
					// shorten enemy reload time
					enemyReloadTime = 100.0f;
					enemyShootAngle = 10.0f;
				
					// Make AI and pickups available again after 5 seconds
					StartCoroutine(enableRenderes(amountOfTimeTillReAppear));
				
					desiredAmountOfEnemyPlayers = 1;
					playerScore = 0;
					enemyScore = 0;
					break;
				}
				case 3: // first environment 1 enemy
				{
				
					// show level text
					GameObject.FindGameObjectWithTag("GUI_score").guiText.text = "0 - 0";
					GameObject.FindGameObjectWithTag("GUI_Level").guiText.text = "Level " + level;
					StartCoroutine(hideLevelText(2.0f));
		
		//
		//	Evening
		//
		string materialPath = "Skyboxes/DawnDusk Skybox";
		Material mat = Resources.Load(materialPath, typeof(Material)) as Material;
		RenderSettings.skybox = mat;
		RenderSettings.ambientLight = new Color(0.7f, 0.3f, 0.3f, 1);
		
		// Edit level color
		GameObject levelThePit = GameObject.FindGameObjectWithTag("ThePit");
		Color clr = new Color(1.0f, 0.8f, 0.8f, 1);
		levelThePit.renderer.material.SetColor("_Color", clr);	
		
		materialPath = "Level/Materials/Default";
		mat = Resources.Load(materialPath, typeof(Material)) as Material;
		levelThePit.renderer.material = mat;
	
		// Add fog to scene
		RenderSettings.fog = false;
		RenderSettings.fogDensity = 0.01f;
		RenderSettings.fogColor = clr;
		//
		//	End Evening
		//
				

					// shorten enemy reload time
					enemyReloadTime = 75.0f;
					enemyShootAngle = 7.5f;
				
					// Make AI and pickups available again after 5 seconds
					StartCoroutine(enableRenderes(amountOfTimeTillReAppear));
				
				
					desiredAmountOfEnemyPlayers = 1;
					playerScore = 0;
					enemyScore = 0;			
					break;
				}
				case 4: // second environment 2 enemies
				{
									// show level text
					GameObject.FindGameObjectWithTag("GUI_score").guiText.text = "0 - 0";
					GameObject.FindGameObjectWithTag("GUI_Level").guiText.text = "Level " + level;
					StartCoroutine(hideLevelText(2.0f));
		//
		//	Snow
		//				
		string materialPath = "Skyboxes/Sunny1 Skybox";
		Material mat = Resources.Load(materialPath, typeof(Material)) as Material;
		RenderSettings.skybox = mat;
		RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f, 1);
		
		// Edit level color
		GameObject levelThePit = GameObject.FindGameObjectWithTag("ThePit");
		Color clr = new Color(1.0f, 1.0f, 1.0f, 1);
		levelThePit.renderer.material.SetColor("_Color", clr);
		
		materialPath = "Level/Materials/Snow";
		mat = Resources.Load(materialPath, typeof(Material)) as Material;
		levelThePit.renderer.material = mat;
	
		// Add fog to scene
		RenderSettings.fog = true;
		RenderSettings.fogDensity = 0.01f;
		RenderSettings.fogColor = clr;
		//
		//	End Snow
		//						

					// shorten enemy reload time
					enemyReloadTime = 50.0f;
					enemyShootAngle = 5.0f;
				
					// Make AI and pickups available again after 5 seconds
					StartCoroutine(enableRenderes(amountOfTimeTillReAppear));
				
				
					desiredAmountOfEnemyPlayers = 1;
					playerScore = 0;
					enemyScore = 0;
					break;
				}
				case 5: // second environment 2 enemies
				{
				
													// show level text
					GameObject.FindGameObjectWithTag("GUI_score").guiText.text = "0 - 0";
					GameObject.FindGameObjectWithTag("GUI_Level").guiText.text = "Level " + level;
					StartCoroutine(hideLevelText(2.0f));
		//
		//	Night
		//						
		string materialPath = "Skyboxes/MoonShine Skybox";
		Material mat = Resources.Load(materialPath, typeof(Material)) as Material;
		RenderSettings.skybox = mat;
		RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.6f, 1);
		
		// Edit level color
		GameObject levelThePit = GameObject.FindGameObjectWithTag("ThePit");
		Color clr = new Color(0.1f, 0.1f, 0.1f, 1);
		levelThePit.renderer.material.SetColor("_Color", clr);	
		
		materialPath = "Level/Materials/Default";
		mat = Resources.Load(materialPath, typeof(Material)) as Material;
		levelThePit.renderer.material = mat;	
	
		// Add fog to scene
		RenderSettings.fog = true;
		RenderSettings.fogDensity = 0.01f;
		RenderSettings.fogColor = clr;
		//
		//	End Night
		//					

					// extend enemy reload time but add enemy
					enemyReloadTime = 100.0f;
					enemyShootAngle = 10.0f;
				
					// Make AI and pickups available again after 5 seconds
					StartCoroutine(enableRenderes(amountOfTimeTillReAppear));
				
					desiredAmountOfEnemyPlayers = 2;
					playerScore = 0;
					enemyScore = 0;
					break;
				}
				case 6: // second environment 2 enemies
				{
				
													// show level text
					GameObject.FindGameObjectWithTag("GUI_score").guiText.text = "0 - 0";
					GameObject.FindGameObjectWithTag("GUI_Level").guiText.text = "Level " + level;
					StartCoroutine(hideLevelText(2.0f));
		//
		//	Alien
		//						
		string materialPath = "Skyboxes/Overcast2 Skybox";
		Material mat = Resources.Load(materialPath, typeof(Material)) as Material;
		RenderSettings.skybox = mat;
		RenderSettings.ambientLight = new Color(1.5f, 0.5f, 1.5f, 1);
		
		// Edit level color
		GameObject levelThePit = GameObject.FindGameObjectWithTag("ThePit");
		Color clr = new Color(0.4f, 1.3f, 0.4f, 1);
		levelThePit.renderer.material.SetColor("_Color", clr);	
	
		materialPath = "Level/Materials/Default";
		mat = Resources.Load(materialPath, typeof(Material)) as Material;
		levelThePit.renderer.material = mat;	
	
		// Add fog to scene
		RenderSettings.fog = true;
		RenderSettings.fogDensity = 0.001f;
		RenderSettings.fogColor = clr;
		//
		//	End Alien
		//			

					// extend enemy reload time but add enemy
					enemyReloadTime = 75.0f;
					enemyShootAngle = 7.5f;
				
					// Make AI and pickups available again after 5 seconds
					StartCoroutine(enableRenderes(amountOfTimeTillReAppear));
				
					desiredAmountOfEnemyPlayers = 2;
					playerScore = 0;
					enemyScore = 0;
					break;
				}
				case 7: // second environment 2 enemies
				{
					Application.LoadLevel("Compleated");
					break;
				}
					
				
			}
			currentLevel = level;
		}
	
	}
		
	IEnumerator enableRenderes(float waitTime) {
        yield return new WaitForSeconds(waitTime);
		
		var spawnpoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
		
		foreach(GameObject obj in objectsToHide)
		{
			try
			{
				obj.SetActive(true);
			//	int randomSpawnNumber = Random.Range(0,spawnpoints.Length);
			//	obj.transform.position = spawnpoints[randomSpawnNumber].transform.position;
			//	obj.transform.rotation = spawnpoints[randomSpawnNumber].transform.rotation;
			}
			catch(MissingReferenceException e)
			{
				// Object in list is not there anymore
				Debug.LogWarning("object missing " + e.Data);
			}
		}
	}
	
	IEnumerator hideLevelText(float waitTime) {
        yield return new WaitForSeconds(waitTime);
		GameObject.FindGameObjectWithTag("GUI_Level").guiText.text = "";
		
	}
	
	
}
