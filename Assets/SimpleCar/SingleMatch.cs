using UnityEngine;
using System.Collections;
using RAIN.Core;

public class SingleMatch : Photon.MonoBehaviour {
	
	public static int playerScore = 0;
	public static int enemyScore = 0;
	
	public static int currentEnemyPlayers = 1;
	public static int desiredAmountOfEnemyPlayers = 1;
	
	// Use this for initialization
	void Start () {
		
		PhotonNetwork.offlineMode = true;
		PhotonNetwork.ConnectUsingSettings("0.2");
	}
	
	// Update is called once per frame
	void OnGUI () {
		GUILayout.Label(PhotonNetwork.connectionStateDetailed.ToString());
	}
	
	void OnJoinedLobby()
	{
		PhotonNetwork.JoinRandomRoom();
	}
	
	void OnPhotonRandomJoinFailed()
	{
		PhotonNetwork.CreateRoom(null, false, false, 1);  // no name (gets a guid), invisible and closed with 1 players max
	}
	
	void OnJoinedRoom()
	{
		
		var spawnpoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
		
		// Add our player to the Room
		var car = PhotonNetwork.Instantiate("CarPrefab", spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position, Quaternion.identity, 0);
		CarDriver controller = car.GetComponent<CarDriver>();
		controller.enabled = true;
		CarCam camControler = car.GetComponent<CarCam>();
		camControler.enabled = true;
		Shoot ShootControler = car.GetComponent<Shoot>();
		ShootControler.enabled = true;
		
		StartCoroutine(WaitAndCreateAI(4.0f));
		
		// Add a PickUp
		var pickup1 = PhotonNetwork.Instantiate("PickUpfab", spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position, Quaternion.identity, 0);
	
	
		StartCoroutine(GoToNextLevel(10.0f));
			
	}
	
	IEnumerator GoToNextLevel(float waitTime) {
        yield return new WaitForSeconds(waitTime);
		
		// level up, add an extra AI player
		desiredAmountOfEnemyPlayers++;
		
		GameObject.FindGameObjectWithTag("GUI_Level").guiText.text = "Level " + desiredAmountOfEnemyPlayers;
		
		
		
	}
	
	public void CreateNewAI()
	{
		currentEnemyPlayers--;
		
		while(currentEnemyPlayers < desiredAmountOfEnemyPlayers)
		{
			currentEnemyPlayers++;
			StartCoroutine(WaitAndCreateAI(4.0f));
		}
	}
	
	
	IEnumerator WaitAndCreateAI(float waitTime) {
        yield return new WaitForSeconds(waitTime);
		
		Debug.Log("start create new AI");
		
		var spawnpoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
					
		GameObject SimpleAICharacter = PhotonNetwork.Instantiate("SimpleAICharacterfab", spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position, Quaternion.identity, 0);
		RAINAgent AIController = SimpleAICharacter.GetComponent<RAINAgent>();
		AIController.enabled = true;
		ShootEnemy ShootControlerEnemy = SimpleAICharacter.GetComponent<ShootEnemy>();
		ShootControlerEnemy.enabled = true;
		
		Debug.Log("done create new AI");
		
		
	}
	
	
}
