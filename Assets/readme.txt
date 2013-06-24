
Photon Unity Networking (PUN)
	This package is a re-implementation of Unity's Networking, using the Photon Cloud Service.
	Also included: a setup wizard, demo scene, documentation and editor extensions.


Help and Documentation
	Please read the included pdf.
	Exit Games Forum: http://forum.exitgames.com/viewforum.php?f=17
	Online documentation: http://doc.exitgames.com/photon-cloud


Integration
	This package adds a editor window:
	Menu -> Window, Photon Unity Networking


Clean PUN Import (no demos)
	To import only the scripts of Photon Unity Networking into an existing project, 
	skip anything except the folders: "Plugins" and "Editor".


Server
	Exit Games Photon can be run on your servers or you can subscribe to our cloud service.
	
	The window "Photon Unity Networking" will help you setup a Photon Cloud account.
	This service is geared towards room-based games and the server cannot be modified.
	Read more about it: http://www.exitgamescloud.com

	Alternatively, download the Server SDK and run your own Photon Server.
	The SDK has the binaries to run immediately but also includes the source code and projects
	for the game logic. You can use that as basis to modify and extend it.
	A 100 concurrent user license is free (also for commercial use) per game.
	Read more about it: http://www.exitgames.com/photon


Subscriptions bought in Asset Store
	Follow these steps, when you bought a package with Photon Cloud Subscription in the Asset Store:

	•	Register a Photon Cloud Account: https://cloud.exitgames.com/Account/SignUp
	•	Get your AppID from the Dashboard
	•	Send a Mail to: developer@exitgames.com
		With:
		o	Your Name and Company (if applicable)
		o	Invoice/Purchase ID from the Asset Store
		o	Photon Cloud AppID


Important Files

	Documentation
		PhotonNetwork-Documentation.pdf
		changelog.txt

	Extensions & Source
		Editor\PhotonNetwork\*.*
		Plugins\PhotonNetwork\*.*

	Demo Scene
		DemoWorker\DemoWorker-Scene.unity
	Tutorial "Marco Polo"
		MarcoPolo-Tutorial\GameScene.unity
	
	The server-setup will be saved as file (created by Wizard but can be edited in inspector)
		Photon Unity Networking\Resources\PhotonServerSettings.asset
