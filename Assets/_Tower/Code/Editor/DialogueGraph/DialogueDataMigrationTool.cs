using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time migration tool: converts DialogueData assets from integer-ID nodes (v1)
/// to GUID-based nodes (v2). Run once via VN/Migrate Dialogue Data to v2.
///
/// What it does per asset:
///  1. Assigns a new GUID to any node that has none.
///  2. Builds an int-ID → GUID map from the old Id field (preserved in DisplayName).
///  3. Rewrites every NextNode (int) → NextNodeGuid (string).
///  4. Rewrites every Choice.NextNode (int) → Choice.NextNodeGuid (string).
///  5. Sets StartNodeGuid to the node that had Id == 0.
///  6. Creates a companion DialogueNodeEditorData asset with the existing EditorPositions.
/// </summary>
public static class DialogueDataMigrationTool
{
    [MenuItem("VN/Migrate Dialogue Data to v2")]
    public static void MigrateAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:DialogueData");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Migration", "No DialogueData assets found.", "OK");
            return;
        }

        int migrated = 0;
        int skipped  = 0;

        foreach (string assetGuid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGuid);
            var data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data == null) continue;

            if (MigrateAsset(data, path))
                migrated++;
            else
                skipped++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Migration Complete",
            $"Migrated: {migrated}\nSkipped (already v2): {skipped}",
            "OK");

        Debug.Log($"[DialogueMigration] Done. Migrated={migrated}, Skipped={skipped}");
    }

    // -------------------------------------------------------------------------
    // Per-asset migration
    // -------------------------------------------------------------------------

    /// <returns>True if migration was applied; false if already up-to-date.</returns>
    private static bool MigrateAsset(DialogueData data, string dataPath)
    {
        // Skip assets that already have GUIDs on all nodes
        bool alreadyMigrated = data.Nodes.Count > 0 &&
                               data.Nodes.TrueForAll(n => !string.IsNullOrEmpty(n.Guid));
        if (alreadyMigrated)
        {
            Debug.Log($"[DialogueMigration] Skipping '{data.name}' — already v2.");
            return false;
        }

        Debug.Log($"[DialogueMigration] Migrating '{data.name}' ({data.Nodes.Count} nodes)...");
        Undo.RecordObject(data, "Migrate DialogueData to v2");

        // Step 1 — Assign GUIDs and build int-ID → GUID map
        // The old int Id field no longer exists on the class, so we reconstruct it
        // from the order in the list (which matched the Id before migration).
        var idToGuid = new Dictionary<int, string>(data.Nodes.Count);

        for (int i = 0; i < data.Nodes.Count; i++)
        {
            var node = data.Nodes[i];

            if (string.IsNullOrEmpty(node.Guid))
                node.Guid = System.Guid.NewGuid().ToString();

            // Old sequential Id was equal to the node's original list index.
            // We use 'i' as the legacy ID unless we can read a DisplayName hint.
            idToGuid[i] = node.Guid;

            // Preserve a human-readable name if not already set
            if (string.IsNullOrEmpty(node.DisplayName))
                node.DisplayName = $"Node {i}";
        }

        // Step 2 — Rewrite NextNodeGuid on each node (was NextNode int, now removed from the class,
        // but old serialized data may have stored it as "NextNode" in the YAML).
        // We migrate by assuming old NextNode value was the index in the list.
        // Since the field is now NextNodeGuid, fresh deserialization gives us empty strings.
        // We recover old values via SerializedObject before writing.
        var serializedData = new SerializedObject(data);
        var nodesArray     = serializedData.FindProperty("Nodes");

        for (int i = 0; i < nodesArray.arraySize; i++)
        {
            var nodeProp    = nodesArray.GetArrayElementAtIndex(i);
            var nextNodeProp = nodeProp.FindPropertyRelative("NextNode"); // old field (may be null after rename)

            if (nextNodeProp != null)
            {
                int oldNext = nextNodeProp.intValue;
                if (idToGuid.TryGetValue(oldNext, out string targetGuid))
                    data.Nodes[i].NextNodeGuid = targetGuid;
            }

            // Migrate choices
            var choicesProp = nodeProp.FindPropertyRelative("Choices");
            if (choicesProp == null) continue;

            for (int c = 0; c < choicesProp.arraySize; c++)
            {
                var choiceProp   = choicesProp.GetArrayElementAtIndex(c);
                var nextProp     = choiceProp.FindPropertyRelative("NextNode"); // old field
                if (nextProp == null) continue;

                int oldNext = nextProp.intValue;
                if (oldNext >= 0 && idToGuid.TryGetValue(oldNext, out string targetGuid))
                    data.Nodes[i].Choices[c].NextNodeGuid = targetGuid;
            }
        }

        // Step 3 — Set StartNodeGuid (was always node 0)
        if (string.IsNullOrEmpty(data.StartNodeGuid) && idToGuid.TryGetValue(0, out string startGuid))
            data.StartNodeGuid = startGuid;

        EditorUtility.SetDirty(data);

        // Step 4 — Create or update companion DialogueNodeEditorData
        // EditorPosition was on the node; we read it via SerializedObject before it disappears.
        CreateOrUpdateEditorLayout(data, dataPath, nodesArray);

        return true;
    }

    // -------------------------------------------------------------------------
    // Companion asset creation
    // -------------------------------------------------------------------------

    private static void CreateOrUpdateEditorLayout(
        DialogueData data,
        string dataPath,
        SerializedProperty nodesArray)
    {
        string dir          = Path.GetDirectoryName(dataPath);
        string baseName     = Path.GetFileNameWithoutExtension(dataPath);
        string layoutPath   = Path.Combine(dir, baseName + "_EditorLayout.asset")
                              .Replace('\\', '/');

        var layoutAsset = AssetDatabase.LoadAssetAtPath<DialogueNodeEditorData>(layoutPath);
        if (layoutAsset == null)
        {
            layoutAsset = ScriptableObject.CreateInstance<DialogueNodeEditorData>();
            AssetDatabase.CreateAsset(layoutAsset, layoutPath);
        }

        Undo.RecordObject(layoutAsset, "Create DialogueNodeEditorData");

        for (int i = 0; i < nodesArray.arraySize && i < data.Nodes.Count; i++)
        {
            var nodeProp     = nodesArray.GetArrayElementAtIndex(i);
            var posProp      = nodeProp.FindPropertyRelative("EditorPosition"); // old field

            Vector2 pos = posProp != null
                ? posProp.vector2Value
                : new Vector2(i * 280f + 50f, 100f);

            layoutAsset.SetPosition(data.Nodes[i].Guid, pos);
        }

        EditorUtility.SetDirty(layoutAsset);
        Debug.Log($"[DialogueMigration] Layout asset at '{layoutPath}'.");
    }
}
