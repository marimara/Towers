using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;

public class DialogueNodeView : Node
{
    public DialogueNode NodeData;
    DialogueData ownerData;

    public Port Input;
    public List<Port> ChoiceOutputs = new();

    VisualElement choicesContainer;

    public DialogueNodeView(DialogueNode node, DialogueData data)
    {
        NodeData = node;
        ownerData = data;

        AddToClassList("dialogue-node");

        Input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        Input.portName = "";
        inputContainer.Add(Input);

        var nextPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        nextPort.portName = "Next";
        outputContainer.Add(nextPort);
        ChoiceOutputs.Add(nextPort);

        var speakerField = new EnumField(node.Speaker);
        speakerField.RegisterValueChangedCallback(e =>
        {
            NodeData.Speaker = (Speaker)e.newValue;
            UpdateSpeakerStyle();
            Save();
        });
        mainContainer.Add(speakerField);

        var textField = new TextField { multiline = true, value = node.Text };
        title = Shorten(node.Text);

        textField.RegisterValueChangedCallback(e =>
        {
            NodeData.Text = e.newValue;
            title = Shorten(NodeData.Text);
            Save();
        });

        mainContainer.Add(textField);

        choicesContainer = new VisualElement();
        choicesContainer.style.marginTop = 6;
        choicesContainer.style.backgroundColor = new Color(.08f,.08f,.08f);
        mainContainer.Add(choicesContainer);

        var addBtn = new Button(AddChoice) { text = "+ Add Choice" };
        mainContainer.Add(addBtn);

        DrawChoices();
        UpdateSpeakerStyle();

        RefreshExpandedState();
        RefreshPorts();
    }

    void AddChoice()
    {
        NodeData.Choices ??= new List<DialogueChoice>();

        NodeData.Choices.Add(new DialogueChoice
        {
            Text = "New choice...",
            NextNode = -1
        });

        Save();
        DrawChoices();
    }

    void DrawChoices()
    {
        choicesContainer.Clear();
        ChoiceOutputs.Clear();

        if (NodeData.Choices == null) return;

        foreach (var choice in NodeData.Choices)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var text = new TextField { value = choice.Text };
            text.style.flexGrow = 1;

            text.RegisterValueChangedCallback(e =>
            {
                choice.Text = e.newValue;
                Save();
            });

            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = "";

            row.Add(text);
            row.Add(port);

            choicesContainer.Add(row);
            ChoiceOutputs.Add(port);
        }

        RefreshPorts();
    }

    void UpdateSpeakerStyle()
    {
        Color col = NodeData.Speaker switch
        {
            Speaker.Left => new(.25f,.45f,1f),
            Speaker.Right => new(1f,.3f,.3f),
            Speaker.Narrator => new(.75f,.75f,.75f),
            _ => Color.gray
        };

        var ui = new StyleColor(col);
        titleContainer.style.backgroundColor = ui;
        style.borderLeftColor = ui;
        style.borderRightColor = ui;
        style.borderTopColor = ui;
        style.borderBottomColor = ui;
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        NodeData.EditorPosition = newPos.position;
        Save();
    }

    void Save()
    {
        EditorUtility.SetDirty(ownerData);
        AssetDatabase.SaveAssets();
    }

    string Shorten(string text)
    {
        if (string.IsNullOrEmpty(text)) return "Dialogue";
        return text.Length > 30 ? text[..30] + "..." : text;
    }
}
