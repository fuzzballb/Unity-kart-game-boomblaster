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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


public class Text
{
    public string WindowTitle = "PUN Wizard";
	public string SetupWizardWarningTitle = "Warning";
	public string SetupWizardWarningMessage = "You have not yet run the Photon setup wizard! Your game won't be able to connect. See Windows -> Photon Unity Networking.";
	public string MainMenuButton = "Main Menu";
	public string ConnectButton = "Connect to Photon Cloud";
	public string UsePhotonLabel = "Using the Photon Cloud is free for development. If you don't have an account yet, enter your email and register.";
	public string SendButton = "Send";
	public string EmailLabel = "Email:";
	public string SignedUpAlreadyLabel = "I am already signed up. Let me enter my AppId.";
	public string SetupButton = "Setup";
	public string RegisterByWebsiteLabel = "I want to register by a website.";
	public string AccountWebsiteButton = "Open account website";
	public string SelfHostLabel = "I want to host my own server. Let me set it up.";
	public string SelfHostSettingsButton = "Open self-hosting settings";
	public string MobileExportNoteLabel = "Note: Export to mobile will require iOS Pro / Android Pro.";
	public string EmailInUseLabel = "The provided e-mail-address has already been registered.";
	public string KnownAppIdLabel = "Ah, I know my Application ID. Get me to setup.";
	public string SeeMyAccountLabel = "Mh, see my account page";
	public string SelfHostSettingButton = "Open self-hosting settings";
	public string OopsLabel = "Oops!";
	public string SeeMyAccountPage = "";
	public string CancelButton = "Cancel";
	public string PhotonCloudConnect = "Connect to Photon Cloud";
	public string SetupOwnHostLabel = "Setup own Photon Host";
	public string PUNWizardLabel = "Photon Unity Networking (PUN) Wizard";
	public string SettingsButton = "Settings";
	public string SetupServerCloudLabel = "Setup wizard for setting up your own server or the cloud.";
	public string WarningPhotonDisconnect = "";
	public string ConverterLabel = "Converter";
	public string StartButton = "Start";
	public string UNtoPUNLabel = "Converts pure Unity Networking to Photon Unity Networking.";
	public string SettingsFileLabel = "Settings File";
	public string LocateSettingsButton = "Locate settings asset";
	public string SettingsHighlightLabel = "Highlights the used photon settings file in the project.";
	public string DocumentationLabel = "Documentation";
	public string OpenPDFText = "Open PDF";
	public string OpenPDFTooltip = "Opens the local documentation pdf.";
	public string OpenDevNetText = "Open DevNet";
	public string OpenDevNetTooltip = "Online documentation for Photon.";
	public string OpenCloudDashboardText = "Open Cloud Dashboard";
	public string OpenCloudDashboardTooltip = "Review Cloud App information and statistics.";
	public string OpenForumText = "Open Forum";
	public string OpenForumTooltip = "Online support for Photon.";
	public string QuestionsLabel = "Questions? Need help or want to give us feedback? You are most welcome!";
	public string SeeForumButton = "See the Photon Forum";
	public string OpenDashboardButton = "Open Dashboard (web)";
	public string AppIdLabel = "Your AppId";
	public string AppIdInfoLabel = "The AppId a Guid that identifies your game in the Photon Cloud. Find it on your dashboard page.";
	public string CloudRegionLabel = "Cloud Region";
	public string RegionalServersInfo = "Photon Cloud has regional servers. Picking one near your customers improves ping times. You could use more than one but this setup does not support it.";
	public string SaveButton = "Save";
	public string SettingsSavedTitle = "Success";
	public string SettingsSavedMessage = "Saved your settings.";
	public string OkButton = "Ok";
	public string SeeMyAccountPageButton = "Mh, see my account page";
	public string SetupOwnServerLabel = "Running my app in the cloud was fun but...\nLet me setup my own Photon server.";
	public string OwnHostCloudCompareLabel = "I am not quite sure how 'my own host' compares to 'cloud'.";
	public string ComparisonPageButton = "See comparison page";
	public string YourPhotonServerLabel = "Your Photon Server";
	public string AddressIPLabel = "Address/ip:";
	public string PortLabel = "Port:";
	public string LicensesLabel = "Licenses";
	public string LicenseDownloadText = "Free License Download";
	public string LicenseDownloadTooltip = "Get your free license for up to 100 concurrent players.";
	public string TryPhotonAppLabel = "Running my own server is too much hassle..\nI want to give Photon's free app a try.";
	public string GetCloudAppButton = "Get the free cloud app";
	public string ConnectionTitle = "Connecting";
	public string ConnectionInfo = "Connecting to the account service..";
	public string ErrorTextTitle = "Error";
	public string ServerSettingsMissingLabel = "Photon Unity Networking (PUN) is missing the 'ServerSettings' script. Re-import PUN to fix this.";
	public string MoreThanOneLabel = "There are more than one ";
	public string FilesInResourceFolderLabel = " files in 'Resources' folder. Check your project to keep only one. Using: ";
	public string IncorrectRPCListTitle = "Warning: RPC-list becoming incompatible!";
	public string IncorrectRPCListLabel = "Your project's RPC-list is full, so we can't add some RPCs just compiled.\n\nBy removing outdated RPCs, the list will be long enough but incompatible with older client builds!\n\nMake sure you change the game version where you use PhotonNetwork.ConnectUsingSettings().";
	public string RemoveOutdatedRPCsLabel = "Remove outdated RPCs";
	public string FullRPCListTitle = "Warning: RPC-list is full!";
	public string FullRPCListLabel = "Your project's RPC-list is too long for PUN.\n\nYou can change PUN's source to use short-typed RPC index. Look for comments 'LIMITS RPC COUNT'\n\nAlternatively, remove some RPC methods (use more parameters per RPC maybe).\n\nAfter a RPC-list refresh, make sure you change the game version where you use PhotonNetwork.ConnectUsingSettings().";
	public string SkipRPCListUpdateLabel = "Skip RPC-list update";
	public string PUNNameReplaceTitle = "Warning: RPC-list Compatibility";
	public string PUNNameReplaceLabel = "PUN replaces RPC names with numbers by using the RPC-list. All clients must use the same list for that.\n\nClearing it most likely makes your client incompatible with previous versions! Change your game version or make sure the RPC-list matches other clients.";
	public string RPCListCleared = "Clear RPC-list";
	public string ServerSettingsCleanedWarning = "Cleared the PhotonServerSettings.RpcList! This makes new builds incompatible with older ones. Better change game version in PhotonNetwork.ConnectUsingSettings().";
}


