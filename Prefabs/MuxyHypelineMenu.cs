using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MuxyGateway;

/// <summary>Creator-facing Hypeline menu and Twitch connection panel.</summary>
public class MuxyHypelineMenu : MonoBehaviour
{
    [InspectorName("Hypeline Manager")]
    public MuxyGatewayManager GatewayManager;
    public Texture2D FoxArtwork;
    public Texture2D WordmarkArtwork;
    [TextArea] public string GameDescription = "Let your live audience interact with your game on Twitch and TikTok.";
    public bool ShowTikTokComingSoon = true;
    public string TwitchSetupUrl = "https://dashboard.twitch.tv/extensions/i575hs2x9lb3u8hqujtezit03w1740";
    public Color DisconnectedColor = new Color(0.67f, 0.70f, 0.76f);
    public Color ConnectingColor = new Color(1f, 0.72f, 0.22f);
    public Color ConnectedColor = new Color(0.30f, 0.86f, 0.52f);
    public Color ErrorColor = new Color(1f, 0.36f, 0.34f);
    public Vector2 MenuButtonPosition = new Vector2(24, -24);
    public Vector2 MenuButtonSize = new Vector2(300, 76);

    // Kept for source and serialized-scene compatibility with the previous menu.
    // Edit the redesigned prefab's RectTransforms to change its panel layout.
    [HideInInspector] public Vector2 PanelSize = new Vector2(760, 760);
    [HideInInspector] public Vector2 WordmarkPosition = new Vector2(-190, 320);
    [HideInInspector] public Vector2 WordmarkSize = new Vector2(310, 83);
    [HideInInspector] public Vector2 CloseButtonPosition = new Vector2(315, 320);
    [HideInInspector] public Vector2 CloseButtonSize = new Vector2(100, 44);
    [HideInInspector] public Vector2 DescriptionPosition = new Vector2(0, 245);
    [HideInInspector] public Vector2 DescriptionSize = new Vector2(660, 60);
    [HideInInspector] public Vector2 PlatformPosition = new Vector2(0, 183);
    [HideInInspector] public Vector2 PlatformSize = new Vector2(660, 38);
    [HideInInspector] public Vector2 InstructionsPosition = new Vector2(0, 95);
    [HideInInspector] public Vector2 InstructionsSize = new Vector2(660, 130);
    [HideInInspector] public Vector2 CodeInputPosition = new Vector2(-140, -12);
    [HideInInspector] public Vector2 CodeInputSize = new Vector2(360, 55);
    [HideInInspector] public Vector2 ConnectButtonPosition = new Vector2(208, -12);
    [HideInInspector] public Vector2 ConnectButtonSize = new Vector2(220, 55);
    [HideInInspector] public Vector2 StatusPosition = new Vector2(0, -85);
    [HideInInspector] public Vector2 StatusSize = new Vector2(660, 30);
    [HideInInspector] public Vector2 MessagePosition = new Vector2(0, -140);
    [HideInInspector] public Vector2 MessageSize = new Vector2(660, 65);
    [HideInInspector] public Vector2 SetupButtonPosition = new Vector2(-184, -230);
    [HideInInspector] public Vector2 SetupButtonSize = new Vector2(290, 55);
    [HideInInspector] public Vector2 DisconnectButtonPosition = new Vector2(184, -230);
    [HideInInspector] public Vector2 DisconnectButtonSize = new Vector2(290, 55);

    public UnityEvent OnPanelOpened = new UnityEvent();
    public UnityEvent OnPanelClosed = new UnityEvent();

    [SerializeField] private Image fox, statusFox, statusBackground;
    [SerializeField] private UnityEngine.UI.Text menuStatus, panelStatus, message;
    [SerializeField] private InputField code;
    [SerializeField] private GameObject modal;
    [SerializeField] private Button menuButton, connectButton, disconnectButton, closeButton, setupButton, configurationButton;
    private bool requested, authError, authenticationTimedOut;
    private float authenticationDeadline;
    private const float AuthenticationTimeoutSeconds = 8f;
    private string lastStatus;
    private GameObject previousSelection;
    private RawImage twitchIndicator, tikTokIndicator;
    private Image connectionProgress;

