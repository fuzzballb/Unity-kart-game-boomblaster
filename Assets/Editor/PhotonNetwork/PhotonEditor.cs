// ----------------------------------------------------------------------------
// <copyright file="PhotonEditor.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   MenuItems and in-Editor scripts for PhotonNetwork.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class PhotonEditor : EditorWindow
{
    protected static AccountService.Origin RegisterOrigin = AccountService.Origin.Pun;

    protected Vector2 scrollPos = Vector2.zero;

    protected static string DocumentationLocation = "Assets/Photon Unity Networking/PhotonNetwork-Documentation.pdf";

    protected static string UrlFreeLicense = "http://www.exitgames.com/Download/Photon";

    protected static string UrlDevNet = "http://doc.exitgames.com/photon-cloud";

    protected static string UrlForum = "http://forum.exitgames.com";

    protected static string UrlCompare = "http://doc.exitgames.com/photon-cloud";

    protected static string UrlHowToSetup = "http://doc.exitgames.com/photon-server/PhotonIn5Min/#cat-First%20Steps";

    protected static string UrlAppIDExplained = "http://doc.exitgames.com/photon-cloud/PhotonDashboard/#cat-getting_started";

    protected static string UrlAccountPage = "https://www.exitgames.com/Account/SignIn?email="; // opened in browser


    private enum GUIState
    {
        Uninitialized, 

        Main, 

        Setup
    }

    private enum PhotonSetupStates
    {
        RegisterForPhotonCloud, 

        EmailAlreadyRegistered, 

        SetupPhotonCloud, 

        SetupSelfHosted
    }

    private GUIState guiState = GUIState.Uninitialized;

    private bool isSetupWizard = false;

    private PhotonSetupStates photonSetupState = PhotonSetupStates.RegisterForPhotonCloud;
    
    private static double lastWarning = 0;

    private string photonAddress = "127.0.0.1";

    private int photonPort = ServerSettings.DefaultMasterPort;

    private string emailAddress = string.Empty;

    private string cloudAppId = string.Empty;
    
    private static UnityEngine.Object lastFirstElement;

    private static bool dontCheckPunSetupField;

    /// <summary>
    /// Can be used to (temporarily) disable the checks for PUN Setup and scene PhotonViews.
    /// This will prevent scene PhotonViews from being updated, so be careful. 
    /// When you re-set this value, checks are used again and scene PhotonViews get IDs as needed.
    /// </summary>
    protected static bool dontCheckPunSetup
    {
        get
        {
            return dontCheckPunSetupField;
        }
        set
        {
            if (dontCheckPunSetupField != value)
            {
                dontCheckPunSetupField = value;
            }
        }
    }

    protected static Type WindowType = typeof(PhotonEditor);

    protected static string WindowTitle = "PUN Setup Wizard";

    [MenuItem("Window/Photon Unity Networking")]
    protected static void Init()
    {
        PhotonEditor.ReLoadCurrentSettings();

        PhotonEditor win = GetWindow(WindowType, false, WindowTitle) as PhotonEditor;
        win.ReApplySettingsToWindow();
    }

    static PhotonEditor()
    {
        EditorApplication.projectWindowChanged += EditorUpdate; 
        EditorApplication.hierarchyWindowChanged += EditorUpdate;
        EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
    }

    // called in editor, opens wizard for initial setup, keeps scene PhotonViews up to date and closes connections when compiling (to avoid issues)
    private static void EditorUpdate()
    {
        if (dontCheckPunSetup)
        {
            return;
        }

        // serverSetting is null when the file gets deleted. otherwise, the wizard should only run once and only if hosting option is not (yet) set
        if (!PhotonEditor.Current.DisableAutoOpenWizard && PhotonEditor.Current.HostType == ServerSettings.HostingOption.NotSet)
        {
            ShowRegistrationWizard();
        }

        // Workaround for TCP crash. Plus this surpresses any other recompile errors.
        if (EditorApplication.isCompiling)
        {
            if (PhotonNetwork.connected)
            {
                if (lastWarning > EditorApplication.timeSinceStartup - 3)
                {
                    // Prevent error spam
                    Debug.LogWarning("Unity recompile forced a Photon Disconnect");
                    lastWarning = EditorApplication.timeSinceStartup;
                }

                PhotonNetwork.Disconnect();
            }
        }
    }

    // called in editor on change of play-mode (used to show a message popup that connection settings are incomplete)
    private static void PlaymodeStateChanged()
    {
        if (dontCheckPunSetup || EditorApplication.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (PhotonEditor.Current.HostType == ServerSettings.HostingOption.NotSet)
        {
            EditorUtility.DisplayDialog("Warning", "You have not yet run the Photon setup wizard! Your game won't be able to connect. See Windows -> Photon Unity Networking.", "Ok");
        }
    }

    private void SwitchMenuState(GUIState newState)
    {
        this.guiState = newState;
        if (this.isSetupWizard && newState != GUIState.Setup)
        {
            this.Close();
        }
    }

    /// <summary>Creates an Editor window, showing the cloud-registration wizard for Photon (entry point to setup PUN).</summary>
    protected static void ShowRegistrationWizard()
    {
        PhotonEditor.Current.DisableAutoOpenWizard = true;
        PhotonEditor.Save();

        PhotonEditor window = (PhotonEditor)GetWindow(WindowType, false, WindowTitle, true);
        window.isSetupWizard = true;
        window.InitPhotonSetupWindow();
    }
    
    /// <summary>Re-initializes the Photon Setup window and shows one of three states: register cloud, setup cloud, setup self-hosted.</summary>
    protected void InitPhotonSetupWindow()
    {
        this.SwitchMenuState(GUIState.Setup);

        this.ReApplySettingsToWindow();

        switch (PhotonEditor.Current.HostType)
        {
            case ServerSettings.HostingOption.PhotonCloud:
                this.photonSetupState = PhotonSetupStates.SetupPhotonCloud;
                break;
            case ServerSettings.HostingOption.SelfHosted:
                this.photonSetupState = PhotonSetupStates.SetupSelfHosted;
                break;
            case ServerSettings.HostingOption.NotSet:
            default:
                this.photonSetupState = PhotonSetupStates.RegisterForPhotonCloud;
                break;
        }
    }

    protected virtual void OnGUI()
    {
        this.scrollPos = GUILayout.BeginScrollView(this.scrollPos);

        if (this.guiState == GUIState.Uninitialized)
        {
            this.ReApplySettingsToWindow();
            this.guiState = (PhotonEditor.Current.HostType == ServerSettings.HostingOption.NotSet) ? GUIState.Setup : GUIState.Main;
        }

        if (this.guiState == GUIState.Main)
        {
            this.OnGuiMainWizard();
        }
        else
        {
            this.OnGuiRegisterCloudApp();
        }

        GUILayout.EndScrollView();
    }

    protected virtual void OnGuiRegisterCloudApp()
    {
        GUI.skin.label.wordWrap = true;
        if (!this.isSetupWizard)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close setup", GUILayout.ExpandWidth(false)))
            {
                this.SwitchMenuState(GUIState.Main);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(15);
        }

        if (this.photonSetupState == PhotonSetupStates.RegisterForPhotonCloud)
        {
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUILayout.Label("Connect to Photon Cloud");
            GUI.skin.label.fontStyle = FontStyle.Normal;

            GUILayout.Label("Your e-mail address is required to access your own free app.");
            this.emailAddress = EditorGUILayout.TextField("Email:", this.emailAddress);

            if (GUILayout.Button("Send"))
            {
                GUIUtility.keyboardControl = 0;
                this.RegisterWithEmail(this.emailAddress);
            }

            EditorGUILayout.Separator();

            GUILayout.Label("I am already signed up. Let me enter my AppId.");
            if (GUILayout.Button("Setup"))
            {
                this.photonSetupState = PhotonSetupStates.SetupPhotonCloud;
            }

            GUILayout.Label("I want to register by a website.");
            if (GUILayout.Button("Open account website"))
            {
                EditorUtility.OpenWithDefaultApp(UrlAccountPage + Uri.EscapeUriString(this.emailAddress));
            }

            EditorGUILayout.Separator();
        }
        else if (this.photonSetupState == PhotonSetupStates.EmailAlreadyRegistered)
        {
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUILayout.Label("Oops!");
            GUI.skin.label.fontStyle = FontStyle.Normal;

            GUILayout.Label("The provided e-mail-address has already been registered.");

            if (GUILayout.Button("Mh, see my account page"))
            {
                EditorUtility.OpenWithDefaultApp(UrlAccountPage + Uri.EscapeUriString(this.emailAddress));
            }

            EditorGUILayout.Separator();

            GUILayout.Label("Ah, I know my Application ID. Get me to setup.");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                this.photonSetupState = PhotonSetupStates.RegisterForPhotonCloud;
            }

            if (GUILayout.Button("Setup"))
            {
                this.photonSetupState = PhotonSetupStates.SetupPhotonCloud;
            }

            GUILayout.EndHorizontal();
        }
        else if (this.photonSetupState == PhotonSetupStates.SetupPhotonCloud)
        {
            // cloud setup
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUILayout.Label("Connect to Photon Cloud");
            GUI.skin.label.fontStyle = FontStyle.Normal;

            EditorGUILayout.Separator();
            this.OnGuiSetupCloudAppId();
            this.OnGuiCompareAndHelpOptions();
        }
        else if (this.photonSetupState == PhotonSetupStates.SetupSelfHosted)
        {
            // self-hosting setup
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUILayout.Label("Setup own Photon Host");
            GUI.skin.label.fontStyle = FontStyle.Normal;

            EditorGUILayout.Separator();

            this.OnGuiSetupSelfhosting();
            this.OnGuiCompareAndHelpOptions();
        }
    }

    protected virtual void OnGuiMainWizard()
    {
        // settings button
        GUILayout.BeginHorizontal();
        GUILayout.Label("Settings", EditorStyles.boldLabel, GUILayout.Width(100));
        if (GUILayout.Button(new GUIContent("Setup", "Setup wizard for setting up your own server or the cloud.")))
        {
            this.InitPhotonSetupWindow();
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(12);

        // converter
        GUILayout.BeginHorizontal();
        GUILayout.Label("Converter", EditorStyles.boldLabel, GUILayout.Width(100));
        if (GUILayout.Button(new GUIContent("Start", "Converts pure Unity Networking to Photon Unity Networking.")))
        {
            PhotonConverter.RunConversion();
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(12);

        // add PhotonView
        GUILayout.BeginHorizontal();
        GUILayout.Label("Component", EditorStyles.boldLabel, GUILayout.Width(100));
        if (GUILayout.Button(new GUIContent("Add PhotonView", "Also in menu: Component, Miscellaneous")))
        {
            if (Selection.activeGameObject != null)
            {
                Selection.activeGameObject.AddComponent<PhotonView>();
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(22);

        // license
        GUILayout.BeginHorizontal();
        GUILayout.Label("Licenses", EditorStyles.boldLabel, GUILayout.Width(100));

        if (GUILayout.Button(new GUIContent("Download Free", "Get your free license for up to 100 concurrent players.")))
        {
            EditorUtility.OpenWithDefaultApp(UrlFreeLicense);
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(12);

        // documentation
        GUILayout.BeginHorizontal();
        GUILayout.Label("Documentation", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.BeginVertical();
        if (GUILayout.Button(new GUIContent("Open PDF", "Opens the local documentation pdf.")))
        {
            EditorUtility.OpenWithDefaultApp(DocumentationLocation);
        }

        if (GUILayout.Button(new GUIContent("Open DevNet", "Online documentation for Photon.")))
        {
            EditorUtility.OpenWithDefaultApp(UrlDevNet);
        }

        if (GUILayout.Button(new GUIContent("Open Cloud Dashboard", "Review Cloud App information and statistics.")))
        {
            EditorUtility.OpenWithDefaultApp(UrlAccountPage + Uri.EscapeUriString(this.emailAddress));
        }

        if (GUILayout.Button(new GUIContent("Open Forum", "Online support for Photon.")))
        {
            EditorUtility.OpenWithDefaultApp(UrlForum);
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();   
    }

    protected virtual void OnGuiCompareAndHelpOptions()
    {
        EditorGUILayout.Separator();
        GUILayout.Label("I am not quite sure how 'my own host' compares to 'cloud'.");
        if (GUILayout.Button("See comparison page"))
        {
            Application.OpenURL(UrlCompare);
        }

        EditorGUILayout.Separator();

        GUILayout.Label("Questions? Need help or want to give us feedback? You are most welcome!");
        if (GUILayout.Button("See the Photon Forum"))
        {
            Application.OpenURL(UrlForum);
        }
    }

    protected virtual void OnGuiSetupCloudAppId()
    {
        GUILayout.Label("Your APP ID:");

        this.cloudAppId = EditorGUILayout.TextField(this.cloudAppId);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel"))
        {
            GUIUtility.keyboardControl = 0;
            this.ReApplySettingsToWindow();
        }

        int selectedRegion = ServerSettings.FindRegionForServerAddress(this.photonAddress);

        if (GUILayout.Button("Save"))
        {
            GUIUtility.keyboardControl = 0;
			this.cloudAppId = this.cloudAppId.Trim();
            PhotonEditor.Current.UseCloud(this.cloudAppId, selectedRegion);
            PhotonEditor.Save();

            EditorUtility.DisplayDialog("Success", "Saved your settings.", "ok");
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Separator();

        
        GUILayout.BeginHorizontal();

        GUILayout.Label("Cloud Region");
        
        int toolbarValue = GUILayout.Toolbar(selectedRegion, ServerSettings.CloudServerRegionNames);
        if (selectedRegion != toolbarValue)
        {
            //Debug.Log("Replacing region: " + selectedRegion + " with: " + toolbarValue);
            this.photonAddress = ServerSettings.FindServerAddressForRegion(toolbarValue);
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Separator();


        GUILayout.Label("Running my app in the cloud was fun but...\nLet me setup my own Photon server.");

        if (GUILayout.Button("Switch to own host"))
        {
            this.photonAddress = ServerSettings.DefaultServerAddress;
            this.photonPort = ServerSettings.DefaultMasterPort;
            this.photonSetupState = PhotonSetupStates.SetupSelfHosted;
        }
    }

    protected virtual void OnGuiSetupSelfhosting()
    {
        GUILayout.Label("Your Photon Host");

        this.photonAddress = EditorGUILayout.TextField("IP:", this.photonAddress);
        this.photonPort = EditorGUILayout.IntField("Port:", this.photonPort);

        // photonProtocol = (ExitGames.Client.Photon.ConnectionProtocol)EditorGUILayout.EnumPopup("Protocol:", photonProtocol);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel"))
        {
            GUIUtility.keyboardControl = 0;
            this.ReApplySettingsToWindow();
        }

        if (GUILayout.Button("Save"))
        {
            GUIUtility.keyboardControl = 0;

            PhotonEditor.Current.UseMyServer(this.photonAddress, this.photonPort, null);
            PhotonEditor.Save();

            EditorUtility.DisplayDialog("Success", "Saved your settings.", "ok");
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Separator();

        GUILayout.Label("Running my own server is too much hassle..\nI want to give Photon's free app a try.");

        if (GUILayout.Button("Get the free cloud app"))
        {
            this.cloudAppId = string.Empty;
            this.photonSetupState = PhotonSetupStates.RegisterForPhotonCloud;
        }
    }

    protected virtual void RegisterWithEmail(string email)
    {
        EditorUtility.DisplayProgressBar("Connecting", "Connecting to the account service..", 0.5f);
        var client = new AccountService();
        client.RegisterByEmail(email, RegisterOrigin); // this is the synchronous variant using the static RegisterOrigin. "result" is in the client

        EditorUtility.ClearProgressBar();
        if (client.ReturnCode == 0)
        {
            PhotonEditor.Current.UseCloud(client.AppId, 0);
            PhotonEditor.Save();
            this.ReApplySettingsToWindow();
            this.photonSetupState = PhotonSetupStates.SetupPhotonCloud;
        }
        else
        {
            if (client.Message.Contains("Email already registered"))
            {
                this.photonSetupState = PhotonSetupStates.EmailAlreadyRegistered;
            }
            else
            {
                EditorUtility.DisplayDialog("Error", client.Message, "OK");
                // Debug.Log(client.Exception);
                this.photonSetupState = PhotonSetupStates.RegisterForPhotonCloud;
            }
        }
    }

    #region SettingsFileHandling

    private static ServerSettings currentSettings;

    public static ServerSettings Current
    {
        get
        {
            if (currentSettings == null)
            {
                ReLoadCurrentSettings();

                // if still not loaded, create one
                if (currentSettings == null)
                {
                    string settingsPath = Path.GetDirectoryName(PhotonNetwork.serverSettingsAssetPath);
                    if (!Directory.Exists(settingsPath))
                    {
                        Directory.CreateDirectory(settingsPath);
                        AssetDatabase.ImportAsset(settingsPath);
                    }

                    currentSettings = (ServerSettings) ScriptableObject.CreateInstance(typeof (ServerSettings));
                    AssetDatabase.CreateAsset(currentSettings, PhotonNetwork.serverSettingsAssetPath);
                }
            }

            return currentSettings;
        }

        protected set
        {
            currentSettings = value;
        }
    }

    public static void Save()
    {
        EditorUtility.SetDirty(PhotonEditor.Current);
    }

    public static void ReLoadCurrentSettings()
    {
        // this now warns developers if there are more than one settings files in resources folders. first will be used.
        UnityEngine.Object[] settingFiles = Resources.LoadAll(PhotonNetwork.serverSettingsAssetFile, typeof(ServerSettings));
        if (settingFiles != null && settingFiles.Length > 0)
        {
            PhotonEditor.Current = (ServerSettings)settingFiles[0];

            if (settingFiles.Length > 1)
            {
                Debug.LogWarning("There are more than one " + PhotonNetwork.serverSettingsAssetFile + " files in 'Resources' folder. Check your project to keep only one. Using: " + AssetDatabase.GetAssetPath(PhotonEditor.Current));
            }
        }
    }

    protected void ReApplySettingsToWindow()
    {
        this.cloudAppId = string.IsNullOrEmpty(PhotonEditor.Current.AppID) ? string.Empty : PhotonEditor.Current.AppID;
        this.photonAddress = string.IsNullOrEmpty(PhotonEditor.Current.ServerAddress) ? string.Empty : PhotonEditor.Current.ServerAddress;
        this.photonPort = PhotonEditor.Current.ServerPort;
    }

    #endregion
}
