using UnityEngine;
using System.Collections;

public class DestroyOnInvisible : MonoBehaviour {
	
	// TODO: Destroy object when set to invisible
	void OnBecameInvisible() {
		PhotonNetwork.Destroy(gameObject);
	}
}
