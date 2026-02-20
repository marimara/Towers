// Temporary migration script — safe to delete after running once.
using UnityEditor;

public class DeleteOldEventData
{
    public static void Execute()
    {
        const string path = "Assets/_Tower/Code/Runtime/EventData.cs";
        bool deleted = AssetDatabase.DeleteAsset(path);
        UnityEngine.Debug.Log(deleted
            ? "[Migration] Deleted old EventData.cs placeholder successfully."
            : $"[Migration] Asset not found or already deleted: {path}");

        AssetDatabase.Refresh();
    }
}
