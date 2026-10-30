using System;
using System.Collections.Generic;
using MuxyGateway;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows live poll results and offers a local editor preview.</summary>
public class MuxyGatewayPolling : MonoBehaviour
{
    [InspectorName("Hypeline Manager")] public MuxyGatewayManager GatewayManager;
    [Tooltip("Optional Hypeline wordmark displayed at the top of the poll.")] public Texture2D WordmarkArtwork;
    [Tooltip("Offset from the upper-right corner in reference-resolution pixels.")] public Vector2 Position = new Vector2(-48, -72);
    public float WinnerPauseDuration = 3f;

    private GameObject display;
    private RectTransform card;
    private Text promptText, statusText;
    private readonly List<Text> optionLabels = new List<Text>();
    private readonly List<Text> voteLabels = new List<Text>();
    private readonly List<Image> fills = new List<Image>();
    private readonly List<Image> rows = new List<Image>();
    private PollConfiguration current;
    private bool isActive, final, testing;
    private float remaining, sampleNextVote, pause;
    private readonly int[] sampleVotes = new int[4];
    private int sampleStep;
    private static readonly Color Surface = new Color(.055f, .075f, .07f, .97f);
    private static readonly Color Section = new Color(.075f, .095f, .09f, 1f);
    private static readonly Color Teal = new Color(.60f, .80f, .80f, 1f);
    private static readonly Color FillColor = new Color(.20f, .38f, .35f, 1f);
    public bool IsTesting { get { return testing; } }

