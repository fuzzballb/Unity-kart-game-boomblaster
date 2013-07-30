#if !(UNITY_WINRT || UNITY_WP8)

using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// This script is automatically added to the PhotonHandler gameobject by PUN
/// It will auto-ping the ExitGames cloud regions via Awake.
/// This is done only once per client and the result is saved in PlayerPrefs. 
/// Use PhotonNetwork.ConnectToBestCloudServer(gameVersion) to connect to clour region with best ping.
/// </summary>
public class PingCloudRegions : MonoBehaviour
{


    static CloudServerRegion closestRegion = CloudServerRegion.US;
    static public PingCloudRegions SP;

    private bool isPinging = false;
    private int lowestRegionAverage = -1;
    private const string playerPrefsKey = "PUNCloudBestRegion";


    void Awake()
    {
        SP = this;

        // load settings and ping only if none available
        if (PlayerPrefs.GetString(playerPrefsKey, "") != "")
        {
            string reg = PlayerPrefs.GetString(playerPrefsKey, "");
            closestRegion = (CloudServerRegion)Enum.Parse(typeof(CloudServerRegion), reg, true);
            return;
        }
        StartCoroutine(PingAllRegions());
    }


    public static void OverrideRegion(CloudServerRegion region)
    {
        SetRegion(region);
    }


    public static void RefreshCloudServerRating()
    {
        if (SP != null)
        {
            SP.StartCoroutine(SP.PingAllRegions());
        }
    }


    public static void ConnectToBestRegion(string gameVersion)
    {
        SP.StartCoroutine(SP.ConnectToBestRegionInternal(gameVersion));
    }


    // Ping all PUN cloud regions (unless offline mode)
    public IEnumerator PingAllRegions()
    {
        ServerSettings settings = (ServerSettings)Resources.Load(PhotonNetwork.serverSettingsAssetFile, typeof(ServerSettings));
        if (settings.HostType == ServerSettings.HostingOption.OfflineMode)
            yield break;

        isPinging = true;
        foreach (CloudServerRegion region in System.Enum.GetValues(typeof(CloudServerRegion)))
        {
            yield return StartCoroutine(PingRegion(region));
        }
        isPinging = false;
    }

    
    IEnumerator PingRegion(CloudServerRegion region)
    {
        // JP can't be pinged at the moment, so we skip it!
        if (region == CloudServerRegion.Japan)
        {
            yield break;
        }

        string hostname = ServerSettings.FindServerAddressForRegion(region);
        string regionIp = ResolveHost(hostname);

        if (string.IsNullOrEmpty(regionIp))
        { 
            Debug.LogError("Could not resolve host: " + hostname);
            yield break;
        }

        int averagePing = 0;
        int tries = 3;
        int skipped = 0;
        float timeout = 0.500f; // 500 milliseconds is our max, after this we assume a timeout.
        for (int i = 0; i < tries; i++)
        {
            float startTime = Time.time;
            Ping ping = new Ping(regionIp);
            while (!ping.isDone && Time.time < startTime + timeout)
            { 
                // Timeout after 500ms: sometimes Unity ping never returns
                yield return 0;
            }
            if (ping.time == -1)
            {
                if (skipped > 5)
                {
                    averagePing += (int)(timeout * 1000) * tries;
                    break;
                }
                else
                {
                    i -= 1; //Sometimes Unity ping doesnt return, we therefor retry a few times..
                    skipped++;
                    continue;
                }
            }

            averagePing += ping.time;
        }

        int regionAverage = averagePing / tries;
        //Debug.LogWarning (hostname + ": " + average + "ms");

        if (regionAverage < lowestRegionAverage || lowestRegionAverage == -1)
        {
            lowestRegionAverage = regionAverage;
            SetRegion(region);
        }
    }

    private static void SetRegion(CloudServerRegion region)
    {
        closestRegion = region;
        PlayerPrefs.SetString(playerPrefsKey, region.ToString());
    }

    IEnumerator ConnectToBestRegionInternal(string gameVersion)
    {
        while (isPinging)
        {
            yield return 0; // wait until pinging finished (offline mode won't ping)
        }

        ServerSettings settings = (ServerSettings)Resources.Load(PhotonNetwork.serverSettingsAssetFile, typeof(ServerSettings));
        if (settings.HostType == ServerSettings.HostingOption.OfflineMode)
        {
            PhotonNetwork.ConnectUsingSettings(gameVersion);
        }
        else
        {
            PhotonNetwork.Connect(ServerSettings.FindServerAddressForRegion(closestRegion), settings.ServerPort, settings.AppID, gameVersion);
        }
    }

    /// <summary>
    /// Attempts to resolve a hostname into an IP string or returns empty string if that fails.
    /// </summary>
    /// <param name="hostString">Hostname to resolve.</param>
    /// <returns>IP string or empty string if resolution fails</returns>
    public static string ResolveHost(string hostString)
    {
        try
        {
            IPAddress[] address = Dns.GetHostAddresses(hostString);

            for (int index = 0; index < address.Length; index++)
            {
                IPAddress ipAddress = address[index];
                if (ipAddress != null && ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ipAddress.ToString();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("Exception caught! " + e.Source + " Message: " + e.Message);
        }

        return string.Empty;
    }
}
#endif