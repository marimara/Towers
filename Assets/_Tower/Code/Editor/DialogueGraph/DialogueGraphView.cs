using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public class DialogueGraphView : GraphView
{
    DialogueData currentData;
    
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
        
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/_Tower/Code/Editor/DialogueGraph/DialogueGraphStyle.uss");

        if (styleSheet != null)
            styleSheets.Add(styleSheet);
        
        graphViewChanged = OnGraphChanged;
    }
    
    private GraphViewChange OnGraphChanged(GraphViewChange change)
    {
        if (currentData == null)
            return change;

        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                var fromView = edge.output.node as DialogueNodeView;
                var fromNode = fromView.NodeData;
                var toNode = (edge.input.node as DialogueNodeView).NodeData;

                int portIndex = fromView.ChoiceOutputs.IndexOf(edge.output);

                if (portIndex < 0)
                    continue;

                if (fromNode.Choices == null)
                    fromNode.Choices = new List<DialogueChoice>();

                while (fromNode.Choices.Count <= portIndex)
                {
                    fromNode.Choices.Add(new DialogueChoice { NextNode = -1 });
                }

                fromNode.Choices[portIndex].NextNode = toNode.Id;

                EditorUtility.SetDirty(currentData);

            }
        }

        if (change.elementsToRemove != null)
        {
            foreach (var element in change.elementsToRemove)
            {
                if (element is Edge edge)
                {
                    var fromNode = (edge.output.node as DialogueNodeView).NodeData;
                    var toNode = (edge.input.node as DialogueNodeView).NodeData;

                    if (fromNode.Choices != null)
                    {
                        fromNode.Choices.RemoveAll(c => c.NextNode == toNode.Id);
                        EditorUtility.SetDirty(currentData);
                    }
                }
            }
        }

        return change;
    }
    
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        evt.menu.AppendAction("Create Dialogue Node", _ =>
        {
            if (currentData == null)
                return;

            DialogueNode dataNode = new DialogueNode
            {
                Id = currentData.Nodes.Count,
                Text = "New dialogue...",
                Speaker = Speaker.Narrator,
                EditorPosition = evt.localMousePosition
            };

            currentData.Nodes.Add(dataNode);
            EditorUtility.SetDirty(currentData);

            var view = CreateNodeView(dataNode);
            AddElement(view);
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
    public void LoadData(DialogueData data)
    {
        currentData = data;

        DeleteElements(graphElements.ToList());

        if (data == null)
            return;

        foreach (var node in data.Nodes)
        {
            var nodeView = CreateNodeView(node);
            AddElement(nodeView);
        }
        foreach (var nodeView in nodes.ToList())
        {
            var fromView = nodeView as DialogueNodeView;
            if (fromView == null) continue;

            if (fromView.NodeData.Choices == null) continue;

            foreach (var choice in fromView.NodeData.Choices)
            {
                var targetView = nodes
                    .OfType<DialogueNodeView>()
                    .FirstOrDefault(n => n.NodeData.Id == choice.NextNode);

                if (targetView == null) continue;

                int index = fromView.NodeData.Choices.IndexOf(choice);

                if (index < 0 || index >= fromView.ChoiceOutputs.Count)
                    continue;

                var port = fromView.ChoiceOutputs[index];

                var edge = port.ConnectTo(targetView.Input);
                AddElement(edge);

            }
        }

    }
    DialogueNodeView CreateNodeView(DialogueNode node)
    {
        var view = new DialogueNodeView(node);
        view.SetPosition(new Rect(node.EditorPosition, new Vector2(250, 180)));
        return view;
    }


}