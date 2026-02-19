using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Visual representation of a single <see cref="DialogueNode"/> in the GraphView.
///
/// Port model (fixed vs. original):
///   InputPort        — Multi-capacity input; one per node; never changes.
///   NextOutputPort   — Single-capacity output for linear flow; hidden when choices exist.
///   ChoiceOutputPorts — One port per DialogueChoice; managed by RebuildChoicePorts().
///
/// Save strategy: mutations call ScheduleSave() which defers SetDirty to the next
/// editor frame via EditorApplication.delayCall. AssetDatabase.SaveAssets() is NOT
/// called here — only from DialogueGraphWindow (toolbar button / OnDisable).
/// </summary>
public class DialogueNodeView : Node
{
    // -------------------------------------------------------------------------
    // Public fields read by DialogueGraphView
    // -------------------------------------------------------------------------

    public DialogueNode NodeData { get; private set; }

    /// <summary>The single input port (accepts connections from any output).</summary>
    public Port InputPort { get; private set; }

    /// <summary>Output port for linear flow. Null GUID = end-of-dialogue. Hidden when choices exist.</summary>
    public Port NextOutputPort { get; private set; }

    /// <summary>One port per NodeData.Choices entry. Index is stable within a RebuildChoicePorts() call.</summary>
    public List<Port> ChoiceOutputPorts { get; private set; } = new();

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private readonly DialogueData _ownerData;
    private VisualElement _choicesSection;
    private bool _savePending;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public DialogueNodeView(DialogueNode node, DialogueData data)
    {
        NodeData    = node;
        _ownerData  = data;

        AddToClassList("dialogue-node");

        // --- Input port (never removed) ---
        InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        InputPort.portName = "";
        inputContainer.Add(InputPort);

        // --- Linear Next output port (hidden when choices exist) ---
        NextOutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        NextOutputPort.portName = "Next";
        outputContainer.Add(NextOutputPort);

        // --- Speaker dropdown ---
        var speakerField = new ObjectField("Speaker") { objectType = typeof(VNCharacter), value = node.Speaker };
        speakerField.RegisterValueChangedCallback(e =>
        {
            Undo.RecordObject(_ownerData, "Change Speaker");
            NodeData.Speaker = (VNCharacter)e.newValue;
            UpdateSpeakerStyle();
            UpdateNodeTypeClasses();
            ScheduleSave();
        });
        mainContainer.Add(speakerField);

        // --- Display name field ---
        var nameField = new TextField { value = node.DisplayName, tooltip = "Node display name" };
        nameField.RegisterValueChangedCallback(e =>
        {
            Undo.RecordObject(_ownerData, "Rename Dialogue Node");
            NodeData.DisplayName = e.newValue;
            title = string.IsNullOrEmpty(e.newValue) ? Shorten(NodeData.Text) : e.newValue;
            ScheduleSave();
        });
        mainContainer.Add(nameField);

        // --- Dialogue text field ---
        var textField = new TextField { multiline = true, value = node.Text };
        title = string.IsNullOrEmpty(node.DisplayName) ? Shorten(node.Text) : node.DisplayName;
        textField.RegisterValueChangedCallback(e =>
        {
            Undo.RecordObject(_ownerData, "Edit Dialogue Text");
            NodeData.Text = e.newValue;
            if (string.IsNullOrEmpty(NodeData.DisplayName))
                title = Shorten(e.newValue);
            ScheduleSave();
        });
        mainContainer.Add(textField);

        // --- Choices section ---
        _choicesSection = new VisualElement();
        _choicesSection.style.marginTop = 6;
        _choicesSection.style.backgroundColor = new Color(.08f, .08f, .08f);
        mainContainer.Add(_choicesSection);

        var addChoiceBtn = new Button(AddChoice) { text = "+ Add Choice" };
        mainContainer.Add(addChoiceBtn);

        // --- Initial build ---
        RebuildChoicePorts();
        UpdateSpeakerStyle();
        UpdateNodeTypeClasses();

        RefreshExpandedState();
        RefreshPorts();
    }

