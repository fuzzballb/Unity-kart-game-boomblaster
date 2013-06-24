// ----------------------------------------------------------------------------
// <copyright file="PhotonView.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

public enum ViewSynchronization { Off, ReliableDeltaCompressed, Unreliable }
public enum OnSerializeTransform { OnlyPosition, OnlyRotation, OnlyScale, PositionAndRotation, All }
public enum OnSerializeRigidBody { OnlyVelocity, OnlyAngularVelocity, All }

/// <summary>
/// PUN's NetworkView replacement class for networking. Use it like a NetworkView.
/// </summary>
/// \ingroup publicApi
[AddComponentMenu("Miscellaneous/Photon View")]
public class PhotonView : Photon.MonoBehaviour
{
    public int subId;

    public int ownerId;
    
    public int group = 0;


    // NOTE: this is now an integer because unity won't serialize short (needed for instantiation). we SEND only a short though!
    // NOTE: prefabs have a prefixBackup of -1. this is replaced with any currentLevelPrefix that's used at runtime. instantiated GOs get their prefix set pre-instantiation (so those are not -1 anymore)
    public int prefix   
    {
        get
        {
            if (this.prefixBackup == -1 && PhotonNetwork.networkingPeer != null)
            {
                this.prefixBackup = PhotonNetwork.networkingPeer.currentLevelPrefix;
            }

            return this.prefixBackup;
        }
        set { this.prefixBackup = value; }
    } 

    // this field is serialized by unity. that means it is copied when instantiating a persistent obj into the scene
    public int prefixBackup = -1;

    /// <summary>
    /// This is the instantiationData that was passed when calling PhotonNetwork.Instantiate* (if that was used to spawn this prefab)
    /// </summary>
    public object[] instantiationData
    {
        get 
        {
            if (!this.didAwake)
            {
                // even though viewID and instantiationID are setup before the GO goes live, this data can't be set. as workaround: fetch it if needed
                this.instantiationDataField = PhotonNetwork.networkingPeer.FetchInstantiationData(this.instantiationId);
            }
            return this.instantiationDataField;
        }
        set { this.instantiationDataField = value; }
    }

    private object[] instantiationDataField;

    /// <summary>
    /// For internal use only, don't use
    /// </summary>
    protected internal object[] lastOnSerializeDataSent = null;
    
    /// <summary>
    /// For internal use only, don't use
    /// </summary>
    protected internal object[] lastOnSerializeDataReceived = null;

    public Component observed;

    public ViewSynchronization synchronization;
    
    public OnSerializeTransform onSerializeTransformOption = OnSerializeTransform.PositionAndRotation;
    
    public OnSerializeRigidBody onSerializeRigidBodyOption = OnSerializeRigidBody.All;

    public int viewID
    {
        get { return ownerId * PhotonNetwork.MAX_VIEW_IDS + subId; }
        set
        {
            // if ID was 0 for an awakened PhotonView, the view should add itself into the networkingPeer.photonViewList after setup
            bool viewMustRegister = this.didAwake && this.subId == 0;

            // TODO: decide if a viewID can be changed once it wasn't 0. most likely that is not a good idea
            // check if this view is in networkingPeer.photonViewList and UPDATE said list (so we don't keep the old viewID with a reference to this object)
            // PhotonNetwork.networkingPeer.RemovePhotonView(this, true);

            this.ownerId = value / PhotonNetwork.MAX_VIEW_IDS;

            this.subId = value % PhotonNetwork.MAX_VIEW_IDS;

            if (viewMustRegister)
            {
                PhotonNetwork.networkingPeer.RegisterPhotonView(this);
            }
            //Debug.Log("Set viewID: " + value + " ->  owner: " + this.ownerId + " subId: " + this.subId);
        }
    }

    public int instantiationId; // if the view was instantiated with a GO, this GO has a instantiationID (first view's viewID)

    public bool isSceneView
    {
        get { return this.ownerId == 0; }
    }

