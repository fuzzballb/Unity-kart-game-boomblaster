// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NetworkingPeer.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Networking (PUN)
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using ExitGames.Client.Photon;
using ExitGames.Client.Photon.Lite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Implements Photon LoadBalancing used in PUN.
/// This class is used internally by PhotonNetwork and not intended as public API.
/// </summary>
internal class NetworkingPeer : LoadbalancingPeer, IPhotonPeerListener
{
    // game properties must be cached, because the game is created on the master and then "re-created" on the game server
    // both must use the same props for the game
    public string mAppVersion;

    private string mAppId;

    /// <summary>
    /// A user's authentication values used during connect for Custom Authentication with Photon (and a custom service/community). 
    /// Set these before calling Connect if you want custom authentication.
    /// </summary>
    public AuthenticationValues AuthValues { get; set; }

    public string MasterServerAddress { get; protected internal set; }

    private string playername = "";

    private IPhotonPeerListener externalListener;

    private JoinType mLastJoinType;

    private bool mPlayernameHasToBeUpdated;

    public string PlayerName
    {
        get
        {
            return this.playername;
        }

        set
        {
            if (string.IsNullOrEmpty(value) || value.Equals(this.playername))
            {
                return;
            }

            if (this.mLocalActor != null)
            {
                this.mLocalActor.name = value;
            }

            this.playername = value;
            if (this.mCurrentGame != null)
            {
                // Only when in a room
                this.SendPlayerName();
            }
        }
    }

    public PeerState State { get; internal set; }

    // "public" access to the current game - is null unless a room is joined on a gameserver
    public Room mCurrentGame
    {
        get
        {
            if (this.mRoomToGetInto != null && this.mRoomToGetInto.isLocalClientInside)
            {
                return this.mRoomToGetInto;
            }

            return null;
        }
    }

    /// <summary>
    /// keeps the custom properties, gameServer address and anything else about the room we want to get into
    /// </summary>
    internal Room mRoomToGetInto { get; set; }

    public Dictionary<int, PhotonPlayer> mActors = new Dictionary<int, PhotonPlayer>();

    public PhotonPlayer[] mOtherPlayerListCopy = new PhotonPlayer[0];
    public PhotonPlayer[] mPlayerListCopy = new PhotonPlayer[0];

    public PhotonPlayer mLocalActor { get; internal set; }

    public PhotonPlayer mMasterClient = null;

	public bool hasSwitchedMC = false;

    public string mGameserver { get; internal set; }

    public bool requestSecurity = true;

    private Dictionary<Type, List<MethodInfo>> monoRPCMethodsCache = new Dictionary<Type, List<MethodInfo>>();

    public static bool UsePrefabCache = true;

    public static Dictionary<string, GameObject> PrefabCache = new Dictionary<string, GameObject>();

    public Dictionary<string, RoomInfo> mGameList = new Dictionary<string, RoomInfo>();
    public RoomInfo[] mGameListCopy = new RoomInfo[0];

    public int mQueuePosition { get; internal set; }

    public bool insideLobby = false;

    /// <summary>Stat value: Count of players on Master (looking for rooms)</summary>
    public int mPlayersOnMasterCount { get; internal set; }

    /// <summary>Stat value: Count of Rooms</summary>
    public int mGameCount { get; internal set; }

    /// <summary>Stat value: Count of Players in rooms</summary>
    public int mPlayersInRoomsCount { get; internal set; }

    /// <summary>
    /// Instantiated objects by their instantiationId. The id (key) is the instantiationId (created per actor).
    /// </summary>
    public Dictionary<int, GameObject> instantiatedObjects = new Dictionary<int, GameObject>();

    private HashSet<int> allowedReceivingGroups = new HashSet<int>();

    private HashSet<int> blockSendingGroups = new HashSet<int>();

    internal protected Dictionary<int, PhotonView> photonViewList = new Dictionary<int, PhotonView>(); //TODO: make private again

    internal protected short currentLevelPrefix = 0;

    private readonly Dictionary<string, int> rpcShortcuts;  // lookup "table" for the index (shortcut) of an RPC name

    public NetworkingPeer(IPhotonPeerListener listener, string playername, ConnectionProtocol connectionProtocol) : base(listener, connectionProtocol)
    {
        this.Listener = this;

        // don't set the field directly! the listener is passed on to other classes, which get updated by the property set method
        this.externalListener = listener;
        this.PlayerName = playername;
        this.mLocalActor = new PhotonPlayer(true, -1, this.playername);
        this.AddNewPlayer(this.mLocalActor.ID, this.mLocalActor);

        // RPC shortcut lookup creation (from list of RPCs, which is updated by Editor scripts)
        rpcShortcuts = new Dictionary<string, int>(PhotonNetwork.PhotonServerSettings.RpcList.Count);
	    for (int index = 0; index < PhotonNetwork.PhotonServerSettings.RpcList.Count; index++)
	    {
	        var name = PhotonNetwork.PhotonServerSettings.RpcList[index];
	        rpcShortcuts[name] = index;
	    }

        this.State = global::PeerState.PeerCreated;
    }

    #region Operations and Connection Methods

    public override bool Connect(string serverAddress, string appID)
    {
        if (PhotonNetwork.connectionStateDetailed == global::PeerState.Disconnecting)
        {
            Debug.LogError("ERROR: Cannot connect to Photon while Disconnecting. Connection failed.");
            return false;
        }

        if (string.IsNullOrEmpty(this.MasterServerAddress))
        {
            this.MasterServerAddress = serverAddress;
        }

        this.mAppId = appID.Trim();

        // connect might fail, if the DNS name can't be resolved or if no network connection is available
        bool connecting = base.Connect(serverAddress, "");
        this.State = connecting ? global::PeerState.Connecting : global::PeerState.Disconnected;

        return connecting;
    }

    /// <summary>
    /// Complete disconnect from photon (and the open master OR game server)
    /// </summary>
    public override void Disconnect()
    {
        if (this.PeerState == PeerStateValue.Disconnected)
        {
            if (this.DebugOut >= DebugLevel.WARNING)
            {
                this.DebugReturn(DebugLevel.WARNING, string.Format("Can't execute Disconnect() while not connected. Nothing changed. State: {0}", this.State));
            }

            return;
        }

        this.State = global::PeerState.Disconnecting;
        base.Disconnect();

        this.LeftRoomCleanup();
        this.LeftLobbyCleanup();
    }

    // just switches servers(Master->Game). don't remove the room, actors, etc
    private void DisconnectFromMaster()
    {
        this.State = global::PeerState.DisconnectingFromMasterserver;
        base.Disconnect();
        LeftLobbyCleanup();
    }

    // switches back from gameserver to master and removes the room, actors, etc
    private void DisconnectFromGameServer()
    {
        this.State = global::PeerState.DisconnectingFromGameserver;
        base.Disconnect();
        this.LeftRoomCleanup();
    }

    /// <summary>
    /// Called at disconnect/leavelobby etc. This CAN also be called when we are not in a lobby (e.g. disconnect from room)
    /// </summary>
    private void LeftLobbyCleanup()
    {
        if (!insideLobby)
        {
            return;
        }

        SendMonoMessage(PhotonNetworkingMessage.OnLeftLobby);
        this.insideLobby = false;
        this.isFetchingFriends = false;
    }

    /// <summary>
    /// Called when "this client" left a room to clean up.
    /// </summary>
    private void LeftRoomCleanup()
    {
        bool wasInRoom = mRoomToGetInto != null;
        // when leaving a room, we clean up depending on that room's settings.
        bool autoCleanupSettingOfRoom = (this.mRoomToGetInto != null) ? this.mRoomToGetInto.autoCleanUp : PhotonNetwork.autoCleanUpPlayerObjects;

        this.hasSwitchedMC = false;
		this.mRoomToGetInto = null;
        this.mActors = new Dictionary<int, PhotonPlayer>();
        mPlayerListCopy = new PhotonPlayer[0];
        mOtherPlayerListCopy = new PhotonPlayer[0];
        this.mMasterClient = null;
        this.allowedReceivingGroups = new HashSet<int>();
        this.blockSendingGroups = new HashSet<int>();
        this.mGameList = new Dictionary<string, RoomInfo>();
        mGameListCopy = new RoomInfo[0];
        this.isFetchingFriends = false;

        this.ChangeLocalID(-1);

        // Cleanup all network objects (all spawned PhotonViews, local and remote)
        if (autoCleanupSettingOfRoom)
        {
            this.LocalCleanupAnythingInstantiated(true);
            PhotonNetwork.manuallyAllocatedViewIds = new List<int>();       // filled and easier to replace completely
        }

        if (wasInRoom)
        {
            SendMonoMessage(PhotonNetworkingMessage.OnLeftRoom);
        }
    }

    protected internal void LocalCleanupAnythingInstantiated(bool destroyInstantiatedGameObjects)
    {
        if (tempInstantiationData.Count > 0)
        {
            Debug.LogWarning("It seems some instantiation is not completed, as instantiation data is used. You should make sure instantiations are paused when calling this method. Cleaning now, despite this.");
        }

        // Destroy GO's (if we should)
        if (destroyInstantiatedGameObjects)
        {
            // Fill list with Instantiated objects
            HashSet<GameObject> goList = new HashSet<GameObject>(this.instantiatedObjects.Values);
            foreach (GameObject go in goList)
            {
                this.RemoveInstantiatedGO(go, true);
            }
        }

        // photonViewList is cleared of anything instantiated (so scene items are left inside)
        // any other lists can be 
        this.tempInstantiationData.Clear(); // should be empty but to be safe we clear (no new list needed)
        this.instantiatedObjects = new Dictionary<int, GameObject>();   // filled and easier to replace completely
        PhotonNetwork.lastUsedViewSubId = 0;
        PhotonNetwork.lastUsedViewSubIdStatic = 0;
    }

    // gameID can be null (optional). The server assigns a unique name if no name is set

    // joins a room and sets your current username as custom actorproperty (will broadcast that)

    #endregion

    #region Helpers

    private void ReadoutProperties(Hashtable gameProperties, Hashtable pActorProperties, int targetActorNr)
    {
        // Debug.LogWarning("ReadoutProperties gameProperties: " + gameProperties.ToStringFull() + " pActorProperties: " + pActorProperties.ToStringFull() + " targetActorNr: " + targetActorNr);
        // read game properties and cache them locally
        if (this.mCurrentGame != null && gameProperties != null)
        {
            this.mCurrentGame.CacheProperties(gameProperties);
			SendMonoMessage(PhotonNetworkingMessage.OnPhotonCustomRoomPropertiesChanged);
            if (PhotonNetwork.automaticallySyncScene)
            {
                this.AutomaticallySyncScene();   // will load new scene if sceneName was changed
            }
        }

        if (pActorProperties != null && pActorProperties.Count > 0)
        {
            if (targetActorNr > 0)
            {
                // we have a single entry in the pActorProperties with one
                // user's name
                // targets MUST exist before you set properties
                PhotonPlayer target = this.GetPlayerWithID(targetActorNr);
                if (target != null)
                {
                    target.InternalCacheProperties(this.GetActorPropertiesForActorNr(pActorProperties, targetActorNr));
					SendMonoMessage(PhotonNetworkingMessage.OnPhotonPlayerPropertiesChanged, target);
                }
            }
            else
            {
                // in this case, we've got a key-value pair per actor (each
                // value is a hashtable with the actor's properties then)
                int actorNr;
                Hashtable props;
                string newName;
                PhotonPlayer target;

                foreach (object key in pActorProperties.Keys)
                {
                    actorNr = (int)key;
                    props = (Hashtable)pActorProperties[key];
                    newName = (string)props[ActorProperties.PlayerName];

                    target = this.GetPlayerWithID(actorNr);
                    if (target == null)
                    {
                        target = new PhotonPlayer(false, actorNr, newName);
                        this.AddNewPlayer(actorNr, target);
                    }

                    target.InternalCacheProperties(props);
					SendMonoMessage(PhotonNetworkingMessage.OnPhotonPlayerPropertiesChanged, target);
                }
            }
        }
    }

    private void AddNewPlayer(int ID, PhotonPlayer player)
    {
        if (!this.mActors.ContainsKey(ID))
        {
            this.mActors[ID] = player;
            RebuildPlayerListCopies();
        }
        else
        {
            Debug.LogError("Adding player twice: " + ID);
        }
    }

    void RemovePlayer(int ID, PhotonPlayer player)
    {
        this.mActors.Remove(ID);
        if (!player.isLocal)
        {
            RebuildPlayerListCopies();
        }
    }

    void RebuildPlayerListCopies()
    {
        this.mPlayerListCopy = new PhotonPlayer[this.mActors.Count];
        this.mActors.Values.CopyTo(this.mPlayerListCopy, 0);

        List<PhotonPlayer> otherP = new List<PhotonPlayer>();
        foreach (PhotonPlayer player in this.mPlayerListCopy)
        {
            if (!player.isLocal)
            {
                otherP.Add(player);
            }
        }

        this.mOtherPlayerListCopy = otherP.ToArray();
    }

    /// <summary>
    /// Resets the PhotonView "lastOnSerializeDataSent" so that "OnReliable" synched PhotonViews send a complete state to new clients (if the state doesnt change, no messages would be send otherwise!).
    /// Note that due to this reset, ALL other players will receive the full OnSerialize.
    /// </summary>
    private void ResetPhotonViewsOnSerialize()
    {
        foreach (PhotonView photonView in this.photonViewList.Values)
        {
            photonView.lastOnSerializeDataSent = null;
        }
    }

