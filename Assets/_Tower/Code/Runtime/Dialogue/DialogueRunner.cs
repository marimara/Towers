using System;
using UnityEngine;

/// <summary>
/// Orchestrates dialogue flow. Pure coordinator — no UI code lives here.
/// All presentation is delegated to an <see cref="IDialoguePresenter"/>.
///
/// Key changes from the original:
///  - GUID-based node lookup (O(1) dictionary) instead of list-index access.
///  - StartDialogue accepts an optional startNodeGuid; falls back to DialogueData.StartNodeGuid.
///  - public OnDialogueEnd event lets callers react without coupling.
///  - Awake() validates all required references with descriptive errors.
///  - No UI code — VNDialoguePresenter owns all TMP_Text / Button refs.
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Data")]
    [SerializeField] private DialogueData _dialogueData;

    [Header("Presenter")]
    [Tooltip("Assign a MonoBehaviour that implements IDialoguePresenter (e.g. VNDialoguePresenter).")]
    [SerializeField] private VNDialoguePresenter _presenter;

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>Fired when the dialogue sequence reaches a terminal node (null or missing NextNodeGuid).</summary>
    public event Action<DialogueData> OnDialogueEnd;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private DialogueNode _currentNode;
    private bool _initialized;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _initialized = ValidateReferences();
    }

    private void Start()
    {
        if (_initialized && _dialogueData != null)
            StartDialogue(_dialogueData);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Begin a dialogue sequence.
    /// Optionally specify a startNodeGuid to override the asset's default start node.
    /// </summary>
    public void StartDialogue(DialogueData data, string startNodeGuid = null)
    {
        if (!_initialized)
        {
            Debug.LogError($"[{name}] DialogueRunner cannot start — missing references.", this);
            return;
        }

        if (data == null)
        {
            Debug.LogError($"[{name}] DialogueRunner.StartDialogue: data is null.", this);
            return;
        }

        _dialogueData = data;
        _dialogueData.BuildLookup();
        gameObject.SetActive(true);

        DialogueNode startNode;

        if (!string.IsNullOrEmpty(startNodeGuid))
        {
            if (!_dialogueData.TryGetNode(startNodeGuid, out startNode))
            {
                Debug.LogError($"[{name}] Start node '{startNodeGuid}' not found in {data.name}. Falling back to asset default.", this);
                startNode = _dialogueData.GetStartNode();
            }
        }
        else
        {
            startNode = _dialogueData.GetStartNode();
        }

        if (startNode == null)
        {
            Debug.LogError($"[{name}] No start node found in {data.name}.", this);
            return;
        }

        PresentNode(startNode);
    }

    // -------------------------------------------------------------------------
    // Flow
    // -------------------------------------------------------------------------

    private void PresentNode(DialogueNode node)
    {
        _currentNode = node;

        // Apply relationship changes via RelationshipSystem
        if (RelationshipSystem.Instance != null && node.RelationshipChanges != null && node.RelationshipChanges.Count > 0)
        {
            Debug.Log($"[DialogueRunner] Applying {node.RelationshipChanges.Count} relationship change(s) from node '{node.DisplayName}'");
            foreach (var change in node.RelationshipChanges)
            {
                if (change.From != null && change.To != null)
                    RelationshipSystem.Instance.ModifyRelationship(change.From, change.To, change.Delta);
            }
        }

        _presenter.PresentNode(node, null);

        if (!node.IsLinear)
        {
            // Branching: show choices and let the presenter call back with choice index
            _presenter.ShowChoices(node.Choices, choiceIndex => OnChoiceSelected(choiceIndex));
        }
        else
        {
            // Linear: hide choices and wire the continue button
            _presenter.HideChoices();
            _presenter.SetContinueAction(() => GoToNode(node.NextNodeGuid));
        }
    }

    private void GoToNode(string guid)
    {
        if (string.IsNullOrEmpty(guid))
        {
            EndDialogue();
            return;
        }

        if (!_dialogueData.TryGetNode(guid, out var node))
        {
            Debug.LogWarning($"[{name}] Node '{guid}' not found in {_dialogueData.name}. Ending dialogue.", this);
            EndDialogue();
            return;
        }

        PresentNode(node);
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        if (_currentNode == null || _currentNode.Choices == null || choiceIndex < 0 || choiceIndex >= _currentNode.Choices.Count)
        {
            Debug.LogError($"[{name}] Invalid choice index {choiceIndex}.", this);
            return;
        }

        var choice = _currentNode.Choices[choiceIndex];

        // Apply relationship effect if delta is non-zero
        if (choice.RelationshipDelta != 0 && RelationshipSystem.Instance != null)
        {
            VNCharacter from, to;

            if (choice.AutoApplyBetweenCurrentSpeakers)
            {
                from = _currentNode.Speaker;
                to = GetOtherActiveCharacter(from);
            }
            else
            {
                from = choice.FromOverride;
                to = choice.ToOverride;
            }

            if (from != null && to != null)
            {
                Debug.Log($"[DialogueRunner] Applying choice relationship effect: '{choice.Text}'");
                RelationshipSystem.Instance.ModifyRelationship(from, to, choice.RelationshipDelta);
            }
            else
            {
                Debug.LogWarning($"[DialogueRunner] Choice '{choice.Text}' has relationship delta but missing From/To characters.");
            }
        }

        // Navigate to next node
        GoToNode(choice.NextNodeGuid);
    }

    private VNCharacter GetOtherActiveCharacter(VNCharacter current)
    {
        if (_presenter == null) return null;

        var leftChar = (_presenter as VNDialoguePresenter)?.GetLeftCharacter();
        var rightChar = (_presenter as VNDialoguePresenter)?.GetRightCharacter();

        if (leftChar == current) return rightChar;
        if (rightChar == current) return leftChar;

        return null;
    }

    private void EndDialogue()
    {
        _presenter.OnDialogueEnd();
        OnDialogueEnd?.Invoke(_dialogueData);
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    private bool ValidateReferences()
    {
        bool ok = true;

        if (_presenter == null)
        {
            Debug.LogError($"[{name}] DialogueRunner._presenter is not assigned. Assign a VNDialoguePresenter.", this);
            ok = false;
        }

        // _dialogueData is optional at Awake — StartDialogue() validates data
        return ok;
    }
}
