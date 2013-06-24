using UnityEngine;
using System.Collections;

public class NetworkCharacterCar : Photon.MonoBehaviour
{
    private Vector3 correctPlayerPos = Vector3.zero; // We lerp towards this
    private Quaternion correctPlayerRot = Quaternion.identity; // We lerp towards this
	
    void Awake()
    {
		// Example to setting character state with a stream
        //ThirdPersonController myC = GetComponent<ThirdPersonController>();
        //myC.isControllable = photonView.isMine;
		//CarDriver myC = GetComponent<CarDriver>();
    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.isMine)
        {
            transform.position = Vector3.Lerp(transform.position, this.correctPlayerPos, Time.deltaTime * 5);
            transform.rotation = Quaternion.Lerp(transform.rotation, this.correctPlayerRot, Time.deltaTime * 5);
        }
		
		// If player isn't shot anymore, make it visible again
		// This function is in this update loop, because CarDriver is not enabled for the network player
		if(!CarDriver.playerShot)
		{
			// set remote visibility to true
			PhotonView otherPhotonView = PhotonView.Get(this);
			otherPhotonView.gameObject.transform.root.GetComponent<CarDriver>().visibility(true);
		}
    }

    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
			
			// Example to setting character state with a stream
            //ThirdPersonController myC = GetComponent<ThirdPersonController>();
            //stream.SendNext((int)myC._characterState);
        }
        else
        {
            // Network player, receive data
            this.correctPlayerPos = (Vector3)stream.ReceiveNext();
            this.correctPlayerRot = (Quaternion)stream.ReceiveNext();
			
			// Example to getting character state with a stream
            //ThirdPersonController myC = GetComponent<ThirdPersonController>();
            //myC._characterState = (CharacterState)stream.ReceiveNext();
        }
    }
}
