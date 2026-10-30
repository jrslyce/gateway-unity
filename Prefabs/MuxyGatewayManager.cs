using System;
using System.Collections.Generic;
using UnityEngine;
using MuxyGateway;
using UnityEngine.Events;


public class MuxyGatewayManager : MonoBehaviour
{

    [HideInInspector]
    public String GameID = "gamelink-testing";

    [SerializeField] private GameMetadata GameMetadata = new GameMetadata();
    [Tooltip("Optional logo texture. It is encoded as a PNG data URI when game metadata is published.")]
    public Texture2D GameLogo;
    [Tooltip("Remember the broadcaster session on this device using Unity PlayerPrefs, which is not encrypted.")]
    public bool RememberSession = true;
    [SerializeField] private List<GameAction> GameActions = new List<GameAction>();
    [SerializeField] private List<GameText> GameTexts = new List<GameText>();
    [SerializeField] private PollConfiguration PollConfiguration = new PollConfiguration();
    public MuxyGatewayPollUpdate OnPollUpdate = new MuxyGatewayPollUpdate();

    [Header("Events")]
    public MuxyGatewayAuthenticationEvent OnAuthentication = new MuxyGatewayAuthenticationEvent();
    public MuxyGatewayGameActionUsedEvent OnAnyGameActionUsed = new MuxyGatewayGameActionUsedEvent();
    public MuxyGatewayBitsUsedEvent OnBitsUsed = new MuxyGatewayBitsUsedEvent();
    public GatewayTransportStateEvent OnTransportStateChanged = new GatewayTransportStateEvent();

    [Serializable]
    public class MuxyGatewayAuthenticationEvent : UnityEvent<AuthenticationResponse> { }
    [Serializable]
    public class MuxyGatewayBitsUsedEvent : UnityEvent<BitsUsed> { }
    [Serializable]
    public class MuxyGatewayPollUpdate : UnityEvent<PollUpdate> { }
    [Serializable]
    public class GatewayTransportStateEvent : UnityEvent<GatewayTransportState> { }


    // Private //

    private const string PLAYER_PREFS_REFRESH_TOKEN = "MuxyGatewayRefreshToken";

    private SDK SDK;
    private SDK.OnAuthenticateDelegate AuthCB;
    private SDK.OnGameActionUsedDelegate ActionCB;
    private SDK.OnBitsUsedDelegate BitsCB;

    private bool DidOpenAndRun = false;
    private GatewayTransportState lastTransportState = GatewayTransportState.Disconnected;
    private float pinStartedAt = -1f;
    private float transportStartedAt = -1f;
    private int connectionGeneration;

    private void SetupCallbacks()
    {
        int generation = connectionGeneration;
        ActionCB = (Used) =>
        {
            if (generation != connectionGeneration) return;
            OnAnyGameActionUsed.Invoke(Used);
        };

        BitsCB = (Used) =>
        {
            if (generation != connectionGeneration) return;
            OnBitsUsed.Invoke(Used);
        };

        AuthCB = (Response) =>
        {
            if (generation != connectionGeneration) return;
            if (pinStartedAt >= 0f)
            {
                Debug.Log("Hypeline: Authentication response received after " + (Time.realtimeSinceStartup - pinStartedAt).ToString("F1") + " seconds.");
                pinStartedAt = -1f;
            }
            if (Response.HasError)
            {
                Debug.LogWarning("Hypeline: Twitch authentication failed. Request a new code and check the extension configuration.");
                PlayerPrefs.SetString(PLAYER_PREFS_REFRESH_TOKEN, "");
                OnAuthentication.Invoke(Response);
                return;
            }

            Debug.Log("Hypeline: Twitch authentication succeeded.");
            if (RememberSession) PlayerPrefs.SetString(PLAYER_PREFS_REFRESH_TOKEN, Response.RefreshToken);
            else ClearSavedSession();

            SDK.OnGameActionUsed(ActionCB);
            SDK.OnBitsUsed(BitsCB);
            SDK.SetGameActions(GameActions.ToArray());
            SDK.SetGameMetadata(BuildGameMetadata());
            SDK.SetGameTexts(GameTexts.ToArray());
            OnAuthentication.Invoke(Response);
        };

    }

    private void CheckForRefreshToken()
    {
        if (!RememberSession) return;
        String RefreshToken = PlayerPrefs.GetString(PLAYER_PREFS_REFRESH_TOKEN, "");
        if (RefreshToken != "")
        {
            Debug.Log("Hypeline: Restoring the saved Twitch session.");
            OpenAndRunSDK();
            SDK.AuthenticateWithRefreshToken(RefreshToken, AuthCB);
        }
    }

    protected void OnDisable()
    {
        connectionGeneration++;
        DidOpenAndRun = false;
        if (SDK != null)
        {
            SDK.StopWebsocketTransport();
            SDK = null;
        }
    }

    protected void OnEnable()
    {
        if (String.IsNullOrWhiteSpace(GameID)) GameID = "gamelink-testing";
        if (SDK == null)
        {
            SDK = new SDK(GameID);
            DidOpenAndRun = false;
            SetupCallbacks();
            CheckForRefreshToken();
        }

        if (GameTexts.Count > 0) SubmitGameTexts();
        if (GameActions.Count > 0) SubmitGameActions();
    }


    public void OpenAndRunSDK()
    {
        if (DidOpenAndRun) return;
        if (SDK.TransportState == GatewayTransportState.Error)
        {
            ResetConnection();
        }
        transportStartedAt = Time.realtimeSinceStartup;
        Debug.Log("Hypeline: Starting the Twitch connection.");
        SDK.RunInProduction();
        DidOpenAndRun = true;
    }

