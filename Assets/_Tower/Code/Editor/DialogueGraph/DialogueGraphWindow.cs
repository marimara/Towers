using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class DialogueGraphWindow : EditorWindow
{
    DialogueGraphView graphView;
    DialogueData dialogueData;

    [MenuItem("VN/Dialogue Graph (Modern)")]
    public static void Open()
    {
        var window = GetWindow<DialogueGraphWindow>();
        window.titleContent = new GUIContent("Dialogue Graph");
    }

    void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
    }

    void ConstructGraphView()
    {
        if (graphView != null)
            rootVisualElement.Remove(graphView);

        graphView = new DialogueGraphView();
        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);
    }

    void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        var objectField = new ObjectField("Dialogue Data")
        {
            objectType = typeof(DialogueData),
            allowSceneObjects = false
        };

        objectField.RegisterValueChangedCallback(evt =>
        {
            dialogueData = evt.newValue as DialogueData;
            graphView.LoadData(dialogueData);
        });

        toolbar.Add(objectField);
        rootVisualElement.Add(toolbar);
    }

    void OnDisable()
    {
        rootVisualElement.Remove(graphView);
    }
}