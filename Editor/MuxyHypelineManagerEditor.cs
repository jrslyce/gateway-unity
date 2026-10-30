using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MuxyHypelineManager), true)]
public sealed class MuxyHypelineManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (EditorUtility.IsPersistent(target) && PrefabUtility.IsPartOfImmutablePrefab(target))
            EditorGUILayout.HelpBox("This package prefab is read-only. Drag MuxyHypelineManager into the scene and select it in the Hierarchy to edit its settings.", MessageType.Info);

        serializedObject.Update();
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(property, true);
            }
            else if (property.propertyPath == "GameMetadata")
            {
                DrawMetadata(property);
            }
            else
            {
                EditorGUILayout.PropertyField(property, true);
                if (property.propertyPath == "GameLogo")
                    EditorGUILayout.HelpBox("Optional: assign a readable logo texture here to show it in the Twitch extension.", MessageType.Info);
                else if (property.propertyPath == "GameTexts" && property.isExpanded)
                    EditorGUILayout.HelpBox("Game Texts appear in the viewer's Game Info tab. For each entry, set the label and value; an icon name is optional. Update values from your game as they change.", MessageType.Info);
            }
        }
        serializedObject.ApplyModifiedProperties();

        SerializedProperty metadata = serializedObject.FindProperty("GameMetadata");
        SerializedProperty name = metadata == null ? null : metadata.FindPropertyRelative("Name");
        if (name != null && string.IsNullOrWhiteSpace(name.stringValue))
            EditorGUILayout.HelpBox("Set a game name so viewers can identify your game in the extension.", MessageType.Info);

        SerializedProperty logo = serializedObject.FindProperty("GameLogo");
        Texture2D texture = logo == null ? null : logo.objectReferenceValue as Texture2D;
        if (texture != null)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !importer.isReadable)
                EditorGUILayout.HelpBox("Game Logo needs Read/Write Enabled in its texture import settings. The runtime also requires RGB24, RGBA32, or ARGB32; use an uncompressed compatible format.", MessageType.Warning);
            else if (!MuxyHypeline.SDK.IsCompatibleFormat(texture.format))
                EditorGUILayout.HelpBox("Game Logo must use RGB24, RGBA32, or ARGB32 at runtime. Set a compatible uncompressed texture format in the import settings.", MessageType.Warning);
        }

        SerializedProperty actions = serializedObject.FindProperty("GameActions");
        if (actions != null)
        {
            HashSet<string> ids = new HashSet<string>();
            for (int i = 0; i < actions.arraySize; i++)
            {
                SerializedProperty id = actions.GetArrayElementAtIndex(i).FindPropertyRelative("ID");
                string value = id == null ? "" : id.stringValue;
                if (string.IsNullOrWhiteSpace(value))
                    EditorGUILayout.HelpBox("Game Action " + (i + 1) + " needs a unique ID.", MessageType.Warning);
                else if (!ids.Add(value))
                    EditorGUILayout.HelpBox("Duplicate Game Action ID: " + value, MessageType.Warning);
            }
        }
    }

    private static void DrawMetadata(SerializedProperty metadata)
    {
        metadata.isExpanded = EditorGUILayout.Foldout(metadata.isExpanded, "Game Metadata", true);
        if (!metadata.isExpanded) return;
        EditorGUI.indentLevel++;
        DrawMetadataField(metadata, "Name", "Game name shown to viewers in the Twitch extension.");
        DrawMetadataField(metadata, "Theme", "Optional extension theme name; use default for the standard theme.");
        EditorGUI.indentLevel--;
    }

    private static void DrawMetadataField(SerializedProperty metadata, string name, string guidance)
    {
        SerializedProperty field = metadata.FindPropertyRelative(name);
        if (field == null) return;
        EditorGUILayout.PropertyField(field, new GUIContent(field.displayName, guidance));
        EditorGUILayout.HelpBox(guidance, MessageType.None);
    }
}
