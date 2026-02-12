using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public class DialogueGraphView : GraphView
{
    public DialogueGraphView()
    {
        style.flexGrow = 1;

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        AddElement(CreateEntryNode());
        
        graphViewChanged = OnGraphChanged;
    }
    
    private GraphViewChange OnGraphChanged(GraphViewChange change)
    {
        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                Debug.Log("Edge created");
            }
        }

        if (change.elementsToRemove != null)
        {
            foreach (var element in change.elementsToRemove)
            {
                if (element is Edge edge)
                {
                    Debug.Log("Edge removed");
                }
            }
        }

        return change;
    }

    DialogueNodeView CreateEntryNode()
    {
        var node = new DialogueNodeView("Start");
        node.SetPosition(new Rect(200, 200, 250, 180));
        return node;
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        evt.menu.AppendAction("Create Dialogue Node", _ =>
        {
            var node = new DialogueNodeView("Dialogue");
            node.SetPosition(new Rect(evt.localMousePosition, new Vector2(250, 180)));
            AddElement(node);
        });
    }
    
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
    {
        var compatible = new List<Port>();

        ports.ForEach(port =>
        {
            if (startPort != port &&
                startPort.node != port.node &&
                startPort.direction != port.direction)
            {
                compatible.Add(port);
            }
        });

        return compatible;
    }

}