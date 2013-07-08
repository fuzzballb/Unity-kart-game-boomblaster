using UnityEngine;
using System.Collections;
using RAIN.Core;

public class SingleMatch : Photon.MonoBehaviour {
	
	public static int playerScore = 0;
	public static int enemyScore = 0;
	
	// Use this for initialization
	void Start () {
		PhotonNetwork.ConnectUsingSettings("0.2");
		//PhotonNetwork.offlineMode = true;
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
		
		// Add an AI character to the room
		GameObject SimpleAICharacter = PhotonNetwork.Instantiate("SimpleAICharacterfab", spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position, Quaternion.identity, 0);
		RAINAgent AIController = SimpleAICharacter.GetComponent<RAINAgent>();
		AIController.enabled = true;
		ShootEnemy ShootControlerEnemy = SimpleAICharacter.GetComponent<ShootEnemy>();
		ShootControlerEnemy.enabled = true;
		
		
		// Add a PickUp
		var pickup1 = PhotonNetwork.Instantiate("PickUpfab", spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position, Quaternion.identity, 0);

		// Add an AI character to the room
		//GameObject SimpleAICharacter2 = PhotonNetwork.Instantiate("SimpleAICharacterfab", spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position, Quaternion.identity, 0);
		//RAINAgent AIController2 = SimpleAICharacter2.GetComponent<RAINAgent>();
		//AIController2.enabled = true;
		//ShootEnemy ShootControlerEnemy2 = SimpleAICharacter2.GetComponent<ShootEnemy>();
		//ShootControlerEnemy2.enabled = true;
	}
	
}