    /// <summary>
    /// Called when the event Leave (of some other player) arrived.
    /// Cleans game objects, views locally. The master will also clean the 
    /// </summary>
    /// <param name="actorID">ID of player who left.</param>
    private void HandleEventLeave(int actorID)
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.DebugReturn(DebugLevel.INFO, "HandleEventLeave actorNr: " + actorID);
        }

        // actorNr is fetched out of event above
        if (actorID < 0 || !this.mActors.ContainsKey(actorID))
        {
            if (this.DebugOut >= DebugLevel.ERROR)
            {
                this.DebugReturn(DebugLevel.ERROR, String.Format("Received event Leave for unknown actorNumber: {0}", actorID));
            }
            return;
        }

        PhotonPlayer player = this.GetPlayerWithID(actorID);
        if (player == null)
        {
            Debug.LogError("Error: HandleEventLeave for actorID=" + actorID + " has no PhotonPlayer!");
        }

        // having a new master before calling destroy for the leaving player is important!
        // so we elect a new masterclient and ignore the leaving player (who is still in playerlists).
        this.CheckMasterClient(actorID);


        // destroy objects & buffered messages
        if (this.mCurrentGame != null && this.mCurrentGame.autoCleanUp)
        {
            this.DestroyPlayerObjects(actorID, true);
        }

        RemovePlayer(actorID, player);

        // finally, send notification (the playerList and masterclient are now updated)
        SendMonoMessage(PhotonNetworkingMessage.OnPhotonPlayerDisconnected, player);
    }

    /// <summary>Picks the new master client from player list, if the current Master is leaving (leavingPlayerId) or if no master was assigned so far.</summary>
    /// <param name="leavingPlayerId">
    /// The ignored player is the one who's leaving and should not become master (again). Pass -1 to select any player from the list.
    /// </param>
    private void CheckMasterClient(int leavingPlayerId)
    {
        bool currentMasterIsLeaving = this.mMasterClient != null && this.mMasterClient.ID == leavingPlayerId;
        bool someoneIsLeaving = leavingPlayerId > 0;

        // return early if SOME player (leavingId > 0) is leaving AND it's NOT the current master
        if (someoneIsLeaving && !currentMasterIsLeaving)
        {
            return;
        }

        // picking the player with lowest ID (longest in game).
        if (this.mActors.Count <= 1)
        {
            this.mMasterClient = this.mLocalActor;
        }
        else
        {
            // keys in mActors are their actorNumbers
            int lowestActorNumber = Int32.MaxValue;
            foreach (int key in this.mActors.Keys)
            {
                if (key < lowestActorNumber && key != leavingPlayerId)
                {
                    lowestActorNumber = key;
                }
            }

            this.mMasterClient = this.mActors[lowestActorNumber];
        }

        // make a callback ONLY when a player/Master left
        if (someoneIsLeaving)
        {
            SendMonoMessage(PhotonNetworkingMessage.OnMasterClientSwitched, this.mMasterClient);
        }
    }

    /// <summary>
    /// Returns the lowest player.ID - used for Master Client picking.
    /// </summary>
    /// <remarks></remarks>
    private static int ReturnLowestPlayerId(PhotonPlayer[] players, int playerIdToIgnore)
    {
        if (players == null || players.Length == 0)
        {
            return -1;
        }

        int lowestActorNumber = Int32.MaxValue;
        for (int i = 0; i < players.Length; i++)
        {
            PhotonPlayer photonPlayer = players[i];
            if (photonPlayer.ID == playerIdToIgnore)
            {
                continue;
            }

            if (photonPlayer.ID < lowestActorNumber)
            {
                lowestActorNumber = photonPlayer.ID;
            }
        }

        return lowestActorNumber;
    }

    internal protected bool SetMasterClient(int playerId, bool sync)
    {
        bool masterReplaced = this.mMasterClient != null && this.mMasterClient.ID != playerId;
        if (!masterReplaced || !this.mActors.ContainsKey(playerId))
        {
            return false;
        }

        if (sync)
        {
            bool sent = this.OpRaiseEvent(PunEvent.AssignMaster, new Hashtable() { { (byte)1, playerId } }, true, 0);
            if (!sent)
            {
                return false;
            }
        }

		this.hasSwitchedMC = true;
        this.mMasterClient = this.mActors[playerId];
        SendMonoMessage(PhotonNetworkingMessage.OnMasterClientSwitched, this.mMasterClient);    // we only callback when an actual change is done
        return true;
    }

    private Hashtable GetActorPropertiesForActorNr(Hashtable actorProperties, int actorNr)
    {
        if (actorProperties.ContainsKey(actorNr))
        {
            return (Hashtable)actorProperties[actorNr];
        }

        return actorProperties;
    }

    private PhotonPlayer GetPlayerWithID(int number)
    {
        if (this.mActors != null && this.mActors.ContainsKey(number))
        {
            return this.mActors[number];
        }

        return null;
    }

    private void SendPlayerName()
    {
        if (this.State == global::PeerState.Joining)
        {
            // this means, the join on the gameServer is sent (with an outdated name). send the new when in game
            this.mPlayernameHasToBeUpdated = true;
            return;
        }

        if (this.mLocalActor != null)
        {
            this.mLocalActor.name = this.PlayerName;
            Hashtable properties = new Hashtable();
            properties[ActorProperties.PlayerName] = this.PlayerName;
            if (this.mLocalActor.ID > 0)
            {
                this.OpSetPropertiesOfActor(this.mLocalActor.ID, properties, true, (byte)0);
                this.mPlayernameHasToBeUpdated = false;
            }
        }
    }

    private void GameEnteredOnGameServer(OperationResponse operationResponse)
    {
        if (operationResponse.ReturnCode != 0)
        {
            switch (operationResponse.OperationCode)
            {
                case OperationCode.CreateGame:
                    this.DebugReturn(DebugLevel.ERROR, "Create failed on GameServer. Changing back to MasterServer. Msg: " + operationResponse.DebugMessage);
                    SendMonoMessage(PhotonNetworkingMessage.OnPhotonCreateRoomFailed);
                    break;
                case OperationCode.JoinGame:
                    this.DebugReturn(DebugLevel.WARNING, "Join failed on GameServer. Changing back to MasterServer. Msg: " + operationResponse.DebugMessage);
                    if (operationResponse.ReturnCode == ErrorCode.GameDoesNotExist)
                    {
                        Debug.Log("Most likely the game became empty during the switch to GameServer.");
                    }
                    SendMonoMessage(PhotonNetworkingMessage.OnPhotonJoinRoomFailed);
                    break;
                case OperationCode.JoinRandomGame:
                    this.DebugReturn(DebugLevel.WARNING, "Join failed on GameServer. Changing back to MasterServer. Msg: " + operationResponse.DebugMessage);
                    if (operationResponse.ReturnCode == ErrorCode.GameDoesNotExist)
                    {
                        Debug.Log("Most likely the game became empty during the switch to GameServer.");
                    }
                    SendMonoMessage(PhotonNetworkingMessage.OnPhotonRandomJoinFailed);
                    break;
            }

            this.DisconnectFromGameServer();
            return;
        }

        this.State = global::PeerState.Joined;
        this.mRoomToGetInto.isLocalClientInside = true;

        Hashtable actorProperties = (Hashtable)operationResponse[ParameterCode.PlayerProperties];
        Hashtable gameProperties = (Hashtable)operationResponse[ParameterCode.GameProperties];
        this.ReadoutProperties(gameProperties, actorProperties, 0);

        // the local player's actor-properties are not returned in join-result. add this player to the list
        int localActorNr = (int)operationResponse[ParameterCode.ActorNr];

        this.ChangeLocalID(localActorNr);
        this.CheckMasterClient(-1);

        if (this.mPlayernameHasToBeUpdated)
        {
            this.SendPlayerName();
        }

        switch (operationResponse.OperationCode)
        {
            case OperationCode.CreateGame:
                SendMonoMessage(PhotonNetworkingMessage.OnCreatedRoom);
                break;
            case OperationCode.JoinGame:
            case OperationCode.JoinRandomGame:
                // the mono message for this is sent at another place
                break;
        }
    }

    private Hashtable GetLocalActorProperties()
    {
        if (PhotonNetwork.player != null)
        {
            return PhotonNetwork.player.allProperties;
        }

        Hashtable actorProperties = new Hashtable();
        actorProperties[ActorProperties.PlayerName] = this.PlayerName;
        return actorProperties;
    }

    public void ChangeLocalID(int newID)
    {
        if (this.mLocalActor == null)
        {
            Debug.LogWarning(
                string.Format(
                    "Local actor is null or not in mActors! mLocalActor: {0} mActors==null: {1} newID: {2}",
                    this.mLocalActor,
                    this.mActors == null,
                    newID));
        }

        if (this.mActors.ContainsKey(this.mLocalActor.ID))
        {
            this.mActors.Remove(this.mLocalActor.ID);
        }

        this.mLocalActor.InternalChangeLocalID(newID);
        this.mActors[this.mLocalActor.ID] = this.mLocalActor;
        this.RebuildPlayerListCopies();
    }

    #endregion

    #region Operations

    public bool OpCreateGame(string gameID, bool isVisible, bool isOpen, byte maxPlayers, bool autoCleanUp, Hashtable customGameProperties, string[] propsListedInLobby)
    {
        this.mRoomToGetInto = new Room(gameID, customGameProperties, isVisible, isOpen, maxPlayers, autoCleanUp, propsListedInLobby);
        bool onGameServer = this.State == global::PeerState.Joining;    // set before this method is called by OpResponse for Auth
        return base.OpCreateRoom(gameID, isVisible, isOpen, maxPlayers, autoCleanUp, customGameProperties, (onGameServer) ? this.GetLocalActorProperties() : null, propsListedInLobby);
    }

    public bool OpJoin(string gameID)
    {
        this.mRoomToGetInto = new Room(gameID, null);
        bool onGameServer = this.State == global::PeerState.Joining;    // set before this method is called by OpResponse for Auth
        return this.OpJoinRoom(gameID, (onGameServer) ? this.GetLocalActorProperties() : null);
    }

    // this override just makes sure we have a mRoomToGetInto, even if it's blank (the properties provided in this method are filters. they are not set when we join the game)
    public override bool OpJoinRandomRoom(Hashtable expectedCustomRoomProperties, byte expectedMaxPlayers, Hashtable playerProperties, MatchmakingMode matchingType)
    {
        this.mRoomToGetInto = new Room(null, null);
        return base.OpJoinRandomRoom(expectedCustomRoomProperties, expectedMaxPlayers, playerProperties, matchingType);
    }

    /// <summary>
    /// Operation Leave will exit any current room.
    /// </summary>
    /// <remarks>
    /// This also happens when you disconnect from the server.
    /// Disconnect might be a step less if you don't want to create a new room on the same server.
    /// </remarks>
    /// <returns></returns>
    public virtual bool OpLeave()
    {
        if (this.State != global::PeerState.Joined)
        {
            this.DebugReturn(DebugLevel.ERROR, "NetworkingPeer::leaveGame() - ERROR: no game is currently joined");
            return false;
        }

        return this.OpCustom((byte)OperationCode.Leave, null, true, 0);
    }

    public override bool OpRaiseEvent(byte eventCode, bool sendReliable, object customEventContent)
    {
        if (PhotonNetwork.offlineMode)
        {
            return false;
        }

        return base.OpRaiseEvent(eventCode, sendReliable, customEventContent);
    }

    public override bool OpRaiseEvent(byte eventCode, bool sendReliable, object customEventContent, byte channelId, EventCaching cache, int[] targetActors, ReceiverGroup receivers, byte interestGroup)
    {
        if (PhotonNetwork.offlineMode)
        {
            return false;
        }

        return base.OpRaiseEvent(eventCode, sendReliable, customEventContent, channelId, cache, targetActors, receivers, interestGroup);
    }

    public override bool OpRaiseEvent(byte eventCode, byte interestGroup, Hashtable evData, bool sendReliable, byte channelId)
    {
        if (PhotonNetwork.offlineMode)
        {
            return false;
        }

        return base.OpRaiseEvent(eventCode, interestGroup, evData, sendReliable, channelId);
    }


    public override bool OpRaiseEvent(byte eventCode, Hashtable evData, bool sendReliable, byte channelId, int[] targetActors, EventCaching cache)
    {
        if (PhotonNetwork.offlineMode)
        {
            return false;
        }

        return base.OpRaiseEvent(eventCode, evData, sendReliable, channelId, targetActors, cache);
    }

    public override bool OpRaiseEvent(byte eventCode, Hashtable evData, bool sendReliable, byte channelId, EventCaching cache, ReceiverGroup receivers)
    {
        if (PhotonNetwork.offlineMode)
        {
            return false;
        }

        return base.OpRaiseEvent(eventCode, evData, sendReliable, channelId, cache, receivers);
    }

    #endregion

    #region Implementation of IPhotonPeerListener

    public void DebugReturn(DebugLevel level, string message)
    {
        this.externalListener.DebugReturn(level, message);
    }

    public void OnOperationResponse(OperationResponse operationResponse)
    {
        if (PhotonNetwork.networkingPeer.State == global::PeerState.Disconnecting)
        {
            if (this.DebugOut >= DebugLevel.INFO)
            {
                this.DebugReturn(DebugLevel.INFO, "OperationResponse ignored while disconnecting: " + operationResponse.OperationCode);
            }

            return;
        }

        // extra logging for error debugging (helping developers with a bit of automated analysis)
        if (operationResponse.ReturnCode == 0)
        {
            if (this.DebugOut >= DebugLevel.INFO)
            {
                this.DebugReturn(DebugLevel.INFO, operationResponse.ToString());
            }
        }
        else
        {
            if (this.DebugOut >= DebugLevel.WARNING)
            {
                if (operationResponse.ReturnCode == ErrorCode.OperationNotAllowedInCurrentState)
                {
                    this.DebugReturn(DebugLevel.WARNING, "Operation could not be executed yet. Wait for state JoinedLobby or ConnectedToMaster and their respective callbacks before calling OPs. Client must be authorized.");
                }

                this.DebugReturn(DebugLevel.WARNING, operationResponse.ToStringFull());
            }
        }

        switch (operationResponse.OperationCode)
        {
            case OperationCode.Authenticate:
                {
                    // PeerState oldState = this.State;

                    if (operationResponse.ReturnCode != 0)
                    {
                        
                        if (operationResponse.ReturnCode == ErrorCode.InvalidOperationCode)
                        {
                            this.DebugReturn(DebugLevel.ERROR, string.Format("If you host Photon yourself, make sure to start the 'Instance LoadBalancing'"));
                        }
                        else if (operationResponse.ReturnCode == ErrorCode.InvalidAuthentication)
                        {
                            this.DebugReturn(DebugLevel.ERROR, string.Format("The appId this client sent is unknown on the server (Cloud). Check settings. If using the Cloud, check account."));
                        }
                        else if (operationResponse.ReturnCode == ErrorCode.CustomAuthenticationFailed)
                        {
                            this.DebugReturn(DebugLevel.ERROR, string.Format("Custom Authentication failed (either due to user-input or configuration or AuthParameter string format). Calling: OnCustomAuthenticationFailed()"));
                            SendMonoMessage(PhotonNetworkingMessage.OnCustomAuthenticationFailed, operationResponse.DebugMessage);
                        }
                        else if (this.DebugOut >= DebugLevel.ERROR)
                        {
                            this.DebugReturn(DebugLevel.ERROR, string.Format("Authentication failed: '{0}' Code: {1}", operationResponse.DebugMessage, operationResponse.ReturnCode));
                        }

                        this.Disconnect();
                        this.State = global::PeerState.Disconnecting;

                        if (operationResponse.ReturnCode == ErrorCode.MaxCcuReached)
                        {
                            this.DebugReturn(DebugLevel.ERROR, string.Format("Currently, the limit of users is reached for this title. Try again later. Disconnecting"));
                            SendMonoMessage(PhotonNetworkingMessage.OnPhotonMaxCccuReached);
                            SendMonoMessage(PhotonNetworkingMessage.OnConnectionFail, DisconnectCause.MaxCcuReached);
                        }
                        else if (operationResponse.ReturnCode == ErrorCode.InvalidRegion)
                        {
                            this.DebugReturn(DebugLevel.ERROR, string.Format("The used master server address is not available with the subscription currently used. Got to Photon Cloud Dashboard or change URL. Disconnecting"));
                            SendMonoMessage(PhotonNetworkingMessage.OnConnectionFail, DisconnectCause.InvalidRegion);
                        }
                        break;
                    }
                    else
                    {
                        if (this.State == global::PeerState.Connected || this.State == global::PeerState.ConnectedComingFromGameserver)
                        {
                            if (operationResponse.Parameters.ContainsKey(ParameterCode.Secret))
                            {
                                if (this.AuthValues != null)
                                {
                                    this.AuthValues.Secret = operationResponse[ParameterCode.Secret] as string;
                                }
                                else
                                {
                                    if (this.DebugOut >= DebugLevel.WARNING)
                                    {
                                        this.DebugReturn(DebugLevel.WARNING, "Server returned secret but AuthValues are null. Won't use this.");
                                    }
                                }
                            }

                            if (PhotonNetwork.autoJoinLobby)
                            {
                                this.OpJoinLobby();
                                this.State = global::PeerState.Authenticated;
                            }
                            else
                            {
                                this.State = global::PeerState.ConnectedToMaster;
                                NetworkingPeer.SendMonoMessage(PhotonNetworkingMessage.OnConnectedToMaster);
                            }
                        }
                        else if (this.State == global::PeerState.ConnectedToGameserver)
                        {
                            this.State = global::PeerState.Joining;

                            if (this.AuthValues != null)
                            {
                                this.AuthValues.Secret = null;  // currently, we discard the secret after it was used on the game server
                            }

                            if (this.mLastJoinType == JoinType.JoinGame || this.mLastJoinType == JoinType.JoinRandomGame)
                            {
                                // if we just "join" the game, do so
                                this.OpJoin(this.mRoomToGetInto.name);
                            }
                            else if (this.mLastJoinType == JoinType.CreateGame)
                            {
                                // on the game server, we have to apply the room properties that were chosen for creation of the room, so we use this.mRoomToGetInto
                                this.OpCreateGame(
                                    this.mRoomToGetInto.name,
                                    this.mRoomToGetInto.visible,
                                    this.mRoomToGetInto.open,
                                    (byte)this.mRoomToGetInto.maxPlayers,
                                    this.mRoomToGetInto.autoCleanUp,
                                    this.mRoomToGetInto.customProperties,
                                    this.mRoomToGetInto.propertiesListedInLobby);
                            }

                            break;
                        }
                    }
                    break;
                }

            case OperationCode.CreateGame:
                {
                    if (this.State != global::PeerState.Joining)
                    {
                        if (operationResponse.ReturnCode != 0)
                        {
                            if (this.DebugOut >= DebugLevel.ERROR)
                            {
                                this.DebugReturn(DebugLevel.ERROR, string.Format("createGame failed, client stays on masterserver: {0}.", operationResponse.ToStringFull()));
                            }

                            SendMonoMessage(PhotonNetworkingMessage.OnPhotonCreateRoomFailed);
                            break;
                        }

                        string gameID = (string)operationResponse[ParameterCode.RoomName];
                        if (!string.IsNullOrEmpty(gameID))
                        {
                            // is only sent by the server's response, if it has not been
                            // sent with the client's request before!
                            this.mRoomToGetInto.name = gameID;
                        }

                        this.mGameserver = (string)operationResponse[ParameterCode.Address];
                        this.DisconnectFromMaster();
                        this.mLastJoinType = JoinType.CreateGame;
                    }
                    else
                    {
                        this.GameEnteredOnGameServer(operationResponse);
                    }

                    break;
                }

            case OperationCode.JoinGame:
                {
                    if (this.State != global::PeerState.Joining)
                    {
                        if (operationResponse.ReturnCode != 0)
                        {
                            SendMonoMessage(PhotonNetworkingMessage.OnPhotonJoinRoomFailed);

                            if (this.DebugOut >= DebugLevel.WARNING)
                            {
                                this.DebugReturn(DebugLevel.WARNING, string.Format("JoinRoom failed (room maybe closed by now). Client stays on masterserver: {0}. State: {1}", operationResponse.ToStringFull(), this.State));
                            }

                            // this.mListener.joinGameReturn(0, null, null, returnCode, debugMsg);
                            break;
                        }

                        this.mGameserver = (string)operationResponse[ParameterCode.Address];
                        this.DisconnectFromMaster();
                        this.mLastJoinType = JoinType.JoinGame;
                    }
                    else
                    {
                        this.GameEnteredOnGameServer(operationResponse);
                    }

                    break;
                }

            case OperationCode.JoinRandomGame:
                {
                    // happens only on master. on gameserver, this is a regular join (we don't need to find a random game again)
                    // the operation OpJoinRandom either fails (with returncode 8) or returns game-to-join information
                    if (operationResponse.ReturnCode != 0)
                    {
                        if (operationResponse.ReturnCode == ErrorCode.NoRandomMatchFound)
                        {
                            this.DebugReturn(DebugLevel.WARNING, "JoinRandom failed: No open game. Client stays in lobby.");
                        }
                        else if (this.DebugOut >= DebugLevel.ERROR)
                        {
                            this.DebugReturn(DebugLevel.ERROR, string.Format("JoinRandom failed: {0}.", operationResponse.ToStringFull()));
                        }
                        SendMonoMessage(PhotonNetworkingMessage.OnPhotonRandomJoinFailed);

                        // this.mListener.createGameReturn(0, null, null, returnCode, debugMsg);
                        break;
                    }

                    string gameID = (string)operationResponse[ParameterCode.RoomName];
                    this.mRoomToGetInto.name = gameID;
                    this.mGameserver = (string)operationResponse[ParameterCode.Address];
                    this.DisconnectFromMaster();
                    this.mLastJoinType = JoinType.JoinRandomGame;
                    break;
                }

            case OperationCode.JoinLobby:
                this.State = global::PeerState.JoinedLobby;
                this.insideLobby = true;
                SendMonoMessage(PhotonNetworkingMessage.OnJoinedLobby);

                // this.mListener.joinLobbyReturn();
                break;
            case OperationCode.LeaveLobby:
                this.State = global::PeerState.Authenticated;
                this.LeftLobbyCleanup();
                break;

            case OperationCode.Leave:
                this.DisconnectFromGameServer();
                break;

            case OperationCode.SetProperties:
                // this.mListener.setPropertiesReturn(returnCode, debugMsg);
                break;

            case OperationCode.GetProperties:
                {
                    Hashtable actorProperties = (Hashtable)operationResponse[ParameterCode.PlayerProperties];
                    Hashtable gameProperties = (Hashtable)operationResponse[ParameterCode.GameProperties];
                    this.ReadoutProperties(gameProperties, actorProperties, 0);

                    // RemoveByteTypedPropertyKeys(actorProperties, false);
                    // RemoveByteTypedPropertyKeys(gameProperties, false);
                    // this.mListener.getPropertiesReturn(gameProperties, actorProperties, returnCode, debugMsg);
                    break;
                }

            case OperationCode.RaiseEvent:
                // this usually doesn't give us a result. only if the caching is affected the server will send one.
                break;

            case OperationCode.FindFriends:
                bool[] onlineList = operationResponse[ParameterCode.FindFriendsResponseOnlineList] as bool[];
                string[] roomList = operationResponse[ParameterCode.FindFriendsResponseRoomIdList] as string[];

                if (onlineList != null && roomList != null && PhotonNetwork.Friends != null && onlineList.Length == PhotonNetwork.Friends.Count)
                {
                    for (int index = 0; index < PhotonNetwork.Friends.Count; index++)
                    {
                        FriendInfo friend = PhotonNetwork.Friends[index];
                        friend.Room = roomList[index];
                        friend.IsOnline = onlineList[index];
                    }
                }
                else
                {
                    // any of the lists is null and shouldn't. print a error
                    this.DebugReturn(DebugLevel.ERROR, "FindFriends failed to apply the result, as a required value wasn't provided or the friend list length differed from result.");
                }

                this.isFetchingFriends = false;
                this.friendListTimestamp = Environment.TickCount;
                if (this.friendListTimestamp == 0)
                {
                    this.friendListTimestamp = 1;   // makes sure the timestamp is not accidentally 0
                }

                SendMonoMessage(PhotonNetworkingMessage.OnUpdatedFriendList);
                break;

            default:
                if (this.DebugOut >= DebugLevel.ERROR)
                {
                    this.DebugReturn(DebugLevel.ERROR, string.Format("operationResponse unhandled: {0}", operationResponse.ToString()));
                }
                break;
        }

        this.externalListener.OnOperationResponse(operationResponse);
    }

    /// <summary>
    /// Age of friend list info (in milliseconds). It's 0 until a friend list is fetched.
    /// </summary>
    protected internal int FriendsListAge { get { return (this.isFetchingFriends || this.friendListTimestamp == 0) ? 0 : Environment.TickCount - this.friendListTimestamp; } }

    private int friendListTimestamp;

    /// <summary>Internal flag to know if the client currently fetches a friend list.</summary>
    private bool isFetchingFriends;

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
    public override bool OpFindFriends(string[] friendsToFind)
    {
        if (this.isFetchingFriends)
        {
            return false;   // fetching friends currently, so don't do it again (avoid changing the list while fetching friends)
        }

        this.isFetchingFriends = true;

        PhotonNetwork.Friends = new List<FriendInfo>(friendsToFind.Length);
        foreach (string name in friendsToFind)
        {
            PhotonNetwork.Friends.Add(new FriendInfo() { Name = name });
        }

        return base.OpFindFriends(friendsToFind);
    }

    public void OnStatusChanged(StatusCode statusCode)
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.DebugReturn(DebugLevel.INFO, string.Format("OnStatusChanged: {0}", statusCode.ToString()));
        }

        switch (statusCode)
        {
            case StatusCode.Connect:
                if (this.State == global::PeerState.ConnectingToGameserver)
                {
                    if (this.DebugOut >= DebugLevel.ALL)
                    {
                        this.DebugReturn(DebugLevel.ALL, "Connected to gameserver.");
                    }
                    this.State = global::PeerState.ConnectedToGameserver;
                }
                else
                {
                    if (this.DebugOut >= DebugLevel.ALL)
                    {
                        this.DebugReturn(DebugLevel.ALL, "Connected to masterserver.");
                    }
                    if (this.State == global::PeerState.Connecting)
                    {
                        SendMonoMessage(PhotonNetworkingMessage.OnConnectedToPhoton);
                        this.State = global::PeerState.Connected;
                    }
                    else
                    {
                        this.State = global::PeerState.ConnectedComingFromGameserver;
                    }
                }

                if (this.requestSecurity || this.AuthValues != null)
                {
                    this.EstablishEncryption();
                }
                else
                {
                    if (!this.OpAuthenticate(this.mAppId, this.mAppVersion, this.PlayerName, this.AuthValues))
                    {
                        this.externalListener.DebugReturn(DebugLevel.ERROR, "Error calling OpAuthenticate! Did not work. Check log output, AuthValues and if you're connected. State: " + this.State);
                    }
                }
                break;

            case StatusCode.Disconnect:
                if (this.State == global::PeerState.DisconnectingFromMasterserver)
                {
                    if (this.Connect(this.mGameserver, this.mAppId))
                    {
                        this.State = global::PeerState.ConnectingToGameserver;
                    }
                }
                else if (this.State == global::PeerState.DisconnectingFromGameserver)
                {
                    if (this.Connect(this.MasterServerAddress, this.mAppId))
                    {
                        this.State = global::PeerState.ConnectingToMasterserver;
                    }
                }
                else
                {
                    this.LeftRoomCleanup();
                    this.State = global::PeerState.PeerCreated;
                    SendMonoMessage(PhotonNetworkingMessage.OnDisconnectedFromPhoton);
                }
                break;

            case StatusCode.SecurityExceptionOnConnect:
            case StatusCode.ExceptionOnConnect:
                this.State = global::PeerState.PeerCreated;

                DisconnectCause cause = (DisconnectCause)statusCode;
                SendMonoMessage(PhotonNetworkingMessage.OnFailedToConnectToPhoton, cause);
                break;

            case StatusCode.Exception:
                if (this.State == global::PeerState.Connecting)
                {
                    this.DebugReturn(DebugLevel.WARNING, "Exception while connecting to: " + this.ServerAddress + ". Check if the server is available.");
                    if (this.ServerAddress == null || this.ServerAddress.StartsWith("127.0.0.1"))
                    {
                        this.DebugReturn(DebugLevel.WARNING, "The server address is 127.0.0.1 (localhost): Make sure the server is running on this machine. Android and iOS emulators have their own localhost.");
                        if (this.ServerAddress == this.mGameserver)
                        {
                            this.DebugReturn(DebugLevel.WARNING, "This might be a misconfiguration in the game server config. You need to edit it to a (public) address.");
                        }
                    }

                    this.State = global::PeerState.PeerCreated;
                    cause = (DisconnectCause)statusCode;
                    SendMonoMessage(PhotonNetworkingMessage.OnFailedToConnectToPhoton, cause);
                }
                else
                {
                    this.State = global::PeerState.PeerCreated;

                    cause = (DisconnectCause)statusCode;
                    SendMonoMessage(PhotonNetworkingMessage.OnConnectionFail, cause);
                }

                this.Disconnect();
                break;

            case StatusCode.TimeoutDisconnect:
            case StatusCode.InternalReceiveException:
            case StatusCode.DisconnectByServer:
            case StatusCode.DisconnectByServerLogic:
            case StatusCode.DisconnectByServerUserLimit:
                if (this.State == global::PeerState.Connecting)
                {
                    this.DebugReturn(DebugLevel.WARNING, statusCode + " while connecting to: " + this.ServerAddress + ". Check if the server is available.");

                    this.State = global::PeerState.PeerCreated;
                    cause = (DisconnectCause)statusCode;
                    SendMonoMessage(PhotonNetworkingMessage.OnFailedToConnectToPhoton, cause);
                }
                else
                {
                    this.State = global::PeerState.PeerCreated;

                    cause = (DisconnectCause)statusCode;
                    SendMonoMessage(PhotonNetworkingMessage.OnConnectionFail, cause);
                }

                this.Disconnect();
                break;

            case StatusCode.SendError:
                // this.mListener.clientErrorReturn(statusCode);
                break;

            case StatusCode.QueueOutgoingReliableWarning:
            case StatusCode.QueueOutgoingUnreliableWarning:
            case StatusCode.QueueOutgoingAcksWarning:
            case StatusCode.QueueSentWarning:

                // this.mListener.warningReturn(statusCode);
                break;

            case StatusCode.EncryptionEstablished:
                if (!this.OpAuthenticate(this.mAppId, this.mAppVersion, this.PlayerName, this.AuthValues))
                {
                    this.externalListener.DebugReturn(DebugLevel.ERROR, "Error calling OpAuthenticate! Did not work. Check log output, AuthValues and if you're connected. State: " + this.State);
                }
                break;
            case StatusCode.EncryptionFailedToEstablish:
                this.externalListener.DebugReturn(DebugLevel.ERROR, "Encryption wasn't established: " + statusCode + ". Going to authenticate anyways.");

                if (!this.OpAuthenticate(this.mAppId, this.mAppVersion, this.PlayerName, this.AuthValues))
                {
                    this.externalListener.DebugReturn(DebugLevel.ERROR, "Error calling OpAuthenticate! Did not work. Check log output, AuthValues and if you're connected. State: " + this.State);
                }
                break;

            // // TCP "routing" is an option of Photon that's not currently needed (or supported) by PUN
            //case StatusCode.TcpRouterResponseOk:
            //    break;
            //case StatusCode.TcpRouterResponseEndpointUnknown:
            //case StatusCode.TcpRouterResponseNodeIdUnknown:
            //case StatusCode.TcpRouterResponseNodeNotReady:

            //    this.DebugReturn(DebugLevel.ERROR, "Unexpected router response: " + statusCode);
            //    break;

            default:

                // this.mListener.serverErrorReturn(statusCode.value());
                this.DebugReturn(DebugLevel.ERROR, "Received unknown status code: " + statusCode);
                break;
        }

        this.externalListener.OnStatusChanged(statusCode);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.DebugReturn(DebugLevel.INFO, string.Format("OnEvent: {0}", photonEvent.ToString()));
        }

        int actorNr = -1;
        PhotonPlayer originatingPlayer = null;

        if (photonEvent.Parameters.ContainsKey(ParameterCode.ActorNr))
        {
            actorNr = (int)photonEvent[ParameterCode.ActorNr];
            if (this.mActors.ContainsKey(actorNr))
            {
                originatingPlayer = (PhotonPlayer)this.mActors[actorNr];
            }
            //else
            //{
            //    // the actor sending this event is not in actorlist. this is usually no problem
            //    if (photonEvent.Code != (byte)LiteOpCode.Join)
            //    {
            //        Debug.LogWarning("Received event, but we do not have this actor:  " + actorNr);
            //    }
            //}
        }

        switch (photonEvent.Code)
        {
            case EventCode.GameList:
                {
                    this.mGameList = new Dictionary<string, RoomInfo>();
                    Hashtable games = (Hashtable)photonEvent[ParameterCode.GameList];
                    foreach (DictionaryEntry game in games)
                    {
                        string gameName = (string)game.Key;
                        this.mGameList[gameName] = new RoomInfo(gameName, (Hashtable)game.Value);
                    }
                    mGameListCopy = new RoomInfo[mGameList.Count];
                    mGameList.Values.CopyTo(mGameListCopy, 0);
                    SendMonoMessage(PhotonNetworkingMessage.OnReceivedRoomListUpdate);
                    break;
                }

            case EventCode.GameListUpdate:
                {
                    Hashtable games = (Hashtable)photonEvent[ParameterCode.GameList];
                    foreach (DictionaryEntry room in games)
                    {
                        string gameName = (string)room.Key;
                        Room game = new Room(gameName, (Hashtable)room.Value);
                        if (game.removedFromList)
                        {
                            this.mGameList.Remove(gameName);
                        }
                        else
                        {
                            this.mGameList[gameName] = game;
                        }
                    }
                    this.mGameListCopy = new RoomInfo[this.mGameList.Count];
                    this.mGameList.Values.CopyTo(this.mGameListCopy, 0);
                    SendMonoMessage(PhotonNetworkingMessage.OnReceivedRoomListUpdate);
                    break;
                }

            case EventCode.QueueState:
                if (photonEvent.Parameters.ContainsKey(ParameterCode.Position))
                {
                    this.mQueuePosition = (int)photonEvent[ParameterCode.Position];
                }
                else
                {
                    this.DebugReturn(DebugLevel.ERROR, "Event QueueState must contain position!");
                }

                if (this.mQueuePosition == 0)
                {
                    // once we're un-queued, let's join the lobby or simply be "connected to master"
                    if (PhotonNetwork.autoJoinLobby)
                    {
                        this.OpJoinLobby();
                        this.State = global::PeerState.Authenticated;
                    }
                    else
                    {
                        this.State = global::PeerState.ConnectedToMaster;
                        NetworkingPeer.SendMonoMessage(PhotonNetworkingMessage.OnConnectedToMaster);
                    }
                }

                break;

            case EventCode.AppStats:
                // Debug.LogInfo("Received stats!");
                this.mPlayersInRoomsCount = (int)photonEvent[ParameterCode.PeerCount];
                this.mPlayersOnMasterCount = (int)photonEvent[ParameterCode.MasterPeerCount];
                this.mGameCount = (int)photonEvent[ParameterCode.GameCount];
                break;

            case EventCode.Join:
                // actorNr is fetched out of event above
                Hashtable actorProperties = (Hashtable)photonEvent[ParameterCode.PlayerProperties];
                if (originatingPlayer == null)
                {
                    bool isLocal = this.mLocalActor.ID == actorNr;
                    this.AddNewPlayer(actorNr, new PhotonPlayer(isLocal, actorNr, actorProperties));
                    this.ResetPhotonViewsOnSerialize(); // This sets the correct OnSerializeState for Reliable OnSerialize
                }

                if (this.mActors[actorNr] == this.mLocalActor)
                {
                    // in this player's 'own' join event, we get a complete list of players in the room, so check if we know all players
                    int[] actorsInRoom = (int[])photonEvent[ParameterCode.ActorList];
                    foreach (int actorNrToCheck in actorsInRoom)
                    {
                        if (this.mLocalActor.ID != actorNrToCheck && !this.mActors.ContainsKey(actorNrToCheck))
                        {
                            Debug.Log("creating player");
                            this.AddNewPlayer(actorNrToCheck, new PhotonPlayer(false, actorNrToCheck, string.Empty));
                        }
                    }

                    SendMonoMessage(PhotonNetworkingMessage.OnJoinedRoom);
                }
                else
                {
                    SendMonoMessage(PhotonNetworkingMessage.OnPhotonPlayerConnected, this.mActors[actorNr]);
                }
                break;

            case EventCode.Leave:
                this.HandleEventLeave(actorNr);
                break;

            case EventCode.PropertiesChanged:
                int targetActorNr = (int)photonEvent[ParameterCode.TargetActorNr];
                Hashtable gameProperties = null;
                Hashtable actorProps = null;
                if (targetActorNr == 0)
                {
                    gameProperties = (Hashtable)photonEvent[ParameterCode.Properties];
                }
                else
                {
                    actorProps = (Hashtable)photonEvent[ParameterCode.Properties];
                }

                this.ReadoutProperties(gameProperties, actorProps, targetActorNr);
                break;

            case PunEvent.RPC:
                //ts: each event now contains a single RPC. execute this
                this.ExecuteRPC(photonEvent[ParameterCode.Data] as Hashtable, originatingPlayer);
                break;

            case PunEvent.SendSerialize:
            case PunEvent.SendSerializeReliable:
                Hashtable serializeData = (Hashtable)photonEvent[ParameterCode.Data];
                //Debug.Log(serializeData.ToStringFull());

                int remoteUpdateServerTimestamp = (int)serializeData[(byte)0];
                short remoteLevelPrefix = -1;
                short initialDataIndex = 1;
                if (serializeData.ContainsKey((byte)1))
                {
                    remoteLevelPrefix = (short)serializeData[(byte)1];
                    initialDataIndex = 2;
                }

                for (short s = initialDataIndex; s < serializeData.Count; s++)
                {
                    this.OnSerializeRead(serializeData[s] as Hashtable, originatingPlayer, remoteUpdateServerTimestamp, remoteLevelPrefix);
                }
                break;

            case PunEvent.Instantiation:
                this.DoInstantiate((Hashtable)photonEvent[ParameterCode.Data], originatingPlayer, null);
                break;

            case PunEvent.CloseConnection:
                // MasterClient "requests" a disconnection from us
                if (originatingPlayer == null || !originatingPlayer.isMasterClient)
                {
                    Debug.LogError("Error: Someone else(" + originatingPlayer + ") then the masterserver requests a disconnect!");
                }
                else
                {
                    PhotonNetwork.LeaveRoom();
                }

                break;

            case PunEvent.DestroyPlayer:
                Hashtable evData = (Hashtable)photonEvent[ParameterCode.Data];
                int targetPlayerId = (int)evData[(byte)0];
                if (targetPlayerId >= 0)
                {
                    this.DestroyPlayerObjects(targetPlayerId, true);
                }
                else
                {
                    Debug.Log("Ev DestroyAll! By PlayerId: " + actorNr);
                    this.DestroyAll(true);
                }
                break;

            case PunEvent.Destroy:
                evData = (Hashtable)photonEvent[ParameterCode.Data];
                int instantiationId = (int)evData[(byte)0];
                Debug.Log("Ev Destroy for viewId: " + instantiationId + " sent by owner: " + (instantiationId / PhotonNetwork.MAX_VIEW_IDS == actorNr) + " this client is owner: " + (instantiationId / PhotonNetwork.MAX_VIEW_IDS == this.mLocalActor.ID));
                
                GameObject goToDestroyLocally = null;
                this.instantiatedObjects.TryGetValue(instantiationId, out goToDestroyLocally);

                if (goToDestroyLocally == null || originatingPlayer == null)
                {
                    Debug.LogError("Can't execute received Destroy request for view ID=" + instantiationId + " as GO can't be foudn. From player/actorNr: " + actorNr + " goToDestroyLocally=" + goToDestroyLocally + "  originating Player=" + originatingPlayer);
                }
                else
                {
                    this.RemoveInstantiatedGO(goToDestroyLocally, true);
                }

                break;

            case PunEvent.AssignMaster:
                evData = (Hashtable)photonEvent[ParameterCode.Data];
                int newMaster = (int)evData[(byte)1];
                this.SetMasterClient(newMaster, false);
                break;

            default:

                // actorNr might be null. it is fetched out of event on top of method
                // Hashtable eventContent = (Hashtable) photonEvent[ParameterCode.Data];
                // this.mListener.customEventAction(actorNr, eventCode, eventContent);
                Debug.LogError("Error. Unhandled event: " + photonEvent);
                break;
        }

        this.externalListener.OnEvent(photonEvent);
    }

    #endregion

    public static void SendMonoMessage(PhotonNetworkingMessage methodString, params object[] parameters)
    {
        HashSet<GameObject> haveSendGOS = new HashSet<GameObject>();
        MonoBehaviour[] mos = (MonoBehaviour[])GameObject.FindObjectsOfType(typeof(MonoBehaviour));
        for (int index = 0; index < mos.Length; index++)
        {
            MonoBehaviour mo = mos[index];
            if (!haveSendGOS.Contains(mo.gameObject))
            {
                haveSendGOS.Add(mo.gameObject);
                if (parameters != null && parameters.Length == 1)
                {
                    mo.SendMessage(methodString.ToString(), parameters[0], SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    mo.SendMessage(methodString.ToString(), parameters, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }

    // PHOTONVIEW/RPC related

    /// <summary>
    /// Executes a received RPC event
    /// </summary>
    public void ExecuteRPC(Hashtable rpcData, PhotonPlayer sender)
    {
        if (rpcData == null || !rpcData.ContainsKey((byte)0))
        {
            this.DebugReturn(DebugLevel.ERROR, "Malformed RPC; this should never occur.");
            return;
        }

        // ts: updated with "flat" event data
        int netViewID = (int)rpcData[(byte)0]; // LIMITS PHOTONVIEWS&PLAYERS
        int otherSidePrefix = 0;    // by default, the prefix is 0 (and this is not being sent)
        if (rpcData.ContainsKey((byte)1))
        {
            otherSidePrefix = (short)rpcData[(byte)1];
        }

        string inMethodName;
        if (rpcData.ContainsKey((byte)5))
        {
            int rpcIndex = (byte)rpcData[(byte)5];  // LIMITS RPC COUNT
            if (rpcIndex > PhotonNetwork.PhotonServerSettings.RpcList.Count - 1)
            {
                Debug.LogError("Could not find RPC with index: " + rpcIndex + ". Going to ignore! Check PhotonServerSettings.RpcList");
                return;
            }
            else
            {
                inMethodName = PhotonNetwork.PhotonServerSettings.RpcList[rpcIndex];
            }
        }
        else
        {
            inMethodName = (string)rpcData[(byte)3];
        }

        object[] inMethodParameters = null;
        if (rpcData.ContainsKey((byte)4))
        {
            inMethodParameters = (object[])rpcData[(byte)4];
        }

        if (inMethodParameters == null)
        {
            inMethodParameters = new object[0];
        }

        PhotonView photonNetview = this.GetPhotonView(netViewID);
        if (photonNetview == null)
        {
            int viewOwnerId = netViewID/PhotonNetwork.MAX_VIEW_IDS;
            bool owningPv = (viewOwnerId == this.mLocalActor.ID);
            bool ownerSent = (viewOwnerId == sender.ID);

            if (owningPv)
            {
                Debug.LogWarning("Received RPC \"" + inMethodName + "\" for viewID " + netViewID + " but this PhotonView does not exist! View was/is ours." + (ownerSent ? " Owner called." : " Remote called."));
            }
            else
            {
                Debug.LogError("Received RPC \"" + inMethodName + "\" for viewID " + netViewID + " but this PhotonView does not exist! Was remote PV." + (ownerSent ? " Owner called." : " Remote called."));
            }
            return;
        }

        if (photonNetview.prefix != otherSidePrefix)
        {
            Debug.LogError(
                "Received RPC \"" + inMethodName + "\" on viewID " + netViewID + " with a prefix of " + otherSidePrefix
                + ", our prefix is " + photonNetview.prefix + ". The RPC has been ignored.");
            return;
        }

        // Get method name
        if (inMethodName == string.Empty)
        {
            this.DebugReturn(DebugLevel.ERROR, "Malformed RPC; this should never occur.");
            return;
        }

        if (this.DebugOut >= DebugLevel.ALL)
        {
            this.DebugReturn(DebugLevel.ALL, "Received RPC; " + inMethodName);
        }

        // SetReceiving filtering
        if (photonNetview.group != 0 && !allowedReceivingGroups.Contains(photonNetview.group))
        {
            return; // Ignore group
        }

        Type[] argTypes = new Type[0];
        if (inMethodParameters.Length > 0)
        {
            argTypes = new Type[inMethodParameters.Length];
            int i = 0;
            for (int index = 0; index < inMethodParameters.Length; index++)
            {
                object objX = inMethodParameters[index];
                if (objX == null)
                {
                    argTypes[i] = null;
                }
                else
                {
                    argTypes[i] = objX.GetType();
                }

                i++;
            }
        }

        int receivers = 0;
        int foundMethods = 0;
        MonoBehaviour[] mbComponents = photonNetview.GetComponents<MonoBehaviour>();    // NOTE: we could possibly also cache MonoBehaviours per view?!
        for (int componentsIndex = 0; componentsIndex < mbComponents.Length; componentsIndex++)
        {
            MonoBehaviour monob = mbComponents[componentsIndex];
            if (monob == null)
            {
                Debug.LogError("ERROR You have missing MonoBehaviours on your gameobjects!");
                continue;
            }

            Type type = monob.GetType();

            // Get [RPC] methods from cache
            List<MethodInfo> cachedRPCMethods = null;
            if (this.monoRPCMethodsCache.ContainsKey(type))
            {
                cachedRPCMethods = this.monoRPCMethodsCache[type];
            }

            if (cachedRPCMethods == null)
            {
                List<MethodInfo> entries = SupportClass.GetMethods(type, typeof(RPC));

                this.monoRPCMethodsCache[type] = entries;
                cachedRPCMethods = entries;
            }

            if (cachedRPCMethods == null)
            {
                continue;
            }

            // Check cache for valid methodname+arguments
            for (int index = 0; index < cachedRPCMethods.Count; index++)
            {
                MethodInfo mInfo = cachedRPCMethods[index];
                if (mInfo.Name == inMethodName)
                {
                    foundMethods++;
                    ParameterInfo[] pArray = mInfo.GetParameters();
                    if (pArray.Length == argTypes.Length)
                    {
                        // Normal, PhotonNetworkMessage left out
                        if (this.CheckTypeMatch(pArray, argTypes))
                        {
                            receivers++;
                            object result = mInfo.Invoke((object)monob, inMethodParameters);
                            if (mInfo.ReturnType == typeof(IEnumerator))
                            {
                                PhotonHandler.SP.StartCoroutine((IEnumerator)result);
                            }
                        }
                    }
                    else if ((pArray.Length - 1) == argTypes.Length)
                    {
                        // Check for PhotonNetworkMessage being the last
                        if (this.CheckTypeMatch(pArray, argTypes))
                        {
                            if (pArray[pArray.Length - 1].ParameterType == typeof(PhotonMessageInfo))
                            {
                                receivers++;

                                int sendTime = (int)rpcData[(byte)2];
                                object[] deParamsWithInfo = new object[inMethodParameters.Length + 1];
                                inMethodParameters.CopyTo(deParamsWithInfo, 0);
                                deParamsWithInfo[deParamsWithInfo.Length - 1] = new PhotonMessageInfo(sender, sendTime, photonNetview);

                                object result = mInfo.Invoke((object)monob, deParamsWithInfo);
                                if (mInfo.ReturnType == typeof(IEnumerator))
                                {
                                    PhotonHandler.SP.StartCoroutine((IEnumerator)result);
                                }
                            }
                        }
                    }
                    else if (pArray.Length == 1 && pArray[0].ParameterType.IsArray)
                    {
                        receivers++;
                        object result = mInfo.Invoke((object)monob, new object[] { inMethodParameters });
                        if (mInfo.ReturnType == typeof(IEnumerator))
                        {
                            PhotonHandler.SP.StartCoroutine((IEnumerator)result);
                        }
                    }
                }
            }
        }

        // Error handling
        if (receivers != 1)
        {
            string argsString = string.Empty;
            for (int index = 0; index < argTypes.Length; index++)
            {
                Type ty = argTypes[index];
                if (argsString != string.Empty)
                {
                    argsString += ", ";
                }

                if (ty == null)
                {
                    argsString += "null";
                }
                else
                {
                    argsString += ty.Name;
                }
            }

            if (receivers == 0)
            {
                if (foundMethods == 0)
                {
                    this.DebugReturn(
                        DebugLevel.ERROR,
                        "PhotonView with ID " + netViewID + " has no method \"" + inMethodName
                        + "\" marked with the [RPC](C#) or @RPC(JS) property! Args: " + argsString);
                }
                else
                {
                    this.DebugReturn(
                        DebugLevel.ERROR,
                        "PhotonView with ID " + netViewID + " has no method \"" + inMethodName + "\" that takes "
                        + argTypes.Length + " argument(s): " + argsString);
                }
            }
            else
            {
                this.DebugReturn(
                    DebugLevel.ERROR,
                    "PhotonView with ID " + netViewID + " has " + receivers + " methods \"" + inMethodName
                    + "\" that takes " + argTypes.Length + " argument(s): " + argsString + ". Should be just one?");
            }
        }
    }

    /// <summary>
    /// Check if all types match with parameters. We can have more paramters then types (allow last RPC type to be different).
    /// </summary>
    /// <param name="methodParameters"></param>
    /// <param name="callParameterTypes"></param>
    /// <returns>If the types-array has matching parameters (of method) in the parameters array (which may be longer).</returns>
    private bool CheckTypeMatch(ParameterInfo[] methodParameters, Type[] callParameterTypes)
    {
        if (methodParameters.Length < callParameterTypes.Length)
        {
            return false;
        }

        for (int index = 0; index < callParameterTypes.Length; index++)
        {
            Type type = methodParameters[index].ParameterType;
            //todo: check metro type usage
            if (callParameterTypes[index] != null && !type.Equals(callParameterTypes[index]))
            {
                return false;
            }
        }

        return true;
    }

    internal Hashtable SendInstantiate(string prefabName, Vector3 position, Quaternion rotation, int group, int[] viewIDs, object[] data, bool isGlobalObject)
    {
        // first viewID is now also the gameobject's instantiateId
        int instantiateId = viewIDs[0];   // LIMITS PHOTONVIEWS&PLAYERS 

        //TODO: reduce hashtable key usage by using a parameter array for the various values
        Hashtable instantiateEvent = new Hashtable(); // This players info is sent via ActorID
        instantiateEvent[(byte)0] = prefabName;

        if (position != Vector3.zero)
        {
            instantiateEvent[(byte)1] = position;
        }

        if (rotation != Quaternion.identity)
        {
            instantiateEvent[(byte)2] = rotation;
        }

        if (group != 0)
        {
            instantiateEvent[(byte)3] = group;
        }

        // send the list of viewIDs only if there are more than one. else the instantiateId is the viewID
        if (viewIDs.Length > 1)
        {
            instantiateEvent[(byte)4] = viewIDs; // LIMITS PHOTONVIEWS&PLAYERS
        }

        if (data != null)
        {
            instantiateEvent[(byte)5] = data;
        }

        if (this.currentLevelPrefix > 0)
        {
            instantiateEvent[(byte)8] = this.currentLevelPrefix;    // photonview's / object's level prefix
        }

        instantiateEvent[(byte)6] = this.ServerTimeInMilliSeconds;
        instantiateEvent[(byte)7] = instantiateId;

        EventCaching cacheMode = (isGlobalObject) ? EventCaching.AddToRoomCacheGlobal : EventCaching.AddToRoomCache;

        this.OpRaiseEvent(PunEvent.Instantiation, instantiateEvent, true, 0, cacheMode, ReceiverGroup.Others);
        return instantiateEvent;
    }

    internal GameObject DoInstantiate(Hashtable evData, PhotonPlayer photonPlayer, GameObject resourceGameObject)
    {
        // some values always present:
        string prefabName = (string)evData[(byte)0];
        int serverTime = (int)evData[(byte)6];
        int instantiationId = (int)evData[(byte)7];

        Vector3 position;
        if (evData.ContainsKey((byte)1))
        {
            position = (Vector3)evData[(byte)1];
        }
        else
        {
            position = Vector3.zero;
        }

        Quaternion rotation = Quaternion.identity;
        if (evData.ContainsKey((byte)2))
        {
            rotation = (Quaternion)evData[(byte)2];
        }

        int group = 0;
        if (evData.ContainsKey((byte)3))
        {
            group = (int)evData[(byte)3];
        }

        short objLevelPrefix = 0;
        if (evData.ContainsKey((byte)8))
        {
            objLevelPrefix = (short)evData[(byte)8];
        }

        int[] viewsIDs;
        if (evData.ContainsKey((byte)4))
        {
            viewsIDs = (int[])evData[(byte)4];
        }
        else
        {
            viewsIDs = new int[1] { instantiationId };
        }

        object[] incomingInstantiationData;
        if (evData.ContainsKey((byte)5))
        {
            incomingInstantiationData = (object[])evData[(byte)5];
        }
        else
        {
            incomingInstantiationData = null;
        }

        // SetReceiving filtering
        if (group != 0 && !this.allowedReceivingGroups.Contains(group))
        {
            return null; // Ignore group
        }

        // load prefab, if it wasn't loaded before (calling methods might do this)
        if (resourceGameObject == null)
        {
            if (!NetworkingPeer.UsePrefabCache || !NetworkingPeer.PrefabCache.TryGetValue(prefabName, out resourceGameObject))
            {
                resourceGameObject = (GameObject)Resources.Load(prefabName, typeof(GameObject));
                if (NetworkingPeer.UsePrefabCache)
                {
                    NetworkingPeer.PrefabCache.Add(prefabName, resourceGameObject);
                }
            }

            if (resourceGameObject == null)
            {
                Debug.LogError("PhotonNetwork error: Could not Instantiate the prefab [" + prefabName + "]. Please verify you have this gameobject in a Resources folder.");
                return null;
            }
        }

        // now modify the loaded "blueprint" object before it becomes a part of the scene (by instantiating it)
        PhotonView[] resourcePVs = resourceGameObject.GetPhotonViewsInChildren();
        if (resourcePVs.Length != viewsIDs.Length)
        {
            throw new Exception("Error in Instantiation! The resource's PhotonView count is not the same as in incoming data.");
        }

        for (int i = 0; i < viewsIDs.Length; i++)
        {
            // NOTE instantiating the loaded resource will keep the viewID but would not copy instantiation data, so it's set below
            // so we only set the viewID and instantiationId now. the instantiationData can be fetched
            resourcePVs[i].viewID = viewsIDs[i];
            resourcePVs[i].prefix = objLevelPrefix;
            resourcePVs[i].instantiationId = instantiationId;
        }

        this.StoreInstantiationData(instantiationId, incomingInstantiationData);

        // load the resource and set it's values before instantiating it:
        // Debug.Log("PreInstantiate");
        GameObject go = (GameObject)GameObject.Instantiate(resourceGameObject, position, rotation);
        // Debug.LogWarning("PostInstantiate");
        for (int i = 0; i < viewsIDs.Length; i++)
        {
            // NOTE instantiating the loaded resource will keep the viewID but would not copy instantiation data, so it's set below
            // so we only set the viewID and instantiationId now. the instantiationData can be fetched
            resourcePVs[i].viewID = 0;
            resourcePVs[i].prefix = -1;
            resourcePVs[i].prefixBackup = -1;
            resourcePVs[i].instantiationId = -1;
        }

        this.RemoveInstantiationData(instantiationId);

        //TODO: remove this debug check
        if (this.instantiatedObjects.ContainsKey(instantiationId))
        {
            GameObject knownGo = this.instantiatedObjects[instantiationId];
            string pvaInfo = "";
            PhotonView[] pva;

            if (knownGo != null)
            {
                pva = knownGo.GetPhotonViewsInChildren();
                foreach (PhotonView view in pva)
                {
                    if (view == null) continue;
                    pvaInfo += view.ToString() + ", ";
                }
            }

            Debug.LogError(string.Format("DoInstantiate re-defines a GameObject. Destroying old entry! New: '{0}' (instantiationID: {1}) Old: {3}. PhotonViews on old: {4}. instantiatedObjects.Count: {2}. PhotonNetwork.lastUsedViewSubId: {5} PhotonNetwork.lastUsedViewSubIdStatic: {6} this.photonViewList.Count {7}.)", go, instantiationId, this.instantiatedObjects.Count, knownGo, pvaInfo, PhotonNetwork.lastUsedViewSubId, PhotonNetwork.lastUsedViewSubIdStatic, this.photonViewList.Count));
            //this.instantiatedObjects.Remove(instantiationId);   // TODO: check if simple remove is ok in all cases. Maybe better Destroy!?
            this.RemoveInstantiatedGO(knownGo, true);
        }

        this.instantiatedObjects.Add(instantiationId, go); //TODO check if instantiatedObjects is (still) needed

        // Send mono event
        // TOD move this callback and script-caching into a method! there should be one already...
        object[] messageInfoParam = new object[1];
        messageInfoParam[0] = new PhotonMessageInfo(photonPlayer, serverTime, null);

        MonoBehaviour[] monos = go.GetComponentsInChildren<MonoBehaviour>();
        for (int index = 0; index < monos.Length; index++)
        {
            MonoBehaviour mono = monos[index];
            MethodInfo methodI;
            if (NetworkingPeer.GetMethod(mono, PhotonNetworkingMessage.OnPhotonInstantiate.ToString(), out methodI))
            {
                object result = methodI.Invoke((object)mono, messageInfoParam);
                if (methodI.ReturnType == typeof(System.Collections.IEnumerator))
                {
                    PhotonHandler.SP.StartCoroutine((IEnumerator)result);
                }
            }
        }

        return go;
    }

    private Dictionary<int, object[]> tempInstantiationData = new Dictionary<int, object[]>();

    private void StoreInstantiationData(int instantiationId, object[] instantiationData)
    {
        // Debug.Log("StoreInstantiationData() instantiationId: " + instantiationId + " tempInstantiationData.Count: " + tempInstantiationData.Count);
        tempInstantiationData[instantiationId] = instantiationData;
    }

    public object[] FetchInstantiationData(int instantiationId)
    {
        object[] data = null;
        if (instantiationId == 0)
        {
            return null;
        }

        tempInstantiationData.TryGetValue(instantiationId, out data);
        // Debug.Log("FetchInstantiationData() instantiationId: " + instantiationId + " tempInstantiationData.Count: " + tempInstantiationData.Count);
        return data;
    }

    private void RemoveInstantiationData(int instantiationId)
    {
        tempInstantiationData.Remove(instantiationId);
    }


    // Removes PhotonNetwork.Instantiate-ed objects.
    // Removes all associated RPCs.
    // Does not remove any manually assigned PhotonViews.
    public void RemoveAllInstantiatedObjects()
    {
        GameObject[] instantiatedGoArray = new GameObject[this.instantiatedObjects.Count];
        this.instantiatedObjects.Values.CopyTo(instantiatedGoArray, 0);

        for (int index = 0; index < instantiatedGoArray.Length; index++)
        {
            GameObject go = instantiatedGoArray[index];
            if (go == null)
            {
                continue;
            }

            this.RemoveInstantiatedGO(go, false);
        }

        if (this.instantiatedObjects.Count > 0)
        {
            Debug.LogError("RemoveAllInstantiatedObjects() this.instantiatedObjects.Count should be 0 by now.");
        }

        this.instantiatedObjects = new Dictionary<int, GameObject>();
    }


    /// <summary>
    /// Destroys all Instantiates and RPCs locally and (if not localOnly) sends EvDestroy(player) and clears related events in the server buffer.
    /// </summary>
    public void DestroyPlayerObjects(int playerId, bool localOnly)
    {
        if (playerId <= 0)
        {
            Debug.LogError("Failed to Destroy objects of playerId: " + playerId);
            return;
        }

        if (!localOnly)
        {
            // clean server's Instantiate and RPC buffers
            this.OpRemoveFromServerInstantiationsOfPlayer(playerId);
            this.OpCleanRpcBuffer(playerId);
            
            // send Destroy(player) to anyone else
            this.SendDestroyOfPlayer(playerId);
        }

        // locally cleaning up that player's objects
        Queue<GameObject> playersGameObjects = new Queue<GameObject>();
        int minPlayerInstantiateId = playerId * PhotonNetwork.MAX_VIEW_IDS;
        int maxPlayerInstantiateId = minPlayerInstantiateId + PhotonNetwork.MAX_VIEW_IDS;

        // find anything that's instantiated by affected player
        foreach (var instantiateEntry in this.instantiatedObjects)
        {
            if (instantiateEntry.Key > minPlayerInstantiateId && instantiateEntry.Key < maxPlayerInstantiateId)
            {
                playersGameObjects.Enqueue(instantiateEntry.Value);
            }
        }

        // any non-local work is already done, so with the list of that player's objects, we can clean up (locally only)
        foreach (GameObject gameObject in playersGameObjects)
        {
            this.RemoveInstantiatedGO(gameObject, true);
        }
    }

    public void DestroyAll(bool localOnly)
    {
        if (!localOnly)
        {
            this.OpRemoveCompleteCache();
            this.SendDestroyOfAll();
        }

        this.LocalCleanupAnythingInstantiated(true);
    }

    /// <summary>Removes GameObject and the PhotonViews on it from local lists and optionally updates remotes. GameObject gets destroyed at end.</summary>
    /// <remarks>
    /// This method might fail and quit early due to several tests.
    /// </remarks>
    /// <param name="go">GameObject to cleanup.</param>
    /// <param name="localOnly">For localOnly, tests of control are skipped and the server is not updated.</param>
    public void RemoveInstantiatedGO(GameObject go, bool localOnly)
    {
        if (go == null)
        {
            if (DebugOut == DebugLevel.ERROR)
            {
                this.DebugReturn(DebugLevel.ERROR, "Failed to 'network-remove' GameObject because it's null.");
            }
            return;
        }

        // Don't remove the GO if it doesn't have any PhotonView
        PhotonView[] views = go.GetComponentsInChildren<PhotonView>();
        if (views == null || views.Length <= 0)
        {
            if (DebugOut == DebugLevel.ERROR)
            {
                this.DebugReturn(DebugLevel.ERROR, "Failed to 'network-remove' GameObject because has no PhotonView components: " + go);
            }
            return;
        }

        PhotonView viewZero = views[0];
        int ownerActorNr = viewZero.OwnerActorNr;   // owner is being checked via IsMine
        int instantiationId = viewZero.instantiationId;   // actual, live InstantiationIds start with 1 and go up


        // Don't remove GOs that are owned by others (unless this is the master and the remote player left)
        if (!localOnly)
        {
            if (!viewZero.isMine && (!this.mLocalActor.isMasterClient || mActors.ContainsKey(ownerActorNr)))
            {
                if (DebugOut == DebugLevel.ERROR)
                {
                    this.DebugReturn(DebugLevel.ERROR, "Failed to 'network-remove' GameObject. Client is neither owner nor masterClient taking over for owner who left: " + viewZero);
                }
                return;
            }
        }

        // Don't remove the Instantiation from the server, if it doesn't have a proper ID
        if (instantiationId < 1)
        {
            if (DebugOut == DebugLevel.ERROR)
            {
                this.DebugReturn(DebugLevel.ERROR, "Failed to 'network-remove' GameObject because it is missing a valid InstantiationId on view: " + viewZero + ". Not Destroying GameObject or PhotonViews!");
            }
            return;
        }


        // cleanup instantiation (event and local list)
        if (!localOnly)
        {
            this.ServerCleanInstantiateAndDestroy(instantiationId, ownerActorNr);   // server cleaning
        }
        this.instantiatedObjects.Remove(instantiationId);   // local


        // cleanup PhotonViews and their RPCs events (if not localOnly)
        for (int j = views.Length - 1; j >= 0; j--)
        {
            PhotonView view = views[j];
            if (view == null)
            {
                continue;
            }

            // we only destroy/clean PhotonViews that were created by PhotonNetwork.Instantiate (and those have an instantiationId!)
            if (view.instantiationId >= 1)
            {
                this.LocalCleanPhotonView(view);
            }
            if (!localOnly)
            {
                this.OpCleanRpcBuffer(view);
            }
        }

        if (this.DebugOut >= DebugLevel.ALL)
        {
            this.DebugReturn(DebugLevel.ALL, "Network destroy Instantiated GO: " + go.name);
        }

        GameObject.Destroy(go);
    }

    /// <summary>
    /// This returns -1 if the GO could not be found in list of instantiatedObjects.
    /// </summary>
    public int GetInstantiatedObjectsId(GameObject go)
    {
        int id = -1;
        if (go == null)
        {
            this.DebugReturn(DebugLevel.ERROR, "GetInstantiatedObjectsId() for GO == null.");
            return id;
        }

        PhotonView[] pvs = go.GetPhotonViewsInChildren();
        if (pvs != null && pvs.Length > 0 && pvs[0] != null)
        {
            return pvs[0].instantiationId;
        }

        if (DebugOut == DebugLevel.ALL)
        {
            this.DebugReturn(DebugLevel.ALL, "GetInstantiatedObjectsId failed for GO: " + go);
        }

        return id;
    }

    /// <summary>
    /// Removes an instantiation event from the server's cache. Needs id and actorNr of player who instantiated.
    /// </summary>
    private void ServerCleanInstantiateAndDestroy(int instantiateId, int actorNr)
    {
        Hashtable removeFilter = new Hashtable();
        removeFilter[(byte)7] = instantiateId;
        this.OpRaiseEvent(PunEvent.Instantiation, removeFilter, true, 0, new int[] { actorNr }, EventCaching.RemoveFromRoomCache);

        Hashtable evData = new Hashtable();
        evData[(byte)0] = instantiateId;
        this.OpRaiseEvent(PunEvent.Destroy, evData, true, 0, EventCaching.DoNotCache, ReceiverGroup.Others);
    }

    private void SendDestroyOfPlayer(int actorNr)
    {
        Hashtable evData = new Hashtable();
        evData[(byte)0] = actorNr;
        this.OpRaiseEvent(PunEvent.DestroyPlayer, evData, true, 0, EventCaching.DoNotCache, ReceiverGroup.Others);
    }

    private void SendDestroyOfAll()
    {
        Hashtable evData = new Hashtable();
        evData[(byte)0] = -1;
        this.OpRaiseEvent(PunEvent.DestroyPlayer, evData, true, 0, EventCaching.DoNotCache, ReceiverGroup.Others);
    }

    private void OpRemoveFromServerInstantiationsOfPlayer(int actorNr)
    {
        // removes all "Instantiation" events of player actorNr. this is not an event for anyone else
        this.OpRaiseEvent(PunEvent.Instantiation, null, true, 0, new int[] { actorNr }, EventCaching.RemoveFromRoomCache);
    }

    public void LocalCleanPhotonView(PhotonView view)
    {
        view.destroyedByPhotonNetworkOrQuit = true;
        this.photonViewList.Remove(view.viewID);
    }


    public PhotonView GetPhotonView(int viewID)
    {
        PhotonView result = null;
        this.photonViewList.TryGetValue(viewID, out result);

        if (result == null)
        {
            PhotonView[] views = GameObject.FindObjectsOfType(typeof(PhotonView)) as PhotonView[];

            foreach (PhotonView view in views)
            {
                if (view.viewID == viewID)
                {
                    Debug.LogWarning("Had to lookup view that wasn't in dict: " + view);
                    return view;
                }
            }
        }

        return result;
    }

    public void RegisterPhotonView(PhotonView netView)
    {
        if (!Application.isPlaying)
        {
            this.photonViewList = new Dictionary<int, PhotonView>();
            return;
        }

        if (netView.subId == 0)
        {
            // don't register views with subId 0 (not initialized). they register when a ID is assigned later on
            // Debug.Log("PhotonView register is ignored, because subId is 0. No id assigned yet to: " + netView);
            return;
        }

        if (this.photonViewList.ContainsKey(netView.viewID))
        {
            // if some other view is in the list already, we got a problem. it might be undestructible. print out error
            if (netView != photonViewList[netView.viewID])
            {
                Debug.LogError(string.Format("PhotonView ID duplicate found: {0}. New: {1} old: {2}. Maybe one wasn't destroyed on scene load?! Check for 'DontDestroyOnLoad'. Destroying old entry, adding new.", netView.viewID, netView, photonViewList[netView.viewID]));
            }

            //this.photonViewList.Remove(netView.viewID); // TODO check if we chould Destroy the GO of this view?!
            this.RemoveInstantiatedGO(photonViewList[netView.viewID].gameObject, true);
        }

        // Debug.Log("adding view to known list: " + netView);
        this.photonViewList.Add(netView.viewID, netView);

        //Debug.LogError("view being added. " + netView);	// Exit Games internal log
        if (this.DebugOut >= DebugLevel.ALL)
        {
            this.DebugReturn(DebugLevel.ALL, "Registered PhotonView: " + netView.viewID);
        }
    }

    ///// <summary>
    ///// Will remove the view from list of views (by its ID).
    ///// </summary>
    //public void RemovePhotonView(PhotonView netView)
    //{
    //    if (!Application.isPlaying)
    //    {
    //        this.photonViewList = new Dictionary<int, PhotonView>();
    //        return;
    //    }

    //    //PhotonView removedView = null;
    //    //this.photonViewList.TryGetValue(netView.viewID, out removedView);
    //    //if (removedView != netView)
    //    //{
    //    //    Debug.LogError("Detected two differing PhotonViews with same viewID: " + netView.viewID);
    //    //}

    //    this.photonViewList.Remove(netView.viewID);

    //    //if (this.DebugOut >= DebugLevel.ALL)
    //    //{
    //    //    this.DebugReturn(DebugLevel.ALL, "Removed PhotonView: " + netView.viewID);
    //    //}
    //}

    /// <summary>
    /// Removes the RPCs of someone else (to be used as master).
    /// This won't clean any local caches. It just tells the server to forget a player's RPCs and instantiates.
    /// </summary>
    /// <param name="actorNumber"></param>
    public void OpCleanRpcBuffer(int actorNumber)
    {
        this.OpRaiseEvent(PunEvent.RPC, null, true, 0, new int[] { actorNumber }, EventCaching.RemoveFromRoomCache);
    }

    /// <summary>
    /// Instead removing RPCs or Instantiates, this removed everything cached by the actor.
    /// </summary>
    /// <param name="actorNumber"></param>
    public void OpRemoveCompleteCacheOfPlayer(int actorNumber)
    {
        this.OpRaiseEvent(0, null, true, 0, new int[] { actorNumber }, EventCaching.RemoveFromRoomCache);
    }


    public void OpRemoveCompleteCache()
    {
        this.OpRaiseEvent(0, null, true, 0, EventCaching.RemoveFromRoomCache, ReceiverGroup.MasterClient);  // TODO: check who gets this event?
    }

    /// This clears the cache of any player/actor who's no longer in the room (making it a simple clean-up option for a new master)
    private void RemoveCacheOfLeftPlayers()
    {
        Dictionary<byte, object> opParameters = new Dictionary<byte, object>();
        opParameters[ParameterCode.Code] = (byte)0;		// any event
        opParameters[ParameterCode.Cache] = (byte)EventCaching.RemoveFromRoomCacheForActorsLeft;    // option to clear the room cache of all events of players who left

        this.OpCustom((byte)OperationCode.RaiseEvent, opParameters, true, 0);
    }

    // Remove RPCs of view (if they are local player's RPCs)
    public void CleanRpcBufferIfMine(PhotonView view)
    {
        if (view.ownerId != this.mLocalActor.ID && !mLocalActor.isMasterClient)
        {
            Debug.LogError("Cannot remove cached RPCs on a PhotonView thats not ours! " + view.owner + " scene: " + view.isSceneView);
            return;
        }

        this.OpCleanRpcBuffer(view);
    }

    /// <summary>Cleans server RPCs for PhotonView (without any further checks).</summary>
    public void OpCleanRpcBuffer(PhotonView view)
    {
        Hashtable rpcFilterByViewId = new Hashtable();
        rpcFilterByViewId[(byte)0] = view.viewID;
        this.OpRaiseEvent(PunEvent.RPC, rpcFilterByViewId, true, 0, EventCaching.RemoveFromRoomCache, ReceiverGroup.Others);
    }

    public void RemoveRPCsInGroup(int group)
    {
        foreach (KeyValuePair<int, PhotonView> kvp in this.photonViewList)
        {
            PhotonView view = kvp.Value;
            if (view.group == group)
            {
                this.CleanRpcBufferIfMine(view);
            }
        }
    }

    public void SetLevelPrefix(short prefix)
    {
        this.currentLevelPrefix = prefix;
        // TODO: should we really change the prefix for existing PVs?! better keep it!
        //foreach (PhotonView view in this.photonViewList.Values)
        //{
        //    view.prefix = prefix;
        //}
    }

    internal void RPC(PhotonView view, string methodName, PhotonPlayer player, params object[] parameters)
    {
        if (this.blockSendingGroups.Contains(view.group))
        {
            return; // Block sending on this group
        }

        if (view.viewID < 1)    //TODO: check why 0 should be illegal
        {
            Debug.LogError("Illegal view ID:" + view.viewID + " method: " + methodName + " GO:" + view.gameObject.name);
        }

        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.DebugReturn(DebugLevel.INFO, "Sending RPC \"" + methodName + "\" to player[" + player + "]");
        }

        //ts: changed RPCs to a one-level hashtable as described in internal.txt
        Hashtable rpcEvent = new Hashtable();
        rpcEvent[(byte)0] = (int)view.viewID; // LIMITS PHOTONVIEWS&PLAYERS
        if (view.prefix > 0)
        {
            rpcEvent[(byte)1] = (short)view.prefix;
        }
        rpcEvent[(byte)2] = this.ServerTimeInMilliSeconds;

        // send name or shortcut (if available)
        int shortcut = 0;
        if (rpcShortcuts.TryGetValue(methodName, out shortcut))
        {
            rpcEvent[(byte)5] = (byte)shortcut; // LIMITS RPC COUNT
        }
        else
        {
            rpcEvent[(byte)3] = methodName;
        }

        if (parameters != null && parameters.Length > 0)
        {
            rpcEvent[(byte) 4] = (object[]) parameters;
        }

        if (this.mLocalActor == player)
        {
            this.ExecuteRPC(rpcEvent, player);
        }
        else
        {
            int[] targetActors = new int[] { player.ID };
            this.OpRaiseEvent(PunEvent.RPC, rpcEvent, true, 0, targetActors);
        }
    }

    /// RPC Hashtable Structure
    /// (byte)0 -> (int) ViewId (combined from actorNr and actor-unique-id)
    /// (byte)1 -> (short) prefix (level)
    /// (byte)2 -> (int) server timestamp
    /// (byte)3 -> (string) methodname
    /// (byte)4 -> (object[]) parameters
    /// (byte)5 -> (byte) method shortcut (alternative to name)
    /// 
    /// This is sent as event (code: 200) which will contain a sender (origin of this RPC).

    internal void RPC(PhotonView view, string methodName, PhotonTargets target, params object[] parameters)
    {
        if (this.blockSendingGroups.Contains(view.group))
        {
            return; // Block sending on this group
        }

        if (view.viewID < 1)
        {
            Debug.LogError("Illegal view ID:" + view.viewID + " method: " + methodName + " GO:" + view.gameObject.name);
        }

        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.DebugReturn(DebugLevel.INFO, "Sending RPC \"" + methodName + "\" to " + target);
        }

        //ts: changed RPCs to a one-level hashtable as described in internal.txt
        Hashtable rpcEvent = new Hashtable();
        rpcEvent[(byte)0] = (int)view.viewID; // LIMITS NETWORKVIEWS&PLAYERS
        if (view.prefix > 0)
        {
            rpcEvent[(byte)1] = (short)view.prefix;
        }
        rpcEvent[(byte)2] = this.ServerTimeInMilliSeconds;


        // send name or shortcut (if available)
        int shortcut = 0;
        if (rpcShortcuts.TryGetValue(methodName, out shortcut))
        {
            rpcEvent[(byte)5] = (byte)shortcut; // LIMITS RPC COUNT
        }
        else
        {
            rpcEvent[(byte)3] = methodName;
        }

        if (parameters != null && parameters.Length > 0)
        {
            rpcEvent[(byte)4] = (object[])parameters;
        }

        // Check scoping
        if (target == PhotonTargets.All)
        {
            this.OpRaiseEvent(PunEvent.RPC, (byte)view.group, rpcEvent, true, 0);
            // Execute local
            this.ExecuteRPC(rpcEvent, this.mLocalActor);
        }
        else if (target == PhotonTargets.Others)
        {
            this.OpRaiseEvent(PunEvent.RPC, (byte)view.group, rpcEvent, true, 0);
        }
        else if (target == PhotonTargets.AllBuffered)
        {
            this.OpRaiseEvent(PunEvent.RPC, rpcEvent, true, 0, EventCaching.AddToRoomCache, ReceiverGroup.Others);

            // Execute local
            this.ExecuteRPC(rpcEvent, this.mLocalActor);
        }
        else if (target == PhotonTargets.OthersBuffered)
        {
            this.OpRaiseEvent(PunEvent.RPC, rpcEvent, true, 0, EventCaching.AddToRoomCache, ReceiverGroup.Others);
        }
        else if (target == PhotonTargets.MasterClient)
        {
            if (this.mMasterClient == this.mLocalActor)
            {
                this.ExecuteRPC(rpcEvent, this.mLocalActor);
            }
            else
            {
                this.OpRaiseEvent(PunEvent.RPC, rpcEvent, true, 0, EventCaching.DoNotCache, ReceiverGroup.MasterClient);//TS: changed from caching to non-cached. this goes to master only
            }
        }
        else
        {
            Debug.LogError("Unsupported target enum: " + target);
        }
    }

    // SetReceiving
    public void SetReceivingEnabled(int group, bool enabled)
    {
        if (group <= 0)
        {
            Debug.LogError("Error: PhotonNetwork.SetReceivingEnabled was called with an illegal group number: " + group + ". The group number should be at least 1.");
            return;
        }

        if (enabled)
        {
            if (!this.allowedReceivingGroups.Contains(group))
            {
                this.allowedReceivingGroups.Add(group);
                byte[] groups = new byte[1] { (byte)group };
                this.OpChangeGroups(null, groups);
            }
        }
        else
        {
            if (this.allowedReceivingGroups.Contains(group))
            {
                this.allowedReceivingGroups.Remove(group);
                byte[] groups = new byte[1] { (byte)group };
                this.OpChangeGroups(groups, null);
            }
        }
    }

    // SetSending
    public void SetSendingEnabled(int group, bool enabled)
    {
        if (!enabled)
        {
            this.blockSendingGroups.Add(group); // can be added to HashSet no matter if already in it
        }
        else
        {
            this.blockSendingGroups.Remove(group);
        }
    }

    public void NewSceneLoaded()
    {
        if (this.loadingLevelAndPausedNetwork && PhotonNetwork.isMessageQueueRunning == false)
        {
            this.loadingLevelAndPausedNetwork = false;
            PhotonNetwork.isMessageQueueRunning = true;
        }
        // Debug.Log("OnLevelWasLoaded photonViewList.Count: " + photonViewList.Count); // Exit Games internal log

        List<int> removeKeys = new List<int>();
        foreach (KeyValuePair<int, PhotonView> kvp in this.photonViewList)
        {
            PhotonView view = kvp.Value;
            if (view == null)
            {
                removeKeys.Add(kvp.Key);
            }
        }

        for (int index = 0; index < removeKeys.Count; index++)
        {
            int key = removeKeys[index];
            this.photonViewList.Remove(key);
        }

        if (removeKeys.Count > 0)
        {
            if (this.DebugOut >= DebugLevel.INFO)
            {
                this.DebugReturn(DebugLevel.INFO, "Removed " + removeKeys.Count + " scene view IDs from last scene.");
            }
        }
    }

    // this is called by Update() and in Unity that means it's single threaded.
    public void RunViewUpdate()
    {
        if (!PhotonNetwork.connected || PhotonNetwork.offlineMode)
        {
            return;
        }

        if (this.mActors == null || this.mActors.Count <= 1)
        {
            return; // No need to send OnSerialize messages (these are never buffered anyway)
        }

        Dictionary<int, Hashtable> dataPerGroupReliable = new Dictionary<int, Hashtable>();
        Dictionary<int, Hashtable> dataPerGroupUnreliable = new Dictionary<int, Hashtable>();

        /* Format of the data hashtable:
         * Hasthable dataPergroup*
         *  [(byte)0] = this.ServerTimeInMilliSeconds;
         *  OPTIONAL: [(byte)1] = currentLevelPrefix;
         *  +  data
         */

        foreach (KeyValuePair<int, PhotonView> kvp in this.photonViewList)
        {
            PhotonView view = kvp.Value;

            if (view.observed != null && view.synchronization != ViewSynchronization.Off)
            {
                // Fetch all sending photonViews
                if (view.owner == this.mLocalActor || (view.isSceneView && this.mMasterClient == this.mLocalActor))
                {
                    #if UNITY_2_6_1 || UNITY_2_6 || UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
                    if (!view.gameObject.active)
                    {
                        continue; // Only on actives
                    }
                    #else
                    if (!view.gameObject.activeInHierarchy)
                    {
                        continue; // Only on actives
                    }
                    #endif

                    if (this.blockSendingGroups.Contains(view.group))
                    {
                        continue; // Block sending on this group
                    }

                    // Run it trough its OnSerialize
                    Hashtable evData = this.OnSerializeWrite(view);
                    if (evData == null)
                    {
                        continue;
                    }

                    if (view.synchronization == ViewSynchronization.ReliableDeltaCompressed)
                    {
                        if (!evData.ContainsKey((byte)1) && !evData.ContainsKey((byte)2))
                        {
                            // Everything has been removed by compression, nothing to send
                        }
                        else
                        {
                            if (!dataPerGroupReliable.ContainsKey(view.group))
                            {
                                dataPerGroupReliable[view.group] = new Hashtable();
                                dataPerGroupReliable[view.group][(byte)0] = this.ServerTimeInMilliSeconds;
                                if (currentLevelPrefix >= 0)
                                {
                                    dataPerGroupReliable[view.group][(byte)1] = this.currentLevelPrefix;
                                }
                            }
                            Hashtable groupHashtable = dataPerGroupReliable[view.group];
                            groupHashtable.Add((short)groupHashtable.Count, evData);
                        }
                    }
                    else
                    {
                        if (!dataPerGroupUnreliable.ContainsKey(view.group))
                        {
                            dataPerGroupUnreliable[view.group] = new Hashtable();
                            dataPerGroupUnreliable[view.group][(byte)0] = this.ServerTimeInMilliSeconds;
                            if (currentLevelPrefix >= 0)
                            {
                                dataPerGroupUnreliable[view.group][(byte)1] = this.currentLevelPrefix;
                            }
                        }
                        Hashtable groupHashtable = dataPerGroupUnreliable[view.group];
                        groupHashtable.Add((short)groupHashtable.Count, evData);
                    }
                }
                else
                {
                    // Debug.Log(" NO OBS on " + view.name + " " + view.owner);
                }
            }
            else
            {
            }
        }

        //Send the messages: every group is send in it's own message and unreliable and reliable are split as well
        foreach (KeyValuePair<int, Hashtable> kvp in dataPerGroupReliable)
        {
            this.OpRaiseEvent(PunEvent.SendSerializeReliable, (byte)kvp.Key, kvp.Value, true, 0);
        }
        foreach (KeyValuePair<int, Hashtable> kvp in dataPerGroupUnreliable)
        {
            this.OpRaiseEvent(PunEvent.SendSerialize, (byte)kvp.Key, kvp.Value, false, 0);
        }
    }
    
    // calls OnPhotonSerializeView (through ExecuteOnSerialize)
    // the content created here is consumed by receivers in: ReadOnSerialize
    private Hashtable OnSerializeWrite(PhotonView view)
    {
        // each view creates a list of values that should be sent
        List<object> data = new List<object>();

        // 1=Specific data
        if (view.observed is MonoBehaviour)
        {
            PhotonStream pStream = new PhotonStream(true, null);
            PhotonMessageInfo info = new PhotonMessageInfo(this.mLocalActor, this.ServerTimeInMilliSeconds, view);

            view.ExecuteOnSerialize(pStream, info);
            if (pStream.Count == 0)
            {
                // if an observed script didn't write any data, we don't send anything
                return null;
            }

            // we want to use the content of the stream (filled in by user scripts)
            data = pStream.data;
        }
        else if (view.observed is Transform)
        {
            Transform trans = (Transform)view.observed;

            if (view.onSerializeTransformOption == OnSerializeTransform.OnlyPosition
                || view.onSerializeTransformOption == OnSerializeTransform.PositionAndRotation
                || view.onSerializeTransformOption == OnSerializeTransform.All)
                data.Add(trans.localPosition);
            else
                data.Add(null);

            if (view.onSerializeTransformOption == OnSerializeTransform.OnlyRotation
                || view.onSerializeTransformOption == OnSerializeTransform.PositionAndRotation
                || view.onSerializeTransformOption == OnSerializeTransform.All)
                data.Add(trans.localRotation);
            else
                data.Add(null);

            if (view.onSerializeTransformOption == OnSerializeTransform.OnlyScale
                || view.onSerializeTransformOption == OnSerializeTransform.All)
                data.Add(trans.localScale);
        }
        else if (view.observed is Rigidbody)
        {
            Rigidbody rigidB = (Rigidbody)view.observed;

            if (view.onSerializeRigidBodyOption != OnSerializeRigidBody.OnlyAngularVelocity)
                data.Add(rigidB.velocity);
            else
                data.Add(null);

            if (view.onSerializeRigidBodyOption != OnSerializeRigidBody.OnlyVelocity)
                data.Add(rigidB.angularVelocity);
        }
        else
        {
            Debug.LogError("Observed type is not serializable: " + view.observed.GetType());
            return null;
        }

        object[] dataArray = data.ToArray();

        // EVDATA:
        // 0=View ID (an int, never compressed cause it's not in the data)
        // 1=data of observed type (different per type of observed object)
        // 2=compressed data (in this case, key 1 is empty)
        // 3=list of values that are actually null (if something was changed but actually IS null)
        Hashtable evData = new Hashtable();
        evData[(byte)0] = (int)view.viewID;
        evData[(byte)1] = dataArray;    // this is the actual data (script or observed object)

        if (view.synchronization == ViewSynchronization.ReliableDeltaCompressed)
        {
            // compress content of data set (by comparing to view.lastOnSerializeDataSent)
            // the "original" dataArray is NOT modified by DeltaCompressionWrite
            // if something was compressed, the evData key 2 and 3 are used (see above)
            bool somethingLeftToSend = this.DeltaCompressionWrite(view, evData);

            // buffer the full data set (for next compression)
            view.lastOnSerializeDataSent = dataArray;

            if (!somethingLeftToSend)
            {
                return null;
            }
        }

        return evData;
    }

    /// <summary>
    /// Reads updates created by OnSerializeWrite
    /// </summary>
    private void OnSerializeRead(Hashtable data, PhotonPlayer sender, int networkTime, short correctPrefix)
    {
        // read view ID from key (byte)0: a int-array (PUN 1.17++)
        int viewID = (int)data[(byte)0];


        PhotonView view = this.GetPhotonView(viewID);
        if (view == null)
        {
            Debug.LogWarning("Received OnSerialization for view ID " + viewID + ". We have no such PhotonView! Ignored this if you're leaving a room. State: " + this.State);
            return;
        }

        if (view.prefix > 0 && correctPrefix != view.prefix)
        {
            Debug.LogError("Received OnSerialization for view ID " + viewID + " with prefix " + correctPrefix + ". Our prefix is " + view.prefix);
            return;
        }

        // SetReceiving filtering
        if (view.group != 0 && !this.allowedReceivingGroups.Contains(view.group))
        {
            return; // Ignore group
        }

        if (view.synchronization == ViewSynchronization.ReliableDeltaCompressed)
        {
            if (!this.DeltaCompressionRead(view, data))
            {
                // Skip this packet as we haven't got received complete-copy of this view yet.                
                this.DebugReturn(DebugLevel.INFO, "Skipping packet for " + view.name + " [" + view.viewID + "] as we haven't received a full packet for delta compression yet. This is OK if it happens for the first few frames after joining a game.");
                return;
            }

            // store last received for delta-compression usage
            view.lastOnSerializeDataReceived = data[(byte)1] as object[];
        }

        // Use incoming data according to observed type
        if (view.observed is MonoBehaviour)
        {
            object[] contents = data[(byte)1] as object[];
            PhotonStream pStream = new PhotonStream(false, contents);
            PhotonMessageInfo info = new PhotonMessageInfo(sender, networkTime, view);

            view.ExecuteOnSerialize(pStream, info);
        }
        else if (view.observed is Transform)
        {
            object[] contents = data[(byte)1] as object[];
            Transform trans = (Transform)view.observed;
            if (contents.Length >= 1 && contents[0] != null)
                trans.localPosition = (Vector3)contents[0];
            if (contents.Length >= 2 && contents[1] != null)
                trans.localRotation = (Quaternion)contents[1];
            if (contents.Length >= 3 && contents[2] != null)
                trans.localScale = (Vector3)contents[2];

        }
        else if (view.observed is Rigidbody)
        {
            object[] contents = data[(byte)1] as object[];
            Rigidbody rigidB = (Rigidbody)view.observed;
            if (contents.Length >= 1 && contents[0] != null)
                rigidB.velocity = (Vector3)contents[0];
            if (contents.Length >= 2 && contents[1] != null)
                rigidB.angularVelocity = (Vector3)contents[1];
        }
        else
        {
            Debug.LogError("Type of observed is unknown when receiving.");
        }
    }

    /// <summary>
    /// Compares the new data with previously sent data and skips values that didn't change.
    /// </summary>
    /// <returns>True if anything has to be sent, false if nothing new or no data</returns>
    private bool DeltaCompressionWrite(PhotonView view, Hashtable data)
    {
        if (view.lastOnSerializeDataSent == null)
        {
            return true; // all has to be sent
        }

        // We can compress as we sent a full update previously (readers can re-use previous values)
        object[] lastData = view.lastOnSerializeDataSent;
        object[] currentContent = data[(byte)1] as object[];

        if (currentContent == null)
        {
            // no data to be sent
            return false;
        }

        if (lastData.Length != currentContent.Length)
        {
            // if new data isn't same length as before, we send the complete data-set uncompressed
            return true;
        }

        object[] compressedContent = new object[currentContent.Length];
        int compressedValues = 0;

        List<int> valuesThatAreChangedToNull = new List<int>();
        for (int index = 0; index < compressedContent.Length; index++)
        {
            object newObj = currentContent[index];
            object oldObj = lastData[index];
            if (this.ObjectIsSameWithInprecision(newObj, oldObj))
            {
                // compress (by using null, instead of value, which is same as before)
                compressedValues++;
                // compressedContent[index] is already null (initialized)
            }
            else
            {
                compressedContent[index] = currentContent[index];

                // value changed, we don't replace it with null
                // new value is null (like a compressed value): we have to mark it so it STAYS null instead of being replaced with previous value
                if (newObj == null)
                {
                    valuesThatAreChangedToNull.Add(index);
                }
            }
        }

        // Only send the list of compressed fields if we actually compressed 1 or more fields.
        if (compressedValues > 0)
        {
            data.Remove((byte)1); // remove the original data (we only send compressed data)

            if (compressedValues == currentContent.Length)
            {
                // all values are compressed to null, we have nothing to send
                return false;
            }

            data[(byte)2] = compressedContent; // current, compressted data is moved to key 2 to mark it as compressed
            if (valuesThatAreChangedToNull.Count > 0)
            {
                data[(byte)3] = valuesThatAreChangedToNull.ToArray(); // data that is actually null (not just cause we didn't want to send it)
            }
        }

        return true;    // some data was compressed but we need to send something
    }

    /// <summary>
    /// reads incoming messages created by "OnSerialize"
    /// </summary>
    private bool DeltaCompressionRead(PhotonView view, Hashtable data)
    {
        if (data.ContainsKey((byte)1))
        {
            // we have a full list of data (cause key 1 is used), so return "we have uncompressed all"
            return true;
        }

        // Compression was applied as data[(byte)2] exists (this is the data with some fields being compressed to null)
        // now we also need a previous "full" list of values to restore values that are null in this msg
        if (view.lastOnSerializeDataReceived == null)
        {
            return false; // We dont have a full match yet, we cannot work with missing values: skip this message
        }

        object[] compressedContents = data[(byte)2] as object[];
        if (compressedContents == null)
        {
            // despite expectation, there is no compressed data in this msg. shouldn't happen. just a null check
            return false;
        }

        int[] indexesThatAreChangedToNull = data[(byte)3] as int[];
        if (indexesThatAreChangedToNull == null)
        {
            indexesThatAreChangedToNull = new int[0];
        }

        object[] lastReceivedData = view.lastOnSerializeDataReceived;
        for (int index = 0; index < compressedContents.Length; index++)
        {
            if (compressedContents[index] == null && !indexesThatAreChangedToNull.Contains(index))
            {
                // we replace null values in this received msg unless a index is in the "changed to null" list
                object lastValue = lastReceivedData[index];
                compressedContents[index] = lastValue;
            }
        }

        data[(byte)1] = compressedContents; // compressedContents are now uncompressed...
        return true;
    }

    /// <summary>
    /// Returns true if both objects are almost identical.
    /// Used to check whether two objects are similar enough to skip an update.
    /// </summary>
    bool ObjectIsSameWithInprecision(object one, object two)
    {
        if (one == null || two == null)
        {
            return one == null && two == null;
        }

        if (!one.Equals(two))
        {
            // if A is not B, lets check if A is almost B
            if (one is Vector3)
            {
                Vector3 a = (Vector3)one;
                Vector3 b = (Vector3)two;
                if (a.AlmostEquals(b, PhotonNetwork.precisionForVectorSynchronization))
                {
                    return true;
                }
            }
            else if (one is Vector2)
            {
                Vector2 a = (Vector2)one;
                Vector2 b = (Vector2)two;
                if (a.AlmostEquals(b, PhotonNetwork.precisionForVectorSynchronization))
                {
                    return true;
                }
            }
            else if (one is Quaternion)
            {
                Quaternion a = (Quaternion)one;
                Quaternion b = (Quaternion)two;
                if (a.AlmostEquals(b, PhotonNetwork.precisionForQuaternionSynchronization))
                {
                    return true;
                }
            }
            else if (one is float)
            {
                float a = (float)one;
                float b = (float)two;
                if (a.AlmostEquals(b, PhotonNetwork.precisionForFloatSynchronization))
                {
                    return true;
                }
            }

            // one does not equal two
            return false;
        }

        return true;
    }

    internal protected static bool GetMethod(MonoBehaviour monob, string methodType, out MethodInfo mi)
    {
        mi = null;

        if (monob == null || string.IsNullOrEmpty(methodType))
        {
            return false;
        }

        List<MethodInfo> methods = SupportClass.GetMethods(monob.GetType(), null);
        for (int index = 0; index < methods.Count; index++)
        {
            MethodInfo methodInfo = methods[index];
            if (methodInfo.Name.Equals(methodType))
            {
                mi = methodInfo;
                return true;
            }
        }

        return false;
    }

    /// <summary>Internally used to flag if the message queue was disabled by a "scene sync" situation (to re-enable it).</summary>
    internal protected bool loadingLevelAndPausedNetwork = false;

    /// <summary>For automatic scene syncing, the loaded scene is put into a room property. This is the name of said prop.</summary>
    internal protected const string CurrentSceneProperty = "curScn";

    /// <summary>Internally used to detect the current scene and load it if PhotonNetwork.automaticallySyncScene is enabled.</summary>
    internal protected void AutomaticallySyncScene()
    {
        if (PhotonNetwork.room != null && PhotonNetwork.automaticallySyncScene && !PhotonNetwork.isMasterClient)
        {
            string sceneName = (string)PhotonNetwork.room.customProperties[NetworkingPeer.CurrentSceneProperty];
            if (!string.IsNullOrEmpty(sceneName))
            {
                if (sceneName != Application.loadedLevelName)
                {
                    PhotonNetwork.LoadLevel(sceneName);
                }
                else
                {
                    if (DebugOut >= DebugLevel.WARNING)
                    {
                        DebugReturn(DebugLevel.WARNING, "Skipped re-loading level due to scene syncing. Level already loaded.");
                    }
                }
            }
        }
    }
}
