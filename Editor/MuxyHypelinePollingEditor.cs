using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MuxyGatewayPolling), true)]
public sealed class MuxyHypelinePollingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Test Poll", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Preview a favorite-color poll with simulated votes. This stays local and does not start a live poll or send votes.", MessageType.Info);
        var poll = target as MuxyGatewayPolling;
        bool canTest = EditorApplication.isPlaying && poll != null && poll.isActiveAndEnabled && !EditorUtility.IsPersistent(poll);
        if (!canTest) EditorGUILayout.HelpBox("Enter Play mode and select an active polling instance in the scene.", MessageType.None);
        using (new EditorGUI.DisabledScope(!canTest))
        {
            if (GUILayout.Button("Start Test Poll")) poll.StartTestPoll();
            using (new EditorGUI.DisabledScope(!canTest || !poll.IsTesting))
            {
                if (GUILayout.Button("Add Sample Vote")) poll.AddTestVotes();
                if (GUILayout.Button("Finish Test Poll")) poll.FinishTestPoll();
            }
        }
    }
}
