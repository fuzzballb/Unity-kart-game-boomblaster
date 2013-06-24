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
			other.gameObject.transform.position = spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position;
			
			if(Application.loadedLevelName.Equals("SinglePlayer"))
			{
				SingleMatch.enemyScore--;
				GameObject.FindGameObjectWithTag("GUI_score").guiText.text = SingleMatch.playerScore + " - " + SingleMatch.enemyScore;
			}
		}
		if(other.gameObject.CompareTag("Player"))
		{
			PhotonNetwork.Instantiate("ParticleFab", other.gameObject.transform.position, Quaternion.identity, 0);
			
			var spawnpoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
			other.gameObject.transform.position = spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position;
			
			if(Application.loadedLevelName.Equals("SinglePlayer"))
			{
				SingleMatch.playerScore--;
				GameObject.FindGameObjectWithTag("GUI_score").guiText.text = SingleMatch.playerScore + " - " + SingleMatch.enemyScore;
			}
		}
    }	
}
