// ----------------------------------------------------------------------------
// <copyright file="PhotonClasses.cs" company="Exit Games GmbH">
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


/// <summary>Class for constants. Defines photon-event-codes for PUN usage.</summary>
internal class PunEvent
{
    public const byte RPC = 200; 
    public const byte SendSerialize = 201;
    public const byte Instantiation = 202;
    public const byte CloseConnection = 203;
    public const byte Destroy = 204;
    public const byte RemoveCachedRPCs = 205;
    public const byte SendSerializeReliable = 206;  // TS: added this but it's not really needed anymore
    public const byte DestroyPlayer = 207;  // TS: added to make others remove all GOs of a player
    public const byte AssignMaster = 208;  // TS: added to assign someone master client (overriding the current)
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

public class PBitStream
{
    List<byte> streamBytes;
    private int currentByte;
    private int totalBits = 0;

    public int ByteCount
    {
        get { return BytesForBits(this.totalBits); }
    }

    public int BitCount
    {
        get { return this.totalBits; }
        private set { this.totalBits = value; }
    }

    public PBitStream()
    {
        this.streamBytes = new List<byte>(1);
    }

    public PBitStream(int bitCount)
    {
        this.streamBytes = new List<byte>(BytesForBits(bitCount));
    }

    public PBitStream(IEnumerable<byte> bytes, int bitCount)
    {
        this.streamBytes = new List<byte>(bytes);
        this.BitCount = bitCount;
    }

    public static int BytesForBits(int bitCount)
    {
        if (bitCount <= 0)
        {
            return 0;
        }

        return ((bitCount - 1) / 8) + 1;
    }

    public void Add(bool val)
    {
        int bytePos = this.totalBits / 8;
        if (bytePos > this.streamBytes.Count-1 || totalBits == 0)
        {
            this.streamBytes.Add(0);
        }

        if (val)
        {
            int currentByteBit = 7 - (this.totalBits % 8);
            this.streamBytes[bytePos] |= (byte)(1 << currentByteBit);
        }

        this.totalBits++;
    }

    public byte[] ToBytes()
    {
        return streamBytes.ToArray();
    }

    public int Position { get; set; }

    public bool GetNext()
    {
        if (this.Position > this.totalBits)
        {
            throw new Exception("End of PBitStream reached. Can't read more.");
        }

        return Get(this.Position++);
    }

    public bool Get(int bitIndex)
    {
        int byteIndex = bitIndex / 8;
        int bitInByIndex = 7 - (bitIndex % 8);
        return ((streamBytes[byteIndex] & (byte)(1 << bitInByIndex)) > 0);
    }

    public void Set(int bitIndex, bool value)
    {
        int byteIndex = bitIndex / 8;
        int bitInByIndex = 7 - (bitIndex % 8);
        this.streamBytes[byteIndex] |= (byte)(1 << bitInByIndex);
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