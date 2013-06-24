// ----------------------------------------------------------------------------
// <copyright file="PhotonClasses.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;


/// <summary>Class for constants. Defines photon-event-codes for PUN usage.</summary>
internal class PhotonNetworkMessages
{
    public const byte RPC = 200; 
    public const byte SendSerialize = 201;
    public const byte Instantiation = 202;
    public const byte CloseConnection = 203;
    public const byte Destroy = 204;
    public const byte RemoveCachedRPCs = 205;
    public const byte SendSerializeReliable = 206;  // TS: added this but it's not really needed anymore
}

/// <summary>Enum of "target" options for RPCs. These define which remote clients get your RPC call. </summary>
/// \ingroup publicApi
public enum PhotonTargets { All, Others, MasterClient, AllBuffered, OthersBuffered } //.MasterClientBuffered? .Server?

/// <summary>Used to define the level of logging output created by the PUN classes. Either log errors, info (some more) or full.</summary>
/// \ingroup publicApi
public enum PhotonLogLevel { ErrorsOnly, Informational, Full }


namespace Photon
{
    /// <summary>
    /// This class adds the property photonView, while logging a warning when your game still uses the networkView.
    /// </summary>
    public class MonoBehaviour : UnityEngine.MonoBehaviour
    {
        public PhotonView photonView
        {
            get
            {
                return PhotonView.Get(this);
            }
        }

        new public PhotonView networkView
        {
            get
            {
                Debug.LogWarning("Why are you still using networkView? should be PhotonView?");
                return PhotonView.Get(this);
            }
        }
    }
}

/// <summary>
/// Container class for info about a particular message, RPC or update.
/// </summary>
/// \ingroup publicApi
public class PhotonMessageInfo
{
    private int timeInt;
    public PhotonPlayer sender;
    public PhotonView photonView;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhotonMessageInfo"/> class. 
    /// To create an empty messageinfo only!
    /// </summary>
    public PhotonMessageInfo()
    {
        this.sender = PhotonNetwork.player;
        this.timeInt = (int)(PhotonNetwork.time * 1000);
        this.photonView = null;
    }

    public PhotonMessageInfo(PhotonPlayer player, int timestamp, PhotonView view)
    {
        this.sender = player;
        this.timeInt = timestamp;
        this.photonView = view;
    }

    public double timestamp
    {
        get { return ((double)(uint)this.timeInt) / 1000.0f; }
    }

    public override string ToString()
    {
        return string.Format("[PhotonMessageInfo: player='{1}' timestamp={0}]", this.timestamp, this.sender);
    }
}

/// <summary>
/// This "container" class is used to carry your data as written by OnPhotonSerializeView.
/// </summary>
/// <seealso cref="PhotonNetworkingMessage"/>
/// \ingroup publicApi
public class PhotonStream
{
    bool write = false;
    internal List<object> data;
    byte currentItem = 0; //Used to track the next item to receive.

    public PhotonStream(bool write, object[] incomingData)
    {
        this.write = write;
        if (incomingData == null)
        {
            this.data = new List<object>();
        }
        else
        {
            this.data = new List<object>(incomingData);
        }
    }
    
    public bool isWriting
    {
        get { return this.write; }
    }

    public bool isReading
    {
        get { return !this.write; }
    }

    public int Count
    {
        get
        {
            return data.Count;
        }
    }

    public object ReceiveNext()
    {
        if (this.write)
        {
            Debug.LogError("Error: you cannot read this stream that you are writing!");
            return null;
        }

        object obj = this.data[this.currentItem];
        this.currentItem++;
        return obj;
    }

    public void SendNext(object obj)
    {
        if (!this.write)
        {
            Debug.LogError("Error: you cannot write/send to this stream that you are reading!");
            return;
        }

        this.data.Add(obj);
    }

    public object[] ToArray()
    {
        return this.data.ToArray();
    }

    public void Serialize(ref bool myBool)
    {
        if (this.write)
        {
            this.data.Add(myBool);
        }
        else
        {
            if (this.data.Count > currentItem)
            {
                myBool = (bool)data[currentItem];
                this.currentItem++;
            }
        }
    }

    public void Serialize(ref int myInt)
    {
        if (write)
        {
            this.data.Add(myInt);
        }
        else
        {
            if (this.data.Count > currentItem)
            {
                myInt = (int)data[currentItem];
                currentItem++;
            }
        }
    }

    public void Serialize(ref string value)
    {
        if (write)
        {
            this.data.Add(value);
        }
        else
        {
            if (this.data.Count > currentItem)
            {
                value = (string)data[currentItem];
                currentItem++;
            }
        }
    }

    public void Serialize(ref char value)
    {
        if (write)
        {
            this.data.Add(value);
        }
        else
        {
            if (this.data.Count > currentItem)
            {
                value = (char)data[currentItem];
                currentItem++;
            }
        }
    }

    public void Serialize(ref short value)
    {
        if (write)
        {
            this.data.Add(value);
        }
        else
        {
            if (this.data.Count > currentItem)
            {
                value = (short)data[currentItem];
                currentItem++;
            }
        }
    }

    public void Serialize(ref float obj)
    {
        if (write)
        {
            this.data.Add(obj);
        }
        else
        {
            if (this.data.Count > currentItem)
            {
                obj = (float)data[currentItem];
                currentItem++;
            }
        }
    }

    public void Serialize(ref PhotonPlayer obj)
    {
        if (write)
        {
            this.data.Add(obj);
        }
        else
        {
            if (this.data.Count > currentItem)
            {
                obj = (PhotonPlayer)data[currentItem];
                currentItem++;
            }
        }
    }

    public void Serialize(ref Vector3 obj)
    {
        if (write)
        {
            this.data.Add(obj);
        }
        else
        {
            if (this.data.Count > currentItem)
            {
                obj = (Vector3)data[currentItem];
                currentItem++;
            }
        }
    }

    public void Serialize(ref Vector2 obj)
    {
        if (write)
        {
            this.data.Add(obj);
        }
        else
        {
            if (this.data.Count > currentItem)
            {
                obj = (Vector2)data[currentItem];
                currentItem++;
            }
        }
    }

    public void Serialize(ref Quaternion obj)
    {
        if (write)
        {
            this.data.Add(obj);
        }
        else
        {
            if (this.data.Count > currentItem)
            {
                obj = (Quaternion)data[currentItem];
                currentItem++;
            }
        }
    }
}