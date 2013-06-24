// ----------------------------------------------------------------------------
// <copyright file="LoadbalancingPeer.cs" company="Exit Games GmbH">
//   Loadbalancing Framework for Photon - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   Provides the operations needed to use the loadbalancing server app(s).
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.Lite;

/// <summary>
/// Internally used by PUN, a LoadbalancingPeer provides the operations and enum 
/// definitions needed to use the Photon Loadbalancing server (or the Photon Cloud).
/// </summary>
/// <remarks>
/// The LoadBalancingPeer does not keep a state, instead this is done by a LoadBalancingClient.
/// </remarks>
internal class LoadbalancingPeer : PhotonPeer
{
    public LoadbalancingPeer(IPhotonPeerListener listener, ConnectionProtocol protocolType) : base(listener, protocolType)
    {
    }

    /// <summary>
    /// Joins the lobby on the Master Server, where you get a list of RoomInfos of currently open rooms.
    /// This is an async request which triggers a OnOperationResponse() call.
    /// </summary>
    /// <returns>If the operation could be sent (has to be connected).</returns>
    public virtual bool OpJoinLobby()
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.Listener.DebugReturn(DebugLevel.INFO, "OpJoinLobby()");
        }

        return this.OpCustom(OperationCode.JoinLobby, null, true);
    }

    /// <summary>
    /// Leaves the lobby on the Master Server.
    /// This is an async request which triggers a OnOperationResponse() call.
    /// </summary>
    /// <returns>If the operation could be sent (has to be connected).</returns>
    public virtual bool OpLeaveLobby()
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.Listener.DebugReturn(DebugLevel.INFO, "OpLeaveLobby()");
        }

        return this.OpCustom(OperationCode.LeaveLobby, null, true);
    }

    /// <summary>
    /// Don't use this method directly, unless you know how to cache and apply customActorProperties.
    /// The PhotonNetwork methods will handle player and room properties for you and call this method.
    /// </summary>
    public virtual bool OpCreateRoom(string gameID, bool isVisible, bool isOpen, byte maxPlayers, bool autoCleanUp, Hashtable customGameProperties, Hashtable customPlayerProperties, string[] customRoomPropertiesForLobby)
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.Listener.DebugReturn(DebugLevel.INFO, "OpCreateRoom()");
        }

        Hashtable gameProperties = new Hashtable();
        gameProperties[GameProperties.IsOpen] = isOpen;
        gameProperties[GameProperties.IsVisible] = isVisible;
        gameProperties[GameProperties.PropsListedInLobby] = customRoomPropertiesForLobby;
        gameProperties.MergeStringKeys(customGameProperties);
        if (maxPlayers > 0)
        {
            gameProperties[GameProperties.MaxPlayers] = maxPlayers;
        }

        Dictionary<byte, object> op = new Dictionary<byte, object>();
        op[ParameterCode.GameProperties] = gameProperties;
        op[ParameterCode.PlayerProperties] = customPlayerProperties;
        op[ParameterCode.Broadcast] = true;

        if (!string.IsNullOrEmpty(gameID))
        {
            op[ParameterCode.RoomName] = gameID;
        }

        if (autoCleanUp)
        {
            op[ParameterCode.CleanupCacheOnLeave] = autoCleanUp;
            gameProperties[GameProperties.CleanupCacheOnLeave] = autoCleanUp;
        }

        return this.OpCustom(OperationCode.CreateGame, op, true);
    }

    /// <summary>
    /// Joins a room by name and sets this player's properties.
    /// </summary>
    /// <param name="roomName"></param>
    /// <param name="playerProperties"></param>
    /// <returns>If the operation could be sent (has to be connected).</returns>
    public virtual bool OpJoinRoom(string roomName, Hashtable playerProperties)
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.Listener.DebugReturn(DebugLevel.INFO, "OpJoinRoom()");
        }

        if (string.IsNullOrEmpty(roomName))
        {
            this.Listener.DebugReturn(DebugLevel.ERROR, "OpJoinRoom() failed. Please specify a roomname.");
            return false;
        }

        Dictionary<byte, object> op = new Dictionary<byte, object>();
        op[ParameterCode.RoomName] = roomName;
        op[ParameterCode.Broadcast] = true;
        if (playerProperties != null)
        {
            op[ParameterCode.PlayerProperties] = playerProperties;
        }

        return this.OpCustom(OperationCode.JoinGame, op, true);
    }

    /// <summary>
    /// Operation to join a random, available room. Overloads take additional player properties.
    /// This is an async request which triggers a OnOperationResponse() call.
    /// If all rooms are closed or full, the OperationResponse will have a returnCode of ErrorCode.NoRandomMatchFound.
    /// If successful, the OperationResponse contains a gameserver address and the name of some room.
    /// </summary>
    /// <param name="expectedCustomRoomProperties">Optional. A room will only be joined, if it matches these custom properties (with string keys).</param>
    /// <param name="expectedMaxPlayers">Filters for a particular maxplayer setting. Use 0 to accept any maxPlayer value.</param>
    /// <param name="playerProperties">This player's properties (custom and well known).</param>
    /// <param name="matchingType">Selects one of the available matchmaking algorithms. See MatchmakingMode enum for options.</param>
    /// <returns>If the operation could be sent currently (requires connection).</returns>
    public virtual bool OpJoinRandomRoom(Hashtable expectedCustomRoomProperties, byte expectedMaxPlayers, Hashtable playerProperties, MatchmakingMode matchingType)
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.Listener.DebugReturn(DebugLevel.INFO, "OpJoinRandomRoom()");
        }

        Hashtable expectedRoomProperties = new Hashtable();
        expectedRoomProperties.MergeStringKeys(expectedCustomRoomProperties);
        if (expectedMaxPlayers > 0)
        {
            expectedRoomProperties[GameProperties.MaxPlayers] = expectedMaxPlayers;
        }

        Dictionary<byte, object> opParameters = new Dictionary<byte, object>();
        if (expectedRoomProperties.Count > 0)
        {
            opParameters[ParameterCode.GameProperties] = expectedRoomProperties;
        }

        if (playerProperties != null && playerProperties.Count > 0)
        {
            opParameters[ParameterCode.PlayerProperties] = playerProperties;
        }

        if (matchingType != MatchmakingMode.FillRoom)
        {
            opParameters[ParameterCode.MatchMakingType] = (byte)matchingType;
        }

        return this.OpCustom(OperationCode.JoinRandomGame, opParameters, true);
    }

    public bool OpSetCustomPropertiesOfActor(int actorNr, Hashtable actorProperties, bool broadcast, byte channelId)
    {
        return this.OpSetPropertiesOfActor(actorNr, actorProperties.StripToStringKeys(), broadcast, channelId);
    }

    protected bool OpSetPropertiesOfActor(int actorNr, Hashtable actorProperties, bool broadcast, byte channelId)
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.Listener.DebugReturn(DebugLevel.INFO, "OpSetPropertiesOfActor()");
        }
            
        Dictionary<byte, object> opParameters = new Dictionary<byte, object>();
        opParameters.Add(ParameterCode.Properties, actorProperties);
        opParameters.Add(ParameterCode.ActorNr, actorNr);
        if (broadcast)
        {
            opParameters.Add(ParameterCode.Broadcast, broadcast);
        }

        return this.OpCustom((byte)OperationCode.SetProperties, opParameters, broadcast, channelId);
    }

    protected void OpSetPropertyOfRoom(byte propCode, object value)
    {
        Hashtable properties = new Hashtable();
        properties[propCode] = value;
        this.OpSetPropertiesOfRoom(properties, true, (byte)0);
    }

    public bool OpSetCustomPropertiesOfRoom(Hashtable gameProperties, bool broadcast, byte channelId)
    {
        return this.OpSetPropertiesOfRoom(gameProperties.StripToStringKeys(), broadcast, channelId);
    }

    public bool OpSetPropertiesOfRoom(Hashtable gameProperties, bool broadcast, byte channelId)
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.Listener.DebugReturn(DebugLevel.INFO, "OpSetPropertiesOfRoom()");
        }

        Dictionary<byte, object> opParameters = new Dictionary<byte, object>();
        opParameters.Add(ParameterCode.Properties, gameProperties);
        if (broadcast)
        {
            opParameters.Add(ParameterCode.Broadcast, broadcast);
        }

        return this.OpCustom((byte)OperationCode.SetProperties, opParameters, broadcast, channelId);
    }

    /// <summary>
    /// Sends this app's appId and appVersion to identify this application server side.
    /// </summary>
    /// <remarks>
    /// This operation makes use of encryption, if it's established beforehand.
    /// See: EstablishEncryption(). Check encryption with IsEncryptionAvailable.
    /// </remarks>
    /// <param name="appId"></param>
    /// <param name="appVersion"></param>
    /// <returns>If the operation could be sent (has to be connected).</returns>
    public virtual bool OpAuthenticate(string appId, string appVersion)
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.Listener.DebugReturn(DebugLevel.INFO, "OpAuthenticate()");
        }

        Dictionary<byte, object> opParameters = new Dictionary<byte, object>();
        opParameters[ParameterCode.AppVersion] = appVersion;
        opParameters[ParameterCode.ApplicationId] = appId;

        return this.OpCustom(OperationCode.Authenticate, opParameters, true, (byte)0, this.IsEncryptionAvailable);
    }

    /// <summary>
    /// Operation to handle this client's interest groups (for events in room).
    /// </summary>
    /// <remarks>
    /// Note the difference between passing null and byte[0]:
    ///   null won't add/remove any groups.
    ///   byte[0] will add/remove all (existing) groups.
    /// First, removing groups is executed. This way, you could leave all groups and join only the ones provided.
    /// </remarks>
    /// <param name="groupsToRemove">Groups to remove from interest. Null will not remove any. A byte[0] will remove all.</param>
    /// <param name="groupsToAdd">Groups to add to interest. Null will not add any. A byte[0] will add all current.</param>
    /// <returns></returns>
    public virtual bool OpChangeGroups(byte[] groupsToRemove, byte[] groupsToAdd)
    {        
        if (this.DebugOut >= DebugLevel.ALL)
        {
            this.Listener.DebugReturn(DebugLevel.ALL, "OpChangeGroups()");
        }

        Dictionary<byte, object> opParameters = new Dictionary<byte, object>();
        if (groupsToRemove != null)
        {
            opParameters[(byte)LiteOpKey.Remove] = groupsToRemove;
        }
        if (groupsToAdd != null)
        {
            opParameters[(byte)LiteOpKey.Add] = groupsToAdd;
        }

        return this.OpCustom((byte)LiteOpCode.ChangeGroups, opParameters, true, 0);
    }

    /// <summary>
    /// Send your custom data as event to an "interest group" in the current Room.
    /// </summary>
    /// <remarks>
    /// No matter if reliable or not, when an event is sent to a interest Group, some users won't get this data.
    /// Clients can control the groups they are interested in by using OpChangeGroups.
    /// </remarks>
    /// <param name="eventCode">Identifies this type of event (and the content). Your game's event codes can start with 0.</param>
    /// <param name="interestGroup">The ID of the interest group this event goes to (exclusively).</param>
    /// <param name="customEventContent">Custom data you want to send along (use null, if none).</param>
    /// <param name="sendReliable">If this event has to arrive reliably (potentially repeated if it's lost).</param>
    /// <param name="channelId">The "channel" to which this event should belong. Per channel, the sequence is kept in order.</param>
    /// <returns>If operation could be enqueued for sending</returns>
    public virtual bool OpRaiseEvent(byte eventCode, byte interestGroup, Hashtable customEventContent, bool sendReliable, byte channelId)
    {
        Dictionary<byte, object> opParameters = new Dictionary<byte, object>();
        opParameters[(byte)LiteOpKey.Data] = customEventContent;
        opParameters[(byte)LiteOpKey.Code] = (byte)eventCode;
        if(interestGroup != (byte)0) opParameters[(byte)LiteOpKey.Group] = (byte)interestGroup;

        return this.OpCustom((byte)LiteOpCode.RaiseEvent, opParameters, sendReliable, channelId);
    }

    /// <summary>
    /// Used in a room to raise (send) an event to the other players. 
    /// Multiple overloads expose different parameters to this frequently used operation.
    /// </summary>
    /// <param name="eventCode">Code for this "type" of event (use a code per "meaning" or content).</param>
    /// <param name="evData">Data to send. Hashtable that contains key-values of Photon serializable datatypes.</param>
    /// <param name="sendReliable">Use false if the event is replaced by a newer rapidly. Reliable events add overhead and add lag when repeated.</param>
    /// <param name="channelId">The "channel" to which this event should belong. Per channel, the sequence is kept in order.</param>
    /// <returns>If the operation could be sent (has to be connected).</returns>
    public virtual bool OpRaiseEvent(byte eventCode, Hashtable evData, bool sendReliable, byte channelId)
    {
        return this.OpRaiseEvent(eventCode, evData, sendReliable, channelId, EventCaching.DoNotCache, ReceiverGroup.Others);
    }

    /// <summary>
    /// Used in a room to raise (send) an event to the other players. 
    /// Multiple overloads expose different parameters to this frequently used operation.
    /// </summary>
    /// <param name="eventCode">Code for this "type" of event (use a code per "meaning" or content).</param>
    /// <param name="evData">Data to send. Hashtable that contains key-values of Photon serializable datatypes.</param>
    /// <param name="sendReliable">Use false if the event is replaced by a newer rapidly. Reliable events add overhead and add lag when repeated.</param>
    /// <param name="channelId">The "channel" to which this event should belong. Per channel, the sequence is kept in order.</param>
    /// <param name="targetActors">Defines the target players who should receive the event (use only for small target groups).</param>
    /// <returns>If the operation could be sent (has to be connected).</returns>
    public virtual bool OpRaiseEvent(byte eventCode, Hashtable evData, bool sendReliable, byte channelId, int[] targetActors)
    {
        return this.OpRaiseEvent(eventCode, evData, sendReliable, channelId, targetActors, EventCaching.DoNotCache);
    }

    /// <summary>
    /// Used in a room to raise (send) an event to the other players. 
    /// Multiple overloads expose different parameters to this frequently used operation.
    /// </summary>
    /// <param name="eventCode">Code for this "type" of event (use a code per "meaning" or content).</param>
    /// <param name="evData">Data to send. Hashtable that contains key-values of Photon serializable datatypes.</param>
    /// <param name="sendReliable">Use false if the event is replaced by a newer rapidly. Reliable events add overhead and add lag when repeated.</param>
    /// <param name="channelId">The "channel" to which this event should belong. Per channel, the sequence is kept in order.</param>
    /// <param name="targetActors">Defines the target players who should receive the event (use only for small target groups).</param>
    /// <param name="cache">Use EventCaching options to store this event for players who join.</param>
    /// <returns>If the operation could be sent (has to be connected).</returns>
    public virtual bool OpRaiseEvent(byte eventCode, Hashtable evData, bool sendReliable, byte channelId, int[] targetActors, EventCaching cache)
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.Listener.DebugReturn(DebugLevel.INFO, "OpRaiseEvent()");
        }

        Dictionary<byte, object> opParameters = new Dictionary<byte, object>();
        opParameters[ParameterCode.Data] = evData;
        opParameters[ParameterCode.Code] = (byte)eventCode;

        if (cache != EventCaching.DoNotCache)
        {
            opParameters[ParameterCode.Cache] = (byte)cache;
        }

        if (targetActors != null)
        {
            opParameters[ParameterCode.ActorList] = targetActors;
        }

        return this.OpCustom(OperationCode.RaiseEvent, opParameters, sendReliable, channelId);
    }

    /// <summary>
    /// Used in a room to raise (send) an event to the other players. 
    /// Multiple overloads expose different parameters to this frequently used operation.
    /// </summary>
    /// <param name="eventCode">Code for this "type" of event (use a code per "meaning" or content).</param>
    /// <param name="evData">Data to send. Hashtable that contains key-values of Photon serializable datatypes.</param>
    /// <param name="sendReliable">Use false if the event is replaced by a newer rapidly. Reliable events add overhead and add lag when repeated.</param>
    /// <param name="channelId">The "channel" to which this event should belong. Per channel, the sequence is kept in order.</param>
    /// <param name="cache">Use EventCaching options to store this event for players who join.</param>
    /// <param name="receivers">ReceiverGroup defines to which group of players the event is passed on.</param>
    /// <returns>If the operation could be sent (has to be connected).</returns>
    public virtual bool OpRaiseEvent(byte eventCode, Hashtable evData, bool sendReliable, byte channelId, EventCaching cache, ReceiverGroup receivers)
    {
        if (this.DebugOut >= DebugLevel.INFO)
        {
            this.Listener.DebugReturn(DebugLevel.INFO, "OpRaiseEvent()");
        }

        Dictionary<byte, object> opParameters = new Dictionary<byte, object>();
        opParameters[ParameterCode.Data] = evData;
        opParameters[ParameterCode.Code] = (byte)eventCode;

        if (receivers != ReceiverGroup.Others)
        {
            opParameters[ParameterCode.ReceiverGroup] = (byte)receivers;
        }

        if (cache != EventCaching.DoNotCache)
        {
            opParameters[ParameterCode.Cache] = (byte)cache;
        }

        return this.OpCustom((byte)OperationCode.RaiseEvent, opParameters, sendReliable, channelId);
    }
}

