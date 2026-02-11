using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class DialogueChoice
{
    [TextArea(2, 4)]
    public string Text;

    public int NextNode;
}