    public PhotonPlayer owner
    {
        get { return PhotonPlayer.Find(this.ownerId); }
    }

    public int OwnerActorNr
    {
        get { return this.ownerId; }
    }

    /// <summary>
    /// Is this photonView mine?
    /// True in case the owner matches the local PhotonPlayer
    /// ALSO true if this is a scene photonview on the Master client
    /// </summary>
    public bool isMine
    {
        get
        {
            return (this.ownerId == PhotonNetwork.player.ID) || (this.isSceneView && PhotonNetwork.isMasterClient);
        }
    }

    private bool didAwake;

    protected internal bool destroyedByPhotonNetworkOrQuit;

    /// <summary>Called by Unity on start of the application and does a setup the PhotonView.</summary>
    public void Awake()
    {
        // registration might be too late when some script (on this GO) searches this view BUT GetPhotonView() can search ALL in that case
        PhotonNetwork.networkingPeer.RegisterPhotonView(this);
        
        this.instantiationDataField = PhotonNetwork.networkingPeer.FetchInstantiationData(this.instantiationId);
        this.didAwake = true;
    }

    public void OnApplicationQuit()
    {
        destroyedByPhotonNetworkOrQuit = true;	// on stop-playing its ok Destroy is being called directly (not by PN.Destroy())
    }

    public void OnDestroy()
    {
        PhotonNetwork.networkingPeer.RemovePhotonView(this);

        if (!this.destroyedByPhotonNetworkOrQuit && !Application.isLoadingLevel)
        {
            if (this.instantiationId > 0)
            {
                // if this viewID was not manually assigned (and we're not shutting down or loading a level), you should use PhotonNetwork.Destroy() to get rid of GOs with PhotonViews
                Debug.LogError("OnDestroy() seems to be called without PhotonNetwork.Destroy()?! GameObject: " + this.gameObject + " Application.isLoadingLevel: " + Application.isLoadingLevel);
            }
            else
            {
                // this seems to be a manually instantiated PV. if it's local, we could warn if the ID is not in the allocated-list
                if (this.viewID <= 0)
                {
                    Debug.LogWarning(string.Format("OnDestroy manually allocated PhotonView {0}. The viewID is 0. Was it ever (manually) set?", this));
                } 
                else if (this.isMine && !PhotonNetwork.manuallyAllocatedViewIds.Contains(this.viewID))
                {
                    Debug.LogWarning(string.Format("OnDestroy manually allocated PhotonView {0}. The viewID is local (isMine) but not in manuallyAllocatedViewIds list. Use UnAllocateViewID() after you destroyed the PV.", this));
                }
            }
        }

        if (PhotonNetwork.networkingPeer.instantiatedObjects.ContainsKey(this.instantiationId))
        {
            Debug.LogWarning(string.Format("OnDestroy for PhotonView {0} but GO is still in instantiatedObjects. instantiationId: {1}. Use PhotonNetwork.Destroy(). {2}", this, this.instantiationId, Application.isLoadingLevel ? "Loading new scene caused this." : ""));
        }
    }

    public void RPC(string methodName, PhotonTargets target, params object[] parameters)
    {
        PhotonNetwork.RPC(this, methodName, target, parameters);
    }

    public void RPC(string methodName, PhotonPlayer targetPlayer, params object[] parameters)
    {
        PhotonNetwork.RPC(this, methodName, targetPlayer, parameters);
    }

    public static PhotonView Get(Component component)
    {
        return component.GetComponent<PhotonView>();
    }

    public static PhotonView Get(GameObject gameObj)
    {
        return gameObj.GetComponent<PhotonView>();
    }

    public static PhotonView Find(int viewID)
    {
        return PhotonNetwork.networkingPeer.GetPhotonView(viewID);
    }

    public override string ToString()
    {
        return string.Format("View ({3}){0} on {1} {2}", this.viewID, this.gameObject.name, (this.isSceneView) ? "(scene)" : string.Empty, this.prefix);
    }
}
