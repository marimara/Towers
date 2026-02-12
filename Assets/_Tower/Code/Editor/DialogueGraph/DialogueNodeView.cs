using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueNodeView : Node
{
    public string DialogueText;

    public DialogueNodeView(string title)
    {
        this.title = title;

        var input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        input.portName = "In";
        inputContainer.Add(input);

        var output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
        output.portName = "Out";
        outputContainer.Add(output);

        var textField = new TextField("Text");
        textField.multiline = true;
        textField.RegisterValueChangedCallback(e =>
        {
            DialogueText = e.newValue;
        });

        mainContainer.Add(textField);

        RefreshExpandedState();
        RefreshPorts();
    }
}