/// <summary>
/// Class for constants. These (int) values represent error codes, as defined and sent by the Photon LoadBalancing logic.
/// Pun uses these constants internally.
/// </summary>
/// <note>Codes from the Photon Core are negative. Default-app error codes go down from short.max.</note>
public class ErrorCode
{
    /// <summary>(0) is always "OK", anything else an error or specific situation.</summary>
    public const int Ok = 0;

    // server - Photon low(er) level: <= 0

    /// <summary>
    /// (-3) Operation can't be executed yet (e.g. OpJoin can't be called before being authenticated, RaiseEvent cant be used before getting into a room).
    /// </summary>
    /// <remarks>
    /// Before you call any operations on the Cloud servers, the automated client workflow must complete its authorization.
    /// In PUN, wait until State is: JoinedLobby (with AutoJoinLobby = true) or ConnectedToMaster (AutoJoinLobby = false)
    /// </remarks>
    public const int OperationNotAllowedInCurrentState = -3;

    /// <summary>(-2) The operation you called is not implemented on the server (application) you connect to. Make sure you run the fitting applications.</summary>
    public const int InvalidOperationCode = -2;

    /// <summary>(-1) Something went wrong in the server. Try to reproduce and contact Exit Games.</summary>
    public const int InternalServerError = -1;

