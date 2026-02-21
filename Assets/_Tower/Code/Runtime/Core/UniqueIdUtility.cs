using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Utility for detecting duplicate IDs across all UniqueIdScriptableObject assets in the project.
///
/// Provides:
///   - HasDuplicate() — quick check if an asset's ID conflicts with others
///   - ScanAndReportDuplicates() — comprehensive audit of all assets (Editor only)
///   - FindAllWithId() — locate all assets sharing a specific ID
///
/// Used internally by UniqueIdScriptableObject to auto-heal duplicate IDs.
/// Also available for manual audits and debugging.
/// </summary>
public static class UniqueIdUtility
{
    // -------------------------------------------------------------------------
    // Duplicate Detection
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns true if another asset has the same ID as <paramref name="asset"/>.
    /// Used by UniqueIdScriptableObject.OnValidate() to trigger ID regeneration.
    /// </summary>
    /// <param name="asset">The asset to check</param>
    /// <returns>True if a duplicate ID is detected</returns>
    public static bool HasDuplicate(UniqueIdScriptableObject asset)
    {
        if (asset == null || string.IsNullOrEmpty(asset.Id))
            return false;

        var allAssets = FindAllWithId(asset.Id);
        return allAssets.Count > 1;
    }

    // -------------------------------------------------------------------------
    // Utility Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Finds all UniqueIdScriptableObject assets with a specific ID.
    /// Returns a list that may include the queried asset itself.
    /// </summary>
    /// <param name="id">The ID to search for</param>
    /// <returns>List of assets with matching ID</returns>
    public static List<UniqueIdScriptableObject> FindAllWithId(string id)
    {
        var results = new List<UniqueIdScriptableObject>();

        if (string.IsNullOrEmpty(id))
            return results;

#if UNITY_EDITOR
        // Find all UniqueIdScriptableObject instances in the project
        string[] assetGuids = AssetDatabase.FindAssets("t:UniqueIdScriptableObject");

        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<UniqueIdScriptableObject>(path);

            if (asset != null && asset.Id == id)
            {
                results.Add(asset);
            }
        }
#endif

        return results;
    }

#if UNITY_EDITOR

    // -------------------------------------------------------------------------
    // Editor Tools (Editor-only)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scans all UniqueIdScriptableObject assets and reports any duplicates.
    /// Editor-only utility for auditing project integrity.
    /// Logs results to the Debug console.
    /// </summary>
    [MenuItem("Tools/Unique IDs/Scan for Duplicates")]
    public static void ScanAndReportDuplicates()
    {
        var idMap = new Dictionary<string, List<UniqueIdScriptableObject>>();

        // Collect all assets and group by ID
        string[] assetGuids = AssetDatabase.FindAssets("t:UniqueIdScriptableObject");

        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<UniqueIdScriptableObject>(path);

            if (asset == null)
                continue;

            string id = asset.Id;
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[UniqueIdUtility] Asset '{path}' has an empty ID.", asset);
                continue;
            }

            if (!idMap.ContainsKey(id))
                idMap[id] = new List<UniqueIdScriptableObject>();

            idMap[id].Add(asset);
        }

        // Report duplicates
        int duplicateCount = 0;
        foreach (var kvp in idMap)
        {
            if (kvp.Value.Count > 1)
            {
                Debug.LogWarning($"[UniqueIdUtility] Duplicate ID found: '{kvp.Key}'");
                foreach (var asset in kvp.Value)
                {
                    Debug.LogWarning($"  - {AssetDatabase.GetAssetPath(asset)}", asset);
                }
                duplicateCount++;
            }
        }

        // Summary
        if (duplicateCount == 0)
        {
            Debug.Log($"[UniqueIdUtility] Scan complete — no duplicates found ({assetGuids.Length} asset(s) checked).");
        }
        else
        {
            Debug.LogWarning($"[UniqueIdUtility] Scan complete — {duplicateCount} duplicate ID set(s) found!");
        }
    }

#endif // UNITY_EDITOR
}
