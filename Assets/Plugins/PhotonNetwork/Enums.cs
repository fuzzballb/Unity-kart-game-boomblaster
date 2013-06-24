// ----------------------------------------------------------------------------
// <copyright file="Enums.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

using ExitGames.Client.Photon;

/// <summary>
/// High level connection state of the client. Better use the more detailed <see cref="PeerState"/>.
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting,
    InitializingApplication
}

/// <summary>
/// Detailed connection / networking peer state.
/// PUN implements a loadbalancing and authentication workflow "behind the scenes", so
/// some states will automatically advance to some follow up state. Those states are 
/// commented with "(will-change)".
/// </summary>
/// \ingroup publicApi
public enum PeerState
{
    /// <summary>Not running. Only set before initialization and first use.</summary>
    Uninitialized,

    /// <summary>Created and available to connect.</summary>
    PeerCreated,

    /// <summary>Working to establish the initial connection to the master server (until this process is finished, no operations can be sent).</summary>
    /// <remarks>(will-change)</remarks>
    Connecting,

    /// <summary>Connection is setup, now PUN will exchange keys for encryption or authenticate.</summary>
    /// <remarks>(will-change)</remarks>
    Connected,

    /// <summary>Not used at the moment.</summary>
    Queued,

    /// <summary>The application is authenticated. PUN usually joins the lobby now.</summary>
    /// <remarks>(will-change) Unless AutoJoinLobby is false.</remarks>
    Authenticated,

    /// <summary>Client is in the lobby of the Master Server and gets room listings.</summary>
    /// <remarks>Use Join, Create or JoinRandom to get into a room to play.</remarks>
    JoinedLobby,

    /// <summary>Disconnecting.</summary>
    /// <remarks>(will-change)</remarks>
    DisconnectingFromMasterserver,

    /// <summary>Connecting to game server (to join/create a room and play).</summary>
    /// <remarks>(will-change)</remarks>
    ConnectingToGameserver,

    /// <summary>Similar to Connected state but on game server. Still in process to join/create room.</summary>
    /// <remarks>(will-change)</remarks>
    ConnectedToGameserver,

    /// <summary>In process to join/create room (on game server).</summary>
    /// <remarks>(will-change)</remarks>
    Joining,

    /// <summary>Final state of a room join/create sequence. This client can now exchange events / call RPCs with other clients.</summary>
    Joined,

    /// <summary>Leaving a room.</summary>
    /// <remarks>(will-change)</remarks>
    Leaving,

    /// <summary>Workflow is leaving the game server and will re-connect to the master server.</summary>
    /// <remarks>(will-change)</remarks>
    DisconnectingFromGameserver,

    /// <summary>Workflow is connected to master server and will establish encryption and authenticate your app.</summary>
    /// <remarks>(will-change)</remarks>
    ConnectingToMasterserver,

    /// <summary>Same as Connected but coming from game server.</summary>
    /// <remarks>(will-change)</remarks>
    ConnectedComingFromGameserver,

    /// <summary>Same Queued but coming from game server.</summary>
    /// <remarks>(will-change)</remarks>
    QueuedComingFromGameserver,

    /// <summary>PUN is disconnecting. This leads to Disconnected.</summary>
    /// <remarks>(will-change)</remarks>
    Disconnecting,

    /// <summary>No connection is setup, ready to connect. Similar to PeerCreated.</summary>
    Disconnected,

    /// <summary>Final state for connecting to master without joining the lobby (AutoJoinLobby is false).</summary>
    ConnectedToMaster
}

/// <summary>
/// Internal state how this peer gets into a particular room (joining it or creating it).
/// </summary>
internal enum JoinType
{
    CreateGame,
    JoinGame,
    JoinRandomGame
}


// Photon properties, internally set by PhotonNetwork (PhotonNetwork builtin properties)

