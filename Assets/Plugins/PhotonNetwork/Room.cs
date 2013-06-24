// ----------------------------------------------------------------------------
// <copyright file="Room.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   Represents a room/game on the server and caches the properties of that.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------
using System;
using System.Collections;
using ExitGames.Client.Photon;

/// <summary>
/// This class resembles a room that PUN joins (or joined).
/// The properties are settable as opposed to those of a RoomInfo and you can close or hide "your" room.
/// </summary>
/// \ingroup publicApi
public class Room : RoomInfo
{
    /// <summary>Count of players in this room.</summary>
    public new int playerCount
    {
        get
        {
            if (PhotonNetwork.playerList != null)
            {
                return PhotonNetwork.playerList.Length;
            }
            else
            {
                return 0;
            }
        }
    }


    /// <summary>The name of a room. Unique identifier (per Loadbalancing group) for a room/match.</summary>
    public new string name
    {
        get
        {
            return this.nameField;
        }

        internal set
        {
            this.nameField = value;
        }
    }
    
    /// <summary>
    /// Sets a limit of players to this room. This property is shown in lobby, too.
    /// If the room is full (players count == maxplayers), joining this room will fail.
    /// </summary>
    public new int maxPlayers
    {
        get
        {
            return (int)this.maxPlayersField;
        }

        set
        {
            if (!this.Equals(PhotonNetwork.room))
            {
                PhotonNetwork.networkingPeer.DebugReturn(DebugLevel.WARNING, "Can't set room properties when not in that room.");
            }

            if (value > 255)
            {
                UnityEngine.Debug.LogError("Error: room.maxPlayers called with value " + value + ". This has been reverted to the max of 255 players, because internally a 'byte' is used.");
                value = 255;
            }

            if (value != this.maxPlayersField && !PhotonNetwork.offlineMode)
            {
                PhotonNetwork.networkingPeer.OpSetPropertiesOfRoom(new Hashtable() { { GameProperties.MaxPlayers, (byte)value } }, true, (byte)0);
            }

            this.maxPlayersField = (byte)value;
        }
    }

    /// <summary>
    /// Defines if the room can be joined.
    /// This does not affect listing in a lobby but joining the room will fail if not open.
    /// If not open, the room is excluded from random matchmaking. 
    /// Due to racing conditions, found matches might become closed before they are joined. 
    /// Simply re-connect to master and find another.
    /// Use property "visible" to not list the room.
    /// </summary>
    public new bool open
    {
        get
        {
            return this.openField;
        }

        set
        {
            if (!this.Equals(PhotonNetwork.room))
            {
                PhotonNetwork.networkingPeer.DebugReturn(DebugLevel.WARNING, "Can't set room properties when not in that room.");
            }

            if (value != this.openField && !PhotonNetwork.offlineMode)
            {
                PhotonNetwork.networkingPeer.OpSetPropertiesOfRoom(new Hashtable() { { GameProperties.IsOpen, value } }, true, (byte)0);
            }

            this.openField = value;
        }
    }

    /// <summary>
    /// Defines if the room is listed in its lobby.
    /// Rooms can be created invisible, or changed to invisible.
    /// To change if a room can be joined, use property: open.
    /// </summary>
    public new bool visible
    {
        get
        {
            return this.visibleField;
        }

        set
        {
            if (!this.Equals(PhotonNetwork.room))
            {
                PhotonNetwork.networkingPeer.DebugReturn(DebugLevel.WARNING, "Can't set room properties when not in that room.");
            }

            if (value != this.visibleField && !PhotonNetwork.offlineMode)
            {
                PhotonNetwork.networkingPeer.OpSetPropertiesOfRoom(new Hashtable() { { GameProperties.IsVisible, value } }, true, (byte)0);
            }

            this.visibleField = value;
        }
    }

    /// <summary>
    /// A list of custom properties that should be forwarded to the lobby and listed there.
    /// </summary>
    public string[] propertiesListedInLobby { get; private set; }

    /// <summary>
    /// Gets if this room uses autoCleanUp to remove all (buffered) RPCs and instantiated GameObjects when a player leaves.
    /// </summary>
    public bool autoCleanUp
    {
        get
        {
            return this.autoCleanUpField;
        }
    }

    internal Room(string roomName, Hashtable properties) : base(roomName, properties)
    {
        this.propertiesListedInLobby = new string[0];
    }

    internal Room(string roomName, Hashtable properties, bool isVisible, bool isOpen, int maxPlayers, bool autoCleanUp, string[] propsListedInLobby) : base(roomName, properties)
    {
        this.visibleField = isVisible;
        this.openField = isOpen;
        this.autoCleanUpField = autoCleanUp;

        if (maxPlayers > 255)
        {
            UnityEngine.Debug.LogError("Error: Room() called with " + maxPlayers + " maxplayers. This has been reverted to the max of 255 players, because internally a 'byte' is used.");
            maxPlayers = 255;
        }

        this.maxPlayersField = (byte)maxPlayers;

        if (propsListedInLobby != null)
        {
            this.propertiesListedInLobby = propsListedInLobby;
        }
        else
        {
            this.propertiesListedInLobby = new string[0];
        }
    }

    /// <summary>
    /// Updates and synchronizes the named properties of this Room with the values of propertiesToSet.
    /// </summary>
    /// <remarks>
    /// Any player can set a Room's properties. Room properties are available until changed, deleted or 
    /// until the last player leaves the room.
    /// Access them by: Room.CustomProperties (read-only!).
    /// 
    /// New properties are added, existing values are updated.
    /// Other values will not be changed, so only provide values that changed or are new.
    /// To delete a named (custom) property of this room, use null as value.
    /// Only string-typed keys are applied (everything else is ignored).
    /// 
    /// Local cache is updated immediately, other clients are updated through Photon with a fitting operation.
    /// To reduce network traffic, set only values that actually changed.
    /// </remarks>
    /// <param name="propertiesToSet">Hashtable of props to udpate, set and sync. See description.</param>
    public void SetCustomProperties(Hashtable propertiesToSet)
    {
        if (propertiesToSet == null)
        {
            return;
        }

        // merge (delete null-values)
        this.customProperties.MergeStringKeys(propertiesToSet); // includes a Equals check (simplifying things)
        this.customProperties.StripKeysWithNullValues();

        // send (sync) these new values
        Hashtable customProps = propertiesToSet.StripToStringKeys() as Hashtable;
        PhotonNetwork.networkingPeer.OpSetCustomPropertiesOfRoom(customProps, true, 0);
    }
}