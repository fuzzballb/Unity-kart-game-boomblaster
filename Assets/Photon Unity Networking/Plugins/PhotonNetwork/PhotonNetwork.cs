// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PhotonNetwork.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Networking
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// The main class to use the PhotonNetwork plugin.
/// This class is static.
/// </summary>
/// \ingroup publicApi
public static class PhotonNetwork
{
    /// <summary>Version number of PUN. Also used in GameVersion to separate client version from each other.</summary>
    public const string versionPUN = "1.22";

    /// <summary>
    /// This Monobehaviour allows Photon to run an Update loop.
    /// </summary>
    internal static readonly PhotonHandler photonMono;

    /// <summary>
    /// Photon peer class that implements LoadBalancing in PUN. 
    /// Primary use is internal (by PUN itself).
    /// </summary>
    internal static readonly NetworkingPeer networkingPeer;

    /// <summary>
    /// The maximum amount of assigned PhotonViews PER player (or scene). See the documentation on how to raise this limitation
    /// </summary>
    public static readonly int MAX_VIEW_IDS = 1000; // VIEW & PLAYER LIMIT CAN BE EASILY CHANGED, SEE DOCS
    

    /// <summary>Name of the PhotonServerSettings file (used to load and by PhotonEditor to save new files).</summary>
    public const string serverSettingsAssetFile = "PhotonServerSettings";

    /// <summary>Path to the PhotonServerSettings file (used by PhotonEditor).</summary>
    public const string serverSettingsAssetPath = "Assets/Photon Unity Networking/Resources/" + PhotonNetwork.serverSettingsAssetFile + ".asset";


    /// <summary>Serialized server settings, written by the Setup Wizard for use in ConnectUsingSettings.</summary>
    public static ServerSettings PhotonServerSettings = (ServerSettings)Resources.Load(PhotonNetwork.serverSettingsAssetFile, typeof(ServerSettings));

    /// <summary>Currently used server address (no matter if master or game server).</summary>
    public static string ServerAddress { get { return (networkingPeer != null) ? networkingPeer.ServerAddress : "<not connected>"; } }

    /// <summary>
    /// The minimum difference that a Vector2 or Vector3(e.g. a transforms rotation) needs to change before we send it via a PhotonView's OnSerialize/ObservingComponent
    /// Note that this is the sqrMagnitude. E.g. to send only after a 0.01 change on the Y-axix, we use 0.01f*0.01f=0.0001f. As a remedy against float inaccuracy we use 0.000099f instead of 0.0001f.
    /// </summary>
    public static float precisionForVectorSynchronization = 0.000099f;

    /// <summary>
    /// The minimum angle that a rotation needs to change before we send it via a PhotonView's OnSerialize/ObservingComponent
    /// </summary>
    public static float precisionForQuaternionSynchronization = 1.0f;

    /// <summary>
    /// The minimum difference between floats before we send it via a PhotonView's OnSerialize/ObservingComponent
    /// </summary>
    public static float precisionForFloatSynchronization = 0.01f;


    // "VARIABLES"

    /// <summary>
    /// Are we connected to the photon server (can be IN or OUTSIDE a room)
    /// </summary>
    public static bool connected
    {
        get
        {
            if (offlineMode)
            {
                return true;
            }

            return connectionState == ConnectionState.Connected;
        }
    }

    /// <summary>
    /// Simplified connection state
    /// </summary>
    public static ConnectionState connectionState
    {
        get
        {
            if (offlineMode)
            {
                return ConnectionState.Connected;
            }

            if (networkingPeer == null)
            {
                return ConnectionState.Disconnected;
            }

            switch (networkingPeer.PeerState)
            {
                case PeerStateValue.Disconnected:
                    return ConnectionState.Disconnected;
                case PeerStateValue.Connecting:
                    return ConnectionState.Connecting;
                case PeerStateValue.Connected:
                    return ConnectionState.Connected;
                case PeerStateValue.Disconnecting:
                    return ConnectionState.Disconnecting;
                case PeerStateValue.InitializingApplication:
                    return ConnectionState.InitializingApplication;
            }

            return ConnectionState.Disconnected;
        }
    }

    /// <summary>
    /// Detailed connection state (ignorant of PUN, so it can be "disconnected" while switching servers).
    /// </summary>
    public static PeerState connectionStateDetailed
    {
        get
        {
            if (offlineMode)
            {
                return PeerState.Connected;
            }

            if (networkingPeer == null)
            {
                return PeerState.Disconnected;
            }

            return networkingPeer.State;
        }
    }

    /// <summary>
    /// A user's authentication values used during connect for Custom Authentication with Photon (and a custom service/community). 
    /// Set these before calling Connect if you want custom authentication.
    /// </summary>
    /// <remarks>
    /// If authentication fails for any values, PUN will call your implementation of OnCustomAuthenticationFailed(string debugMsg).
    /// See: PhotonNetworkingMessage.OnCustomAuthenticationFailed
    /// </remarks>
    public static AuthenticationValues AuthValues
    {
        get { return (networkingPeer != null) ? networkingPeer.AuthValues : null; } 
        set { if (networkingPeer != null) networkingPeer.AuthValues = value; }
    }

    /// <summary>
    /// Get the room we're currently in. Null if we aren't in any room.
    /// </summary>
    public static Room room
    {
        get
        {
            if (isOfflineMode)
            {
                if (offlineMode_inRoom)
                {
                    return new Room("OfflineRoom", new Hashtable());
                }
                else
                {
                    return null;
                }
            }

            return networkingPeer.mCurrentGame;
        }
    }

    /// <summary>
    /// Network log level. Controls how verbose PUN is.
    /// </summary>
    public static PhotonLogLevel logLevel = PhotonLogLevel.ErrorsOnly;

    /// <summary>
    /// The local PhotonPlayer. Always available and represents this player.
    /// CustomProperties can be set before entering a room and will be synced as well.
    /// </summary>
    public static PhotonPlayer player
    {
        get
        {
            if (networkingPeer == null)
            {
                return null; // Surpress ExitApplication errors
            }

            return networkingPeer.mLocalActor;
        }
    }

    /// <summary>
    /// The PhotonPlayer of the master client. The master client is the 'virtual owner' of the room. You can use it if you need authorative decision made by one of the players.
    /// </summary>
    /// <remarks>
    /// The masterClient is null until a room is joined and becomes null again when the room is left.
    /// </remarks>
    public static PhotonPlayer masterClient
    {
        get
        {
            if (networkingPeer == null)
            {
                return null;
            }

            return networkingPeer.mMasterClient;
        }
    }

    /// <summary>
    /// Allows the current Master Client to assign someone else as MC - custom selection should pick the same user on any client.
    /// </summary>
    /// <remarks>
    /// The ReceiverGroup.MasterClient (usable in RPCs) is not affected by this (still points to lowest player.ID in room).
    /// Avoid using this enum value (and send to a specific player instead).
    /// 
    /// If the current Master Client leaves, PUN will detect a new one by "lowest player ID". Implement OnMasterClientSwitched
    /// to get a callback in this case. The PUN-selected Master Client might assign a new one. 
    /// 
    /// Make sure you don't create an endless loop of Master-assigning! When selecting a custom Master Client, all clients
    /// should point to the same player, no matter who actually assigns this player.
    /// 
    /// Locally the Master Client is immediately switched, while remote clients get an event. This means the game 
    /// is tempoarily without Master Client like when a current Master Client leaves.
    /// 
    /// When switching the Master Client manually, keep in mind that this user might leave and not do it's work, just like 
    /// any Master Client.
    /// </remarks>
    /// <param name="playerId">The player.ID of the next Master Client.</param>
    /// <returns>False when this synced action couldn't be done. Must be online and Master Client.</returns>
    public static bool SetMasterClient(PhotonPlayer player)
    {
        if (!VerifyCanUseNetwork() || !isMasterClient)
        {
            return false;
        }

        return networkingPeer.SetMasterClient(player.ID, true);
    }

    /// <summary>
    /// This local player's name.
    /// </summary>
    /// <remarks>Setting the name will automatically send it, if connected. Setting null, won't change the name.</remarks>
    public static string playerName
    {
        get
        {
            return networkingPeer.PlayerName;
        }

        set
        {
            networkingPeer.PlayerName = value;
        }
    }

    /// <summary>
    /// The full PhotonPlayer list, including the local player.
    /// </summary>
    public static PhotonPlayer[] playerList
    {
        get
        {
            if (networkingPeer == null)
                return new PhotonPlayer[0];

            return networkingPeer.mPlayerListCopy;
        }
    }

    /// <summary>
    /// The other PhotonPlayers, not including our local player.
    /// </summary>
    public static PhotonPlayer[] otherPlayers
    {
        get
        {
            if (networkingPeer == null)
                return new PhotonPlayer[0];

            return networkingPeer.mOtherPlayerListCopy;
        }
    }