    // server - PhotonNetwork: 0x7FFF and down
    // logic-level error codes start with short.max

    /// <summary>(32767) Authentication failed. Possible cause: AppId is unknown to Photon (in cloud service).</summary>
    public const int InvalidAuthentication = 0x7FFF;

    /// <summary>(32766) GameId (name) already in use (can't create another). Change name.</summary>
    public const int GameIdAlreadyExists = 0x7FFF - 1;

    /// <summary>(32765) Game is full. This can when players took over while you joined the game.</summary>
    public const int GameFull = 0x7FFF - 2;

    /// <summary>(32764) Game is closed and can't be joined. Join another game.</summary>
    public const int GameClosed = 0x7FFF - 3;

    [Obsolete("No longer used, cause random matchmaking is no longer a process.")]
    public const int AlreadyMatched = 0x7FFF - 4;

    /// <summary>(32762) Not in use currently.</summary>
    public const int ServerFull = 0x7FFF - 5;

    /// <summary>(32761) Not in use currently.</summary>
    public const int UserBlocked = 0x7FFF - 6;

    /// <summary>(32760) Random matchmaking only succeeds if a room exists thats neither closed nor full. Repeat in a few seconds or create a new room.</summary>
    public const int NoRandomMatchFound = 0x7FFF - 7;

