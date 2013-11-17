using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using RAIN.Core;

public class BulletAi : Photon.MonoBehaviour {
	
	public float force = 20000.0f;
	public float timeToLive = 1000.0f;
	private int randomSpawnNumber = 0;
	private int previousRandomSpawnNumber = 0;
	
	// Use this for initialization
	void Start() {
		// Shoot bullet forward
    	rigidbody.AddForce(transform.forward * force,ForceMode.Impulse);
	}
	
	void Update () {
		// Destroy bullet when it is old
		StartCoroutine(DestroyOverTime(2.0f));
	}
	
	
	IEnumerator DestroyOverTime(float waitTime) {
        yield return new WaitForSeconds(waitTime);
		
		if(PhotonNetwork.offlineMode)
		{
			Destroy(gameObject);
		}
		else
		{
			PhotonNetwork.Destroy(gameObject);
		}
	}
	
	
	void OnTriggerEnter(Collider other) {
		if(other.gameObject.CompareTag("Enemy"))
		{
			// instantiate eplosion effect
			PhotonNetwork.Instantiate("ParticleFab", other.gameObject.transform.position, Quaternion.identity, 0);
			
			
			// destroy the bullet
			if(PhotonNetwork.offlineMode)
			{
				Destroy(gameObject);
			}
			else
			{
				PhotonNetwork.Destroy(gameObject);
			}			
			
			// destroy enemy game object
			if(PhotonNetwork.offlineMode)
			{
				SingleMatch.objectsToHide.Remove(gameObject);
				Destroy(other.gameObject);
			}
			else
			{
				PhotonNetwork.Destroy(other.gameObject);
			}

			
			

			
			if(Application.loadedLevelName.Equals("SinglePlayer"))
			{
				//
				// Create new enemy game object	
				//
				Debug.Log("starting coroutine in main script");
				SingleMatch rb = GameObject.Find("Scripts").GetComponent<SingleMatch>();
				rb.CreateNewAI(); // createNewAI could not be static
				
				//
				// Opdate Scrore
				//
				rb.playerScore++;
				rb.UpdateScore();
				
				
			}
		}
		if(other.gameObject.CompareTag("Player"))
		{
			PhotonView otherPhotonView = PhotonView.Get(other.gameObject);
			
			var spawnpoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
			
			// Code to make shure the spawn is not to close to the previous spawn
			// TODO: this code is not working as planed
			randomSpawnNumber = Random.Range(0,spawnpoints.Length);
			while(randomSpawnNumber <= previousRandomSpawnNumber + 4 && randomSpawnNumber >= previousRandomSpawnNumber - 4 )
			{
				randomSpawnNumber = Random.Range(0,spawnpoints.Length);
			}
			previousRandomSpawnNumber = randomSpawnNumber;
			
			
			if(Application.loadedLevelName.Equals("SinglePlayer"))
			{
				SingleMatch rb = GameObject.Find("Scripts").GetComponent<SingleMatch>();
				rb.enemyScore++;
				rb.UpdateScore();
			}
			
			
			
			
			if(!otherPhotonView.isMine)
			{
				//	Debug.Log("otherPhotonView NOT mine");
				
				// Add a particle prefab to show an explosion
				// TODO: needs to be destroyed after effect
				PhotonNetwork.Instantiate("ParticleFab", other.gameObject.transform.position, Quaternion.identity, 0);
				
				// Position and visibility, after respawn, needs to be set on other clients
				// TODO: Only other player should have visibility to false
				otherPhotonView.gameObject.transform.root.GetComponent<CarDriver>().visibility(false);
				otherPhotonView.transform.position = spawnpoints[randomSpawnNumber].transform.position;
				otherPhotonView.transform.rotation = spawnpoints[randomSpawnNumber].transform.rotation;

				// Update scoreboard
				PhotonView carView = other.gameObject.transform.root.GetComponent<PhotonView>();

				// Add a hit to the score to the car being hit
				photonView.RPC("UpdateScore", PhotonTargets.AllBuffered, carView.owner.ID, 1);
			}
			else
			{
			//	Debug.Log("otherPhotonView IS mine" + Time.deltaTime);
				
				// Set other object in this network instance to new position
				// had to remove bullet lerp to make shure this would always fire
				other.gameObject.transform.root.GetComponent<CarDriver>().visibility(false);
				other.gameObject.transform.position = spawnpoints[randomSpawnNumber].transform.position;
				other.gameObject.transform.rotation = spawnpoints[randomSpawnNumber].transform.rotation;
			}
		}
    }
	
	
	[RPC]
	void UpdateScore(int carBeingEffected, int amount ,PhotonMessageInfo info)
	{
		// Remove a point for the player that has been hit
		if (RandomMatchmakerCar.scoreCount.ContainsKey(carBeingEffected))
		{
		    RandomMatchmakerCar.scoreCount[carBeingEffected] -= amount; 
		}
		
		// Cunstruct the score string
		string scoreText = "";
		foreach (var pair in RandomMatchmakerCar.scoreCount)
		{
			// TODO: realy remove the players that are not in this list from this collection
			if(isInPlayerlist(pair.Key))
			{
		    	scoreText += " Ply " + pair.Key + ": " + pair.Value + "    ";
			}
		}
		
		GameObject.FindGameObjectWithTag("GUI_score").guiText.text = scoreText;
	    //Debug.Log(string.Format("Info: {0} {1} {2}", info.sender, info.photonView, info.timestamp));
	}
	
	
	// Only display scores that are still active players, not left the game
	bool isInPlayerlist(int id)
	{
		foreach(PhotonPlayer player in PhotonNetwork.playerList)
		{
			if(player.ID == id)
			{
				return true;
			}
		}
		return false;
	}
	
	
	

	
}