    /// <summary>
    /// Read-only list of friends, their online status and the room they are in. Null until initialized by a FindFriends call.
    /// </summary>
    /// <remarks>
    /// Do not modify this list!
    /// It's internally handles by FindFriends and only useful to read the values.
    /// The value of FriendsListAge tells you how old the data is in milliseconds.
    /// 
    /// Don't get this list more often than useful (> 10 seconds). In best case, keep the list you fetch really short. 
    /// You could (e.g.) get the full list only once, then request a few updates only for friends who are online. 
    /// After a while (e.g. 1 minute), you can get the full list again (to update online states).
    /// </remarks>
    public static List<FriendInfo> Friends { get; set; }

    /// <summary>
    /// Age of friend list info (in milliseconds). It's 0 until a friend list is fetched.
    /// </summary>
    public static int FriendsListAge
    {
        get { return (networkingPeer != null) ? networkingPeer.FriendsListAge : 0; }
    }

    /// <summary>
    /// While enabled (true), Instantiate uses PhotonNetwork.PrefabCache to keep game objects in memory (improving instantiation of the same prefab).
    /// </summary>
    /// <remarks>
    /// Setting UsePrefabCache to false during runtime will not clear PrefabCache but will ignore it right away.
    /// You could clean and modify the cache yourself. Read its comments.
    /// </remarks>
    public static bool UsePrefabCache = true;

    /// <summary>
    /// Keeps references to GameObjects for frequent instantiation (out of memory instead of loading the Resources).
    /// </summary>
    /// <remarks>
    /// You should be able to modify the cache anytime you like, except while Instantiate is used. Best do it only in the main-Thread.
    /// </remarks>
    public static Dictionary<string, GameObject> PrefabCache = new Dictionary<string, GameObject>();

    /// <summary>
    /// Offline mode can be set to re-use your multiplayer code in singleplayer game modes. 
    /// When this is on PhotonNetwork will not create any connections and there is near to 
    /// no overhead. Mostly usefull for reusing RPC's and PhotonNetwork.Instantiate
    /// </summary>
    public static bool offlineMode
    {
        get
        {
            return isOfflineMode;
        }

        set
        {
            if (value == isOfflineMode)
            {
                return;
            }

            if (value && connected)
            {
                Debug.LogError("Can't start OFFLINE mode while connected!");
            }
            else
            {
                networkingPeer.Disconnect(); // Cleanup (also calls OnLeftRoom to reset stuff)
                isOfflineMode = value;
                if (isOfflineMode)
                {
                    NetworkingPeer.SendMonoMessage(PhotonNetworkingMessage.OnConnectedToPhoton);
                    networkingPeer.ChangeLocalID(1);
                    networkingPeer.mMasterClient = player;
                }
                else
                {
                    networkingPeer.ChangeLocalID(-1);
                    networkingPeer.mMasterClient = null;
                }
            }
        }
    }

    private static bool isOfflineMode = false;
    private static bool offlineMode_inRoom = false;

    /// <summary>
    /// The maximum number of players for a room. Better: Set it in CreateRoom.
    /// If no room is opened, this will return 0.
    /// </summary>
    [Obsolete("Used for compatibility with Unity networking only.")]
    public static int maxConnections
    {
        get
        {
            if (room == null)
            {
                return 0;
            }

            return (int)room.maxPlayers;
        }

        set
        {
            room.maxPlayers = value;
        }
    }

    /// <summary>
    /// If true, PUN will make sure that all users are in the same scene at all times. 
    /// If the MasterClient switches, all clients will load the new scene. This also takes care of smooth loading of the game scene after joining a game from your main menu.
    /// </summary>
    /// <value>
    /// <c>true</c> if automatically sync scene; otherwise, <c>false</c>.
    /// </value>
    public static bool automaticallySyncScene
    {
        get
        {
            return _mAutomaticallySyncScene;
        }
        set
        {
            _mAutomaticallySyncScene = value;
            if (_mAutomaticallySyncScene && room != null)
            {
                networkingPeer.AutomaticallySyncScene();
            }
        }
    }

    private static bool _mAutomaticallySyncScene = false;

    /// <summary>
    /// This setting defines if players in a room should destroy a leaving player's instantiated GameObjects and PhotonViews.
    /// 
    /// When "this client" creates a room/game, autoCleanUpPlayerObjects is copied to that room's properties and used by all 
    /// PUN clients in that room (no matter what their autoCleanUpPlayerObjects value is).
    /// 
    /// If room.AutoCleanUp is enabled in a room, the PUN clients will destroy a player's objects on leave.
    /// </summary>
    /// <remarks>
    /// When enabled, the server will clean RPCs, instantiated GameObjects and PhotonViews of the leaving player and joining 
    /// players won't get those at anymore.
    ///
    /// Once a room is created, this setting can't be changed anymore.
    /// 
    /// Enabled by default.
    /// </remarks>
    public static bool autoCleanUpPlayerObjects
    {
        get
        {
            return m_autoCleanUpPlayerObjects;
        }
        set
        {
            if (room != null)
                Debug.LogError("Setting autoCleanUpPlayerObjects while in a room is not supported.");
            m_autoCleanUpPlayerObjects = value;
        }
    }

    private static bool m_autoCleanUpPlayerObjects = true;

    /// <summary>
    /// Defines if the PhotonNetwork should join the "lobby" when connected to the Master server.
    /// If this is false, OnConnectedToMaster() will be called when connection to the Master is available.
    /// OnJoinedLobby() will NOT be called if this is false.
    /// 
    /// Enabled by default.
    /// </summary>
    /// <remarks>
    /// The room listing will not become available.
    /// Rooms can be created and joined (randomly) without joining the lobby (and getting sent the room list).
    /// </remarks>
    public static bool autoJoinLobby
    {
        get
        {
            return autoJoinLobbyField;
        }
        set
        {
            autoJoinLobbyField = value;
        }
    }

    /// <summary>
    /// Backing field.
    /// </summary>
    private static bool autoJoinLobbyField = true;

    /// <summary>
    /// Returns true when we are connected to Photon and in the lobby state
    /// </summary>
    public static bool insideLobby
    {
        get
        {
            return networkingPeer.insideLobby;
        }
    }

    /// <summary>
    /// Defines how many times per second PhotonNetwork should send a package. If you change
    /// this, do not forget to also change 'sendRateOnSerialize'.
    /// </summary>
    /// <remarks>
    /// Less packages are less overhead but more delay.
    /// Setting the sendRate to 50 will create up to 50 packages per second (which is a lot!).
    /// Keep your target platform in mind: mobile networks are slower and less reliable.
    /// </remarks>
    public static int sendRate
    {
        get
        {
            return 1000 / sendInterval;
        }

        set
        {
            sendInterval = 1000 / value;
            if (photonMono != null)
            {
                photonMono.updateInterval = sendInterval;
            }

            if (value < sendRateOnSerialize)
            {
                // sendRateOnSerialize needs to be <= sendRate
                sendRateOnSerialize = value;
            }
        }
    }

    /// <summary>
    /// Defines how many times per second OnPhotonSerialize should be called on PhotonViews.
    /// </summary>
    /// <remarks>
    /// Choose this value in relation to 'sendRate'. OnPhotonSerialize will creart the commands to be put into packages.
    /// A lower rate takes up less performance but will cause more lag.
    /// </remarks>
    public static int sendRateOnSerialize
    {
        get
        {
            return 1000 / sendIntervalOnSerialize;
        }

        set
        {
            if (value > sendRate)
            {
                Debug.LogError("Error, can not set the OnSerialize SendRate more often then the overall SendRate");
                value = sendRate;
            }

            sendIntervalOnSerialize = 1000 / value;
            if (photonMono != null)
            {
                photonMono.updateIntervalOnSerialize = sendIntervalOnSerialize;
            }
        }
    }

    private static int sendInterval = 50; // in miliseconds.

    private static int sendIntervalOnSerialize = 100; // in miliseconds. I.e. 100 = 100ms which makes 10 times/second

    /// <summary>
    /// Can be used to pause dispatch of incoming evtents (RPCs, Instantiates and anything else incoming).
    /// This can be useful if you first want to load a level, then go on receiving data of PhotonViews and RPCs.
    /// The client will go on receiving and sending acknowledgements for incoming packages and your RPCs/Events.
    /// This adds "lag" and can cause issues when the pause is longer, as all incoming messages are just queued.
    /// </summary>
    public static bool isMessageQueueRunning
    {
        get
        {
            return m_isMessageQueueRunning;
        }

        set
        {
            networkingPeer.IsSendingOnlyAcks = !value;
            m_isMessageQueueRunning = value;
        }
    }

    /// <summary>Backup for property isMessageQueueRunning.</summary>
    private static bool m_isMessageQueueRunning = true;

