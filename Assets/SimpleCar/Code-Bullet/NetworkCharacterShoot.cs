using UnityEngine;
using System.Collections;

public class NetworkCharacterShoot : Photon.MonoBehaviour
{
    private Vector3 correctPlayerPos = Vector3.zero; // We lerp towards this
    private Quaternion correctPlayerRot = Quaternion.identity; // We lerp towards this

    void Awake()
    {
    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
		// If player postition is still 0 then we did not jet recieve a network message
		if(correctPlayerPos != Vector3.zero)
		{
	        if (!photonView.isMine)
	        {
	            //transform.position = Vector3.Lerp(transform.position, this.correctPlayerPos, Time.deltaTime * 10);
	            //transform.rotation = Quaternion.Lerp(transform.rotation, this.correctPlayerRot, Time.deltaTime * 10);
				
				// lerping couses issue when bullet hits the player on one client but not jet on the other
				// TODO: check if slight lerping also couses an issue
				transform.position = this.correctPlayerPos;
				transform.rotation = this.correctPlayerRot;
	        }
		}
    }

    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // Network player, receive data
            this.correctPlayerPos = (Vector3)stream.ReceiveNext();
            this.correctPlayerRot = (Quaternion)stream.ReceiveNext();
        }
    }
}
