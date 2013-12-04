using UnityEngine;
using System.Collections;

public class CarDriver : Photon.MonoBehaviour {
	public float forwardSpeed = 5.0f;
	public float backwardSpeed = 2.0f;
	public float turnRate = 80.0f;
	
	public float timeToReappear = 100.0f;
	public static bool playerShot = false;
	private Transform _transform;
	private Rigidbody _rigidbody;
	private ChangeTextureKart _ChangeTextureKart;
	private ChangeTextureKart _MarioModel;
	private ChangeTextureHelmet _HelmetModel;
	
	
	
	public Transform lowestGroudObject;
	public Transform respawnPosition;
	
	private float guiRotation= 0;  
	private float guiVerticalInput = 0.0f;
	
	
	
	public float reloadTime = 10.0f;
	private float tempReloadTime = 0.0f;
	public float spawnDistanceForward = 12.0f; // don't want the bullet spawn in centre
	public float spawnDistanceUp = 6.0f;
	
	// Use this for initialization
	void Start () {
		
		if(PhotonNetwork.offlineMode)
		{
			// add players to local list for keeping score
		}	
		else
		{
			// Add player to score dictionary using RPC call
			PhotonView photonView = PhotonView.Get(this);
			photonView.RPC("AddPlayer", PhotonTargets.AllBuffered , PhotonNetwork.player.ID, 10);
		}
		
		
		_rigidbody = rigidbody;
		
		_rigidbody.centerOfMass = new Vector3(0,-2,0);
		
		
		_ChangeTextureKart = GameObject.FindGameObjectWithTag("PlayerKartModel").GetComponent<ChangeTextureKart>();
		_MarioModel = GameObject.FindGameObjectWithTag("PlayerKartModel").GetComponent<ChangeTextureKart>();
		_HelmetModel = GameObject.FindGameObjectWithTag("HelmetModel").GetComponent<ChangeTextureHelmet>();
		
		
		_transform = transform;
	}
	
	
	
	void OnGUI () {
		
	//	float horizRatio = Screen.width / 1024.0f;
	//	float vertRatio = Screen.height / 768.0f;
	//	var tOffset = new Vector3 (0, 0, 0);
	//	var tRotation = Quaternion.identity;
	//	var tScale = new Vector3(horizRatio, vertRatio, 1.0f);
	//	var tMatrix = Matrix4x4.TRS(tOffset, tRotation, tScale);
	//	GUI.matrix = tMatrix;

		
		// Make a background box
	#if UNITY_METRO
		
		
		
		
	//	Rect joystickRect = new Rect(0, 0, Screen.width/3, Screen.height * 1.0f);
	//	Rect joystickRectRight = 		new Rect(Screen.width - (Screen.width/3), 0, 			   Screen.width/3, Screen.height/2);
	//	Rect joystickRectRightFire =    new Rect(Screen.width - (Screen.width/3), Screen.height/2, Screen.width/3, Screen.height/2);
			
	//	GUI.Box(joystickRect, "");
	//	GUI.Box(joystickRectRight, "");
	//	GUI.Box(joystickRectRightFire, "");
		
		//GUI.Box(new Rect(10,950,Screen.width/3,100), "");
		//GUI.Box(new Rect(1700,950,160,100), "");
	#endif	

		

		
		

	}
	
	
	
	
	[RPC]
	void AddPlayer(int playerID, int startScore ,PhotonMessageInfo info)
	{
		RandomMatchmakerCar.scoreCount.Add(playerID,startScore);
	}
	
	
	private void SetPlayerToTransperant()
	{
		_ChangeTextureKart.makeTransparant(true);
		_MarioModel.makeTransparant(true);
		_HelmetModel.makeTransparant(true);
	}
	
