using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using RAIN.Core;

public class RandomMatchmakerCar : Photon.MonoBehaviour {
	// Use this for initialization
	public static int playerScore = 0;
	public static int enemyScore = 0;
	
	// cached objects
	GameObject GUI_Score;
	
	
	
	// Dictionary to keep track of scores for multiplayer
	public static Dictionary<int, int> scoreCount = new Dictionary<int, int>();
	
	void Start () {
		PhotonNetwork.ConnectUsingSettings("0.2");
		//PhotonNetwork.offlineMode = false;
		
		GUI_Score = GameObject.FindGameObjectWithTag("GUI_score");
	}
	
	// Update is called once per frame
	void OnGUI () {
		GUIStyle myStyle = new GUIStyle();
		myStyle.fontSize = 20;
		myStyle.normal.textColor = Color.red;
		
		GUILayout.Label(PhotonNetwork.connectionStateDetailed.ToString(), myStyle);
		
		
		if(GUI_Score != null)
		{
			if(GUI_Score.guiText.text.Equals("Waiting for other player") )
			{
				if(PhotonNetwork.room != null && PhotonNetwork.room.playerCount > 1)
				{
					GUI_Score.guiText.text = "Player entered the room";
				}
			}
		}
	}
	
	void OnJoinedLobby()
	{
		PhotonNetwork.JoinRandomRoom();
	}
	
	void OnPhotonRandomJoinFailed()
	{
		PhotonNetwork.CreateRoom(null, true, true, 4);  // no name (gets a guid), visible and open with 4 players max
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
	}
}
