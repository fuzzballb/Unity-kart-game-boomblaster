using UnityEngine;
using System.Collections;

public class DestroyOnInvisible : Photon.MonoBehaviour {
	
	// TODO: Destroy object when set to invisible
	void OnBecameInvisible() {
		
		PhotonView myPhotonView = PhotonView.Get(gameObject);
		
		if(myPhotonView.isMine)
		{
			PhotonNetwork.Destroy(gameObject);
		}
	}
}
