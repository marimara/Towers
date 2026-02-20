using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Editor window for the dialogue graph.
///
/// Layout fix: uses a flex-column root so the toolbar renders above the graph view
/// at its natural height. The old code called StretchToParentSize() on the graph
/// view first, then added the toolbar — the absolute-positioned graph covered it.
///
/// Save policy: AssetDatabase.SaveAssets() is called ONLY here (toolbar Save button
/// and OnDisable). Nodes call SetDirty via a deferred delayCall but never SaveAssets.
/// </summary>
public class DialogueGraphWindow : EditorWindow
{
    private DialogueGraphView _graphView;
    private DialogueData _dialogueData;
    private ObjectField _leftCharField;
    private ObjectField _rightCharField;

    [MenuItem("VN/Dialogue Graph")]
    public static void Open()
    {
        var window = GetWindow<DialogueGraphWindow>();
        window.titleContent = new GUIContent("Dialogue Graph");
        window.minSize = new Vector2(800, 500);
    }

    private void OnEnable()
    {
        // Flex-column layout: toolbar at natural height, graph fills the rest
        rootVisualElement.style.flexDirection = FlexDirection.Column;

        BuildToolbar();
        BuildGraphView();
    }

    private void OnDisable()
    {
        _graphView?.SaveAssets();

        if (_graphView != null)
            rootVisualElement.Remove(_graphView);
    }

    // -------------------------------------------------------------------------
    // Toolbar
    // -------------------------------------------------------------------------

    private void BuildToolbar()
    {
        var toolbar = new Toolbar();

        // Asset picker
        var assetField = new ObjectField("Dialogue Data")
        {
            objectType      = typeof(DialogueData),
            allowSceneObjects = false
        };
        assetField.style.flexGrow = 1;
        assetField.RegisterValueChangedCallback(evt =>
        {
            _dialogueData = evt.newValue as DialogueData;
            _graphView.LoadData(_dialogueData);
            
            // Update character fields
            if (_leftCharField != null)
                _leftCharField.value = _dialogueData?.LeftCharacter;
            if (_rightCharField != null)
                _rightCharField.value = _dialogueData?.RightCharacter;
        });

        // Left Character field
        _leftCharField = new ObjectField("Left Character")
        {
            objectType = typeof(VNCharacter),
            allowSceneObjects = false,
            value = _dialogueData?.LeftCharacter
        };
        _leftCharField.style.width = 200;
        _leftCharField.RegisterValueChangedCallback(evt =>
        {
            if (_dialogueData != null)
            {
                Undo.RecordObject(_dialogueData, "Change Left Character");
                _dialogueData.LeftCharacter = evt.newValue as VNCharacter;
                EditorUtility.SetDirty(_dialogueData);
            }
        });

        // Right Character field
        _rightCharField = new ObjectField("Right Character")
        {
            objectType = typeof(VNCharacter),
            allowSceneObjects = false,
            value = _dialogueData?.RightCharacter
        };
        _rightCharField.style.width = 200;
        _rightCharField.RegisterValueChangedCallback(evt =>
        {
            if (_dialogueData != null)
            {
                Undo.RecordObject(_dialogueData, "Change Right Character");
                _dialogueData.RightCharacter = evt.newValue as VNCharacter;
                EditorUtility.SetDirty(_dialogueData);
            }
        });

        // Explicit save button — the ONLY place SaveAssets() is called at user request
        var saveBtn = new ToolbarButton(() =>
        {
            _graphView?.SaveAssets();
            Debug.Log("[DialogueGraph] Saved.");
        })
        { text = "Save" };

        // Centre-on-graph button
        var frameBtn = new ToolbarButton(() => _graphView?.FrameAll())
        { text = "Frame All" };

        toolbar.Add(assetField);
        toolbar.Add(_leftCharField);
        toolbar.Add(_rightCharField);
        toolbar.Add(saveBtn);
        toolbar.Add(frameBtn);

        // Toolbar goes in FIRST so it occupies the top of the flex column
        rootVisualElement.Add(toolbar);
    }

    // -------------------------------------------------------------------------
    // Graph view
    // -------------------------------------------------------------------------

    private void BuildGraphView()
    {
        if (_graphView != null)
            rootVisualElement.Remove(_graphView);

        _graphView = new DialogueGraphView();

        // flexGrow = 1 fills remaining space below the toolbar (not StretchToParentSize)
        _graphView.style.flexGrow = 1;

        // Graph view goes in AFTER toolbar
        rootVisualElement.Add(_graphView);

        // Re-load the current asset if the window was rebuilt (e.g. script recompile)
        if (_dialogueData != null)
            _graphView.LoadData(_dialogueData);
    }
}