[InitializeOnLoad]
public class PhotonEditor : EditorWindow
{
    public static Text CurrentLang = new Text();

    protected static AccountService.Origin RegisterOrigin = AccountService.Origin.Pun;

    protected Vector2 scrollPos = Vector2.zero;

    protected static string DocumentationLocation = "Assets/Photon Unity Networking/PhotonNetwork-Documentation.pdf";

    protected static string UrlFreeLicense = "http://www.exitgames.com/Download/Photon";

    protected static string UrlDevNet = "http://doc.exitgames.com/photon-cloud";

    protected static string UrlForum = "http://forum.exitgames.com";

    protected static string UrlCompare = "http://doc.exitgames.com/photon-cloud/PhotonCloudvsServer";

    protected static string UrlHowToSetup = "http://doc.exitgames.com/photon-server/PhotonIn5Min";

    protected static string UrlAppIDExplained = "http://doc.exitgames.com/photon-cloud/PhotonDashboard";

    protected static string UrlAccountPage = "https://www.exitgames.com/Account/SignIn?email="; // opened in browser

    protected static string UrlCloudDashboard = "https://cloud.exitgames.com/Dashboard?email=";


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
    
    private static bool dontCheckPunSetupField;

    private static Texture2D HelpIcon;
    private static Texture2D WizardIcon;

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


    private static string[] cloudServerRegionNames;

