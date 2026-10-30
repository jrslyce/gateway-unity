using MuxyHypeline;
using UnityEditor;
using UnityEngine;

/// <summary>Editor-only, offline preview for the scene's interaction alert.</summary>
[CustomEditor(typeof(MuxyHypelineInteractionAlert))]
public sealed class MuxyHypelineInteractionAlertEditor : Editor
{
    private string previewViewer = "Sample viewer";
    private string previewAction = "Spawn Healthpack";
    private GameActionCategory previewCategory = GameActionCategory.Help;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Test Alert", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Play-mode visual and sound preview only. No Twitch interaction, game effect, or transaction is sent.", MessageType.Info);
        previewViewer = EditorGUILayout.TextField("Viewer Name", previewViewer);
        previewAction = EditorGUILayout.TextField("Action Name", previewAction);
        previewCategory = (GameActionCategory)EditorGUILayout.EnumPopup("Category", previewCategory);

        var alert = target as MuxyHypelineInteractionAlert;
        bool canPreview = CanPreview(alert);
        if (!canPreview)
            EditorGUILayout.HelpBox("Enter Play mode and select an active alert instance in the scene to test it.", MessageType.None);
        using (new EditorGUI.DisabledScope(!canPreview))
        {
            if (GUILayout.Button("Test Alert"))
                TryPreview(alert, previewViewer, previewAction, previewCategory);
        }
    }

    private static bool CanPreview(MuxyHypelineInteractionAlert alert)
    {
        return EditorApplication.isPlaying && alert != null && alert.isActiveAndEnabled && !EditorUtility.IsPersistent(alert);
    }

    private static bool TryPreview(MuxyHypelineInteractionAlert alert, string viewer, string action, GameActionCategory category)
    {
        if (!CanPreview(alert)) return false;
        alert.ShowAlert(viewer, action, category);
        return true;
    }
}