    /// <summary>(32758) Join can fail if the room (name) is not existing (anymore). This can happen when players leave while you join.</summary>
    public const int GameDoesNotExist = 0x7FFF - 9;

    /// <summary>(32757) Authorization on the Photon Cloud failed because the concurrent users (CCU) limit of the app's subscription is reached.</summary>
    /// <remarks>
    /// Unless you have a plan with "CCU Burst", clients might fail the authentication step during connect. 
    /// Affected client are unable to call operations. Please note that players who end a game and return 
    /// to the master server will disconnect and re-connect, which means that they just played and are rejected 
    /// in the next minute / re-connect.
    /// This is a temporary measure. Once the CCU is below the limit, players will be able to connect an play again.
    /// 
    /// OpAuthorize is part of connection workflow but only on the Photon Cloud, this error can happen. 
    /// Self-hosted Photon servers with a CCU limited license won't let a client connect at all.
    /// </remarks>
    public const int MaxCcuReached = 0x7FFF - 10;

    /// <summary>(32756) Authorization on the Photon Cloud failed because the app's subscription does not allow to use a particular region's server.</summary>
    /// <remarks>
    /// Some subscription plans for the Photon Cloud are region-bound. Servers of other regions can't be used then.
    /// Check your master server address and compare it with your Photon Cloud Dashboard's info.
    /// https://cloud.exitgames.com/dashboard
    /// 
    /// OpAuthorize is part of connection workflow but only on the Photon Cloud, this error can happen. 
    /// Self-hosted Photon servers with a CCU limited license won't let a client connect at all.
    /// </remarks>
    public const int InvalidRegion = 0x7FFF - 11;
}