    static PhotonEditor()
    {
        EditorApplication.projectWindowChanged += EditorUpdate;
        EditorApplication.hierarchyWindowChanged += EditorUpdate;
        EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
        EditorApplication.update += OnUpdate;
        
        HelpIcon = AssetDatabase.LoadAssetAtPath("Assets/Photon Unity Networking/Editor/PhotonNetwork/help.png", typeof(Texture2D)) as Texture2D;
        WizardIcon = AssetDatabase.LoadAssetAtPath("Assets/Photon Unity Networking/photoncloud-icon.png", typeof(Texture2D)) as Texture2D;

        // to be used in toolbar, the enum needs conversion to string[] being done here, once.
        Array enumValues = Enum.GetValues(typeof(CloudServerRegion));
        cloudServerRegionNames = new string[enumValues.Length];
        for (int i = 0; i < cloudServerRegionNames.Length; i++)
        {
            cloudServerRegionNames[i] = enumValues.GetValue(i).ToString();
        }
    }

    [MenuItem("Window/Photon Unity Networking &p")]
    protected static void Init()
    {
        PhotonEditor win = GetWindow(WindowType, false, CurrentLang.WindowTitle, true) as PhotonEditor;
        win.InitPhotonSetupWindow();
     
        win.isSetupWizard = false;
        win.SwitchMenuState(GUIState.Main);
    }


    /// <summary>Creates an Editor window, showing the cloud-registration wizard for Photon (entry point to setup PUN).</summary>
    protected static void ShowRegistrationWizard()
    {
        PhotonEditor win = GetWindow(WindowType, false, CurrentLang.WindowTitle, true) as PhotonEditor;
        win.isSetupWizard = true;
        win.InitPhotonSetupWindow();
    }

