using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// ScriptableObject container for a complete dialogue conversation.
/// Nodes are identified by stable GUIDs; use <see cref="TryGetNode"/> for O(1) lookup.
/// </summary>
[CreateAssetMenu(menuName = "VN/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    /// <summary>GUID of the node where this conversation begins. Falls back to Nodes[0] if unset.</summary>
    [LabelText("Start Node GUID")]
    public string StartNodeGuid;

    [TableList(AlwaysExpanded = true)]
    public List<DialogueNode> Nodes = new();

    // -------------------------------------------------------------------------
    // Runtime lookup (never serialized)
    // -------------------------------------------------------------------------

    private Dictionary<string, DialogueNode> _nodeMap;

    /// <summary>
    /// Build the GUID→node dictionary. Must be called once before using
    /// <see cref="TryGetNode"/>. Called automatically by <see cref="GetStartNode"/>.
    /// </summary>
    public void BuildLookup()
    {
        _nodeMap = new Dictionary<string, DialogueNode>(Nodes.Count);
        foreach (var node in Nodes)
        {
            if (string.IsNullOrEmpty(node.Guid))
            {
                Debug.LogWarning($"[DialogueData] {name}: Node with DisplayName '{node.DisplayName}' has no GUID. Run VN/Migrate Dialogue Data to v2.", this);
                continue;
            }
            if (!_nodeMap.TryAdd(node.Guid, node))
                Debug.LogWarning($"[DialogueData] {name}: Duplicate GUID '{node.Guid}' on node '{node.DisplayName}'. Check asset integrity.", this);
        }
    }

    /// <summary>O(1) GUID-based node lookup. Builds the lookup on first call if needed.</summary>
    public bool TryGetNode(string guid, out DialogueNode node)
    {
        if (_nodeMap == null)
            BuildLookup();

        if (string.IsNullOrEmpty(guid))
        {
            node = null;
            return false;
        }

        return _nodeMap.TryGetValue(guid, out node);
    }

    /// <summary>
    /// Returns the designated start node.
    /// Falls back to <see cref="Nodes"/>[0] if <see cref="StartNodeGuid"/> is unset.
    /// Returns null if the asset has no nodes.
    /// </summary>
    public DialogueNode GetStartNode()
    {
        if (!string.IsNullOrEmpty(StartNodeGuid) && TryGetNode(StartNodeGuid, out var startNode))
            return startNode;

        if (Nodes.Count > 0)
        {
            Debug.LogWarning($"[DialogueData] {name}: StartNodeGuid is unset. Falling back to Nodes[0].", this);
            return Nodes[0];
        }

        Debug.LogError($"[DialogueData] {name}: No nodes found.", this);
        return null;
    }

    /// <summary>Invalidates the lookup cache. Call after adding/removing nodes at runtime (rare).</summary>
    public void InvalidateLookup() => _nodeMap = null;
}
