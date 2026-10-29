using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MuxyGateway;

/// <summary>Creator-facing Hypeline menu and Twitch connection panel.</summary>
public class MuxyHypelineMenu : MonoBehaviour
{
    public MuxyGatewayManager GatewayManager;
    public Texture2D FoxArtwork;
    public Texture2D WordmarkArtwork;
    [TextArea] public string GameDescription = "Let your live audience interact with your game.";
    public bool ShowTikTokComingSoon = true;
    public string TwitchSetupUrl = "https://dashboard.twitch.tv/extensions/i575hs2x9lb3u8hqujtezit03w1740";
    public Color DisconnectedColor = new Color(0.67f, 0.70f, 0.76f);
    public Color ConnectingColor = new Color(1f, 0.72f, 0.22f);
    public Color ConnectedColor = new Color(0.30f, 0.86f, 0.52f);
    public Color ErrorColor = new Color(1f, 0.36f, 0.34f);
    public Vector2 MenuButtonPosition = new Vector2(24, -24);
    public Vector2 MenuButtonSize = new Vector2(300, 76);

    [Header("Panel Layout")]
    public Vector2 PanelSize = new Vector2(760, 760);
    public Vector2 WordmarkPosition = new Vector2(-190, 320);
    public Vector2 WordmarkSize = new Vector2(310, 83);
    public Vector2 CloseButtonPosition = new Vector2(315, 320);
    public Vector2 CloseButtonSize = new Vector2(100, 44);
    public Vector2 DescriptionPosition = new Vector2(0, 245);
    public Vector2 DescriptionSize = new Vector2(660, 60);
    public Vector2 PlatformPosition = new Vector2(0, 183);
    public Vector2 PlatformSize = new Vector2(660, 38);
    public Vector2 InstructionsPosition = new Vector2(0, 95);
    public Vector2 InstructionsSize = new Vector2(660, 130);
    public Vector2 CodeInputPosition = new Vector2(-140, -12);
    public Vector2 CodeInputSize = new Vector2(360, 55);
    public Vector2 ConnectButtonPosition = new Vector2(208, -12);
    public Vector2 ConnectButtonSize = new Vector2(220, 55);
    public Vector2 StatusPosition = new Vector2(0, -85);
    public Vector2 StatusSize = new Vector2(660, 30);
    public Vector2 MessagePosition = new Vector2(0, -140);
    public Vector2 MessageSize = new Vector2(660, 65);
    public Vector2 SetupButtonPosition = new Vector2(-184, -230);
    public Vector2 SetupButtonSize = new Vector2(290, 55);
    public Vector2 DisconnectButtonPosition = new Vector2(184, -230);
    public Vector2 DisconnectButtonSize = new Vector2(290, 55);
    public UnityEvent OnPanelOpened = new UnityEvent();
    public UnityEvent OnPanelClosed = new UnityEvent();