    /// <summary>
    /// Used once per dispatch to limit unreliable commands per channel (so after a pause, many channels can still cause a lot of unreliable commands)
    /// </summary>
    public static int unreliableCommandsLimit
    {
        get
        {
            return networkingPeer.LimitOfUnreliableCommands;
        }

        set
        {
            networkingPeer.LimitOfUnreliableCommands = value;
        }
    }

    /// <summary>
    /// Photon network time, synched with the server
    /// </summary>
    /// <remarks>
    /// v1.3:
    /// This time reflects milliseconds since start of the server, cut down to 4 bytes.
    /// It will overflow every 49 days from a high value to 0. We do not (yet) compensate this overflow.
    /// Master- and Game-Server will have different time values.
    /// v1.10:
    /// Fixed issues with precision for high server-time values. This should update with 15ms precision by default.
    /// </remarks>
    public static double time
    {
        get
        {
            if (offlineMode)
            {
                return Time.time;
            }
            else
            {
                return ((double)(uint)networkingPeer.ServerTimeInMilliSeconds) / 1000.0f;
            }
        }
    }

    /// <summary>
    /// Are we the master client?
    /// </summary>
    public static bool isMasterClient
    {
        get
        {
            if (offlineMode)
            {
                return true;
            }
            else
            {
                return networkingPeer.mMasterClient == networkingPeer.mLocalActor;
            }
        }
    }

    /// <summary>
    /// True if we are in a room (client) and NOT the room's masterclient
    /// </summary>
    public static bool isNonMasterClientInRoom
    {
        get
        {
            return !isMasterClient && room != null;
        }
    }

    /// <summary>
    /// The count of players currently looking for a room.
    /// This is updated on the MasterServer (only) in 5sec intervals (if any count changed).
    /// </summary>
    public static int countOfPlayersOnMaster
    {
        get
        {
            return networkingPeer.mPlayersOnMasterCount;
        }
    }

    /// <summary>
    /// The count of players currently inside a room
    /// This is updated on the MasterServer (only) in 5sec intervals (if any count changed).
    /// </summary>
    public static int countOfPlayersInRooms
    {
        get
        {
            return networkingPeer.mPlayersInRoomsCount;
        }
    }

    /// <summary>
    /// The count of players currently using this application.
    /// This is updated on the MasterServer (only) in 5sec intervals (if any count changed).
    /// </summary>
    public static int countOfPlayers
    {
        get
        {
            return networkingPeer.mPlayersInRoomsCount + networkingPeer.mPlayersOnMasterCount;
        }
    }

    /// <summary>
    /// The count of rooms currently in use.
    /// When inside the lobby this is based on PhotonNetwork.GetRoomList().Length.
    /// When not inside the lobby, this value updated on the MasterServer (only) in 5sec intervals (if any count changed).
    /// </summary>
    public static int countOfRooms
    {
        get
        {
            if (insideLobby)
            {
                return GetRoomList().Length;
            }
            else
            {
                return networkingPeer.mGameCount;
            }
        }
    }

    /// <summary>
    /// Enables or disables the collection of statistics about this client's traffic.
    /// If you encounter issues with clients, the traffic stats are a good starting point to find solutions.
    /// </summary>
    /// <remarks>
    /// Only with enabled stats, you can use GetVitalStats
    /// </remarks>
    public static bool NetworkStatisticsEnabled
    {
        get
        {
            return networkingPeer.TrafficStatsEnabled;
        }

        set
        {
            networkingPeer.TrafficStatsEnabled = value;
        }
    }

    /// <summary>
    /// Count of commands that got repeated (due to local repeat-timing before an ACK was received).
    /// </summary>
    public static int ResentReliableCommands
    {
        get { return networkingPeer.ResentReliableCommands; }
    }

    /// <summary>
    /// Resets the traffic stats and re-enables them.
    /// </summary>
    public static void NetworkStatisticsReset()
    {
        networkingPeer.TrafficStatsReset();
    }


    /// <summary>
    /// Only available when NetworkStatisticsEnabled was used to gather some stats.
    /// </summary>
    /// <returns>A string with vital networking statistics.</returns>
    public static string NetworkStatisticsToString()
    {
        if (networkingPeer == null || offlineMode)
        {
            return "Offline or in OfflineMode. No VitalStats available.";
        }

        return networkingPeer.VitalStatsToString(false);
    }

    /// <summary>
    /// Static constructor used for basic setup.
    /// </summary>
    static PhotonNetwork()
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            //Debug.Log(string.Format("PhotonNetwork.ctor() Not playing {0} {1}", UnityEditor.EditorApplication.isPlaying, UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode));
            return;
        }

        // This can happen when you recompile a script IN play made
        // This helps to surpress some errors, but will not fix breaking
        PhotonHandler[] photonHandlers = GameObject.FindObjectsOfType(typeof(PhotonHandler)) as PhotonHandler[];
        if (photonHandlers != null && photonHandlers.Length > 0)
        {
            Debug.LogWarning("Unity recompiled. Connection gets closed and replaced. You can connect as 'new' client.");
            foreach (PhotonHandler photonHandler in photonHandlers)
            {
                //Debug.Log("Handler: " + photonHandler + " photonHandler.gameObject: " + photonHandler.gameObject);
                photonHandler.gameObject.hideFlags = 0;
                GameObject.DestroyImmediate(photonHandler.gameObject);
                Component.DestroyImmediate(photonHandler);
            }
        }
#endif

        Application.runInBackground = true;

        // Set up a MonoBehaviour to run Photon, and hide it
        GameObject photonGO = new GameObject();
        photonMono = (PhotonHandler)photonGO.AddComponent<PhotonHandler>();

#if !(UNITY_WINRT || UNITY_WP8)
        photonGO.AddComponent<PingCloudRegions>();
#endif
        photonGO.name = "PhotonMono";
        photonGO.hideFlags = HideFlags.HideInHierarchy;

        // Set up the NetworkingPeer
        networkingPeer = new NetworkingPeer(photonMono, string.Empty, ConnectionProtocol.Udp);
        networkingPeer.LimitOfUnreliableCommands = 40;

        // Local player
        CustomTypes.Register();
    }

    /// <summary>
    /// Internally used by Editor scripts, called on Hierarchy change (includes scene save) to remove surplus hidden PhotonHandlers.
    /// </summary>
    public static void InternalCleanPhotonMonoFromSceneIfStuck()
    {
        PhotonHandler[] photonHandlers = GameObject.FindObjectsOfType(typeof(PhotonHandler)) as PhotonHandler[];
        if (photonHandlers != null && photonHandlers.Length > 0)
        {
            Debug.Log("Cleaning up hidden PhotonHandler instances in scene. Please save it. This is not an issue.");
            foreach (PhotonHandler photonHandler in photonHandlers)
            {
                // Debug.Log("Removing Handler: " + photonHandler + " photonHandler.gameObject: " + photonHandler.gameObject);
                photonHandler.gameObject.hideFlags = 0;

                if (photonHandler.gameObject != null && photonHandler.gameObject.name == "PhotonMono")
                {
                    GameObject.DestroyImmediate(photonHandler.gameObject);
                }

                Component.DestroyImmediate(photonHandler);
            }
        }
    }

    // FUNCTIONS

    /// <summary>
    /// Connect to the configured Photon server:
    /// Reads PhotonNetwork.serverSettingsAssetPath and connects to cloud or your own server.
    /// </summary>
    /// <remarks>
    /// The PUN Setup Wizard stores your appID in a settings file and applies a server address/port.
    /// This is used for Connect(string serverAddress, int port, string appID, string gameVersion).
    /// 
    /// To connect to the Photon Cloud, a valid AppId must be in the settings file (shown in the Photon Cloud Dashboard).
    /// https://cloud.exitgames.com/dashboard
    /// 
    /// Connecting to the Photon Cloud might fail due to:
    /// - Network issues (calls: OnFailedToConnectToPhoton())
    /// - Invalid region (calls: OnConnectionFail() with DisconnectCause.InvalidRegion)
    /// - Subscription CCU limit reached (calls: OnConnectionFail() with DisconnectCause.MaxCcuReached. also calls: OnPhotonMaxCccuReached())
    /// 
    /// More about the connection limitations:
    /// http://doc.exitgames.com/photon-cloud
    /// </remarks>
    /// <param name="gameVersion">This client's version number. Users are separated from each other by gameversion (which allows you to make breaking changes).</param>
    public static void ConnectUsingSettings(string gameVersion)
    {
        if (PhotonServerSettings == null)
        {
            Debug.LogError("Can't connect: Loading settings failed. ServerSettings asset must be in any 'Resources' folder as: " + PhotonNetwork.serverSettingsAssetFile);
            return;
        }

        if (PhotonServerSettings.HostType == ServerSettings.HostingOption.OfflineMode)
        {
            offlineMode = true;
            return;//
        }
        else
        {
            Connect(PhotonServerSettings.ServerAddress, PhotonServerSettings.ServerPort, PhotonServerSettings.AppID, gameVersion);
        }
    }

