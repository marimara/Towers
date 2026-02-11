using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DialogueRunner : MonoBehaviour
{
    [Header("Data")]
    public DialogueData dialogueData;

    private int currentNodeId;

    [Header("UI References")]

    public GameObject justDialogue;
    public GameObject dialogueWithChoices;

    public TMP_Text dialogueTextSimple;
    public TMP_Text dialogueTextChoices;

    public TMP_Text nameTextSimple;
    public TMP_Text nameTextChoices;

    public Transform choicesContainer;
    public Button choiceButtonPrefab;

    public Button continueButton;

    [Header("Actors")]
    public GameObject leftActor;
    public GameObject rightActor;

    void Start()
    {
        StartDialogue(0);
    }

    public void StartDialogue(int startNode)
    {
        currentNodeId = startNode;
        ShowNode();
    }

    void ShowNode()
    {
        DialogueNode node = dialogueData.Nodes[currentNodeId];

        UpdateActors(node.Speaker);

        bool hasChoices = node.Choices != null && node.Choices.Count > 0;

        justDialogue.SetActive(!hasChoices);
        dialogueWithChoices.SetActive(hasChoices);

        if (hasChoices)
        {
            dialogueTextChoices.text = node.Text;
            nameTextChoices.text = node.Speaker.ToString();
            BuildChoices(node.Choices);
        }
        else
        {
            dialogueTextSimple.text = node.Text;
            nameTextSimple.text = node.Speaker.ToString();

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() =>
            {
                GoToNode(node.NextNode);
            });
        }
    }

    void BuildChoices(List<DialogueChoice> choices)
    {
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        foreach (var choice in choices)
        {
            Button btn = Instantiate(choiceButtonPrefab, choicesContainer);
            btn.GetComponentInChildren<TMP_Text>().text = choice.Text;

            int next = choice.NextNode;
            btn.onClick.AddListener(() => GoToNode(next));
        }
    }

    void GoToNode(int nextNode)
    {
        if (nextNode < 0 || nextNode >= dialogueData.Nodes.Count)
        {
            Debug.Log("Dialogue finished");
            return;
        }

        currentNodeId = nextNode;
        ShowNode();
    }

    void UpdateActors(Speaker speaker)
    {
        leftActor.SetActive(false);
        rightActor.SetActive(false);

        switch (speaker)
        {
            case Speaker.Left:
                leftActor.SetActive(true);
                break;

            case Speaker.Right:
                rightActor.SetActive(true);
                break;

            case Speaker.Narrator:
                break;
        }
    }
}
