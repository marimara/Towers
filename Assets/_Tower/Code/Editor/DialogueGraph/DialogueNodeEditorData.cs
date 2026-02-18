using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Editor-only companion asset that stores per-node graph positions.
/// Lives next to the DialogueData asset as &lt;DialogueDataName&gt;_EditorLayout.asset.
/// Never included in builds — Editor folder placement ensures stripping.
/// </summary>
public class DialogueNodeEditorData : ScriptableObject
{
    [System.Serializable]
    public class NodeLayout
    {
        public string NodeGuid;
        public Vector2 Position;
    }

    public List<NodeLayout> Layouts = new();

    // Runtime dictionary — not serialized
    private Dictionary<string, NodeLayout> _map;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public Vector2 GetPosition(string guid)
    {
        EnsureMap();
        return _map.TryGetValue(guid, out var layout) ? layout.Position : new Vector2(100f, 100f);
    }

    public void SetPosition(string guid, Vector2 pos)
    {
        EnsureMap();
        if (_map.TryGetValue(guid, out var layout))
        {
            layout.Position = pos;
        }
        else
        {
            var newLayout = new NodeLayout { NodeGuid = guid, Position = pos };
            Layouts.Add(newLayout);
            _map[guid] = newLayout;
        }
    }

    public void RemoveNode(string guid)
    {
        EnsureMap();
        if (_map.Remove(guid, out var layout))
            Layouts.Remove(layout);
    }

    /// <summary>Invalidate the in-memory map (e.g. after an undo that modifies Layouts).</summary>
    public void InvalidateMap() => _map = null;

    // -------------------------------------------------------------------------
    // Internal
    // -------------------------------------------------------------------------

    private void EnsureMap()
    {
        if (_map != null) return;
        _map = new Dictionary<string, NodeLayout>(Layouts.Count);
        foreach (var l in Layouts)
            if (!string.IsNullOrEmpty(l.NodeGuid))
                _map[l.NodeGuid] = l;
    }
}