/// <summary>
/// Class for constants. These (byte) values define "well known" properties for an Actor / Player.
/// Pun uses these constants internally.
/// </summary>
/// <remarks>
/// "Custom properties" have to use a string-type as key. They can be assigned at will.
/// </remarks>
public class ActorProperties
{
    /// <summary>(255) Name of a player/actor.</summary>
    public const byte PlayerName = 255; // was: 1
}

/// <summary>
/// Class for constants. These (byte) values are for "well known" room/game properties used in Photon Loadbalancing.
/// Pun uses these constants internally.
/// </summary>
/// <remarks>
/// "Custom properties" have to use a string-type as key. They can be assigned at will.
/// </remarks>
public class GameProperties
{
    /// <summary>(255) Max number of players that "fit" into this room. 0 is for "unlimited".</summary>
    public const byte MaxPlayers = 255;
    /// <summary>(254) Makes this room listed or not in the lobby on master.</summary>
    public const byte IsVisible = 254;
    /// <summary>(253) Allows more players to join a room (or not).</summary>
    public const byte IsOpen = 253;
    /// <summary>(252) Current count od players in the room. Used only in the lobby on master.</summary>
    public const byte PlayerCount = 252;
    /// <summary>(251) True if the room is to be removed from room listing (used in update to room list in lobby on master)</summary>
    public const byte Removed = 251;
    /// <summary>(250) A list of the room properties to pass to the RoomInfo list in a lobby. This is used in CreateRoom, which defines this list once per room.</summary>
    public const byte PropsListedInLobby = 250;
    /// <summary>Equivalent of Operation Join parameter CleanupCacheOnLeave.</summary>
    public const byte CleanupCacheOnLeave = 249;
}

