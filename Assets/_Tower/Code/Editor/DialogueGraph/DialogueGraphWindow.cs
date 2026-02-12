using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

public class DialogueGraphWindow : EditorWindow
{
    DialogueGraphView graphView;

    [MenuItem("VN/Dialogue Graph (Modern)")]
    public static void Open()
    {
        var window = GetWindow<DialogueGraphWindow>();
        window.titleContent = new GUIContent("Dialogue Graph");
    }

    void OnEnable()
    {
        graphView = new DialogueGraphView();
        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);
    }

    void OnDisable()
    {
        rootVisualElement.Remove(graphView);
    }
}