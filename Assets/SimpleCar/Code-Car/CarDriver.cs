using UnityEngine;
using System.Collections;

public class CarDriver : MonoBehaviour {
	public float forwardSpeed = 5.0f;
	public float backwardSpeed = 2.0f;
	public float turnRate = 80.0f;
	
	public float timeToReappear = 100.0f;
	public static bool playerShot = false;
	
	
	public Transform lowestGroudObject;
	public Transform respawnPosition;
	
	
	
	// Use this for initialization
	void Start () {
		// Add player to score dictionary using RPC call
		PhotonView photonView = PhotonView.Get(this);
		photonView.RPC("AddPlayer", PhotonTargets.AllBuffered , PhotonNetwork.player.ID, 10);
		
		rigidbody.centerOfMass = new Vector3(0,-2,0);
		
	}
	
	[RPC]
	void AddPlayer(int playerID, int startScore ,PhotonMessageInfo info)
	{
		RandomMatchmakerCar.scoreCount.Add(playerID,startScore);
	}
	
	
	private void SetPlayerToTransperant()
	{
		GameObject.FindGameObjectWithTag("PlayerKartModel").GetComponent<ChangeTextureKart>().makeTransparant(true);
		GameObject.FindGameObjectWithTag("MarioModel").GetComponent<ChangeTextureMario>().makeTransparant(true);
		GameObject.FindGameObjectWithTag("HelmetModel").GetComponent<ChangeTextureHelmet>().makeTransparant(true);
	}
	
	private void SetPlayerToOpage()
	{
		GameObject.FindGameObjectWithTag("PlayerKartModel").GetComponent<ChangeTextureKart>().makeTransparant(false);
		GameObject.FindGameObjectWithTag("MarioModel").GetComponent<ChangeTextureMario>().makeTransparant(false);
		GameObject.FindGameObjectWithTag("HelmetModel").GetComponent<ChangeTextureHelmet>().makeTransparant(false);
	}
	
	// Update is called once per frame
	private void Update () {
		if(playerShot)
		{	
			timeToReappear--;
			if(timeToReappear == 50.0f)
			{
				// set local visibility to true
				
				// toggles the visibility of this gameobject and all it's children
				Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
			    foreach(Renderer r in renderers) {
			        r.enabled = true;
			    }
				
				SetPlayerToTransperant();
			}
			
			if(timeToReappear <= 0.0f)
			{
				visibility(true);
				SetPlayerToOpage();
				// set remote visibility to true this is done in NetworkCharacterCar, 
				// because that script is turned on on remote clients
				
				timeToReappear = 100.0f;
			}
		}
		
		// Check if jump key (SPACEBAR) is pressed to reset player to default position
		if(Input.GetButtonDown("Jump")){
			transform.position += Vector3.up;
			rigidbody.velocity = Vector3.zero;
			rigidbody.angularVelocity = Vector3.zero;
			transform.rotation = Quaternion.identity;
		}
		
		// Update audio according to the speed of the player
		audio.pitch = rigidbody.velocity.magnitude / 80 +1;
		
		// Check if ESC key is pressed to leave game
		if(Input.GetKey(KeyCode.Escape))
		{
			Application.Quit ();
		}
	}
	
 
	public void visibility(bool visibility) {
		
		if(!visibility)
		{
			playerShot = true;
		}
		else
		{
			playerShot = false;
		}
		
	    // toggles the visibility of this gameobject and all it's children
		Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
	    foreach(Renderer r in renderers) {
	        r.enabled = visibility;
	    }
	}
	
	// fixed timing, Physics uses this instead of normal update for more consistand simulation
	// TODO: check if Time.deltaTime isn't always the same in Fixed update.
	void FixedUpdate(){
	   float horizontalInput = Input.GetAxis("Horizontal");
	   float verticalInput = Input.GetAxis("Vertical");
	 
	   Vector3 a = transform.eulerAngles;
	 
	   float rotation= 0; 
		
	   if(horizontalInput > 0.1)
		{
	      rotation= a.y + (10.0f  * Time.deltaTime * 10);
			
	      transform.eulerAngles = new Vector3(a.x, rotation, a.z);
		}
	   else if(horizontalInput < 0)
		{
		  rotation= a.y - (10.0f  * Time.deltaTime * 10);	
	      transform.eulerAngles = new Vector3(a.x, rotation, a.z);
		}
	 
	   Vector3 moveDirection = new Vector3(0,0,verticalInput*forwardSpeed);
	 
	   if(verticalInput > 0.1)
	   {
	      rigidbody.AddRelativeForce(moveDirection,ForceMode.Acceleration);
	   }
	}
	
}
