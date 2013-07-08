using UnityEngine;
using System.Collections;
using RAIN.Core;

public class ColliderPickUp : MonoBehaviour {
	
	public int ammo;
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	
	void OnTriggerEnter(Collider other) {
		if(other.tag.Equals("EnemySphereCollider"))
		{
			//http://support.rivaltheory.com/vanilla/index.php?p=/discussion/comment/1447#Comment_1447		
			//http://support.rivaltheory.com/vanilla/index.php?p=/discussion/468/how-to-stop-a-moveto-operation-if-the-target-gameobject-is-destroyed/p1
				
			// Remove Rain indie components
			Destroy(gameObject.GetComponent("Decoration"));
			Destroy(gameObject.GetComponent("Entity"));
			
			// Reset the senses of the AI, after Decoration en Entity are removed
			// to make shure the AI agent isn't holding a lock on the object
			RAINAgent ai = other.transform.parent.gameObject.GetComponent<RAINAgent>();
			ai.Agent.Mind.BeginSense();
			
			// add Ammo to the AI character
			ai.Agent.actionContext.SetContextItem<int>("ammo", ammo);
			
			// destroy the game object after 0.2f seconds, zo the AI is done refreshing senses
			StartCoroutine(WaitAndDestroy(0.3f));

		}
		
		
		
		if(other.tag.Equals("PlayerKartModel"))
		{
			//http://support.rivaltheory.com/vanilla/index.php?p=/discussion/comment/1447#Comment_1447	
			//http://support.rivaltheory.com/vanilla/index.php?p=/discussion/468/how-to-stop-a-moveto-operation-if-the-target-gameobject-is-destroyed/p1
				
			// Remove Rain indie components
			Destroy(gameObject.GetComponent("Decoration"));
			Destroy(gameObject.GetComponent("Entity"));
			
			// Reset the senses of the AI, after Decoration en Entity are removed
			// to make shure the AI agent isn't holding a lock on the object
			RAINAgent ai = GameObject.FindGameObjectWithTag("Enemy").GetComponent<RAINAgent>();
			ai.Agent.Mind.BeginSense();
			
			
			// destroy the game object after 0.2f seconds, zo the AI is done refreshing senses
			StartCoroutine(WaitAndDestroy(0.3f));
			
		}
		
    }
	
	IEnumerator WaitAndDestroy(float waitTime) {
        yield return new WaitForSeconds(waitTime);
		PhotonNetwork.Destroy(gameObject);
		
		// create new pick up
		var spawnpoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
		var pickup1 = PhotonNetwork.Instantiate("PickUpfab", spawnpoints[Random.Range(0,spawnpoints.Length)].transform.position, Quaternion.identity, 0);
	
		StartCoroutine(WaitAndUpdateMind(0.5f));	
	}
	
	
	IEnumerator WaitAndUpdateMind(float waitTime) {
    	yield return new WaitForSeconds(waitTime);
		
		// update IA senses to notice the new pickup
		RAINAgent ai = GameObject.FindGameObjectWithTag("Enemy").GetComponent<RAINAgent>();
		ai.Agent.Mind.BeginSense();
	}
	
}