    protected void Update()
    {
        if (SDK == null) return;
        GatewayTransportState state = SDK.TransportState;
        if (state != lastTransportState)
        {
            lastTransportState = state;
            if (state == GatewayTransportState.Connected && transportStartedAt >= 0f)
            {
                Debug.Log("Hypeline: Connection transport opened after " + (Time.realtimeSinceStartup - transportStartedAt).ToString("F1") + " seconds.");
                transportStartedAt = -1f;
            }
            if (state == GatewayTransportState.Error)
                Debug.LogWarning("Hypeline: Connection transport failed. Check the network and earlier transport warnings.");
            else
                Debug.Log("Hypeline: Connection transport is " + state + ".");
            OnTransportStateChanged.Invoke(state);
            if (SDK == null) return;
        }
        if (SDK.TransportState == GatewayTransportState.Error) DidOpenAndRun = false;
        SDK.Update();
    }

    /// <summary>Abandons the current connection so a fresh attempt cannot receive stale callbacks.</summary>
    public void ResetConnection()
    {
        connectionGeneration++;
        SDK previous = SDK;
        SDK = null;
        DidOpenAndRun = false;
        lastTransportState = GatewayTransportState.Disconnected;
        pinStartedAt = -1f;
        transportStartedAt = -1f;
        if (previous != null) previous.StopWebsocketTransport();
        if (isActiveAndEnabled)
        {
            if (String.IsNullOrWhiteSpace(GameID)) GameID = "gamelink-testing";
            SDK = new SDK(GameID);
            SetupCallbacks();
        }
        Debug.Log("Hypeline: Connection reset. Enter a new Twitch code to retry.");
    }

    public void AuthenticateWithPIN(String PIN)
    {
        pinStartedAt = Time.realtimeSinceStartup;
        Debug.Log("Hypeline: Twitch code submitted; waiting for authentication.");
        OpenAndRunSDK();
        SDK.AuthenticateWithPIN(PIN, AuthCB);
    }

    public bool IsAuthenticated()
    {
        return SDK != null && SDK.IsAuthenticated;
    }

    /// <summary>Remove the locally stored session without disconnecting the current session.</summary>
    public void ClearSavedSession()
    {
        PlayerPrefs.DeleteKey(PLAYER_PREFS_REFRESH_TOKEN);
    }

    public GatewayTransportState TransportState
    {
        get { return SDK == null ? GatewayTransportState.Disconnected : SDK.TransportState; }
    }

    public void Deauthenticate()
    {
        ClearSavedSession();
        if (SDK != null) SDK.Deauthenticate();
    }

    public void AddGameText(GameText Text)
    {
        GameTexts.Add(Text);
    }

    public void ClearGameTexts()
    {
        GameTexts.Clear();
    }

    public void SubmitGameTexts()
    {
        SDK.SetGameTexts(GameTexts.ToArray());
    }

    public GameText GetGameTextAt(int Index)
    {
        return GameTexts[Index];
    }

    public void SetGameTextAt(GameText Text, int Index)
    {
        GameTexts[Index] = Text;
        SDK.SetGameTexts(GameTexts.ToArray());
    }

    public void RemoveGameTextAt(int Index)
    {
        GameTexts.RemoveAt(Index);
        SDK.SetGameTexts(GameTexts.ToArray());
    }

    public GameAction GetGameActionAt(int Index)
    {
        return GameActions[Index];
    }

    public GameAction? FindGameAction(string ActionID)
    {
        foreach (GameAction Action in GameActions)
        {
            if (Action.ID == ActionID) return Action;
        }

        return null;
    }

    public void SetGameActionAt(GameAction Action, int Index)
    {
        GameActions[Index] = Action;
    }

    public void RemoveGameActionAt(int Index)
    {
        GameActions.RemoveAt(Index);
    }

    public void SubmitGameActions()
    {
        SDK.SetGameActions(GameActions.ToArray());
    }

    public void SetPollConfiguration(PollConfiguration Config)
    {
        PollConfiguration = Config;
    }

    public PollConfiguration GetPollConfiguration()
    {
        return PollConfiguration;
    }

    public void StartPoll()
    {
        SDK.StartPoll(PollConfiguration);
    }

    public void StopPoll()
    {
        SDK.StopPoll();
    }
    public void SetGameMetadata(GameMetadata Metadata)
    {
        GameMetadata = Metadata;
        SDK.SetGameMetadata(BuildGameMetadata());
    }

    private GameMetadata BuildGameMetadata()
    {
        GameMetadata metadata = new GameMetadata
        {
            Name = GameMetadata.Name,
            Logo = GameMetadata.Logo,
            Theme = GameMetadata.Theme
        };

#if UNITY_EDITOR || UNITY_STANDALONE
        if (GameLogo != null)
        {
            try
            {
                metadata.Logo = SDK.ConvertTextureToBase64(GameLogo);
            }
            catch (Exception exception)
            {
                Debug.LogError("Muxy Gateway logo could not be encoded. Enable Read/Write and use RGB24, RGBA32, or ARGB32 on the texture. " + exception.Message);
            }
        }
#endif

        return metadata;
    }

    public GameMetadata GetGameMetadata()
    {
        return GameMetadata;
    }

    public void AcceptGameAction(GameActionUsed Used, string Description)
    {
        SDK.AcceptGameAction(Used, Description);
    }

    public void RefundGameAction(GameActionUsed Used, string Description)
    {
        SDK.RefundGameAction(Used, Description);
    }

    public void SetAllGameActionStates(GameActionState State)
    {
        foreach (var Action in GameActions)
        {
            Action.State = State;
        }
        SDK.SetGameActions(GameActions.ToArray());
    }
}
