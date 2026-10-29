using System.Collections;
using System.Collections.Generic;
using System;

using MuxyGateway;
using UnityEngine;
using UnityEngine.UI;

public class MuxyGatewayPolling : MonoBehaviour
{
    [Header("Muxy Gateway Manager")]
    public MuxyGatewayManager GatewayManager;

    public float WinnerPauseDuration = 3.0f;

    private GameObject Elements;
    private GameObject Canvas;
    private Text PromptText;
    private Text TimerText;

    private bool IsActive = false;
    private bool IsFinalUpdate = false;
    private float Timer = 0;
    private float PauseTimer = 0;

    private void OnEnable()
    {
        GatewayManager.OnPollUpdate.AddListener(OnPollUpdate);
    }

    private void OnDisable()
    {
        GatewayManager.OnPollUpdate.RemoveListener(OnPollUpdate);
    }

    public void ShowPrompt()
    {
        PollConfiguration Config = GatewayManager.GetPollConfiguration();

        for (int i = 0; i < Elements.transform.childCount; i++)
        {
            GameObject Ele = Elements.transform.GetChild(i).gameObject;
            Ele.SetActive(false);
            RawImage BG = Ele.transform.Find("BGPercent").gameObject.GetComponent<RawImage>();
            BG.color = new Color(255, 255, 255);
        }

        int Count = Math.Min(Elements.transform.childCount, Config.Options.Count);
        for (int i = 0; i < Count; i++)
        {
            GameObject Ele = Elements.transform.GetChild(i).gameObject;
            Ele.SetActive(true);
            Text OptionText = Elements.transform.GetChild(i).transform.Find("Option").gameObject.GetComponent<Text>();
            OptionText.text = Config.Options[i];
        }
        
        Canvas.SetActive(true);
        IsActive = true;
        PromptText.text = Config.Prompt;
    }

    public void HidePrompt()
    {
        IsActive = false;
        IsFinalUpdate = false;
        Canvas.SetActive(false);
    }

    private void OnPollUpdate(MuxyGateway.PollUpdate Update)
    {
        if (!IsActive)
        {
            ShowPrompt();
        }

        if (Update.IsFinal)
        {
            PauseTimer = WinnerPauseDuration;
            IsFinalUpdate = true;
            RawImage BG = Elements.transform.GetChild(Update.Winner).transform.Find("BGPercent").gameObject.GetComponent<RawImage>();
            BG.color = new Color(0, 0, 255);
        }

        PollConfiguration Config = GatewayManager.GetPollConfiguration();
        int Count = Math.Min(Elements.transform.childCount, Config.Options.Count);
        for (int i = 0; i < Count; i++)
        {
            Text PercentText = Elements.transform.GetChild(i).transform.Find("Percent").gameObject.GetComponent<Text>();
            PercentText.text = Update.Results[i].ToString();
        }
        
    }

    void Start()
    {
        Elements = this.transform.Find("Canvas/Elements").gameObject; 
        Canvas = this.transform.Find("Canvas").gameObject;
        PromptText = this.transform.Find("Canvas/Prompt").GetComponent<Text>();
        TimerText = this.transform.Find("Canvas/Timer").GetComponent<Text>();
    }

    void Update()
    { 
        if (!IsActive) return;

        if (IsFinalUpdate)
        {
            PauseTimer -= Time.deltaTime; 
            if (PauseTimer <= 0)
            {
                HidePrompt();
            }
        }

        PollConfiguration Config = GatewayManager.GetPollConfiguration();

        TimerText.text = "Time Remaining: " + string.Format("{0:HH:mm:ss}", Timer);
        Timer -= Time.deltaTime; 

    }
}

