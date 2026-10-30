using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class MuxyGatewayAuthentication : MonoBehaviour
{
    [Header("Hypeline Manager")]
    [InspectorName("Hypeline Manager")]
    public MuxyGatewayManager GatewayManager;

    private InputField PINInput;
    private Text FailedAuthText;
    private GameObject PINGroup;
    private GameObject SuccessGroup;

    protected void OnEnable()
    {
        GatewayManager.OnAuthentication.AddListener(OnAuthenticationAttempt);
    }

    protected void OnDisable()
    {
        GatewayManager.OnAuthentication.RemoveListener(OnAuthenticationAttempt);
    }

    protected void Start()
    {
        PINInput = this.transform.Find("Canvas/PINGroup/PINInput").GetComponent<InputField>();
        FailedAuthText = this.transform.Find("Canvas/FailedAuthText").GetComponent<Text>();
        SuccessGroup = this.transform.Find("Canvas/SuccessGroup").gameObject;
        PINGroup = this.transform.Find("Canvas/PINGroup").gameObject;
    }

    private void ChangeUI(bool DidAuth, bool DidError)
    {
        if (DidAuth)
        {
            FailedAuthText.gameObject.SetActive(false);
            SuccessGroup.SetActive(true);
            PINGroup.SetActive(false);
        }
        else
        {
            if (DidError) FailedAuthText.gameObject.SetActive(true);
            SuccessGroup.SetActive(false);
            PINGroup.SetActive(true);
        }
    }

    private void OnAuthenticationAttempt(MuxyGateway.AuthenticationResponse Response)
    {
        if (Response.HasError)
        {
            ChangeUI(false, true);
            return;
        }

        ChangeUI(true, false);
    }

    public void OnAuthenticateClick()
    {
        GatewayManager.AuthenticateWithPIN(PINInput.text);
    }

    public void OnDeauthenticateClick()
    {
        GatewayManager.Deauthenticate();
        ChangeUI(false, false);
    }

    public void OnGetExtensionClick()
    {
        Application.OpenURL("https://dashboard.twitch.tv/extensions/i575hs2x9lb3u8hqujtezit03w1740");
    }

}