    [SerializeField] private Image fox;
    [SerializeField] private UnityEngine.UI.Text menuStatus, panelStatus, message;
    [SerializeField] private InputField code;
    [SerializeField] private GameObject modal;
    [SerializeField] private Button menuButton, connectButton, disconnectButton, closeButton, setupButton;
    private bool requested, authError;
    private string lastStatus;
    private GameObject previousSelection;

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
        if (modal == null) BuildUI();
        else
        {
            menuButton.onClick.AddListener(OpenPanel);
            closeButton.onClick.AddListener(ClosePanel);
            connectButton.onClick.AddListener(Connect);
            setupButton.onClick.AddListener(OpenTwitchSetup);
            disconnectButton.onClick.AddListener(Disconnect);
            fox.sprite = MakeSprite(FoxArtwork);
            var mark = modal.transform.Find("Card/Hypeline wordmark");
            if (mark != null) mark.GetComponent<Image>().sprite = MakeSprite(WordmarkArtwork);
            var description = modal.transform.Find("Card/Description");
            if (description != null) description.GetComponent<UnityEngine.UI.Text>().text = GameDescription;
            var platform = modal.transform.Find("Card/Platform");
            if (platform != null) platform.GetComponent<UnityEngine.UI.Text>().text = ShowTikTokComingSoon ? "TWITCH    |    TikTok - Coming soon" : "TWITCH";
        }
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
        menuStatus.text = status;
        panelStatus.text = status;
        if (status != lastStatus)
        {
            lastStatus = status;
            if (status == "Connected") message.text = "Your Twitch connection is ready.";
            else if (status == "Connection issue") message.text = "Could not connect. Check your internet connection or get a new code and try again.";
            else if (status == "Reconnecting...") message.text = "Trying to restore your connection.";
            else if (status == "Setup required") message.text = "Assign the Gateway Manager in the Inspector.";
        }
        connectButton.interactable = GatewayManager != null && status != "Connected" && status != "Connecting..." && status != "Reconnecting...";
        disconnectButton.interactable = GatewayManager != null && (status == "Connected" || status == "Reconnecting...");
    }

    private void AuthenticationFinished(AuthenticationResponse response)
    {
        requested = false;
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
        requested = true;
        message.text = "Connecting to Twitch...";
        GatewayManager.AuthenticateWithPIN(pin);
        code.text = "";
    }

    private void Disconnect()
    {
        GatewayManager.Deauthenticate();
        requested = false;
        authError = false;
        message.text = "Disconnected. Enter a new Twitch code to connect again.";
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
        Text("Label", image.transform, label, Vector2.zero, size - new Vector2(12, 6), 21, TextAnchor.MiddleCenter).color = new Color(0.05098f, 0.05098f, 0.05098f);
        return button;
    }

    private static Sprite MakeSprite(Texture2D texture)
    {
        return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private void BuildUI()
    {
        Color surface = new Color(0.145098f, 0.145098f, 0.145098f);
        Color buttonColor = new Color(0.6f, 0.8f, 0.8f);
        Image menu = Box("Stream Interactivity", transform, new Vector2(0, 1), new Vector2(0, 1), MenuButtonPosition + new Vector2(MenuButtonSize.x / 2, -MenuButtonSize.y / 2), MenuButtonSize, surface);
        AddTealOutline(menu);
        menuButton = menu.gameObject.AddComponent<Button>(); menuButton.targetGraphic = menu; menuButton.onClick.AddListener(OpenPanel);
        fox = Box("Fox status", menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-112, 0), new Vector2(48, 48), DisconnectedColor);
        fox.sprite = MakeSprite(FoxArtwork); fox.preserveAspect = true; fox.raycastTarget = false;
        Text("Title", menu.transform, "Stream Interactivity", new Vector2(20, 13), new Vector2(224, 27), 21, TextAnchor.MiddleLeft).color = new Color(0.6f, 0.8f, 0.8f);
        menuStatus = Text("Status", menu.transform, "Not connected", new Vector2(20, -14), new Vector2(224, 22), 16, TextAnchor.MiddleLeft);

        modal = Box("Hypeline Panel", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.72f)).gameObject;
        Image card = Box("Card", modal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize, surface);
        AddTealOutline(card);
        Image mark = Box("Hypeline wordmark", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), WordmarkPosition, WordmarkSize, Color.white);
        mark.sprite = MakeSprite(WordmarkArtwork); mark.preserveAspect = true; mark.raycastTarget = false;
        closeButton = MakeButton("Close", card.transform, "Close", CloseButtonPosition, CloseButtonSize, buttonColor, ClosePanel);
        Text("Description", card.transform, GameDescription, DescriptionPosition, DescriptionSize, 23, TextAnchor.MiddleLeft);
        Text("Platform", card.transform, ShowTikTokComingSoon ? "TWITCH    |    TikTok - Coming soon" : "TWITCH", PlatformPosition, PlatformSize, 20, TextAnchor.MiddleLeft);
        Text("Instructions", card.transform, "1. Install and activate the extension in your Twitch Creator Dashboard.\n2. Open Configure and get a code.\n3. Enter the code below within five minutes.", InstructionsPosition, InstructionsSize, 21, TextAnchor.UpperLeft);
        Image inputBg = Box("Code", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), CodeInputPosition, CodeInputSize, new Color(0.05098f, 0.05098f, 0.05098f));
        AddTealOutline(inputBg);
        code = inputBg.gameObject.AddComponent<InputField>();
        RectTransform viewport = Rect("Text Area", inputBg.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24, -8));
        viewport.gameObject.AddComponent<RectMask2D>();
        UnityEngine.UI.Text inputText = Text("Text", viewport, "", Vector2.zero, Vector2.zero, 22, TextAnchor.MiddleLeft);
        RectTransform textRect = inputText.rectTransform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.sizeDelta = Vector2.zero;
        UnityEngine.UI.Text placeholder = Text("Placeholder", viewport, "Six-character code", Vector2.zero, Vector2.zero, 22, TextAnchor.MiddleLeft);
        placeholder.rectTransform.anchorMin = Vector2.zero; placeholder.rectTransform.anchorMax = Vector2.one; placeholder.rectTransform.sizeDelta = Vector2.zero; placeholder.color = new Color(0.7f, 0.7f, 0.7f);
        code.textComponent = inputText; code.placeholder = placeholder; code.characterLimit = 6; code.lineType = InputField.LineType.SingleLine;
        connectButton = MakeButton("Connect", card.transform, "Connect", ConnectButtonPosition, ConnectButtonSize, buttonColor, Connect);
        panelStatus = Text("Connection status", card.transform, "Not connected", StatusPosition, StatusSize, 22, TextAnchor.MiddleLeft);
        message = Text("Help message", card.transform, "A code expires after five minutes. Get a new one if needed.", MessagePosition, MessageSize, 18, TextAnchor.MiddleLeft);
        setupButton = MakeButton("Open Twitch setup", card.transform, "Open Twitch setup", SetupButtonPosition, SetupButtonSize, buttonColor, OpenTwitchSetup);
        disconnectButton = MakeButton("Disconnect", card.transform, "Disconnect", DisconnectButtonPosition, DisconnectButtonSize, buttonColor, Disconnect);
        modal.SetActive(false);
    }
}
