using UnityEngine;
using System.Collections;

public class FirePitt : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void OnTriggerEnter(Collider other) {
		if(other.gameObject.CompareTag("Enemy"))
		{
			PhotonNetwork.Instantiate("ParticleFab", other.gameObject.transform.position, Quaternion.identity, 0);
			
			var spawnpoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
			int randomSpawnNumber = Random.Range(0,spawnpoints.Length);
			other.gameObject.transform.position = spawnpoints[randomSpawnNumber].transform.position;
			other.gameObject.transform.rotation = spawnpoints[randomSpawnNumber].transform.rotation;
			
			if(Application.loadedLevelName.Equals("SinglePlayer"))
			{
				SingleMatch rb = GameObject.Find("Scripts").GetComponent<SingleMatch>();
				rb.enemyScore--;
				rb.UpdateScore();
			}
		}
		if(other.gameObject.CompareTag("Player"))
		{
			PhotonNetwork.Instantiate("ParticleFab", other.gameObject.transform.position, Quaternion.identity, 0);
			
			var spawnpoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
			int randomSpawnNumber = Random.Range(0,spawnpoints.Length);
			other.gameObject.transform.position = spawnpoints[randomSpawnNumber].transform.position;
			other.gameObject.transform.rotation = spawnpoints[randomSpawnNumber].transform.rotation;
			
			if(Application.loadedLevelName.Equals("SinglePlayer"))
			{
				SingleMatch rb = GameObject.Find("Scripts").GetComponent<SingleMatch>();
				rb.playerScore--;
				rb.UpdateScore();
			}
		}
    }	
}
