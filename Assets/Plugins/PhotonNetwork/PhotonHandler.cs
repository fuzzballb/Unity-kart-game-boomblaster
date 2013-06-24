// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PhotonHandler.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Networking
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;

/// <summary>
/// Internal Monobehaviour that allows Photon to run an Update loop.
/// </summary>
internal class PhotonHandler : Photon.MonoBehaviour, IPhotonPeerListener
{
    public static PhotonHandler SP;

    public int updateInterval;  // time [ms] between consecutive SendOutgoingCommands calls

    public int updateIntervalOnSerialize;  // time [ms] between consecutive RunViewUpdate calls (sending syncs, etc)

    private int nextSendTickCount = 0;

    private int nextSendTickCountOnSerialize = 0;

    private void Awake()
    {
        if (SP != null && SP != this && SP.gameObject != null)
        {
            GameObject.DestroyImmediate(SP.gameObject);
        }

        SP = this;
        DontDestroyOnLoad(this.gameObject);

        this.updateInterval = 1000 / PhotonNetwork.sendRate;
        this.updateIntervalOnSerialize = 1000 / PhotonNetwork.sendRateOnSerialize;
    }

    private void Update()
    {
        if (PhotonNetwork.networkingPeer == null)
        {
            Debug.LogError("NetworkPeer broke!");
            return;
        }

        if (PhotonNetwork.connectionStateDetailed == PeerState.PeerCreated || PhotonNetwork.connectionStateDetailed == PeerState.Disconnected)
        {
            return;
        }

        // the messageQueue might be paused. in that case a thread will send acknowledgements only. nothing else to do here.
        if (!PhotonNetwork.isMessageQueueRunning)
        {
            return;
        }

        bool doDispatch = true;
        while (PhotonNetwork.isMessageQueueRunning && doDispatch)
        {
            // DispatchIncomingCommands() returns true of it found any command to dispatch (event, result or state change)
            Profiler.BeginSample("DispatchIncomingCommands");
            doDispatch = PhotonNetwork.networkingPeer.DispatchIncomingCommands();
            Profiler.EndSample();
        }

        int currentMsSinceStart = (int)(Time.realtimeSinceStartup * 1000);  // avoiding Environment.TickCount, which could be negative on long-running platforms
        if (PhotonNetwork.isMessageQueueRunning && currentMsSinceStart > this.nextSendTickCountOnSerialize)
        {
            PhotonNetwork.networkingPeer.RunViewUpdate();
            this.nextSendTickCountOnSerialize = currentMsSinceStart + this.updateIntervalOnSerialize;
            this.nextSendTickCount = 0;     // immediately send when synchronization code was running
        }

        currentMsSinceStart = (int)(Time.realtimeSinceStartup * 1000);
        if (currentMsSinceStart > this.nextSendTickCount)
        {
            bool doSend = true;
            while (PhotonNetwork.isMessageQueueRunning && doSend)
            {
                // Send all outgoing commands
                Profiler.BeginSample("SendOutgoingCommands");
                doSend = PhotonNetwork.networkingPeer.SendOutgoingCommands();
                Profiler.EndSample();
            }

            this.nextSendTickCount = currentMsSinceStart + this.updateInterval;
        }
    }

    /// <summary>Called by Unity when the application is closed. Tries to disconnect.</summary>
    protected void OnApplicationQuit()
    {
        PhotonNetwork.Disconnect();
    }

    /// <summary>Called by Unity after a new level was loaded.</summary>
    protected void OnLevelWasLoaded(int level)
    {
        PhotonNetwork.networkingPeer.NewSceneLoaded();

        if (PhotonNetwork.automaticallySyncScene)
        {
            this.SetSceneInProps();
        }
    }

    protected void OnJoinedRoom()
    {
        PhotonNetwork.networkingPeer.AutomaticallySyncScene();
    }

    protected void OnCreatedRoom()
    {
        if (PhotonNetwork.automaticallySyncScene)
        {
            this.SetSceneInProps();
        }
    }

    protected internal void SetSceneInProps()
    {
        if (PhotonNetwork.isMasterClient)
        {
            Hashtable setScene = new Hashtable();
            setScene[NetworkingPeer.CurrentSceneProperty] = Application.loadedLevelName;
            PhotonNetwork.room.SetCustomProperties(setScene);
        }
    }

    public static void StartThread()
    {
        System.Threading.Thread sendThread = new System.Threading.Thread(new System.Threading.ThreadStart(MyThread));
        sendThread.Start();
    }

    // keeps connection alive while loading
    public static void MyThread()
    {
        while (PhotonNetwork.networkingPeer != null && PhotonNetwork.networkingPeer.IsSendingOnlyAcks)
        {
            while (PhotonNetwork.networkingPeer.SendAcksOnly())
            {
            }

            System.Threading.Thread.Sleep(50);
        }
    }

    #region Implementation of IPhotonPeerListener

    public void DebugReturn(DebugLevel level, string message)
    {
        if (level == DebugLevel.ERROR)
        {
            Debug.LogError(message);
        }
        else if (level == DebugLevel.WARNING)
        {
            Debug.LogWarning(message);
        }
        else if (level == DebugLevel.INFO && PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
        {
            Debug.Log(message);
        }
        else if (level == DebugLevel.ALL && PhotonNetwork.logLevel == PhotonLogLevel.Full)
        {
            Debug.Log(message);
        }
    }

    public void OnOperationResponse(OperationResponse operationResponse)
    {
    }

    public void OnStatusChanged(StatusCode statusCode)
    {
    }

    public void OnEvent(EventData photonEvent)
    {
    }

    #endregion
}
