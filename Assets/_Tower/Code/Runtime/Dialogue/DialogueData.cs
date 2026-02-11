using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "VN/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [TableList(AlwaysExpanded = true)]
    public List<DialogueNode> Nodes = new();
}