/// <summary>
/// This enum makes up the set of MonoMessages sent by Photon Unity Networking.
/// Implement any of these constant names as method and it will be called
/// in the respective situation.
/// </summary>
/// <example>
/// Implement: 
/// public void OnLeftRoom() { //some work }
/// </example>
/// \ingroup publicApi
public enum PhotonNetworkingMessage
{
    /// <summary>
    /// Called when the server is available and before client authenticates. Wait for the call to OnJoinedLobby (or OnConnectedToMaster) before the client does anything!
    /// Example: void OnConnectedToPhoton(){ ... }
    /// </summary>
    /// <remarks>This is not called for transitions from the masterserver to game servers, which is hidden for PUN users.</remarks>
    OnConnectedToPhoton,

    /// <summary>
    /// Called once the local user left a room.
    /// Example: void OnLeftRoom(){ ... }
    /// </summary>
    OnLeftRoom,

    /// <summary>
    /// Called -after- switching to a new MasterClient because the previous MC left the room (not when getting into a room). The last MC will already be removed at this time.
    /// Example: void OnMasterClientSwitched(PhotonPlayer newMasterClient){ ... }
    /// </summary>
    OnMasterClientSwitched,

    /// <summary>
    /// Called if a CreateRoom() call failed. Most likely because the room name is already in use.
    /// Example: void OnPhotonCreateRoomFailed(){ ... }
    /// </summary>
    OnPhotonCreateRoomFailed,

    /// <summary>
    /// Called if a JoinRoom() call failed. Most likely because the room does not exist or the room is full.
    /// Example: void OnPhotonJoinRoomFailed(){ ... }
    /// </summary>
    OnPhotonJoinRoomFailed,

    /// <summary>
    /// Called when CreateRoom finishes creating the room. After this, OnJoinedRoom will be called, too (no matter if creating one or joining).
    /// Example: void OnCreatedRoom(){ ... }
    /// </summary>
    /// <remarks>This implies the local client is the MasterClient.</remarks>
    OnCreatedRoom,

    /// <summary>
    /// Called on entering the Master Server's lobby. Client can create/join rooms but room list is not available until OnReceivedRoomListUpdate is called!
    /// Example: void OnJoinedLobby(){ ... }
    /// </summary>
    /// <remarks>
    /// Note: When PhotonNetwork.autoJoinLobby is false, OnConnectedToMaster will be called instead and the room list won't be available.
    /// While in the lobby, the roomlist is automatically updated in fixed intervals (which you can't modify).
    /// </remarks>
    OnJoinedLobby,

    /// <summary>
    /// Called after leaving the lobby.
    /// Example: void OnLeftLobby(){ ... }
    /// </summary>
    OnLeftLobby,

    /// <summary>
    /// Called after disconnecting from the Photon server. 
    /// In some cases, other events are sent before OnDisconnectedFromPhoton is called. Examples: OnConnectionFail and OnFailedToConnectToPhoton.
    /// Example: void OnDisconnectedFromPhoton(){ ... }
    /// </summary>
    OnDisconnectedFromPhoton,

    /// <summary>
    /// Called when something causes the connection to fail (after it was established), followed by a call to OnDisconnectedFromPhoton.
    /// If the server could not be reached in the first place, OnFailedToConnectToPhoton is called instead.
    /// The reason for the error is provided as StatusCode.
    /// Example: void OnConnectionFail(DisconnectCause cause){ ... }
    /// </summary>
    OnConnectionFail,

    /// <summary>
    /// Called if a connect call to the Photon server failed before the connection was established, followed by a call to OnDisconnectedFromPhoton.
    /// If the connection was established but then fails, OnConnectionFail is called.
    /// Example: void OnFailedToConnectToPhoton(DisconnectCause cause){ ... }
    /// </summary>
    OnFailedToConnectToPhoton,

    /// <summary>
    /// Called for any update of the room listing (no matter if "new" list or "update for known" list). Only called in the Lobby state (on master server).
    /// Example: void OnReceivedRoomListUpdate(){ ... }
    /// </summary>
    OnReceivedRoomListUpdate,

    /// <summary>
    /// Called when entering a room (by creating or joining it). Called on all clients (including the Master Client).
    /// Example: void OnJoinedRoom(){ ... }
    /// </summary>
    OnJoinedRoom,

