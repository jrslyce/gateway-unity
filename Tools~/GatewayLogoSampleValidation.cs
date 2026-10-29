// Copied into a disposable Unity project's Editor folder by the validation harness.
using System;
using System.IO;
using MuxyGateway;
using UnityEditor;
using UnityEngine;

public class MockLogoDemo : HypelineLogoDemo
{
    public int Accepted;
    public int Refunded;
    protected override void Accept(GameActionUsed action, string description) { Accepted++; }
    protected override void Refund(GameActionUsed action, string description) { Refunded++; }
}

public static class GatewayLogoSampleValidation
{
    private static MockLogoDemo demo;
    private static GameObject logo;
    private static MuxyGatewayManager manager;
    private static double start;

    public static void Run()
    {
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try
        {
            var managerObject = new GameObject("Manager");
            managerObject.SetActive(false);
            manager = managerObject.AddComponent<MuxyGatewayManager>();
            var demoObject = new GameObject("Demo");
            demoObject.SetActive(false);
            logo = new GameObject("Logo");
            demo = demoObject.AddComponent<MockLogoDemo>();
            demo.GatewayManager = manager;
            demo.LogoObject = logo;
            demo.DisplaySeconds = 0.15f;
            demoObject.SetActive(true);
            Require(!logo.activeSelf, "Logo should start hidden");
            manager.OnAnyGameActionUsed.Invoke(new GameActionUsed { ActionID = "other", TransactionID = "ignored" });
            Require(!logo.activeSelf && demo.Accepted == 0, "Unrelated actions must be ignored");
            var action = new GameActionUsed { ActionID = "show-muxy-logo", TransactionID = "one" };
            manager.OnAnyGameActionUsed.Invoke(action);
            Require(logo.activeSelf && demo.Accepted == 1, "Matching action should show logo and request acceptance");
            manager.OnAnyGameActionUsed.Invoke(action);
            Require(demo.Accepted == 1, "Duplicate transaction must not be accepted twice");
            start = EditorApplication.timeSinceStartup;
            EditorApplication.update += CheckHidden;
        }
        catch (Exception error) { Fail(error); }
    }

    private static void CheckHidden()
    {
        if (EditorApplication.timeSinceStartup - start < 0.8) return;
        EditorApplication.update -= CheckHidden;
        try
        {
            Require(!logo.activeSelf, "Logo should hide after its timer");
            demo.LogoObject = null;
            manager.OnAnyGameActionUsed.Invoke(new GameActionUsed { ActionID = "show-muxy-logo", TransactionID = "two" });
            Require(demo.Refunded == 1, "Unavailable logo should request a refund");
            File.WriteAllText(Path.Combine(Application.dataPath, "../logo-sample-result.txt"), "passed");
            Debug.Log("GATEWAY_LOGO_SAMPLE_PASSED");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Fail(error); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Fail(Exception error)
    {
        Debug.LogException(error);
        EditorApplication.Exit(1);
    }
}