    private void Awake()
    {
        if (GetComponentInParent<Canvas>() == null)
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
        }
        // Old serialized menu instances are upgraded without touching unrelated children.
        if (GameDescription == "Let your live audience interact with your game.")
            GameDescription = "Let your live audience interact with your game on Twitch and TikTok.";
        if (modal != null && modal.transform.Find("Card/Headline") == null)
        {
            if (menuButton != null) { menuButton.gameObject.SetActive(false); Destroy(menuButton.gameObject); }
            modal.SetActive(false);
            Destroy(modal);
            modal = null;
        }
        if (modal == null) BuildUI();
        else
        {
            menuButton.onClick.AddListener(OpenPanel);
            closeButton.onClick.AddListener(ClosePanel);
            connectButton.onClick.AddListener(Connect);
            setupButton.onClick.AddListener(OpenTwitchSetup);
            configurationButton.onClick.AddListener(OpenTwitchSetup);
            disconnectButton.onClick.AddListener(Disconnect);
            fox.sprite = MakeSprite(FoxArtwork);
            statusFox.sprite = MakeSprite(FoxArtwork);
            var mark = modal.transform.Find("Card/Hypeline wordmark");
            if (mark != null) mark.GetComponent<Image>().sprite = MakeSprite(WordmarkArtwork);
            var description = modal.transform.Find("Card/Description");
            if (description != null) description.GetComponent<UnityEngine.UI.Text>().text = GameDescription;
            var platform = modal.transform.Find("Card/TikTok");
            if (platform != null) platform.gameObject.SetActive(ShowTikTokComingSoon);
        }
        EnsurePlatformIndicators();
        EnsureConnectionProgress();
        // The prefab can be saved without a parent canvas; set this after it is instantiated.
        var overlayCanvas = modal.GetComponent<Canvas>();
        if (overlayCanvas != null) { overlayCanvas.overrideSorting = true; overlayCanvas.sortingOrder = 10000; }
        modal.SetActive(false);
    }

    private void OnEnable()
    {
        if (GatewayManager != null) GatewayManager.OnAuthentication.AddListener(AuthenticationFinished);
    }

    private void OnDisable()
    {
        if (GatewayManager != null) GatewayManager.OnAuthentication.RemoveListener(AuthenticationFinished);
    }

    private void Update()
    {
        if (requested && (GatewayManager == null || !GatewayManager.IsAuthenticated()) && Time.realtimeSinceStartup >= authenticationDeadline)
        {
            requested = false;
            authenticationTimedOut = true;
            authError = true;
            lastStatus = null;
            Debug.LogWarning("Hypeline: No Twitch authentication response after 8 seconds. Select Try again to reset the connection, then request a new code.");
        }
        string status;
        Color color;
        if (GatewayManager == null) { status = "Setup required"; color = ErrorColor; }
        else if (GatewayManager.TransportState == GatewayTransportState.Error || authError)
        { status = "Connection issue"; color = ErrorColor; }
        else if (GatewayManager.TransportState == GatewayTransportState.Reconnecting)
        { status = "Reconnecting..."; color = ConnectingColor; }
        else if (GatewayManager.TransportState == GatewayTransportState.Connected && GatewayManager.IsAuthenticated())
        { status = "Connected"; color = ConnectedColor; requested = false; }
        else if (requested || GatewayManager.TransportState == GatewayTransportState.Connecting || GatewayManager.TransportState == GatewayTransportState.Connected)
        { status = "Connecting..."; color = ConnectingColor; }
        else { status = "Not connected"; color = DisconnectedColor; }

        fox.color = color;
        UpdatePlatformIndicators(status == "Connected");
        menuStatus.text = status;
        float remaining = Mathf.Max(0f, authenticationDeadline - Time.realtimeSinceStartup);
        bool showingProgress = requested && status == "Connecting...";
        panelStatus.text = status == "Connected" ? "You are connected" : showingProgress ? "Connecting... " + Mathf.CeilToInt(remaining) + "s" : status;
        panelStatus.color = color;
        statusFox.color = showingProgress ? new Color(.20f, .28f, .25f, .85f) : color;
        if (connectionProgress != null)
        {
            connectionProgress.gameObject.SetActive(showingProgress);
            if (showingProgress) connectionProgress.fillAmount = Mathf.Clamp01(1f - remaining / AuthenticationTimeoutSeconds);
        }
        statusBackground.color = status == "Connected" ? new Color(.025f, .17f, .09f) : new Color(.075f, .095f, .09f);
        if (status != lastStatus)
        {
            lastStatus = status;
            if (status == "Connected") message.text = "Twitch is ready.";
            else if (status == "Not connected") message.text = "Follow the steps above to connect Twitch.";
            else if (status == "Connection issue") message.text = authenticationTimedOut
                ? "No response yet. Select Try again, then get a new code."
                : "Could not connect. Check your internet connection or get a new code and try again.";
            else if (status == "Reconnecting...") message.text = "Trying to restore your connection.";
            else if (status == "Setup required") message.text = "Assign the Hypeline Manager in the Inspector.";
        }
        if (requested && status == "Connecting...")
            message.text = GatewayManager.TransportState == GatewayTransportState.Connected
                ? "Connected to the service; waiting for Twitch authentication."
                : "Opening the connection to the service...";
        connectButton.interactable = GatewayManager != null && !authenticationTimedOut && status != "Connected" && status != "Connecting..." && status != "Reconnecting...";
        bool canReset = GatewayManager != null && status == "Connection issue";
        disconnectButton.interactable = GatewayManager != null && (status == "Connected" || status == "Reconnecting..." || canReset);
        var disconnectLabel = disconnectButton.GetComponentInChildren<UnityEngine.UI.Text>();
        if (disconnectLabel != null) disconnectLabel.text = canReset ? "Try again" : "Disconnect";
    }

    private void AuthenticationFinished(AuthenticationResponse response)
    {
        requested = false;
        authenticationTimedOut = false;
        authError = response.HasError;
        if (code != null) code.text = "";
    }

    public void OpenPanel()
    {
        if (EventSystem.current != null) previousSelection = EventSystem.current.currentSelectedGameObject;
        modal.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(code.gameObject);
        OnPanelOpened.Invoke();
    }

    public void ClosePanel()
    {
        modal.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection != null ? previousSelection : menuButton.gameObject);
        OnPanelClosed.Invoke();
    }

    private void Connect()
    {
        string pin = code.text.Trim();
        if (pin.Length != 6)
        {
            message.text = "Enter the six-character code from Twitch.";
            return;
        }
        authError = false;
        authenticationTimedOut = false;
        requested = true;
        authenticationDeadline = Time.realtimeSinceStartup + AuthenticationTimeoutSeconds;
        message.text = "Connecting to Twitch...";
        GatewayManager.AuthenticateWithPIN(pin);
        code.text = "";
    }

    private void Disconnect()
    {
        if (GatewayManager.IsAuthenticated()) GatewayManager.Deauthenticate();
        else GatewayManager.ClearSavedSession();
        GatewayManager.ResetConnection();
        requested = false;
        authError = false;
        authenticationTimedOut = false;
        lastStatus = null;
        if (code != null) code.text = "";
        message.text = "Connection reset. Enter a new Twitch code to connect.";
    }

    private void OpenTwitchSetup() { Application.OpenURL(TwitchSetupUrl); }

