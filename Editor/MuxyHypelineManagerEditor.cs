using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MuxyHypelineManager), true)]
public sealed class MuxyHypelineManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (PrefabUtility.IsPartOfImmutablePrefab(target))
            EditorGUILayout.HelpBox("This package prefab is read-only. Drag MuxyHypelineManager into the scene, select it in the Hierarchy, and expand Game Metadata to edit it.", MessageType.Info);

        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.Update();

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
            else if (!MuxyGateway.SDK.IsCompatibleFormat(texture.format))
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
}