#if !(UNITY_WINRT || UNITY_WP8)
    /// <summary>
    /// Connect to the PUN cloud server with the lowest ping.
    /// Will save the result of pinging all cloud servers in PlayerPrefs. Calling this the first time can take +-2 seconds.
    /// The ping result can be overridden via PhotonNetwork.OverrideBestCloudServer(..)
    /// </summary>
    /// <remarks>
    /// This call can take up to 2 seconds if it is the first time you are using this, all Cloud servers will be pinged to check for the best region.
    /// 
    /// The PUN Setup Wizard stores your appID in a settings file and applies a server address/port.
    /// This is used for Connect(string serverAddress, int port, string appID, string gameVersion).
    /// 
    /// To connect to the Photon Cloud, a valid AppId must be in the settings file (shown in the Photon Cloud Dashboard).
    /// https://cloud.exitgames.com/dashboard
    /// 
    /// Connecting to the Photon Cloud might fail due to:
    /// - Network issues (calls: OnFailedToConnectToPhoton())
    /// - Invalid region (calls: OnConnectionFail() with DisconnectCause.InvalidRegion)
    /// - Subscription CCU limit reached (calls: OnConnectionFail() with DisconnectCause.MaxCcuReached. also calls: OnPhotonMaxCccuReached())
    /// 
    /// More about the connection limitations:
    /// http://doc.exitgames.com/photon-cloud
    /// </remarks>
    /// <param name="gameVersion">This client's version number. Users are separated from each other by gameversion (which allows you to make breaking changes).</param>
    public static void ConnectToBestCloudServer(string gameVersion)
    {
        if (PhotonServerSettings == null)
        {
            Debug.LogError("Can't connect: Loading settings failed. ServerSettings asset must be in any 'Resources' folder as: " + PhotonNetwork.serverSettingsAssetFile);
            return;
        }

        if (PhotonServerSettings.HostType == ServerSettings.HostingOption.OfflineMode)
        {
            offlineMode = true;
            return;//
        }
        else
        {
            PingCloudRegions.ConnectToBestRegion(gameVersion);
        }
    }

    /// <summary>
    /// Overwrites the region that is used for  ConnectToBestCloudServer(string gameVersion)
    /// This will overwrite the result that was gotten by pinging all cloud servers. 
    /// </summary>
    /// <remarks>
    /// Use this to allow your users to specify their region manually.
    /// </remarks>	
    public static void OverrideBestCloudServer(CloudServerRegion region)
    {
        PingCloudRegions.OverrideRegion(region);
    }

    /// <summary>Pings all cloud servers again to find the one with best ping (currently).</summary>
    public static void RefreshCloudServerRating()
    {
        PingCloudRegions.RefreshCloudServerRating();
    }