/// <summary>
/// Class for constants. These values are for events defined by Photon Loadbalancing.
/// Pun uses these constants internally.
/// </summary>
/// <remarks>They start at 255 and go DOWN. Your own in-game events can start at 0.</remarks>
public class EventCode
{
    /// <summary>(230) Initial list of RoomInfos (in lobby on Master)</summary>
    public const byte GameList = 230;
    /// <summary>(229) Update of RoomInfos to be merged into "initial" list (in lobby on Master)</summary>
    public const byte GameListUpdate = 229;
    /// <summary>(228) Currently not used. State of queueing in case of server-full</summary>
    public const byte QueueState = 228;
    /// <summary>(227) Currently not used. Event for matchmaking</summary>
    public const byte Match = 227;
    /// <summary>(226) Event with stats about this application (players, rooms, etc)</summary>
    public const byte AppStats = 226;
    /// <summary>(210) Internally used in case of hosting by Azure</summary>
    public const byte AzureNodeInfo = 210;
    /// <summary>(255) Event Join: someone joined the game. The new actorNumber is provided as well as the properties of that actor (if set in OpJoin).</summary>
    public const byte Join = (byte)LiteEventCode.Join;
    /// <summary>(254) Event Leave: The player who left the game can be identified by the actorNumber.</summary>
    public const byte Leave = (byte)LiteEventCode.Leave;
    /// <summary>(253) When you call OpSetProperties with the broadcast option "on", this event is fired. It contains the properties being set.</summary>
    public const byte PropertiesChanged = (byte)LiteEventCode.PropertiesChanged;
    /// <summary>(253) When you call OpSetProperties with the broadcast option "on", this event is fired. It contains the properties being set.</summary>
    [Obsolete("Use PropertiesChanged now.")]
    public const byte SetProperties = (byte)LiteEventCode.PropertiesChanged;
}