    // -------------------------------------------------------------------------
    // Context menu — "Set as Start Node"
    // -------------------------------------------------------------------------

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);
        evt.menu.AppendSeparator();
        evt.menu.AppendAction("Set as Start Node", _ =>
        {
            Undo.RecordObject(_ownerData, "Set Start Node");
            _ownerData.StartNodeGuid = NodeData.Guid;
            EditorUtility.SetDirty(_ownerData);

            // Notify the parent graph view to refresh start-node indicator visuals
            GetFirstAncestorOfType<DialogueGraphView>()?.RefreshStartNodeIndicators();
        });
    }

    // -------------------------------------------------------------------------
    // Port management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fully rebuilds the choice output ports from NodeData.Choices.
    /// Disconnects and removes old ports before creating new ones to avoid ghost connections.
    /// </summary>
    public void RebuildChoicePorts()
    {
        // 1. Disconnect and remove all existing choice ports
        foreach (var port in ChoiceOutputPorts)
        {
            // Disconnect each edge on this port before removing the port itself
            foreach (var edge in port.connections.ToList())
            {
                edge.input?.Disconnect(edge);
                edge.output?.Disconnect(edge);
                edge.parent?.Remove(edge);
            }
            outputContainer.Remove(port);
        }
        ChoiceOutputPorts.Clear();
        _choicesSection.Clear();

        bool hasChoices = NodeData.Choices != null && NodeData.Choices.Count > 0;

        // 2. When transitioning to branching, purge any live NextOutputPort edge
        //    and clear the data field so RestoreEdges cannot re-create a ghost.
        if (hasChoices)
        {
            var nextConnections = NextOutputPort.connections.ToList();
            if (nextConnections.Count > 0 || !string.IsNullOrEmpty(NodeData.NextNodeGuid))
            {
                Undo.RecordObject(_ownerData, "Clear Linear Connection");
                foreach (var edge in nextConnections)
                {
                    edge.input?.Disconnect(edge);
                    edge.output?.Disconnect(edge);
                    edge.parent?.Remove(edge);
                }
                NodeData.NextNodeGuid = null;
                EditorUtility.SetDirty(_ownerData);
            }
        }

        // Show/hide the linear Next port based on whether choices exist
        NextOutputPort.style.display = hasChoices ? DisplayStyle.None : DisplayStyle.Flex;

        if (!hasChoices)
        {
            RefreshExpandedState();
            RefreshPorts();
            return;
        }

        // 3. Create one row + port per choice
        foreach (var choice in NodeData.Choices)
        {
            var choiceRef = choice; // capture for closures

            var choiceContainer = new VisualElement();
            choiceContainer.style.marginBottom = 4;

            // Main row: delete button, text field, port
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;

            // Delete button for this choice
            var deleteBtn = new Button(() => RemoveChoice(choiceRef)) { text = "✕" };
            deleteBtn.style.width      = 20;
            deleteBtn.style.marginRight = 4;

            // Choice text field
            var textField = new TextField { value = choice.Text };
            textField.style.flexGrow = 1;
            textField.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(_ownerData, "Edit Choice Text");
                choiceRef.Text = e.newValue;
                ScheduleSave();
            });

            // Output port for this choice
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = "";

            row.Add(deleteBtn);
            row.Add(textField);
            row.Add(port);

            choiceContainer.Add(row);

            // Relationship toggle
            var hasRelToggle = new Toggle("Affects Relationship") 
            { 
                value = choice.RelationshipChanges != null
            };
            hasRelToggle.style.marginLeft = 24;
            hasRelToggle.style.marginTop = 2;
            hasRelToggle.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(_ownerData, "Toggle Relationship Effects");
                if (e.newValue)
                {
                    if (choiceRef.RelationshipChanges == null)
                        choiceRef.RelationshipChanges = new System.Collections.Generic.List<RelationshipChange>();
                }
                else
                {
                    choiceRef.RelationshipChanges = null;
                }
                EditorUtility.SetDirty(_ownerData);
                RebuildChoicePorts(); // Rebuild to show/hide relationship UI
            });
            choiceContainer.Add(hasRelToggle);

            // Relationship changes list (only shown when list is not null)
            if (choice.RelationshipChanges != null)
            {
                var relSection = new VisualElement();
                relSection.style.marginLeft = 24;
                relSection.style.marginTop = 2;
                relSection.style.paddingLeft = 4;
                relSection.style.paddingRight = 4;
                relSection.style.paddingTop = 2;
                relSection.style.paddingBottom = 2;
                relSection.style.backgroundColor = new Color(.05f, .05f, .05f);

                var relLabel = new Label($"Relationship Changes ({choice.RelationshipChanges.Count})");
                relLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                relLabel.style.marginBottom = 4;
                relSection.Add(relLabel);

                // Display each relationship change entry
                for (int i = 0; i < choice.RelationshipChanges.Count; i++)
                {
                    var changeRef = choice.RelationshipChanges[i];
                    int changeIndex = i; // capture for closures

                    var changeContainer = new VisualElement();
                    changeContainer.style.marginBottom = 4;
                    changeContainer.style.paddingLeft = 2;
                    changeContainer.style.paddingRight = 2;
                    changeContainer.style.paddingTop = 2;
                    changeContainer.style.paddingBottom = 2;
                    changeContainer.style.backgroundColor = new Color(.03f, .03f, .03f);

                    // Auto toggle
                    var autoToggle = new Toggle("Auto Between Current Speakers") { value = changeRef.AutoBetweenCurrentSpeakers };
                    autoToggle.RegisterValueChangedCallback(e =>
                    {
                        Undo.RecordObject(_ownerData, "Toggle Auto Between Speakers");
                        changeRef.AutoBetweenCurrentSpeakers = e.newValue;
                        ScheduleSave();
                        RebuildChoicePorts(); // Rebuild to show/hide From/To fields
                    });
                    changeContainer.Add(autoToggle);

                    // From field (hidden when auto is enabled)
                    if (!changeRef.AutoBetweenCurrentSpeakers)
                    {
                        var fromField = new ObjectField("From") { objectType = typeof(VNCharacter), value = changeRef.From };
                        fromField.RegisterValueChangedCallback(e =>
                        {
                            Undo.RecordObject(_ownerData, "Change From Character");
                            changeRef.From = (VNCharacter)e.newValue;
                            ScheduleSave();
                        });
                        changeContainer.Add(fromField);

                        var toField = new ObjectField("To") { objectType = typeof(VNCharacter), value = changeRef.To };
                        toField.RegisterValueChangedCallback(e =>
                        {
                            Undo.RecordObject(_ownerData, "Change To Character");
                            changeRef.To = (VNCharacter)e.newValue;
                            ScheduleSave();
                        });
                        changeContainer.Add(toField);
                    }

                    // Delta field
                    var deltaField = new IntegerField("Delta") { value = changeRef.Delta };
                    deltaField.RegisterValueChangedCallback(e =>
                    {
                        Undo.RecordObject(_ownerData, "Change Relationship Delta");
                        changeRef.Delta = e.newValue;
                        ScheduleSave();
                    });
                    changeContainer.Add(deltaField);

                    // Mutual toggle
                    var mutualToggle = new Toggle("Mutual") { value = changeRef.Mutual };
                    mutualToggle.RegisterValueChangedCallback(e =>
                    {
                        Undo.RecordObject(_ownerData, "Toggle Mutual");
                        changeRef.Mutual = e.newValue;
                        ScheduleSave();
                    });
                    changeContainer.Add(mutualToggle);

                    // Action buttons row
                    var btnRow = new VisualElement();
                    btnRow.style.flexDirection = FlexDirection.Row;
                    btnRow.style.marginTop = 2;

                    var makeMutualBtn = new Button(() =>
                    {
                        if (changeRef.From == null || changeRef.To == null)
                        {
                            Debug.LogWarning("[DialogueNodeView] Cannot make mutual - From or To is null.");
                            return;
                        }

                        Undo.RecordObject(_ownerData, "Make Relationship Mutual");

                        // Check if reverse already exists
                        bool reverseExists = false;
                        foreach (var existing in choiceRef.RelationshipChanges)
                        {
                            if (existing.From == changeRef.To && existing.To == changeRef.From)
                            {
                                existing.Delta = changeRef.Delta;
                                reverseExists = true;
                                break;
                            }
                        }

                        if (!reverseExists)
                        {
                            choiceRef.RelationshipChanges.Add(new RelationshipChange
                            {
                                From = changeRef.To,
                                To = changeRef.From,
                                Delta = changeRef.Delta,
                                AutoBetweenCurrentSpeakers = false,
                                Mutual = false
                            });
                        }

                        EditorUtility.SetDirty(_ownerData);
                        RebuildChoicePorts();
                    }) { text = "Make Mutual" };
                    makeMutualBtn.style.flexGrow = 1;

                    var removeBtn = new Button(() =>
                    {
                        Undo.RecordObject(_ownerData, "Remove Relationship Change");
                        choiceRef.RelationshipChanges.RemoveAt(changeIndex);
                        EditorUtility.SetDirty(_ownerData);
                        RebuildChoicePorts();
                    }) { text = "Remove" };
                    removeBtn.style.flexGrow = 1;

                    btnRow.Add(makeMutualBtn);
                    btnRow.Add(removeBtn);
                    changeContainer.Add(btnRow);

                    relSection.Add(changeContainer);
                }

                // Add new change button
                var addRelBtn = new Button(() =>
                {
                    Undo.RecordObject(_ownerData, "Add Relationship Change");
                    choiceRef.RelationshipChanges.Add(new RelationshipChange());
                    EditorUtility.SetDirty(_ownerData);
                    RebuildChoicePorts();
                }) { text = "+ Add Change" };
                addRelBtn.style.marginTop = 2;
                relSection.Add(addRelBtn);

                choiceContainer.Add(relSection);
            }

            _choicesSection.Add(choiceContainer);
            outputContainer.Add(port);
            ChoiceOutputPorts.Add(port);
        }

        RefreshExpandedState();
        RefreshPorts();
    }

    // -------------------------------------------------------------------------
    // Choice management
    // -------------------------------------------------------------------------

    private void AddChoice()
    {
        Undo.RecordObject(_ownerData, "Add Dialogue Choice");
        NodeData.Choices ??= new List<DialogueChoice>();
        NodeData.Choices.Add(new DialogueChoice { Text = "New choice...", NextNodeGuid = null });
        EditorUtility.SetDirty(_ownerData); // immediate: structural mutation must not be deferred
        ScheduleSave();                      // safety net for text-field callbacks in RebuildChoicePorts
        RebuildChoicePorts();
        UpdateNodeTypeClasses();
    }

    private void RemoveChoice(DialogueChoice choice)
    {
        Undo.RecordObject(_ownerData, "Remove Dialogue Choice");
        NodeData.Choices.Remove(choice);
        EditorUtility.SetDirty(_ownerData); // immediate: structural mutation must not be deferred
        ScheduleSave();                      // safety net for text-field callbacks in RebuildChoicePorts
        RebuildChoicePorts();
        UpdateNodeTypeClasses();
    }

    // -------------------------------------------------------------------------
    // Position persistence (editor-layout asset, not runtime data)
    // -------------------------------------------------------------------------

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        // Position is written to the companion DialogueNodeEditorData by the graph view
        // when it calls view.SetPosition(). The view passes editorData.SetPosition() directly.
    }

    // -------------------------------------------------------------------------
    // Styling
    // -------------------------------------------------------------------------

    private void UpdateSpeakerStyle()
    {
        Color col = NodeData.Speaker != null && NodeData.Speaker.NameColor != default
            ? NodeData.Speaker.NameColor
            : Color.gray;

        var styleColor = new StyleColor(col);
        titleContainer.style.backgroundColor = styleColor;
        style.borderLeftColor   = styleColor;
        style.borderRightColor  = styleColor;
        style.borderTopColor    = styleColor;
        style.borderBottomColor = styleColor;
    }

    private void UpdateNodeTypeClasses()
    {
        EnableInClassList("node-linear",    NodeData.IsLinear);
        EnableInClassList("node-branching", !NodeData.IsLinear);
    }

    /// <summary>Visually marks this node as the conversation start point.</summary>
    public void SetStartNodeIndicator(bool isStart)
    {
        EnableInClassList("start-node", isStart);
    }

    // -------------------------------------------------------------------------
    // Deferred save (no SaveAssets on every keystroke)
    // -------------------------------------------------------------------------

    private void ScheduleSave()
    {
        if (_savePending) return;
        _savePending = true;
        EditorApplication.delayCall += ExecuteSave;
    }

    private void ExecuteSave()
    {
        _savePending = false;
        if (_ownerData != null)
            EditorUtility.SetDirty(_ownerData);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string Shorten(string text)
    {
        if (string.IsNullOrEmpty(text)) return "Dialogue";
        return text.Length > 30 ? text[..30] + "..." : text;
    }
}