    protected void Awake()
    {
        Transform oldCanvas = transform.Find("Canvas");
        if (oldCanvas != null) oldCanvas.gameObject.SetActive(false);
        BuildUI();
    }
    protected void OnEnable() { if (GatewayManager != null) GatewayManager.OnPollUpdate.AddListener(OnPollUpdate); }
    protected void OnDisable()
    {
        if (GatewayManager != null) GatewayManager.OnPollUpdate.RemoveListener(OnPollUpdate);
        HidePrompt();
    }
    public void ShowPrompt()
    {
        if (GatewayManager == null) { Debug.LogWarning("Hypeline poll needs a Hypeline Manager reference for live updates.", this); return; }
        testing = false;
        ShowConfiguration(GatewayManager.GetPollConfiguration());
    }
    public void HidePrompt()
    {
        isActive = false; final = false; testing = false;
        if (display != null) display.SetActive(false);
    }
    private void OnPollUpdate(PollUpdate update)
    {
        if (update == null || GatewayManager == null) return;
        if (!isActive || testing || (final && !update.IsFinal) || current != GatewayManager.GetPollConfiguration()) ShowPrompt();
        if (!isActive) return;
        ApplyResults(update.Results, update.IsFinal, update.Winner, false);
    }
    private void ShowConfiguration(PollConfiguration config)
    {
        if (config == null || config.Options == null) return;
        current = config; isActive = true; final = false;
        foreach (Image row in rows) if (row != null) Destroy(row.gameObject);
        rows.Clear(); optionLabels.Clear(); voteLabels.Clear(); fills.Clear();
        int count = config.Options.Count;
        float height = 150f + count * 53f;
        card.sizeDelta = new Vector2(480, height);
        promptText.text = string.IsNullOrWhiteSpace(config.Prompt) ? "Audience poll" : config.Prompt;
        for (int i = 0; i < count; i++)
        {
            float y = -132f - i * 53f;
            Image row = Box("Option " + (i + 1), card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, y), new Vector2(432, 43), Section, new Vector2(0, 1));
            Image fill = Box("Vote fill", row.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FillColor, new Vector2(.5f, .5f));
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            Text label = MakeText("Option label", row.transform, config.Options[i], new Vector2(12, 0), new Vector2(305, 43), 19, TextAnchor.MiddleLeft, new Vector2(0, .5f));
            Text votes = MakeText("Votes", row.transform, "0 votes  ·  0%", new Vector2(-12, 0), new Vector2(135, 43), 15, TextAnchor.MiddleRight, new Vector2(1, .5f));
            rows.Add(row); fills.Add(fill); optionLabels.Add(label); voteLabels.Add(votes);
        }
        statusText.text = "POLL LIVE";
        display.SetActive(true);
    }
    private void ApplyResults(IList<int> results, bool isFinal, int winner, bool isSample)
    {
        if (!isActive) return;
        int total = 0;
        for (int i = 0; i < rows.Count; i++) total += results != null && i < results.Count ? Mathf.Max(0, results[i]) : 0;
        for (int i = 0; i < rows.Count; i++)
        {
            int votes = results != null && i < results.Count ? Mathf.Max(0, results[i]) : 0;
            float ratio = total > 0 ? (float)votes / total : 0f;
            fills[i].rectTransform.anchorMax = new Vector2(ratio, 1f);
            bool isWinner = isFinal && total > 0 && i == winner;
            rows[i].color = isWinner ? new Color(.15f, .28f, .25f, 1f) : Section;
            fills[i].color = isWinner ? new Color(.30f, .52f, .48f, 1f) : FillColor;
            optionLabels[i].color = Color.white;
            voteLabels[i].color = isWinner ? Color.white : Teal;
            voteLabels[i].text = votes + (votes == 1 ? " vote" : " votes") + "  ·  " + Mathf.RoundToInt(ratio * 100) + "%";
        }
        if (isFinal)
        {
            final = true; pause = Mathf.Max(0f, WinnerPauseDuration);
            statusText.text = isSample ? "SAMPLE POLL COMPLETE" : "POLL COMPLETE";
        }
        else statusText.text = isSample ? "SAMPLE POLL  ·  " + Mathf.CeilToInt(remaining) + "s" : "POLL LIVE";
    }
    /// <summary>Starts a local-only sample; it does not start a poll or submit votes.</summary>
    public void StartTestPoll()
    {
        Array.Clear(sampleVotes, 0, sampleVotes.Length); sampleStep = 0; remaining = 15f; sampleNextVote = .5f;
        ShowConfiguration(new PollConfiguration { Prompt = "Choose your favorite color", Options = new List<string> { "Blue", "Green", "Purple", "Red" } });
        testing = true; ApplyResults(sampleVotes, false, -1, true);
    }
    /// <summary>Adds sample votes locally to the preview.</summary>
    public void AddTestVotes()
    {
        if (!testing || final) return;
        int[] sequence = { 0, 1, 0, 2, 3, 0, 1, 2 };
        sampleVotes[sequence[sampleStep++ % sequence.Length]]++;
        ApplyResults(sampleVotes, false, -1, true);
    }
    public void FinishTestPoll()
    {
        if (!testing || final) return;
        int winner = -1;
        for (int i = 0; i < sampleVotes.Length; i++) if (sampleVotes[i] > 0 && (winner < 0 || sampleVotes[i] > sampleVotes[winner])) winner = i;
        ApplyResults(sampleVotes, true, winner, true);
    }
    protected void Update()
    {
        if (!isActive) return;
        if (final) { pause -= Time.unscaledDeltaTime; if (pause <= 0f) HidePrompt(); return; }
        if (!testing) return;
        remaining -= Time.unscaledDeltaTime; sampleNextVote -= Time.unscaledDeltaTime;
        if (remaining <= 0f) { FinishTestPoll(); return; }
        if (sampleNextVote <= 0f) { AddTestVotes(); sampleNextVote += 1f; }
        else statusText.text = "SAMPLE POLL  ·  " + Mathf.CeilToInt(remaining) + "s";
    }
    private void BuildUI()
    {
        display = new GameObject("Hypeline poll display", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        display.transform.SetParent(transform, false);
        Canvas canvas = display.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        CanvasScaler scaler = display.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        Image background = Box("Poll card", display.transform, Vector2.one, Vector2.one, Position, new Vector2(480, 362), Surface, Vector2.one);
        card = background.rectTransform;
        Outline outline = background.gameObject.AddComponent<Outline>(); outline.effectColor = Teal; outline.effectDistance = new Vector2(1, -1);
        if (WordmarkArtwork != null)
        {
            RectTransform logo = MakeRect("Hypeline wordmark", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -16), new Vector2(150, 35), new Vector2(0, 1));
            RawImage image = logo.gameObject.AddComponent<RawImage>(); image.texture = WordmarkArtwork; image.raycastTarget = false;
            float height = Mathf.Min(35f, 150f * WordmarkArtwork.height / Mathf.Max(1f, WordmarkArtwork.width)); logo.sizeDelta = new Vector2(150, height);
        }
        statusText = MakeText("Poll status", card, "POLL LIVE", new Vector2(-24, -25), new Vector2(235, 28), 15, TextAnchor.MiddleRight, new Vector2(1, 1)); statusText.color = Teal;
        promptText = MakeText("Poll question", card, "Audience poll", new Vector2(24, -62), new Vector2(432, 54), 24, TextAnchor.MiddleLeft, new Vector2(0, 1)); promptText.fontStyle = FontStyle.Bold;
        display.SetActive(false);
    }
    private static RectTransform MakeRect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform)); var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = pos; rect.sizeDelta = size; return rect;
    }
    private static Image Box(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Color color, Vector2 pivot)
    {
        Image image = MakeRect(name, parent, min, max, pos, size, pivot).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    private static Text MakeText(string name, Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, TextAnchor alignment, Vector2 anchor)
    {
        Text label = MakeRect(name, parent, anchor, anchor, pos, size, anchor).gameObject.AddComponent<Text>();
#if UNITY_2022_2_OR_NEWER
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        label.text = text; label.fontSize = fontSize; label.color = Color.white; label.alignment = alignment; label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; return label;
    }
}