/// <summary>
/// Class for constants. Codes for parameters of Operations and Events.
/// Pun uses these constants internally.
/// </summary>
public class ParameterCode
{
    /// <summary>(230) Address of a (game) server to use.</summary>
    public const byte Address = 230;
    /// <summary>(229) Count of players in rooms (connected to game servers for this application, used in stats event)</summary>
    public const byte PeerCount = 229;
    /// <summary>(228) Count of games in this application (used in stats event)</summary>
    public const byte GameCount = 228;
    /// <summary>(227) Count of players on the master server (connected to master server for this application, looking for games, used in stats event)</summary>
    public const byte MasterPeerCount = 227;
    /// <summary>(225) User's ID</summary>
    public const byte UserId = 225;
    /// <summary>(224) Your application's ID: a name on your own Photon or a GUID on the Photon Cloud</summary>
    public const byte ApplicationId = 224;
    /// <summary>(223) Not used (as "Position" currently). If you get queued before connect, this is your position</summary>
    public const byte Position = 223;
    /// <summary>(223) Modifies the matchmaking algorithm used for OpJoinRandom. Allowed parameter values are defined in enum MatchmakingMode.</summary>
    public const byte MatchMakingType = 223;
    /// <summary>(222) List of RoomInfos about open / listed rooms</summary>
    public const byte GameList = 222;
    /// <summary>(221) Internally used to establish encryption</summary>
    public const byte Secret = 221;
    /// <summary>(220) Version of your application</summary>
    public const byte AppVersion = 220;
    /// <summary>(210) Internally used in case of hosting by Azure</summary>
    public const byte AzureNodeInfo = 210;	// only used within events, so use: EventCode.AzureNodeInfo
    /// <summary>(209) Internally used in case of hosting by Azure</summary>
    public const byte AzureLocalNodeId = 209;
    /// <summary>(208) Internally used in case of hosting by Azure</summary>
    public const byte AzureMasterNodeId = 208;

    /// <summary>(255) Code for the gameId/roomName (a unique name per room). Used in OpJoin and similar.</summary>
    public const byte RoomName = (byte)LiteOpKey.GameId;
    /// <summary>(250) Code for broadcast parameter of OpSetProperties method.</summary>
    public const byte Broadcast = (byte)LiteOpKey.Broadcast;
    /// <summary>(252) Code for list of players in a room. Currently not used.</summary>
    public const byte ActorList = (byte)LiteOpKey.ActorList;
    /// <summary>(254) Code of the Actor of an operation. Used for property get and set.</summary>
    public const byte ActorNr = (byte)LiteOpKey.ActorNr;
    /// <summary>(249) Code for property set (Hashtable).</summary>
    public const byte PlayerProperties = (byte)LiteOpKey.ActorProperties;
    /// <summary>(245) Code of data/custom content of an event. Used in OpRaiseEvent.</summary>
    public const byte CustomEventContent = (byte)LiteOpKey.Data;
    /// <summary>(245) Code of data of an event. Used in OpRaiseEvent.</summary>
    public const byte Data = (byte)LiteOpKey.Data;
    /// <summary>(244) Code used when sending some code-related parameter, like OpRaiseEvent's event-code.</summary>
    /// <remarks>This is not the same as the Operation's code, which is no longer sent as part of the parameter Dictionary in Photon 3.</remarks>
    public const byte Code = (byte)LiteOpKey.Code;
    /// <summary>(248) Code for property set (Hashtable).</summary>
    public const byte GameProperties = (byte)LiteOpKey.GameProperties;
    /// <summary>
    /// (251) Code for property-set (Hashtable). This key is used when sending only one set of properties.
    /// If either ActorProperties or GameProperties are used (or both), check those keys.
    /// </summary>
    public const byte Properties = (byte)LiteOpKey.Properties;
    /// <summary>(253) Code of the target Actor of an operation. Used for property set. Is 0 for game</summary>
    public const byte TargetActorNr = (byte)LiteOpKey.TargetActorNr;
    /// <summary>(246) Code to select the receivers of events (used in Lite, Operation RaiseEvent).</summary>
    public const byte ReceiverGroup = (byte)LiteOpKey.ReceiverGroup;
    /// <summary>(247) Code for caching events while raising them.</summary>
    public const byte Cache = (byte)LiteOpKey.Cache;
    /// <summary>(241) Bool parameter of CreateGame Operation. If true, server cleans up roomcache of leaving players (their cached events get removed).</summary>
    public const byte CleanupCacheOnLeave = (byte)241;