    /// <summary>
    /// Called after a remote player connected to the room. This PhotonPlayer is already added to the playerlist at this time.
    /// Example: void OnPhotonPlayerConnected(PhotonPlayer newPlayer){ ... }
    /// </summary>
    OnPhotonPlayerConnected,

    /// <summary>
    /// Called after a remote player disconnected from the room. This PhotonPlayer is already removed from the playerlist at this time.
    /// Example: void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer){ ... }
    /// </summary>
    OnPhotonPlayerDisconnected,

    /// <summary>
    /// Called after a JoinRandom() call failed. Most likely all rooms are full or no rooms are available.
    /// Example: void OnPhotonRandomJoinFailed(){ ... }
    /// </summary>
    OnPhotonRandomJoinFailed,

    /// <summary>
    /// Called after the connection to the master is established and authenticated but only when PhotonNetwork.AutoJoinLobby is false.
    /// If AutoJoinLobby is false, the list of available rooms won't become available but you could join (random or by name) and create rooms anyways.
    /// Example: void OnConnectedToMaster(){ ... }
    /// </summary>
    OnConnectedToMaster,

    /// <summary>
    /// Called every network 'update' on MonoBehaviours that are being observed by a PhotonView.
    /// Example: void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info){ ... }
    /// </summary>
    OnPhotonSerializeView,

    /// <summary>
    /// Called on all scripts on a GameObject(and it's children) that have been spawned using PhotonNetwork.Instantiate
    /// Example: void OnPhotonInstantiate(PhotonMessageInfo info){ ... }
    /// </summary>
    OnPhotonInstantiate,

    /// <summary>
    /// Because the concurrent user limit was (temporarily) reached, this client is rejected by the server and disconnecting.
    /// </summary>
    /// <remarks>
    /// When this happens, the user might try again later. You can't create or join rooms in OnPhotonMaxCcuReached(), cause the client will be disconnecting.
    /// You can raise the CCU limits with a new license (when you host yourself) or extended subscription (when using the Photon Cloud).
    /// The Photon Cloud will mail you when the CCU limit was reached. This is also visible in the Dashboard (webpage).
    /// Example: void OnPhotonMaxCccuReached(){ ... }
    /// </remarks>
    OnPhotonMaxCccuReached
}

/// <summary>
/// Summarizes the cause for a disconnect. Used in: OnConnectionFail and OnFailedToConnectToPhoton.
/// </summary>
/// <remarks>Extracted from the status codes from ExitGames.Client.Photon.StatusCode.</remarks>
/// <seealso cref="PhotonNetworkingMessage"/>
/// \ingroup publicApi
public enum DisconnectCause
{
    /// <summary>Connection could not be established.
    /// Possible cause: Local server not running.</summary>
    ExceptionOnConnect = StatusCode.ExceptionOnConnect,

    /// <summary>Connection timed out.
    /// Possible cause: Remote server not running or required ports blocked (due to router or firewall).</summary>
    TimeoutDisconnect = StatusCode.TimeoutDisconnect,
    
    /// <summary>Exception in the receive-loop.
    /// Possible cause: Socket failure.</summary>
    InternalReceiveException = StatusCode.InternalReceiveException,

    /// <summary>Server actively disconnected this client.</summary>
    DisconnectByServer = StatusCode.DisconnectByServer,

    /// <summary>Server actively disconnected this client.
    /// Possible cause: Server's send buffer full (too much data for client).</summary>
    DisconnectByServerLogic = StatusCode.DisconnectByServerLogic,

    /// <summary>Server actively disconnected this client. 
    /// Possible cause: The server's user limit was hit and client was forced to disconnect (on connect).</summary>
    DisconnectByServerUserLimit = StatusCode.DisconnectByServerUserLimit,

    /// <summary>Some exception caused the connection to close.</summary>
    Exception = StatusCode.Exception,

    /// <summary>(32756) Authorization on the Photon Cloud failed because the app's subscription does not allow to use a particular region's server.</summary>
    InvalidRegion = ErrorCode.InvalidRegion,

    /// <summary>(32757) Authorization on the Photon Cloud failed because the concurrent users (CCU) limit of the app's subscription is reached.</summary>
    MaxCcuReached = ErrorCode.MaxCcuReached,
}