#if UNITY_EDITOR
    // Used by the package's prefab-generation tool; the resulting hierarchy is serialized.
    public void BuildEditableHierarchy()
    {
        if (modal != null || transform.childCount != 0) throw new InvalidOperationException("Menu already has children.");
        BuildUI();
        modal.SetActive(true);
    }
#endif

    private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private static Image Box(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Color color)
    {
        Image image = Rect(name, parent, min, max, pos, size).gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

#if UNITY_2022_2_OR_NEWER
    private const string BuiltinFontName = "LegacyRuntime.ttf";
#else
    private const string BuiltinFontName = "Arial.ttf";
#endif

    private static UnityEngine.UI.Text Text(string name, Transform parent, string value, Vector2 pos, Vector2 size, float fontSize, TextAnchor alignment)
    {
        UnityEngine.UI.Text text = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size).gameObject.AddComponent<UnityEngine.UI.Text>();
        text.text = value; text.font = Resources.GetBuiltinResource<Font>(BuiltinFontName); text.fontSize = Mathf.RoundToInt(fontSize); text.alignment = alignment; text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static void AddTealOutline(Image image)
    {
        Outline outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.8f, 0.8f);
        outline.effectDistance = new Vector2(1, -1);
    }

    private static Button MakeButton(string name, Transform parent, string label, Vector2 pos, Vector2 size, Color color, UnityAction action)
    {
        Image image = Box(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, color);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image; button.onClick.AddListener(action);
        Text("Label", image.transform, label, Vector2.zero, size - new Vector2(12, 6), 17, TextAnchor.MiddleCenter).color = new Color(0.05098f, 0.05098f, 0.05098f);
        return button;
    }

    private void EnsureConnectionProgress()
    {
        if (statusFox == null) return;
        Transform existing = statusFox.transform.Find("Connection progress");
        RectTransform rect = existing != null ? existing.GetComponent<RectTransform>() : Rect("Connection progress", statusFox.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        if (rect == null) return;
        connectionProgress = rect.GetComponent<Image>();
        if (connectionProgress == null) connectionProgress = rect.gameObject.AddComponent<Image>();
        connectionProgress.sprite = statusFox.sprite;
        connectionProgress.preserveAspect = true;
        connectionProgress.type = Image.Type.Filled;
        connectionProgress.fillMethod = Image.FillMethod.Vertical;
        connectionProgress.fillOrigin = (int)Image.OriginVertical.Bottom;
        connectionProgress.fillAmount = 0f;
        connectionProgress.color = ConnectedColor;
        connectionProgress.raycastTarget = false;
        connectionProgress.gameObject.SetActive(false);
    }

    private void EnsurePlatformIndicators()
    {
        if (menuButton == null) return;
        Transform parent = menuButton.transform;
        twitchIndicator = MakePlatformIndicator(parent, "Twitch indicator", "HypelinePlatformIcons/twitch", new Vector2(83, -14));
        tikTokIndicator = MakePlatformIndicator(parent, "TikTok indicator", "HypelinePlatformIcons/tiktok", new Vector2(116, -14));
        if (tikTokIndicator != null) { tikTokIndicator.color = new Color(.49f, .52f, .51f, .8f); tikTokIndicator.gameObject.SetActive(ShowTikTokComingSoon); }
    }

    private void UpdatePlatformIndicators(bool connected)
    {
        if (twitchIndicator != null) twitchIndicator.color = connected ? new Color(.66f, .42f, 1f) : new Color(.49f, .52f, .51f, .8f);
        if (tikTokIndicator != null) tikTokIndicator.gameObject.SetActive(ShowTikTokComingSoon);
    }

    private static RawImage MakePlatformIndicator(Transform parent, string name, string resource, Vector2 position)
    {
        Transform existing = parent.Find(name);
        RectTransform rect = existing != null ? existing.GetComponent<RectTransform>() : Rect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, new Vector2(22, 22));
        if (rect == null) return null;
        RawImage image = rect.GetComponent<RawImage>();
        if (image == null) image = rect.gameObject.AddComponent<RawImage>();
        image.texture = Resources.Load<Texture2D>(resource);
        image.raycastTarget = false;
        return image;
    }

    private static Sprite MakeSprite(Texture2D texture)
    {
        return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private void BuildUI()
    {
        Color surface = new Color(.055f, .075f, .07f);
        Color section = new Color(.075f, .095f, .09f);
        Color teal = new Color(.60f, .80f, .80f);
        Vector2 center = new Vector2(.5f, .5f);
        Image menu = Box("Stream Interactivity", transform, new Vector2(0, 1), new Vector2(0, 1), MenuButtonPosition + new Vector2(MenuButtonSize.x / 2, -MenuButtonSize.y / 2), MenuButtonSize, surface);
        AddTealOutline(menu);
        menuButton = menu.gameObject.AddComponent<Button>(); menuButton.targetGraphic = menu; menuButton.onClick.AddListener(OpenPanel);
        fox = Box("Fox status", menu.transform, center, center, new Vector2(-112, 0), new Vector2(48, 48), DisconnectedColor);
        fox.sprite = MakeSprite(FoxArtwork); fox.preserveAspect = true; fox.raycastTarget = false;
        Text("Title", menu.transform, "Stream Interactivity", new Vector2(30, 13), new Vector2(204, 27), 21, TextAnchor.MiddleLeft).color = teal;
        menuStatus = Text("Status", menu.transform, "Not connected", new Vector2(-9, -14), new Vector2(126, 22), 15, TextAnchor.MiddleLeft);

        modal = Box("Hypeline Panel", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, .72f)).gameObject;
        // Keep the modal above game sprites even when the parent canvas uses a low sorting order.
        Canvas modalCanvas = modal.AddComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = 10000;
        modal.AddComponent<GraphicRaycaster>();
        Image card = Box("Card", modal.transform, center, center, Vector2.zero, new Vector2(720, 680), surface);
        AddTealOutline(card);
        Image mark = Box("Hypeline wordmark", card.transform, center, center, new Vector2(-190, 292), new Vector2(270, 62), Color.white);
        mark.sprite = MakeSprite(WordmarkArtwork); mark.preserveAspect = true; mark.raycastTarget = false;
        closeButton = MakeButton("Close", card.transform, "Close", new Vector2(290, 292), new Vector2(90, 42), teal, ClosePanel);
        var headline = Text("Headline", card.transform, "Play with your audience", new Vector2(0, 230), new Vector2(640, 49), 34, TextAnchor.MiddleLeft);
        headline.fontStyle = FontStyle.Bold;
        Text("Description", card.transform, GameDescription, new Vector2(0, 170), new Vector2(640, 52), 20, TextAnchor.MiddleLeft).lineSpacing = 1.2f;
        Image twitch = Box("Twitch", card.transform, center, center, new Vector2(-165, 110), new Vector2(310, 46), section);
        AddTealOutline(twitch);
        Text("Label", twitch.transform, "TWITCH", Vector2.zero, new Vector2(290, 40), 20, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
        Image tiktok = Box("TikTok", card.transform, center, center, new Vector2(165, 110), new Vector2(310, 46), section);
        Text("Label", tiktok.transform, "TikTok  -  Coming soon", Vector2.zero, new Vector2(290, 40), 18, TextAnchor.MiddleCenter).color = new Color(.60f, .64f, .62f);
        tiktok.gameObject.SetActive(ShowTikTokComingSoon);

        Image steps = Box("Setup steps", card.transform, center, center, new Vector2(0, -8), new Vector2(640, 172), section);
        AddTealOutline(steps);
        Text("Step 1", steps.transform, "1   Install and activate the extension", new Vector2(-108, 55), new Vector2(400, 40), 19, TextAnchor.MiddleLeft);
        setupButton = MakeButton("Open Twitch", steps.transform, "Open Twitch", new Vector2(205, 55), new Vector2(205, 42), teal, OpenTwitchSetup);
        Text("Step 2", steps.transform, "2   Open Configure and get a code", new Vector2(-108, 0), new Vector2(400, 40), 19, TextAnchor.MiddleLeft);
        configurationButton = MakeButton("Open configuration", steps.transform, "Open configuration", new Vector2(205, 0), new Vector2(205, 42), teal, OpenTwitchSetup);
        Text("Step 3", steps.transform, "3   Connect your game", new Vector2(-200, -55), new Vector2(215, 40), 19, TextAnchor.MiddleLeft);
        Image inputBg = Box("Code", steps.transform, center, center, new Vector2(26, -55), new Vector2(205, 42), new Color(.025f, .035f, .032f));
        AddTealOutline(inputBg);
        code = inputBg.gameObject.AddComponent<InputField>();
        RectTransform viewport = Rect("Text Area", inputBg.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-18, -6));
        viewport.gameObject.AddComponent<RectMask2D>();
        UnityEngine.UI.Text inputText = Text("Text", viewport, "", Vector2.zero, Vector2.zero, 20, TextAnchor.MiddleLeft);
        inputText.rectTransform.anchorMin = Vector2.zero; inputText.rectTransform.anchorMax = Vector2.one; inputText.rectTransform.sizeDelta = Vector2.zero;
        UnityEngine.UI.Text placeholder = Text("Placeholder", viewport, "Six-character code", Vector2.zero, Vector2.zero, 17, TextAnchor.MiddleLeft);
        placeholder.rectTransform.anchorMin = Vector2.zero; placeholder.rectTransform.anchorMax = Vector2.one; placeholder.rectTransform.sizeDelta = Vector2.zero; placeholder.color = new Color(.7f, .7f, .7f);
        code.textComponent = inputText; code.placeholder = placeholder; code.characterLimit = 6; code.lineType = InputField.LineType.SingleLine;
        connectButton = MakeButton("Connect", steps.transform, "Connect", new Vector2(235, -55), new Vector2(145, 42), teal, Connect);

        statusBackground = Box("Status card", card.transform, center, center, new Vector2(0, -180), new Vector2(640, 135), section);
        AddTealOutline(statusBackground);
        statusFox = Box("Status fox", statusBackground.transform, center, center, new Vector2(-260, 0), new Vector2(64, 64), DisconnectedColor);
        statusFox.sprite = MakeSprite(FoxArtwork); statusFox.preserveAspect = true; statusFox.raycastTarget = false;
        panelStatus = Text("Connection status", statusBackground.transform, "Not connected", new Vector2(48, 24), new Vector2(480, 38), 25, TextAnchor.MiddleLeft);
        panelStatus.fontStyle = FontStyle.Bold;
        message = Text("Help message", statusBackground.transform, "Follow the steps above to connect Twitch.", new Vector2(48, -27), new Vector2(480, 55), 17, TextAnchor.MiddleLeft);
        disconnectButton = MakeButton("Disconnect", card.transform, "Disconnect", new Vector2(217, -292), new Vector2(205, 45), teal, Disconnect);
        modal.SetActive(false);
    }
}