    /// <summary>(240) Code for "group" operation-parameter (as used in Op RaiseEvent).</summary>
    public const byte Group = LiteOpKey.Group;
    /// <summary>(239) The "Remove" operation-parameter can be used to remove something from a list. E.g. remove groups from player's interest groups.</summary>
    public const byte Remove = LiteOpKey.Remove;
    /// <summary>(238) The "Add" operation-parameter can be used to add something to some list or set. E.g. add groups to player's interest groups.</summary>
    public const byte Add = LiteOpKey.Add;
}

/// <summary>
/// Class for constants. Contains operation codes.
/// Pun uses these constants internally.
/// </summary>
public class OperationCode
{
    /// <summary>(230) Authenticates this peer and connects to a virtual application</summary>
    public const byte Authenticate = 230;
    /// <summary>(229) Joins lobby (on master)</summary>
    public const byte JoinLobby = 229;
    /// <summary>(228) Leaves lobby (on master)</summary>
    public const byte LeaveLobby = 228;
    /// <summary>(227) Creates a game (or fails if name exists)</summary>
    public const byte CreateGame = 227;
    /// <summary>(226) Join game (by name)</summary>
    public const byte JoinGame = 226;
    /// <summary>(225) Joins random game (on master)</summary>
    public const byte JoinRandomGame = 225;

    // public const byte CancelJoinRandom = 224; // obsolete, cause JoinRandom no longer is a "process". now provides result immediately

    /// <summary>(254) Code for OpLeave, to get out of a room.</summary>
    public const byte Leave = (byte)LiteOpCode.Leave;
    /// <summary>(253) Raise event (in a room, for other actors/players)</summary>
    public const byte RaiseEvent = (byte)LiteOpCode.RaiseEvent;
    /// <summary>(252) Set Properties (of room or actor/player)</summary>
    public const byte SetProperties = (byte)LiteOpCode.SetProperties;
    /// <summary>(251) Get Properties</summary>
    public const byte GetProperties = (byte)LiteOpCode.GetProperties;

    /// <summary>(248) Operation code to change interest groups in Rooms (Lite application and extending ones).</summary>
    public const byte ChangeGroups = (byte)LiteOpCode.ChangeGroups;
}

/// <summary>
/// Options for matchmaking rules for OpJoinRandom.
/// </summary>
public enum MatchmakingMode : byte
{
    /// <summary>Fills up rooms (oldest first) to get players together as fast as possible. Default.</summary>
    /// <remarks>Makes most sense with MaxPlayers > 0 and games that can only start with more players.</remarks>
    FillRoom = 0,
    /// <summary>Distributes players across available rooms sequentially but takes filter into account. Without filter, rooms get players evenly distributed.</summary>
    SerialMatching = 1,
    /// <summary>Joins a (fully) random room. Expected properties must match but aside from this, any available room might be selected.</summary>
    RandomMatching = 2
}
