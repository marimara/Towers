using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class DialogueNodeView : Node
{
    public DialogueNode NodeData;

    public Port Input;
    public List<Port> ChoiceOutputs = new List<Port>();

    VisualElement choicesContainer;

    public DialogueNodeView(DialogueNode node)
    {
        NodeData = node;

        title = $"Node {node.Id}";
        AddToClassList("dialogue-node");

        Input = InstantiatePort(
            Orientation.Horizontal,
            Direction.Input,
            Port.Capacity.Multi,
            typeof(bool));

        Input.portName = "";
        inputContainer.Add(Input);
        
        var nextPort = InstantiatePort(
            Orientation.Horizontal,
            Direction.Output,
            Port.Capacity.Single,
            typeof(bool));

        nextPort.portName = "Next";
        outputContainer.Add(nextPort);
        
        ChoiceOutputs.Insert(0, nextPort);
        
        var speakerField = new EnumField(node.Speaker);
        speakerField.RegisterValueChangedCallback(e =>
        {
            node.Speaker = (Speaker)e.newValue;
            UpdateSpeakerStyle();
        });

        mainContainer.Add(speakerField);

        var textField = new TextField();
        textField.multiline = true;
        textField.value = node.Text;

        title = Shorten(node.Text);

        textField.RegisterValueChangedCallback(e =>
        {
            node.Text = e.newValue;
            title = Shorten(node.Text);
        });

        mainContainer.Add(textField);

        choicesContainer = new VisualElement();
        mainContainer.Add(choicesContainer);

        var addChoiceBtn = new Button(() =>
        {
            AddChoice();
        })
        {
            text = "+ Add Choice"
        };

        mainContainer.Add(addChoiceBtn);

        DrawChoices();
        UpdateSpeakerStyle();

        RefreshExpandedState();
        RefreshPorts();
    }

    void AddChoice()
    {
        if (NodeData.Choices == null)
            NodeData.Choices = new List<DialogueChoice>();

        NodeData.Choices.Add(new DialogueChoice
        {
            Text = "New choice...",
            NextNode = -1
        });

        DrawChoices();
    }

    void DrawChoices()
    {
        choicesContainer.Clear();
        ChoiceOutputs.Clear();

        if (NodeData.Choices == null)
            return;

        foreach (var choice in NodeData.Choices)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var text = new TextField();
            text.value = choice.Text;
            text.style.flexGrow = 1;

            text.RegisterValueChangedCallback(e =>
            {
                choice.Text = e.newValue;
            });

            var port = InstantiatePort(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Single,
                typeof(bool));

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
            Speaker.Left => new Color(0.25f, 0.45f, 1f),
            Speaker.Right => new Color(1f, 0.3f, 0.3f),
            Speaker.Narrator => new Color(0.75f, 0.75f, 0.75f),
            _ => Color.gray
        };

        var uiCol = new StyleColor(col);

        titleContainer.style.backgroundColor = uiCol;

        style.borderLeftColor = uiCol;
        style.borderRightColor = uiCol;
        style.borderTopColor = uiCol;
        style.borderBottomColor = uiCol;
    }
    
    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        NodeData.EditorPosition = newPos.position;
    }
    string Shorten(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "Dialogue";

        return text.Length > 30
            ? text.Substring(0, 30) + "..."
            : text;
    }

}
