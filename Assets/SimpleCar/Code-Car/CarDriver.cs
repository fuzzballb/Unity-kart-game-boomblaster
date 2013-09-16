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
	
	private float guiRotation= 0;  
	private float guiVerticalInput = 0.0f;
	
	// Use this for initialization
	void Start () {
		// Add player to score dictionary using RPC call
		PhotonView photonView = PhotonView.Get(this);
		photonView.RPC("AddPlayer", PhotonTargets.AllBuffered , PhotonNetwork.player.ID, 10);
		
		rigidbody.centerOfMass = new Vector3(0,-2,0);
		
	}
	
	
	
	void OnGUI () {
		
		// Make a background box
	#if UNITY_METRO
		GUI.Box(new Rect(10,950,Screen.width/3,100), "");
		GUI.Box(new Rect(1700,950,160,100), "");
	#endif	

		

		
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
		
		if(Input.GetButtonDown("Fire1")){
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


		if (  Application.platform == RuntimePlatform.MetroPlayerX64 ||
         Application.platform == RuntimePlatform.MetroPlayerX86 ||
         Application.platform == RuntimePlatform.MetroPlayerARM)
		{
			Rect joystickRect = new Rect(0, 0, Screen.width/3, Screen.height * 1.0f);
			Rect joystickRectRight = new Rect(Screen.width/2, 0, Screen.width/2, Screen.height * 1.0f);
			
	
			    // do joystick stuff
				int count  = Input.touchCount;
				
			
				//GUILayout.Label("fingers on screen" + count.ToString());
			
				// check all fingers
				for (var i = 0;  i < count;  i++)
				{  
					Touch touch = Input.GetTouch(i);
					
					// if joystick finger
				
					// How ON earth does this touch zone start in the top right corner ????
					if(joystickRect.Contains(touch.position))
					{
					
						//GUILayout.Label("fingerposition" + touch.position.ToString());
					
						// and finger has moved
						if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
						{
							if(touch.position.x < Screen.width/3)
							{
								// 210 (touch.position.x)  - (1800/2)/2 (450) = 10 
								float turnAmount = touch.position.x - ((Screen.width/3)/2);
								guiRotation = a.y + ((turnAmount/4) * Time.deltaTime);	
							}
						}
					}
					else if(joystickRectRight.Contains(touch.position))
					{	
						//GUILayout.Label("fingerposition" + touch.position.ToString());
					
						if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
						{
							guiVerticalInput = 1.0f;
						}
						else
						{
							guiVerticalInput = 0.0f;
						}
					}
				}
	
	
			Vector3 guiMoveDirection = new Vector3(0,0,guiVerticalInput*forwardSpeed);
		   	if(guiVerticalInput > 0.1)
		   	{
		   	   rigidbody.AddRelativeForce(guiMoveDirection,ForceMode.Acceleration);
		   	}
	
			transform.eulerAngles = new Vector3(a.x, guiRotation, a.z);
		}
		
	}
	
}