	private void SetPlayerToOpage()
	{
		_ChangeTextureKart.makeTransparant(false);
		_MarioModel.makeTransparant(false);
		_HelmetModel.makeTransparant(false);
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
		

		
		// Update audio according to the speed of the player
		audio.pitch = _rigidbody.velocity.magnitude / 80 +1;
		
		// Check if ESC key is pressed to leave game
		if(Input.GetKey(KeyCode.Escape))
		{
			PhotonNetwork.LeaveRoom();
			PhotonNetwork.Disconnect();
			Application.LoadLevel("Menu");
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
	 
	   Vector3 a = _transform.eulerAngles;
	 
	   float rotation= 0; 
		
	   if(horizontalInput > 0.1)
		{
	      rotation= a.y + (10.0f  * Time.deltaTime * 10);
			
	      _transform.eulerAngles = new Vector3(a.x, rotation, a.z);
		}
	   else if(horizontalInput < 0)
		{
		  rotation= a.y - (10.0f  * Time.deltaTime * 10);	
	      _transform.eulerAngles = new Vector3(a.x, rotation, a.z);
		}
	 
	   Vector3 moveDirection = new Vector3(0,0,verticalInput*forwardSpeed);
	 
		
		
		
		//Debug.Log(" x " +  transform.rotation.x + " y " +  transform.rotation.y + " z " +  transform.rotation.z  );
		
		
	   if(verticalInput > 0.1)
	   {

			if(_transform.rotation.x < 0.05f && _transform.rotation.x > -0.05f && _transform.rotation.z < 0.05f && _transform.rotation.z > -0.05f)
			{
	      		_rigidbody.AddRelativeForce(moveDirection,ForceMode.Acceleration);
			}
	   }

		
		
				// Cool downs for Player weapon
			tempReloadTime -= 10.0f * Time.deltaTime;
		
		

		if (  Application.platform == RuntimePlatform.MetroPlayerX64 ||
         Application.platform == RuntimePlatform.MetroPlayerX86 ||
         Application.platform == RuntimePlatform.MetroPlayerARM ||
			Application.platform == RuntimePlatform.WP8Player ||
			Application.platform == RuntimePlatform.IPhonePlayer ||
			Application.platform == RuntimePlatform.Android)
		{
			
			Rect joystickRect = new Rect(0, 0, Screen.width/3, Screen.height/2);
			Rect joystickRectMenu = new Rect(0, Screen.height/2, Screen.width/3, Screen.height * 1.0f);
			Rect joystickRectRight = 		new Rect(Screen.width - (Screen.width/3), 0, 			   Screen.width/3, Screen.height/2);
			Rect joystickRectRightFire =    new Rect(Screen.width - (Screen.width/3), Screen.height/2, Screen.width/3, Screen.height/2);
		
		    // do joystick stuff
			int count  = Input.touchCount;
			guiVerticalInput = 0.0f;
		
			//GUILayout.Label("fingers on screen" + count.ToString());
		
			// check all fingers
			for (var i = 0;  i < count;  i++)
			{  
				Touch touch = Input.GetTouch(i);
				
				// if joystick finger presses the back button
				if(joystickRectMenu.Contains(touch.position))
				{
					if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
					{
						Application.LoadLevel("Menu");
					}
				}
				
				
			
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
							guiRotation = a.y + ((turnAmount/2) * Time.deltaTime);	
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
				}
				else if(joystickRectRightFire.Contains(touch.position))
				{	
					//GUILayout.Label("fingerposition" + touch.position.ToString());
				
					if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
					{
						if(tempReloadTime < 0.0f)
						{
							GameObject bullet = PhotonNetwork.Instantiate("Bomfab", _transform.position + (spawnDistanceForward * _transform.forward)+ (spawnDistanceUp * _transform.up),_transform.rotation, 0);
							BulletAi controller = bullet.GetComponent<BulletAi>();
							controller.enabled = true;
			
							tempReloadTime = reloadTime;
						}
					}
					
				}
			}

			Vector3 guiMoveDirection = new Vector3(0,0,guiVerticalInput*forwardSpeed);
		   	if(guiVerticalInput > 0.1)
		   	{
				if(_transform.rotation.x < 0.05f && _transform.rotation.x > -0.05f && _transform.rotation.z < 0.05f && _transform.rotation.z > -0.05f)
				{
		   		   _rigidbody.AddRelativeForce(guiMoveDirection,ForceMode.Acceleration);
				}
		   	}
	
			_transform.eulerAngles = new Vector3(a.x, guiRotation, a.z);
		}
		
	}
	
}