#endif

    /// <summary>
    /// Connect to the Photon server by address, port, appID and game(client) version.
    /// </summary>
    /// <remarks>
    /// This method is used by ConnectUsingSettings and ConnectToBestCloudServer.
    /// 
    /// To connect to the Photon Cloud, a valid AppId must be in the settings file (shown in the Photon Cloud Dashboard).
    /// https://cloud.exitgames.com/dashboard
    /// 
    /// Connecting to the Photon Cloud might fail due to:
    /// - Network issues (calls: OnFailedToConnectToPhoton())
    /// - Subscription CCU limit reached (calls: OnConnectionFail() with DisconnectCause.MaxCcuReached. also calls: OnPhotonMaxCccuReached())
    /// 
    /// More about the connection limitations:
    /// http://doc.exitgames.com/photon-cloud/
    /// </remarks>
    /// <param name="serverAddress">The server's address (either your own or Photon Cloud address).</param>
    /// <param name="port">The server's port to connect to.</param>
    /// <param name="appID">Your application ID (Photon Cloud provides you with a GUID for your game).</param>
    /// <param name="gameVersion">This client's version number. Users are separated by gameversion (which allows you to make breaking changes).</param>
    public static void Connect(string serverAddress, int port, string appID, string gameVersion)
    {
        if (serverAddress.Length <= 2)
        {
            Debug.LogError("Aborted Connect: invalid serverAddress: " + serverAddress);
            return;
        }

        if (networkingPeer.PeerState != PeerStateValue.Disconnected)
        {
            Debug.LogWarning("Connect() only works when disconnected. Current state: " + networkingPeer.PeerState);
            return;
        }

        if (offlineMode)
        {
            offlineMode = false; // Cleanup offline mode
            Debug.LogWarning("Shut down offline mode due to a connect attempt");
        }

        if (!isMessageQueueRunning)
        {
            isMessageQueueRunning = true;
            Debug.LogWarning("Forced enabling of isMessageQueueRunning because of a Connect()");
        }

        networkingPeer.mAppVersion = gameVersion + "_" + versionPUN;
        networkingPeer.MasterServerAddress = serverAddress + ":" + port;

        networkingPeer.Connect(networkingPeer.MasterServerAddress, appID);
    }

    /// <summary>
    /// Makes this client disconnect from the photon server, a process that leaves any room and calls OnDisconnectedFromPhoton on completition.
    /// </summary>
    /// <remarks>
    /// When the client is connected, the server is being informed that this client disconnects. 
    /// This speeds up leave/disconnect messages for players in the same room as you (otherwise the server would timeout this client's connection).
    /// When used in offlineMode, the state-change and event-call OnDisconnectedFromPhoton are immediate. 
    /// Offline mode is set to false as well.
    /// Once disconnected, the client can connect again. Use ConnectUsingSettings.
    /// </remarks>
    public static void Disconnect()
    {
        if (offlineMode)
        {
            offlineMode = false;
            networkingPeer.State = PeerState.Disconnecting;
            networkingPeer.OnStatusChanged(StatusCode.Disconnect);
            return;
        }

        if (networkingPeer == null)
        {
            return; // Surpress error when quitting playmode in the editor
        }

        networkingPeer.Disconnect();
    }

    /// <summary>
    /// Used for compatibility with Unity networking only. Encryption is automatically initialized while connecting.
    /// </summary>
    [Obsolete("Used for compatibility with Unity networking only. Encryption is automatically initialized while connecting.")]
    public static void InitializeSecurity()
    {
        return;
    }

    /// <summary>
    /// Request the rooms and online status for a list of friends. All client must set a unique username via PlayerName property. The result is available in this.Friends.
    /// </summary>
    /// <remarks>
    /// Used on Master Server to find the rooms played by a selected list of users.
    /// The result will be mapped to LoadBalancingClient.Friends when available. 
    /// The list is initialized by OpFindFriends on first use (before that, it is null).
    /// 
    /// Users identify themselves by setting a PlayerName in the LoadBalancingClient instance.
    /// This in turn will send the name in OpAuthenticate after each connect (to master and game servers).
    /// Note: Changing a player's name doesn't make sense when using a friend list.
    ///  
    /// The list of usernames must be fetched from some other source (not provided by Photon).
    /// 
    /// 
    /// Internal:
    /// The server response includes 2 arrays of info (each index matching a friend from the request):
    /// ParameterCode.FindFriendsResponseOnlineList = bool[] of online states
    /// ParameterCode.FindFriendsResponseRoomIdList = string[] of room names (empty string if not in a room)
    /// </remarks>
    /// <param name="friendsToFind">Array of friend's names (make sure they are unique).</param>
    /// <returns>If the operation could be sent (requires connection, only one request is allowed at any time). Always false in offline mode.</returns>
    public static bool FindFriends(string[] friendsToFind)
    {
        if (networkingPeer == null || isOfflineMode)
        {
            return false;
        }

        return networkingPeer.OpFindFriends(friendsToFind);
    }

    /// <summary>
    /// Creates a room with given name but fails if this room is existing already.
    /// </summary>
    /// <remarks>
    /// If you don't want to create a unique room-name, pass null or "" as name and the server will assign a roomName (a GUID as string).
    /// Call this only on the master server. 
    /// Internally, the master will respond with a server-address (and roomName, if needed). Both are used internally
    /// to switch to the assigned game server and roomName.
    /// 
    /// PhotonNetwork.autoCleanUpPlayerObjects will become this room's AutoCleanUp property and that's used by all clients that join this room.
    /// </remarks>
    /// <param name="roomName">Unique name of the room to create.</param>
    public static void CreateRoom(string roomName)
    {
        CreateRoom(roomName, true, true, 0, null, null);
    }

    /// <summary>
    /// Creates a room with given name but fails if this room is existing already.
    /// </summary>
    /// <remarks>
    /// If you don't want to create a unique room-name, pass null or "" as name and the server will assign a roomName (a GUID as string).
    /// Call this only on the master server. 
    /// Internally, the master will respond with a server-address (and roomName, if needed). Both are used internally
    /// to switch to the assigned game server and roomName
    /// </remarks>
    /// <param name="roomName">Unique name of the room to create. Pass null or "" to make the server generate a name.</param>
    /// <param name="isVisible">Shows (or hides) this room from the lobby's listing of rooms.</param>
    /// <param name="isOpen">Allows (or disallows) others to join this room.</param>
    /// <param name="maxPlayers">Max number of players that can join the room.</param>
    public static void CreateRoom(string roomName, bool isVisible, bool isOpen, int maxPlayers)
    {
        CreateRoom(roomName, isVisible, isOpen, maxPlayers, null, null);
    }

    /// <summary>
    /// Creates a room with given name but fails if this room is existing already.
    /// </summary>
    /// <remarks>
    /// If you don't want to create a unique room-name, pass null or "" as name and the server will assign a roomName (a GUID as string).
    /// Call this only on the master server. 
    /// Internally, the master will respond with a server-address (and roomName, if needed). Both are used internally
    /// to switch to the assigned game server and roomName.
    /// 
    /// PhotonNetwork.autoCleanUpPlayerObjects will become this room's AutoCleanUp property and that's used by all clients that join this room.
    /// </remarks>
    /// <param name="roomName">Unique name of the room to create. Pass null or "" to make the server generate a name.</param>
    /// <param name="isVisible">Shows (or hides) this room from the lobby's listing of rooms.</param>
    /// <param name="isOpen">Allows (or disallows) others to join this room.</param>
    /// <param name="maxPlayers">Max number of players that can join the room.</param>
    /// <param name="customRoomProperties">Custom properties of the new room (set on create, so they are immediately available).</param>
    /// <param name="propsToListInLobby">Array of custom-property-names that should be forwarded to the lobby (include only the useful ones).</param>
    public static void CreateRoom(string roomName, bool isVisible, bool isOpen, int maxPlayers, Hashtable customRoomProperties, string[] propsToListInLobby)
    {
        if (connectionStateDetailed == PeerState.Joining || connectionStateDetailed == PeerState.Joined || connectionStateDetailed == PeerState.ConnectedToGameserver)
        {
            Debug.LogError("CreateRoom aborted: You can only create a room while not currently connected/connecting to a room.");
        }
        else if (room != null)
        {
            Debug.LogError("CreateRoom aborted: You are already in a room!");
        }
        else
        {
            if (offlineMode)
            {
                offlineMode_inRoom = true;
                NetworkingPeer.SendMonoMessage(PhotonNetworkingMessage.OnCreatedRoom);
                NetworkingPeer.SendMonoMessage(PhotonNetworkingMessage.OnJoinedRoom);
            }
            else
            {
                if (maxPlayers > 255)
                {
                    Debug.LogError("Error: CreateRoom called with " + maxPlayers + " maxplayers. This has been reverted to the max of 255 players because internally a 'byte' is used.");
                    maxPlayers = 255;
                }

                networkingPeer.OpCreateGame(roomName, isVisible, isOpen, (byte)maxPlayers, autoCleanUpPlayerObjects, customRoomProperties, propsToListInLobby);
            }
        }
    }

    /// <summary>
    /// Join room by room.Name.
    /// This fails if the room is either full or no longer available (might close at the same time).
    /// </summary>
    /// <param name="roomName">The room instance to join (only listedRoom.Name is used).</param>
    public static void JoinRoom(RoomInfo listedRoom)
    {
        if (listedRoom == null)
        {
            Debug.LogError("JoinRoom aborted: you passed a NULL room");
            return;
        }

        JoinRoom(listedRoom.name);
    }

    /// <summary>
    /// Join room with given title.
    /// This fails if the room is either full or no longer available (might close at the same time).
    /// </summary>
    /// <param name="roomName">Unique name of the room to create.</param>
    public static void JoinRoom(string roomName)
    {
        if (connectionStateDetailed == PeerState.Joining || connectionStateDetailed == PeerState.Joined || connectionStateDetailed == PeerState.ConnectedToGameserver)
        {
            Debug.LogError("JoinRoom aborted: You can only join a room while not currently connected/connecting to a room.");
        }
        else if (room != null)
        {
            Debug.LogError("JoinRoom aborted: You are already in a room!");
        }
        else if (roomName == string.Empty)
        {
            Debug.LogError("JoinRoom aborted: You must specifiy a room name!");
        }
        else
        {
            if (offlineMode)
            {
                offlineMode_inRoom = true;
                NetworkingPeer.SendMonoMessage(PhotonNetworkingMessage.OnJoinedRoom);
            }
            else
            {
                networkingPeer.OpJoin(roomName);
            }
        }
    }

    /// <summary>
    /// Joins any available room but will fail if none is currently available.
    /// </summary>
    /// <remarks>
    /// If this fails, you can still create a room (and make this available for the next who uses JoinRandomRoom).
    /// Alternatively, try again in a moment.
    /// </remarks>
    public static void JoinRandomRoom()
    {
        JoinRandomRoom(null, 0);
    }

    /// <summary>
    /// Attempts to join an open room with fitting, custom properties but fails if none is currently available.
    /// </summary>
    /// <remarks>
    /// If this fails, you can still create a room (and make this available for the next who uses JoinRandomRoom).
    /// Alternatively, try again in a moment.
    /// </remarks>
    /// <param name="expectedCustomRoomProperties">Filters for rooms that match these custom properties (string keys and values). To ignore, pass null.</param>
    /// <param name="expectedMaxPlayers">Filters for a particular maxplayer setting. Use 0 to accept any maxPlayer value.</param>
    public static void JoinRandomRoom(Hashtable expectedCustomRoomProperties, byte expectedMaxPlayers)
    {
        JoinRandomRoom(expectedCustomRoomProperties, expectedMaxPlayers, MatchmakingMode.FillRoom);
    }

    /// <summary>
    /// Attempts to join an open room with fitting, custom properties but fails if none is currently available.
    /// </summary>
    /// <remarks>
    /// If this fails, you can still create a room (and make this available for the next who uses JoinRandomRoom).
    /// Alternatively, try again in a moment.
    /// </remarks>
    /// <param name="expectedCustomRoomProperties">Filters for rooms that match these custom properties (string keys and values). To ignore, pass null.</param>
    /// <param name="expectedMaxPlayers">Filters for a particular maxplayer setting. Use 0 to accept any maxPlayer value.</param>
    /// <param name="matchingType">Selects one of the available matchmaking algorithms. See MatchmakingMode enum for options.</param>
    public static void JoinRandomRoom(Hashtable expectedCustomRoomProperties, byte expectedMaxPlayers, MatchmakingMode matchingType)
    {
        if (connectionStateDetailed == PeerState.Joining || connectionStateDetailed == PeerState.Joined || connectionStateDetailed == PeerState.ConnectedToGameserver)
        {
            Debug.LogError("JoinRandomRoom aborted: You can only join a room while not currently connected/connecting to a room.");
            return;
        }

        if (room != null)
        {
            Debug.LogError("JoinRandomRoom aborted: You are already in a room!");
            return;
        }

        if (offlineMode)
        {
            offlineMode_inRoom = true;
            NetworkingPeer.SendMonoMessage(PhotonNetworkingMessage.OnJoinedRoom);
        }
        else
        {
            Hashtable expectedRoomProperties = new Hashtable();
            expectedRoomProperties.MergeStringKeys(expectedCustomRoomProperties);
            if (expectedMaxPlayers > 0)
            {
                expectedRoomProperties[GameProperties.MaxPlayers] = expectedMaxPlayers;
            }

            networkingPeer.OpJoinRandomRoom(expectedRoomProperties, 0, null, matchingType);
        }
    }

    /// <summary>
    /// Leave the current room
    /// </summary>
    public static void LeaveRoom()
    {
        if (!offlineMode && connectionStateDetailed != PeerState.Joined)
        {
            Debug.LogError("PhotonNetwork: Error, you cannot leave a room if you're not in a room!(1)");
            return;
        }
        else if (room == null)
        {
            Debug.LogError("PhotonNetwork: Error, you cannot leave a room if you're not in a room!(2)");
            return;
        }

        if (offlineMode)
        {
            offlineMode_inRoom = false;
            NetworkingPeer.SendMonoMessage(PhotonNetworkingMessage.OnLeftRoom);
        }
        else
        {
            networkingPeer.OpLeave();
        }
    }

    /// <summary>
    /// Gets an array of (currently) known rooms as RoomInfo.
    /// This list is automatically updated every few seconds while this client is in the lobby (on the Master Server).
    /// Not available while being in a room.
    /// </summary>
    /// <remarks>Creates a new instance of the list each time called. Copied from networkingPeer.mGameList.</remarks>
    /// <returns>RoomInfo[] of current rooms in lobby.</returns>
    public static RoomInfo[] GetRoomList()
    {
        if (offlineMode)
        {
            return new RoomInfo[0];
        }

        if (networkingPeer == null)
        {
            return new RoomInfo[0]; // Surpress erorrs when quitting game
        }

        return networkingPeer.mGameListCopy;
    }

    /// <summary>
    /// Sets this (local) player's properties.
    /// This caches the properties in PhotonNetwork.player.customProperties.
    /// CreateRoom, JoinRoom and JoinRandomRoom will all apply your player's custom properties when you enter the room.
    /// While in a room, your properties are synced with the other players.
    /// If the Hashtable is null, the custom properties will be cleared.
    /// Custom properties are never cleared automatically, so they carry over to the next room, if you don't change them.
    /// </summary>
    /// <remarks>
    /// Don't set properties by modifying PhotonNetwork.player.customProperties!
    /// </remarks>
    /// <param name="customProperties">Only string-typed keys will be used from this hashtable. If null, custom properties are all deleted.</param>
    public static void SetPlayerCustomProperties(Hashtable customProperties)
    {
        if (customProperties == null)
        {
            customProperties = new Hashtable();
            foreach (object k in player.customProperties.Keys)
            {
                customProperties[(string)k] = null;
            }
        }

        if (room != null && room.isLocalClientInside)
        {
            player.SetCustomProperties(customProperties);
        }
        else
        {
            player.InternalCacheProperties(customProperties);
        }
    }

    internal static int lastUsedViewSubId = 0;  // each player only needs to remember it's own (!) last used subId to speed up assignment
    internal static int lastUsedViewSubIdStatic = 0;  // per room, the master is able to instantiate GOs. the subId for this must be unique too
    internal static List<int> manuallyAllocatedViewIds = new List<int>();

    /// <summary>
    /// Allocates a viewID that's valid for the current/local player.
    /// </summary>
    /// <returns>A viewID that can be used for a new PhotonView.</returns>
    public static int AllocateViewID()
    {
        int manualId = AllocateViewID(player.ID);
        manuallyAllocatedViewIds.Add(manualId);
        return manualId;
    }

    /// <summary>
    /// Unregister a viewID (of manually instantiated and destroyed networked objects).
    /// </summary>
    /// <param name="viewID">A viewID manually allocated by this player.</param>
    public static void UnAllocateViewID(int viewID)
    {
        manuallyAllocatedViewIds.Remove(viewID);

        if (networkingPeer.photonViewList.ContainsKey(viewID))
        {
            Debug.LogWarning(string.Format("Unallocated manually used viewID: {0} but found it used still in a PhotonView: {1}", viewID, networkingPeer.photonViewList[viewID]));
        }
    }

    // use 0 for scene-targetPhotonView-ids
    // returns viewID (combined owner and sub id)
    private static int AllocateViewID(int ownerId)
    {
        if (ownerId == 0)
        {
            // we look up a fresh subId for the owner "room" (mind the "sub" in subId)
            int newSubId = lastUsedViewSubIdStatic;
            int newViewId;
            int ownerIdOffset = ownerId * MAX_VIEW_IDS;
            for (int i = 1; i < MAX_VIEW_IDS; i++)
            {
                newSubId = (newSubId + 1) % MAX_VIEW_IDS;
                if (newSubId == 0)
                {
                    continue;   // avoid using subID 0
                }

                newViewId = newSubId + ownerIdOffset;
                if (!networkingPeer.photonViewList.ContainsKey(newViewId))
                {
                    lastUsedViewSubIdStatic = newSubId;
                    return newViewId;
                }
            }

            // this is the error case: we didn't find any (!) free subId for this user
            throw new Exception(string.Format("AllocateViewID() failed. Room (user {0}) is out of subIds, as all room viewIDs are used.", ownerId));
        }
        else
        {
            // we look up a fresh SUBid for the owner
            int newSubId = lastUsedViewSubId;
            int newViewId;
            int ownerIdOffset = ownerId * MAX_VIEW_IDS;
            for (int i = 1; i < MAX_VIEW_IDS; i++)
            {
                newSubId = (newSubId + 1) % MAX_VIEW_IDS;
                if (newSubId == 0)
                {
                    continue;   // avoid using subID 0
                }

                newViewId = newSubId + ownerIdOffset;
                if (!networkingPeer.photonViewList.ContainsKey(newViewId) && !manuallyAllocatedViewIds.Contains(newViewId))
                {
                    lastUsedViewSubId = newSubId;
                    return newViewId;
                }
            }

            throw new Exception(string.Format("AllocateViewID() failed. User {0} is out of subIds, as all viewIDs are used.", ownerId));
        }
    }

    private static int[] AllocateSceneViewIDs(int countOfNewViews)
    {
        int[] viewIDs = new int[countOfNewViews];
        for (int view = 0; view < countOfNewViews; view++)
        {
            viewIDs[view] = AllocateViewID(0);
        }

        return viewIDs;
    }

    /// <summary>
    /// Instantiate a prefab over the network. This prefab needs to be located in the root of a "Resources" folder.
    /// </summary>
    /// <remarks>Instead of using prefabs in the Resources folder, you can manually Instantiate and assign PhotonViews. See doc.</remarks>
    /// <param name="prefabName">Name of the prefab to instantiate.</param>
    /// <param name="position">Position Vector3 to apply on instantiation.</param>
    /// <param name="rotation">Rotation Quaternion to apply on instantiation.</param>
    /// <param name="group">The group for this PhotonView.</param>
    /// <returns>The new instance of a GameObject with initialized PhotonView.</returns>
    public static GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, int group)
    {
        return Instantiate(prefabName, position, rotation, group, null);
    }

    /// <summary>
    /// Instantiate a prefab over the network. This prefab needs to be located in the root of a "Resources" folder.
    /// </summary>
    /// <remarks>Instead of using prefabs in the Resources folder, you can manually Instantiate and assign PhotonViews. See doc.</remarks>
    /// <param name="prefabName">Name of the prefab to instantiate.</param>
    /// <param name="position">Position Vector3 to apply on instantiation.</param>
    /// <param name="rotation">Rotation Quaternion to apply on instantiation.</param>
    /// <param name="group">The group for this PhotonView.</param>
    /// <param name="data">Optional instantiation data. This will be saved to it's PhotonView.instantiationData.</param>
    /// <returns>The new instance of a GameObject with initialized PhotonView.</returns>
    public static GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, int group, object[] data)
    {
        if (!VerifyCanUseNetwork())
        {
            Debug.LogError("PhotonNetwork error: Could not Instantiate the prefab [" + prefabName + "] as the game is not connected.");
            return null;
        }

        GameObject prefabGo;
        if (!UsePrefabCache || !PrefabCache.TryGetValue(prefabName, out prefabGo))
        {
            prefabGo = (GameObject)Resources.Load(prefabName, typeof(GameObject));
            if (UsePrefabCache)
            {
                PrefabCache.Add(prefabName, prefabGo);
            }
        }

        if (prefabGo == null)
        {
            Debug.LogError("PhotonNetwork error: Could not Instantiate the prefab [" + prefabName + "]. Please verify you have this gameobject in a Resources folder (and not in a subfolder)");
            return null;
        }

        if (prefabGo.GetComponent<PhotonView>() == null)
        {
            Debug.LogError("PhotonNetwork error: Could not Instantiate the prefab [" + prefabName + "] as it has no PhotonView attached to the root.");
            return null;
        }

        Component[] views = (Component[])prefabGo.GetComponentsInChildren<PhotonView>(true);
        int[] viewIDs = new int[views.Length];
        for (int i = 0; i < viewIDs.Length; i++)
        {
            //Debug.Log("Instantiate prefabName: " + prefabName + " player.ID: " + player.ID);
            viewIDs[i] = AllocateViewID(player.ID);
        }

        // Send to others, create info
        Hashtable instantiateEvent = networkingPeer.SendInstantiate(prefabName, position, rotation, group, viewIDs, data, false);

        // Instantiate the GO locally (but the same way as if it was done via event). This will also cache the instantiationId
        return networkingPeer.DoInstantiate(instantiateEvent, networkingPeer.mLocalActor, prefabGo);
    }


    /// <summary>
    /// Instantiate a scene-owned prefab over the network. The PhotonViews will be controllable by the MasterClient. This prefab needs to be located in the root of a "Resources" folder.
    /// </summary>
    /// <remarks>
    /// Only the master client can Instantiate scene objects.
    /// Instead of using prefabs in the Resources folder, you can manually Instantiate and assign PhotonViews. See doc.
    /// </remarks>
    /// <param name="prefabName">Name of the prefab to instantiate.</param>
    /// <param name="position">Position Vector3 to apply on instantiation.</param>
    /// <param name="rotation">Rotation Quaternion to apply on instantiation.</param>
    /// <param name="group">The group for this PhotonView.</param>
    /// <param name="data">Optional instantiation data. This will be saved to it's PhotonView.instantiationData.</param>
    /// <returns>The new instance of a GameObject with initialized PhotonView.</returns>
    public static GameObject InstantiateSceneObject(string prefabName, Vector3 position, Quaternion rotation, int group, object[] data)
    {
        if (!VerifyCanUseNetwork())
        {
            return null;
        }
        if (!isMasterClient)
        {
            Debug.LogError("PhotonNetwork error [InstantiateSceneObject]: Only the master client can Instantiate scene objects");
            return null;
        }

        GameObject prefabGo;
        if (!UsePrefabCache || !PrefabCache.TryGetValue(prefabName, out prefabGo))
        {
            prefabGo = (GameObject)Resources.Load(prefabName, typeof(GameObject));
            if (UsePrefabCache)
            {
                PrefabCache.Add(prefabName, prefabGo);
            }
        }

        if (prefabGo == null)
        {
            Debug.LogError("PhotonNetwork error [InstantiateSceneObject]: Could not Instantiate the prefab [" + prefabName + "]. Please verify you have this gameobject in a Resources folder (and not in a subfolder)");
            return null;
        }

        // a scene object instantiated with network visibility has to contain a PhotonView
        if (prefabGo.GetComponent<PhotonView>() == null)
        {
            Debug.LogError("PhotonNetwork error [InstantiateSceneObject]: Could not Instantiate the prefab [" + prefabName + "] as it has no PhotonView attached to the root.");
            return null;
        }

        Component[] views = (Component[])prefabGo.GetPhotonViewsInChildren();
        int[] viewIDs = AllocateSceneViewIDs(views.Length);

        if (viewIDs == null)
        {
            Debug.LogError("PhotonNetwork error [InstantiateSceneObject]: Could not Instantiate the prefab [" + prefabName + "] as no ViewIDs are free to use. Max is: " + MAX_VIEW_IDS);
            return null;
        }

        // Send to others, create info
        Hashtable instantiateEvent = networkingPeer.SendInstantiate(prefabName, position, rotation, group, viewIDs, data, true);

        // Instantiate the GO locally (but the same way as if it was done via event). This will also cache the instantiationId
        return networkingPeer.DoInstantiate(instantiateEvent, networkingPeer.mLocalActor, prefabGo);
    }

    /// <summary>
    /// The current roundtrip time to the photon server
    /// </summary>
    /// <returns>Roundtrip time (to server and back).</returns>
    public static int GetPing()
    {
        return networkingPeer.RoundTripTime;
    }

    /// <summary>Refreshes the server timestamp (async operation, takes a roundtrip).</summary>
    /// <remarks>Can be useful if a bad connection made the timestamp unusable or imprecise.</remarks>
    public static void FetchServerTimestamp()
    {
        if (networkingPeer != null)
        {
            networkingPeer.FetchServerTimestamp();
        }
    }

    /// <summary>
    /// Can be used to immediately send the RPCs and Instantiates just made, 
    /// so they are on their way to the other players.
    /// </summary>
    /// <remarks>
    /// This could be useful if you do a RPC to load a level and then load it yourself.
    /// While loading, no RPCs are sent to others, so this would delay the "load" RPC.
    /// You can send the RPC to "others", use this method, disable the message queue 
    /// (by isMessageQueueRunning) and then load.
    /// </remarks>
    public static void SendOutgoingCommands()
    {
        if (!VerifyCanUseNetwork())
        {
            return;
        }

        while (networkingPeer.SendOutgoingCommands())
        {
        }
    }

    /// <summary>
    /// Request a client to disconnect (KICK). Only the master client can do this.
    /// </summary>
    /// <param name="kickPlayer">The PhotonPlayer to kick.</param>
    public static void CloseConnection(PhotonPlayer kickPlayer)
    {
        if (!VerifyCanUseNetwork())
        {
            return;
        }

        if (!player.isMasterClient)
        {
            Debug.LogError("CloseConnection: Only the masterclient can kick another player.");
        }

        if (kickPlayer == null)
        {
            Debug.LogError("CloseConnection: No such player connected!");
        }
        else
        {
            int[] rec = new int[1];
            rec[0] = kickPlayer.ID;
            networkingPeer.OpRaiseEvent(PunEvent.CloseConnection, null, true, 0, rec);
        }
    }

    /// <summary>
    /// Network-Destroy the GameObject associated with the PhotonView, unless the PhotonView is static or not under this client's control.
    /// </summary>
    /// <remarks>
    /// Destroying a networked GameObject includes:
    /// - Removal of the Instantiate call from the server's room buffer.
    /// - Removing RPCs buffered for PhotonViews that got created indirectly with the PhotonNetwork.Instantiate call.
    /// - Sending a message to other clients to remove the GameObject also (affected by network lag).
    /// 
    /// Destroying networked objects works only if they got created with PhotonNetwork.Instantiate().
    /// Objects loaded with a scene are ignored, no matter if they have PhotonView components.
    /// 
    /// The GameObject must be under this client's control:
    /// - Instantiated and owned by this client.
    /// - Instantiated objects of players who left the room are controlles by the Master Client.
    /// - Scene-owned game objects are controlled by the Master Client.
    /// </remarks>
    /// <returns>Nothing. Check error debug log for any issues.</returns>
    public static void Destroy(PhotonView targetView)
    {
        if (targetView != null)
        {
            networkingPeer.RemoveInstantiatedGO(targetView.gameObject, false);
        }
        else
        {
            Debug.LogError("Destroy(targetPhotonView) failed, cause targetPhotonView is null.");
        }
    }

    /// <summary>
    /// Network-Destroy the GameObject, unless it is static or not under this client's control.
    /// </summary>
    /// <remarks>
    /// Destroying a networked GameObject includes:
    /// - Removal of the Instantiate call from the server's room buffer.
    /// - Removing RPCs buffered for PhotonViews that got created indirectly with the PhotonNetwork.Instantiate call.
    /// - Sending a message to other clients to remove the GameObject also (affected by network lag).
    /// 
    /// Destroying networked objects works only if they got created with PhotonNetwork.Instantiate().
    /// Objects loaded with a scene are ignored, no matter if they have PhotonView components.
    /// 
    /// The GameObject must be under this client's control:
    /// - Instantiated and owned by this client.
    /// - Instantiated objects of players who left the room are controlles by the Master Client.
    /// - Scene-owned game objects are controlled by the Master Client.
    /// </remarks>
    /// <returns>Nothing. Check error debug log for any issues.</returns>
    public static void Destroy(GameObject targetGo)
    {
        networkingPeer.RemoveInstantiatedGO(targetGo, false);
    }

    /// <summary>
    /// Network-Destroy all GameObjects, PhotonViews and their RPCs of targetPlayer. Can only be called on local player (for "self") or Master Client (for anyone).
    /// </summary>
    /// <remarks>
    /// Destroying a networked GameObject includes:
    /// - Removal of the Instantiate call from the server's room buffer.
    /// - Removing RPCs buffered for PhotonViews that got created indirectly with the PhotonNetwork.Instantiate call.
    /// - Sending a message to other clients to remove the GameObject also (affected by network lag).
    /// 
    /// Destroying networked objects works only if they got created with PhotonNetwork.Instantiate().
    /// Objects loaded with a scene are ignored, no matter if they have PhotonView components.
    /// </remarks>
    /// <returns>Nothing. Check error debug log for any issues.</returns>
    public static void DestroyPlayerObjects(PhotonPlayer targetPlayer)
    {
        if (player == null)
        {
            Debug.LogError("DestroyPlayerObjects() failed, cause parameter 'targetPlayer' was null.");
        }

        DestroyPlayerObjects(targetPlayer.ID);
    }

    /// <summary>
    /// Network-Destroy all GameObjects, PhotonViews and their RPCs of this player (by ID). Can only be called on local player (for "self") or Master Client (for anyone).
    /// </summary>
    /// <remarks>
    /// Destroying a networked GameObject includes:
    /// - Removal of the Instantiate call from the server's room buffer.
    /// - Removing RPCs buffered for PhotonViews that got created indirectly with the PhotonNetwork.Instantiate call.
    /// - Sending a message to other clients to remove the GameObject also (affected by network lag).
    /// 
    /// Destroying networked objects works only if they got created with PhotonNetwork.Instantiate().
    /// Objects loaded with a scene are ignored, no matter if they have PhotonView components.
    /// </remarks>
    /// <returns>Nothing. Check error debug log for any issues.</returns>
    public static void DestroyPlayerObjects(int targetPlayerId)
    {
        if (!VerifyCanUseNetwork())
        {
            return;
        }
        if (player.isMasterClient || targetPlayerId == player.ID)
        {
            networkingPeer.DestroyPlayerObjects(targetPlayerId, false);
        }
        else
        {
            Debug.LogError("DestroyPlayerObjects() failed, cause players can only destroy their own GameObjects. A Master Client can destroy anyone's. This is master: " + PhotonNetwork.isMasterClient);
        }
    }

    /// <summary>
    /// Network-Destroy all GameObjects, PhotonViews and their RPCs in the room. Removes anything buffered from the server. Can only be called by Master Client (for anyone).
    /// </summary>
    /// <remarks>
    /// Can only be called by Master Client (for anyone).
    /// Unlike the Destroy methods, this will remove anything from the server's room buffer. If your game
    /// buffers anything beyond Instantiate and RPC calls, that will be cleaned as well from server.
    /// 
    /// Destroying all includes:
    /// - Remove anything from the server's room buffer (Instantiate, RPCs, anything buffered).
    /// - Sending a message to other clients to destroy everything locally, too (affected by network lag).
    /// 
    /// Destroying networked objects works only if they got created with PhotonNetwork.Instantiate().
    /// Objects loaded with a scene are ignored, no matter if they have PhotonView components.
    /// </remarks>
    /// <returns>Nothing. Check error debug log for any issues.</returns>
    public static void DestroyAll()
    {
        if (isMasterClient)
        {
            networkingPeer.DestroyAll(false);
        }
        else
        {
            Debug.LogError("Couldn't call RemoveAllInstantiatedObjects as only the master client is allowed to call this.");
        }
    }

    /// <summary>
    /// Remove all buffered RPCs from server that were sent by targetPlayer. Can only be called on local player (for "self") or Master Client (for anyone).
    /// </summary>
    /// <remarks>
    /// This method requires either:
    /// - This is the targetPlayer's client.
    /// - This client is the Master Client (can remove any PhotonPlayer's RPCs).
    /// 
    /// If the targetPlayer calls RPCs at the same time that this is called, 
    /// network lag will determine if those get buffered or cleared like the rest.
    /// </remarks>
    /// <param name="targetPlayer">This player's buffered RPCs get removed from server buffer.</param>
    public static void RemoveRPCs(PhotonPlayer targetPlayer)
    {
        if (!VerifyCanUseNetwork())
        {
            return;
        }

        if (!targetPlayer.isLocal && !isMasterClient)
        {
            Debug.LogError("Error; Only the MasterClient can call RemoveRPCs for other players.");
            return;
        }

        networkingPeer.OpCleanRpcBuffer(targetPlayer.ID);
    }

    /// <summary>
    /// Remove all buffered RPCs from server that were sent via targetPhotonView. The Master Client and the owner of the targetPhotonView may call this.
    /// </summary>
    /// <remarks>
    /// This method requires either:
    /// - The targetPhotonView is owned by this client (Instantiated by it).
    /// - This client is the Master Client (can remove any PhotonView's RPCs).
    /// </remarks>
    /// <param name="targetPhotonView">RPCs buffered for this PhotonView get removed from server buffer.</param>
    public static void RemoveRPCs(PhotonView targetPhotonView)
    {
        if (!VerifyCanUseNetwork())
        {
            return;
        }

        networkingPeer.CleanRpcBufferIfMine(targetPhotonView);
    }

    /// <summary>
    /// Remove all buffered RPCs from server that were sent in the targetGroup, if this is the Master Client or if this controls the individual PhotonView.
    /// </summary>
    /// <remarks>
    /// This method requires either:
    /// - This client is the Master Client (can remove any RPCs per group).
    /// - Any other client: each PhotonView is checked if it is under this client's control. Only those RPCs are removed.
    /// </remarks>
    /// <param name="targetGroup">Interest group that gets all RPCs removed.</param>
    public static void RemoveRPCsInGroup(int targetGroup)
    {
        if (!VerifyCanUseNetwork())
        {
            return;
        }
        
        networkingPeer.RemoveRPCsInGroup(targetGroup);
    }

    /// <summary>
    /// Internal to send an RPC on given PhotonView. Do not call this directly but use: PhotonView.RPC!
    /// </summary>
    internal static void RPC(PhotonView view, string methodName, PhotonTargets target, params object[] parameters)
    {
        if (!VerifyCanUseNetwork())
        {
            return;
        }

        if (room == null)
        {
            Debug.LogWarning("Cannot send RPCs in Lobby! RPC dropped.");
            return;
        }

        if (networkingPeer != null)
        {
            networkingPeer.RPC(view, methodName, target, parameters);
        }
        else
        {
            Debug.LogWarning("Could not execute RPC " + methodName + ". Possible scene loading in progress?");
        }
    }

    /// <summary>
    /// Internal to send an RPC on given PhotonView. Do not call this directly but use: PhotonView.RPC!
    /// </summary>
    internal static void RPC(PhotonView view, string methodName, PhotonPlayer targetPlayer, params object[] parameters)
    {
        if (!VerifyCanUseNetwork())
        {
            return;
        }

        if (room == null)
        {
            Debug.LogWarning("Cannot send RPCs in Lobby, only processed locally");
            return;
        }

        if (player == null)
        {
            Debug.LogError("Error; Sending RPC to player null! Aborted \"" + methodName + "\"");
        }

        if (networkingPeer != null)
        {
            networkingPeer.RPC(view, methodName, targetPlayer, parameters);
        }
        else
        {
            Debug.LogWarning("Could not execute RPC " + methodName + ". Possible scene loading in progress?");
        }
    }

    /// <summary>
    /// Enable/disable receiving on given group (applied to PhotonViews)
    /// </summary>
    /// <param name="group">The interest group to affect.</param>
    /// <param name="enabled">Sets if receiving from group to enabled (or not).</param>
    public static void SetReceivingEnabled(int group, bool enabled)
    {
        if (!VerifyCanUseNetwork())
        {
            return;
        }
        networkingPeer.SetReceivingEnabled(group, enabled);
    }

    /// <summary>
    /// Enable/disable sending on given group (applied to PhotonViews)
    /// </summary>
    /// <param name="group">The interest group to affect.</param>
    /// <param name="enabled">Sets if sending to group is enabled (or not).</param>
    public static void SetSendingEnabled(int group, bool enabled)
    {
        if (!VerifyCanUseNetwork())
        {
            return;
        }

        networkingPeer.SetSendingEnabled(group, enabled);
    }

    /// <summary>
    /// Sets level prefix for PhotonViews instantiated later on. Don't set it if you need only one!
    /// </summary>
    /// <remarks>
    /// Important: If you don't use multiple level prefixes, simply don't set this value. The
    /// default value is optimized out of the traffic.
    /// 
    /// This won't affect existing PhotonViews (they can't be changed yet for existing PhotonViews).
    /// 
    /// Messages sent with a different level prefix will be received but not executed. This affects 
    /// RPCs, Instantiates and synchronization.
    /// 
    /// Be aware that PUN never resets this value, you'll have to do so yourself.
    /// </remarks>
    /// <param name="prefix">Max value is short.MaxValue = 32767</param>
    public static void SetLevelPrefix(short prefix)
    {
        if (!VerifyCanUseNetwork())
        {
            return;
        }

        networkingPeer.SetLevelPrefix(prefix);
    }

    /// <summary>
    /// Helper function which is called inside this class to erify if certain functions can be used (e.g. RPC when not connected)
    /// </summary>
    /// <returns></returns>
    private static bool VerifyCanUseNetwork()
    {
        if (networkingPeer != null && (offlineMode || connected))
        {
            return true;
        }

        Debug.LogError("Cannot send messages when not connected; Either connect to Photon OR use offline mode!");
        return false;
    }

    /// <summary>
    /// Loads the level and automatically pauses the network queue. Call this in OnJoinedRoom to make sure no cached RPCs are fired in the wrong scene.
    /// </summary>
    /// <param name='levelNumber'>
    /// Number of the level to load (make sure it's in the build preferences).
    /// </param>
    public static void LoadLevel(int levelNumber)
    {
        PhotonNetwork.isMessageQueueRunning = false;
        networkingPeer.loadingLevelAndPausedNetwork = true;
        Application.LoadLevel(levelNumber);
    }

    /// <summary>
    /// Loads the level and automatically pauses the network queue. Call this in OnJoinedRoom to make sure no cached RPCs are fired in the wrong scene.
    /// </summary>
    /// <param name='levelTitle'>
    /// Name of the level to load.
    /// </param>
    public static void LoadLevel(string levelTitle)
    {
        PhotonNetwork.isMessageQueueRunning = false;
        networkingPeer.loadingLevelAndPausedNetwork = true;
        Application.LoadLevel(levelTitle);
    }
}