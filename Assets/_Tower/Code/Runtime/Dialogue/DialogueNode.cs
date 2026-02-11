using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class DialogueNode
{
    [HorizontalGroup("Row", Width = 80)]
    [LabelText("ID")]
    public int Id;

    [HorizontalGroup("Row", Width = 120)]
    public Speaker Speaker;

    [TextArea(3, 6)]
    public string Text;

    [ListDrawerSettings(Expanded = true)]
    public List<DialogueChoice> Choices = new();

    [ShowIf(nameof(HasNoChoices))]
    public int NextNode;

    [field: SerializeField] public Vector2 EditorPosition { get; set; } = new Vector2(100, 100);

    private bool HasNoChoices()
    {
        return Choices == null || Choices.Count == 0;
    }
}