    /// <summary>Re-initializes the Photon Setup window and shows one of three states: register cloud, setup cloud, setup self-hosted.</summary>
    protected void InitPhotonSetupWindow()
    {
        this.minSize = MinSize;

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

    static bool DidRefresh;

    // called 100 times / sec but we only check if isCompiling
    private static void OnUpdate()
    {
        if (!DidRefresh && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // Debug.Log("Post-compile refresh of RPC index. isPlayingOrWillChangePlaymode: " + EditorApplication.isPlayingOrWillChangePlaymode);
            DidRefresh = true;
            UpdateRpcList();
        }
    }

    // called in editor, opens wizard for initial setup, keeps scene PhotonViews up to date and closes connections when compiling (to avoid issues)
    private static void EditorUpdate()
    {
        if (dontCheckPunSetup || PhotonEditor.Current == null)
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
                    Debug.LogWarning(CurrentLang.WarningPhotonDisconnect);
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
            EditorUtility.DisplayDialog(CurrentLang.SetupWizardWarningTitle, CurrentLang.SetupWizardWarningMessage, CurrentLang.OkButton);
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
            if (GUILayout.Button(CurrentLang.MainMenuButton, GUILayout.ExpandWidth(false)))
            {
                this.SwitchMenuState(GUIState.Main);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(15);
        }

        if (this.photonSetupState == PhotonSetupStates.RegisterForPhotonCloud)
        {
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUILayout.Label(CurrentLang.ConnectButton);
            EditorGUILayout.Separator();
            GUI.skin.label.fontStyle = FontStyle.Normal;

            GUILayout.Label(CurrentLang.UsePhotonLabel);
            EditorGUILayout.Separator();
            this.emailAddress = EditorGUILayout.TextField(CurrentLang.EmailLabel, this.emailAddress);

            if (GUILayout.Button(CurrentLang.SendButton))
            {
                GUIUtility.keyboardControl = 0;
                this.RegisterWithEmail(this.emailAddress);
            }

            GUILayout.Space(20);


            GUILayout.Label(CurrentLang.SignedUpAlreadyLabel);
            if (GUILayout.Button(CurrentLang.SetupButton))
            {
                this.photonSetupState = PhotonSetupStates.SetupPhotonCloud;
            }
            EditorGUILayout.Separator();


            GUILayout.Label(CurrentLang.RegisterByWebsiteLabel);
            if (GUILayout.Button(CurrentLang.AccountWebsiteButton))
            {
                EditorUtility.OpenWithDefaultApp(UrlAccountPage + Uri.EscapeUriString(this.emailAddress));
            }

            EditorGUILayout.Separator();

            GUILayout.Label(CurrentLang.SelfHostLabel);

            if (GUILayout.Button(CurrentLang.SelfHostSettingsButton))
            {
                this.photonAddress = ServerSettings.DefaultServerAddress;
                this.photonPort = ServerSettings.DefaultMasterPort;
                this.photonSetupState = PhotonSetupStates.SetupSelfHosted;
            }

            GUILayout.FlexibleSpace();


            if (!InternalEditorUtility.HasAdvancedLicenseOnBuildTarget(BuildTarget.Android) || !InternalEditorUtility.HasAdvancedLicenseOnBuildTarget(BuildTarget.iPhone))
            {
                GUILayout.Label(CurrentLang.MobileExportNoteLabel);
            }
            EditorGUILayout.Separator();
        }
        else if (this.photonSetupState == PhotonSetupStates.EmailAlreadyRegistered)
        {
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUILayout.Label(CurrentLang.OopsLabel);
            GUI.skin.label.fontStyle = FontStyle.Normal;

            GUILayout.Label(CurrentLang.EmailInUseLabel);

            if (GUILayout.Button(CurrentLang.SeeMyAccountPageButton))
            {
                EditorUtility.OpenWithDefaultApp(UrlCloudDashboard + Uri.EscapeUriString(this.emailAddress));
            }

            EditorGUILayout.Separator();

            GUILayout.Label(CurrentLang.KnownAppIdLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(CurrentLang.CancelButton))
            {
                this.photonSetupState = PhotonSetupStates.RegisterForPhotonCloud;
            }

            if (GUILayout.Button(CurrentLang.SetupButton))
            {
                this.photonSetupState = PhotonSetupStates.SetupPhotonCloud;
            }

            GUILayout.EndHorizontal();
        }
        else if (this.photonSetupState == PhotonSetupStates.SetupPhotonCloud)
        {
            // cloud setup
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUILayout.Label(CurrentLang.PhotonCloudConnect);
            GUI.skin.label.fontStyle = FontStyle.Normal;

            EditorGUILayout.Separator();
            this.OnGuiSetupCloudAppId();
            this.OnGuiCompareAndHelpOptions();
        }
        else if (this.photonSetupState == PhotonSetupStates.SetupSelfHosted)
        {
            // self-hosting setup
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUILayout.Label(CurrentLang.SetupOwnHostLabel);
            GUI.skin.label.fontStyle = FontStyle.Normal;

            EditorGUILayout.Separator();

            this.OnGuiSetupSelfhosting();
            this.OnGuiCompareAndHelpOptions();
        }
    }

    protected virtual void OnGuiMainWizard()
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(WizardIcon);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUILayout.Separator();

        GUILayout.Label(CurrentLang.PUNWizardLabel, EditorStyles.boldLabel);
        if (!InternalEditorUtility.HasAdvancedLicenseOnBuildTarget(BuildTarget.Android) || !InternalEditorUtility.HasAdvancedLicenseOnBuildTarget(BuildTarget.iPhone))
        {
            GUILayout.Label(CurrentLang.MobileExportNoteLabel);
        }
        EditorGUILayout.Separator();


        // settings button
        GUILayout.BeginHorizontal();
        GUILayout.Label(CurrentLang.SettingsButton, EditorStyles.boldLabel, GUILayout.Width(100));
        if (GUILayout.Button(new GUIContent(CurrentLang.SetupButton, CurrentLang.SetupServerCloudLabel)))
        {
            this.InitPhotonSetupWindow();
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.Separator();


        // find / select settings asset
        GUILayout.BeginHorizontal();
        GUILayout.Label(CurrentLang.SettingsFileLabel, EditorStyles.boldLabel, GUILayout.Width(100));
        if (GUILayout.Button(new GUIContent(CurrentLang.LocateSettingsButton, CurrentLang.SettingsHighlightLabel)))
        {
            EditorGUIUtility.PingObject(PhotonEditor.Current);
        }

        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();


        // converter
        GUILayout.BeginHorizontal();
        GUILayout.Label(CurrentLang.ConverterLabel, EditorStyles.boldLabel, GUILayout.Width(100));
        if (GUILayout.Button(new GUIContent(CurrentLang.StartButton, CurrentLang.UNtoPUNLabel)))
        {
            PhotonConverter.RunConversion();
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.Separator();


        // documentation
        GUILayout.BeginHorizontal();
        GUILayout.Label(CurrentLang.DocumentationLabel, EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.BeginVertical();
        if (GUILayout.Button(new GUIContent(CurrentLang.OpenPDFText, CurrentLang.OpenPDFTooltip)))
        {
            EditorUtility.OpenWithDefaultApp(DocumentationLocation);
        }

        if (GUILayout.Button(new GUIContent(CurrentLang.OpenDevNetText, CurrentLang.OpenDevNetTooltip)))
        {
            EditorUtility.OpenWithDefaultApp(UrlDevNet);
        }

        if (GUILayout.Button(new GUIContent(CurrentLang.OpenCloudDashboardText, CurrentLang.OpenCloudDashboardTooltip)))
        {
            EditorUtility.OpenWithDefaultApp(UrlCloudDashboard + Uri.EscapeUriString(this.emailAddress));
        }

        if (GUILayout.Button(new GUIContent(CurrentLang.OpenForumText, CurrentLang.OpenForumTooltip)))
        {
            EditorUtility.OpenWithDefaultApp(UrlForum);
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();   
    }

    protected virtual void OnGuiCompareAndHelpOptions()
    {
        GUILayout.FlexibleSpace();

        GUILayout.Label(CurrentLang.QuestionsLabel);
        if (GUILayout.Button(CurrentLang.SeeForumButton))
        {
            Application.OpenURL(UrlForum);
        }

        if (photonSetupState != PhotonSetupStates.SetupSelfHosted)
        {
            if (GUILayout.Button(CurrentLang.OpenDashboardButton))
            {
                EditorUtility.OpenWithDefaultApp(UrlCloudDashboard + Uri.EscapeUriString(this.emailAddress));
            }
        }
    }

    bool open = false;
    bool helpRegion = false;

    protected virtual void OnGuiSetupCloudAppId()
    {
        GUILayout.Label(CurrentLang.AppIdLabel);

        GUILayout.BeginHorizontal();
        this.cloudAppId = EditorGUILayout.TextField(this.cloudAppId);
        
        open = GUILayout.Toggle(open, HelpIcon, GUIStyle.none, GUILayout.ExpandWidth(false));
        
        GUILayout.EndHorizontal();
        
        if (open) GUILayout.Label(CurrentLang.AppIdInfoLabel);



        EditorGUILayout.Separator();

        GUILayout.Label(CurrentLang.CloudRegionLabel);

        int selectedRegion = ServerSettings.FindRegionForServerAddress(this.photonAddress);


        GUILayout.BeginHorizontal();
        int toolbarValue = GUILayout.Toolbar(selectedRegion, cloudServerRegionNames);   // the enum CloudServerRegion is converted into a string[] in init (toolbar can't use enum)
        helpRegion = GUILayout.Toggle(helpRegion, HelpIcon, GUIStyle.none, GUILayout.ExpandWidth(false));
        GUILayout.EndHorizontal();

        if (helpRegion) GUILayout.Label(CurrentLang.RegionalServersInfo);

        if (selectedRegion != toolbarValue)
        {
            //Debug.Log("Replacing region: " + selectedRegion + " with: " + toolbarValue);
            this.photonAddress = ServerSettings.FindServerAddressForRegion(toolbarValue);
        }

        EditorGUILayout.Separator();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(CurrentLang.CancelButton))
        {
            GUIUtility.keyboardControl = 0;
            this.ReApplySettingsToWindow();
        }

        

        if (GUILayout.Button(CurrentLang.SaveButton))
        {
            GUIUtility.keyboardControl = 0;
			this.cloudAppId = this.cloudAppId.Trim();
            PhotonEditor.Current.UseCloud(this.cloudAppId, selectedRegion);
            PhotonEditor.Save();

            EditorUtility.DisplayDialog(CurrentLang.SettingsSavedTitle, CurrentLang.SettingsSavedMessage, CurrentLang.OkButton);
        }

        GUILayout.EndHorizontal();


        
        GUILayout.Space(20);

        GUILayout.Label(CurrentLang.SetupOwnServerLabel);

        if (GUILayout.Button(CurrentLang.SelfHostSettingsButton))
        {
            this.photonAddress = ServerSettings.DefaultServerAddress;
            this.photonPort = ServerSettings.DefaultMasterPort;
            this.photonSetupState = PhotonSetupStates.SetupSelfHosted;
        }

        EditorGUILayout.Separator();
        GUILayout.Label(CurrentLang.OwnHostCloudCompareLabel);
        if (GUILayout.Button(CurrentLang.ComparisonPageButton))
        {
            Application.OpenURL(UrlCompare);
        }
    }

    protected virtual void OnGuiSetupSelfhosting()
    {
        GUILayout.Label(CurrentLang.YourPhotonServerLabel);

        this.photonAddress = EditorGUILayout.TextField(CurrentLang.AddressIPLabel, this.photonAddress);
        this.photonPort = EditorGUILayout.IntField(CurrentLang.PortLabel, this.photonPort);

        EditorGUILayout.Separator();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(CurrentLang.CancelButton))
        {
            GUIUtility.keyboardControl = 0;
            this.ReApplySettingsToWindow();
        }

        if (GUILayout.Button(CurrentLang.SaveButton))
        {
            GUIUtility.keyboardControl = 0;

            PhotonEditor.Current.UseMyServer(this.photonAddress, this.photonPort, null);
            PhotonEditor.Save();

            EditorUtility.DisplayDialog(CurrentLang.SettingsSavedTitle, CurrentLang.SettingsSavedMessage, CurrentLang.OkButton);
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Separator();

        // license
        GUILayout.BeginHorizontal();
        GUILayout.Label(CurrentLang.LicensesLabel, EditorStyles.boldLabel, GUILayout.Width(100));

        if (GUILayout.Button(new GUIContent(CurrentLang.LicenseDownloadText, CurrentLang.LicenseDownloadTooltip)))
        {
            EditorUtility.OpenWithDefaultApp(UrlFreeLicense);
        }

        GUILayout.EndHorizontal();


        GUILayout.Space(20);


        GUILayout.Label(CurrentLang.TryPhotonAppLabel);

        if (GUILayout.Button(CurrentLang.GetCloudAppButton))
        {
            this.cloudAppId = string.Empty;
            this.photonSetupState = PhotonSetupStates.RegisterForPhotonCloud;
        }

        EditorGUILayout.Separator();
        GUILayout.Label(CurrentLang.OwnHostCloudCompareLabel);
        if (GUILayout.Button(CurrentLang.ComparisonPageButton))
        {
            Application.OpenURL(UrlCompare);
        }
    }

    protected virtual void RegisterWithEmail(string email)
    {
        EditorUtility.DisplayProgressBar(CurrentLang.ConnectionTitle, CurrentLang.ConnectionInfo, 0.5f);
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
            if (client.Message.Contains(CurrentLang.EmailInUseLabel))
            {
                this.photonSetupState = PhotonSetupStates.EmailAlreadyRegistered;
            }
            else
            {
                EditorUtility.DisplayDialog(CurrentLang.ErrorTextTitle, client.Message, CurrentLang.OkButton);
                // Debug.Log(client.Exception);
                this.photonSetupState = PhotonSetupStates.RegisterForPhotonCloud;
            }
        }
    }

    #region SettingsFileHandling

    private static ServerSettings currentSettings;
    private Vector2 MinSize = new Vector2(350, 400);

    public static ServerSettings Current
    {
        get
        {
            if (currentSettings == null)
            {
                // find out if ServerSettings can be instantiated (existing script check)
                ScriptableObject serverSettingTest = CreateInstance("ServerSettings");
                if (serverSettingTest == null)
                {
                    Debug.LogError(CurrentLang.ServerSettingsMissingLabel);
                    return null;
                }
                DestroyImmediate(serverSettingTest);

                // try to load settings from file
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

                    currentSettings = (ServerSettings) ScriptableObject.CreateInstance("ServerSettings");
                    if (currentSettings != null)
                    {
                        AssetDatabase.CreateAsset(currentSettings, PhotonNetwork.serverSettingsAssetPath);
                    }
                    else
                    {
                        Debug.LogError(CurrentLang.ServerSettingsMissingLabel);
                    }
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
                Debug.LogWarning(CurrentLang.MoreThanOneLabel + PhotonNetwork.serverSettingsAssetFile + CurrentLang.FilesInResourceFolderLabel + AssetDatabase.GetAssetPath(PhotonEditor.Current));
            }
        }
    }

    protected void ReApplySettingsToWindow()
    {
        this.cloudAppId = string.IsNullOrEmpty(PhotonEditor.Current.AppID) ? string.Empty : PhotonEditor.Current.AppID;
        this.photonAddress = string.IsNullOrEmpty(PhotonEditor.Current.ServerAddress) ? string.Empty : PhotonEditor.Current.ServerAddress;
        this.photonPort = PhotonEditor.Current.ServerPort;
    }
    
    public static void UpdateRpcList()
    {
        HashSet<string> additionalRpcs = new HashSet<string>();
        HashSet<string> currentRpcs = new HashSet<string>();

        var types = GetAllSubTypesInScripts(typeof(MonoBehaviour));

        foreach (var mono in types)
        {
            MethodInfo[] methods = mono.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (MethodInfo method in methods)
            {
                if (method.IsDefined(typeof(UnityEngine.RPC), false))
                {
                    currentRpcs.Add(method.Name);

                    if (!PhotonEditor.Current.RpcList.Contains(method.Name))
                    {
                        additionalRpcs.Add(method.Name);
                    }
                }
            }
        }

        if (additionalRpcs.Count > 0)
        {
            // LIMITS RPC COUNT
            if (additionalRpcs.Count + PhotonEditor.Current.RpcList.Count >= byte.MaxValue)
            {
                if (currentRpcs.Count <= byte.MaxValue)
                {
                    bool clearList = EditorUtility.DisplayDialog(CurrentLang.IncorrectRPCListTitle, CurrentLang.IncorrectRPCListLabel, CurrentLang.RemoveOutdatedRPCsLabel, CurrentLang.CancelButton);
                    if (clearList)
                    {
                        PhotonEditor.Current.RpcList.Clear();
                        PhotonEditor.Current.RpcList.AddRange(currentRpcs);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog(CurrentLang.FullRPCListTitle, CurrentLang.FullRPCListLabel, CurrentLang.SkipRPCListUpdateLabel);
                    return;
                }
            }

            PhotonEditor.Current.RpcList.AddRange(additionalRpcs);
            EditorUtility.SetDirty(PhotonEditor.Current);
        }
    }

    public static void ClearRpcList()
    {
        bool clearList = EditorUtility.DisplayDialog(CurrentLang.PUNNameReplaceTitle, CurrentLang.PUNNameReplaceLabel, CurrentLang.RPCListCleared, CurrentLang.CancelButton);
        if (clearList)
        {
            PhotonEditor.Current.RpcList.Clear();
            Debug.LogWarning(CurrentLang.ServerSettingsCleanedWarning);
        }
    }

    public static System.Type[] GetAllSubTypesInScripts(System.Type aBaseClass)
    {
        var result = new System.Collections.Generic.List<System.Type>();
        System.Reflection.Assembly[] AS = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var A in AS)
        {
            // this skips all but the Unity-scripted assemblies for RPC-list creation. You could remove this to search all assemblies in project
            if (!A.FullName.StartsWith("Assembly-"))
            {
                // Debug.Log("Skipping Assembly: " + A);
                continue;
            }

            //Debug.Log("Assembly: " + A.FullName);
            System.Type[] types = A.GetTypes();
            foreach (var T in types)
            {
                if (T.IsSubclassOf(aBaseClass))
                    result.Add(T);
            }
        }
        return result.ToArray();
    }

    #endregion
}
