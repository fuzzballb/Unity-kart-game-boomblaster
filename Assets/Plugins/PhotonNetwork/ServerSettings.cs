using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Collection of connection-relevant settings, used internally by PhotonNetwork.ConnectUsingSettings.
/// </summary>
[System.Serializable]
public class ServerSettings : ScriptableObject
{
    public static string DefaultCloudServerUrl = "app-eu.exitgamescloud.com";
    
    // per region name and server-prefix
    public static string[] CloudServerRegionNames = new string[] { "EU", "US", "Asia", "Japan" };
    public static string[] CloudServerRegionPrefixes = new string[] {"app-eu", "app-us", "app-asia", "app-jp"};

    public static string DefaultServerAddress = "127.0.0.1";
    public static int DefaultMasterPort = 5055;  // default port for master server
    public static string DefaultAppID = "Master";

    public enum HostingOption { NotSet, PhotonCloud, SelfHosted, OfflineMode }

    public HostingOption HostType = HostingOption.NotSet;
    public string ServerAddress = DefaultServerAddress;
    public int ServerPort = 5055;
    public string AppID = "";

    [HideInInspector]
    public bool DisableAutoOpenWizard;

    public static int FindRegionForServerAddress(string server)
    {
        int result = 0;

        for (int i = 0; i < CloudServerRegionPrefixes.Length; i++)
        {
            if (server.StartsWith(CloudServerRegionPrefixes[i]))
            {
                return i;
            }
        }
        
        return result;
    }

    public static string FindServerAddressForRegion(int regionIndex)
    {
        return ServerSettings.DefaultCloudServerUrl.Replace("app-eu", ServerSettings.CloudServerRegionPrefixes[regionIndex]);
    }

    public void UseCloud(string cloudAppid, int regionIndex)
    {
        this.HostType = HostingOption.PhotonCloud;
        this.AppID = cloudAppid;
        this.ServerAddress = FindServerAddressForRegion(regionIndex);
        this.ServerPort = DefaultMasterPort;
    }

    public void UseMyServer(string serverAddress, int serverPort, string application)
    {
        this.HostType = HostingOption.SelfHosted;
        this.AppID = (application != null) ? application : DefaultAppID;
        this.ServerAddress = serverAddress;
        this.ServerPort = serverPort;
    }

    public override string ToString()
    {
        return "ServerSettings: " + HostType + " " + ServerAddress;
    }
}
