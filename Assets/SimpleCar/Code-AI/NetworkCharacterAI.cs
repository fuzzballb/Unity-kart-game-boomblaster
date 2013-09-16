using UnityEngine;
using System.Collections;

public class NetworkCharacterAI : Photon.MonoBehaviour {

    private Vector3 correctPlayerPos = Vector3.zero; // We lerp towards this
    private Quaternion correctPlayerRot = Quaternion.identity; // We lerp towards this
	private Transform _transform;
	
    void Awake()
    {
    }

    // Use this for initialization
    void Start()
    {
		_transform = transform;
    }

    // Update is called once per frame
    void Update()
    {
		if(correctPlayerPos != Vector3.zero)
		{
	        if (!photonView.isMine)
	      {
	            _transform.position = Vector3.Lerp(_transform.position, this.correctPlayerPos, Time.deltaTime * 10);
	            _transform.rotation = Quaternion.Lerp(_transform.rotation, this.correctPlayerRot, Time.deltaTime * 10);
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
