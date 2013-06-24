// ----------------------------------------------------------------------------
// <copyright file="CustomTypes.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------
using System;
using System.IO;
using ExitGames.Client.Photon;
using UnityEngine;

/// <summary>
/// Internally used class, containing de/serialization methods for various Unity-specific classes.
/// Adding those to the Photon serialization protocol allows you to send them in events, etc.
/// </summary>
internal static class CustomTypes
{
    /// <summary>Register</summary>
    internal static void Register()
    {
        PhotonPeer.RegisterType(typeof(Vector2), (byte)'W', SerializeVector2, DeserializeVector2);
        PhotonPeer.RegisterType(typeof(Vector3), (byte)'V', SerializeVector3, DeserializeVector3);
        PhotonPeer.RegisterType(typeof(Transform), (byte)'T', SerializeTransform, DeserializeTransform);
        PhotonPeer.RegisterType(typeof(Quaternion), (byte)'Q', SerializeQuaternion, DeserializeQuaternion);
        PhotonPeer.RegisterType(typeof(PhotonPlayer), (byte)'P', SerializePhotonPlayer, DeserializePhotonPlayer);
    }

    #region Custom De/Serializer Methods

    private static byte[] SerializeTransform(object customobject)
    {
        Transform t = (Transform)customobject;

        Vector3[] parts = new Vector3[2];
        parts[0] = t.position;
        parts[1] = t.eulerAngles;

        return Protocol.Serialize(parts);
    }

    private static object DeserializeTransform(byte[] serializedcustomobject)
    {
        object x = Protocol.Deserialize(serializedcustomobject);
        return x;
    }

    private static byte[] SerializeVector3(object customobject)
    {
        Vector3 vo = (Vector3)customobject;
        int index = 0;

        ////TODO: check if the float almost 0 situation is actually happening a lot in games or if this doesn't save a lot
        //byte skipFlags = 0;
        //byte floatsNeeded = 3;
        //if (vo.x < PhotonNetwork.precisionForFloatSynchronization && vo.x > -PhotonNetwork.precisionForFloatSynchronization)
        //{
        //    skipFlags += 1;
        //    floatsNeeded--;
        //}

        //if (vo.y < PhotonNetwork.precisionForFloatSynchronization && vo.y > -PhotonNetwork.precisionForFloatSynchronization)
        //{
        //    skipFlags += 2;
        //    floatsNeeded--;
        //}

        //if (vo.z < PhotonNetwork.precisionForFloatSynchronization && vo.z > -PhotonNetwork.precisionForFloatSynchronization)
        //{
        //    skipFlags += 4;
        //    floatsNeeded--;
        //}

        //if (skipFlags != 0)
        //{
        //    byte[] cutBytes = new byte[1 + floatsNeeded * 4];
        //    cutBytes[index++] = skipFlags;

        //    if ((skipFlags & 1) == 0) Protocol.Serialize(vo.x, cutBytes, ref index);
        //    if ((skipFlags & 2) == 0) Protocol.Serialize(vo.y, cutBytes, ref index);
        //    if ((skipFlags & 4) == 0) Protocol.Serialize(vo.z, cutBytes, ref index);
        //    return cutBytes;
        //}

        byte[] bytes = new byte[3 * 4];
        Protocol.Serialize(vo.x, bytes, ref index);
        Protocol.Serialize(vo.y, bytes, ref index);
        Protocol.Serialize(vo.z, bytes, ref index);
        return bytes;
    }

    private static object DeserializeVector3(byte[] bytes)
    {
        Vector3 vo = new Vector3();
        int index = 0;

        //if (bytes.Length < 12)
        //{
        //    byte skipFlags = bytes[index++];
        //    if ((skipFlags & 1) == 0) Protocol.Deserialize(out vo.x, bytes, ref index);
        //    if ((skipFlags & 2) == 0) Protocol.Deserialize(out vo.y, bytes, ref index);
        //    if ((skipFlags & 4) == 0) Protocol.Deserialize(out vo.z, bytes, ref index);

        //    return vo;
        //}

        Protocol.Deserialize(out vo.x, bytes, ref index);
        Protocol.Deserialize(out vo.y, bytes, ref index);
        Protocol.Deserialize(out vo.z, bytes, ref index);

        return vo;
    }

    private static byte[] SerializeVector2(object customobject)
    {
        Vector2 vo = (Vector2)customobject;
        MemoryStream ms = new MemoryStream(2 * 4);

        ms.Write(BitConverter.GetBytes(vo.x), 0, 4);
        ms.Write(BitConverter.GetBytes(vo.y), 0, 4);
        return ms.ToArray();
    }

    private static object DeserializeVector2(byte[] bytes)
    {
        Vector2 vo = new Vector2();
        vo.x = BitConverter.ToSingle(bytes, 0);
        vo.y = BitConverter.ToSingle(bytes, 4);
        return vo;
    }

    private static byte[] SerializeQuaternion(object obj)
    {
        Quaternion o = (Quaternion)obj;
        MemoryStream ms = new MemoryStream(4 * 4);

        ms.Write(BitConverter.GetBytes(o.w), 0, 4);
        ms.Write(BitConverter.GetBytes(o.x), 0, 4);
        ms.Write(BitConverter.GetBytes(o.y), 0, 4);
        ms.Write(BitConverter.GetBytes(o.z), 0, 4);
        return ms.ToArray();
    }

    private static object DeserializeQuaternion(byte[] bytes)
    {
        Quaternion o = new Quaternion();
        o.w = BitConverter.ToSingle(bytes, 0);
        o.x = BitConverter.ToSingle(bytes, 4);
        o.y = BitConverter.ToSingle(bytes, 8);
        o.z = BitConverter.ToSingle(bytes, 12);

        return o;
    }

    private static byte[] SerializePhotonPlayer(object customobject)
    {
        int ID = ((PhotonPlayer)customobject).ID;
        return BitConverter.GetBytes(ID);
    }

    private static object DeserializePhotonPlayer(byte[] bytes)
    {
        int ID = BitConverter.ToInt32(bytes, 0);
        if (PhotonNetwork.networkingPeer.mActors.ContainsKey(ID))
        {
            return PhotonNetwork.networkingPeer.mActors[ID];
        }
        else
        {
            return null;
        }
    }

    #endregion
}
