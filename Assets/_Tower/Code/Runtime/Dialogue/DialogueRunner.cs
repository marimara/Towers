using System;
using System.Linq;
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

        // Register dialogue participants with RelationshipSystem
        if (RelationshipSystem.Instance != null)
        {
            var participants = new System.Collections.Generic.List<VNCharacter>();
            if (data.LeftCharacter != null) participants.Add(data.LeftCharacter);
            if (data.RightCharacter != null) participants.Add(data.RightCharacter);

            if (participants.Count > 0)
            {
                RelationshipSystem.Instance.Reinitialize(participants);
                Debug.Log($"[DialogueRunner] Registered {participants.Count} dialogue participant(s) with RelationshipSystem.");
            }
        }

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

        // Override dialogue participants if specified
        if (node.OverrideParticipants)
        {
            _dialogueData.LeftCharacter = node.LeftParticipant;
            _dialogueData.RightCharacter = node.RightParticipant;

            if (RelationshipSystem.Instance != null)
            {
                var participants = new System.Collections.Generic.List<VNCharacter>();
                if (node.LeftParticipant != null) participants.Add(node.LeftParticipant);
                if (node.RightParticipant != null) participants.Add(node.RightParticipant);

                if (participants.Count > 0)
                {
                    RelationshipSystem.Instance.Reinitialize(participants);
                    Debug.Log($"[DialogueRunner] Node '{node.DisplayName}' overrode participants. Reinitialized RelationshipSystem.");
                }
            }
        }

        // Apply relationship changes via RelationshipSystem
        if (RelationshipSystem.Instance != null && node.RelationshipChanges != null && node.RelationshipChanges.Count > 0)
        {
            Debug.Log($"[DialogueRunner] Applying {node.RelationshipChanges.Count} relationship change(s) from node '{node.DisplayName}'");
            foreach (var change in node.RelationshipChanges)
            {
                ApplyRelationshipChange(change, node);
            }
        }

        _presenter.PresentNode(node, null);

        if (!node.IsLinear)
        {
            // Branching: filter choices by relationship tier requirements
            var allowedChoices = FilterChoicesByRelationshipTier(node);
            _presenter.ShowChoices(allowedChoices, choiceIndex => OnChoiceSelected(choiceIndex));
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

        // Apply relationship changes from choice
        if (RelationshipSystem.Instance != null && choice.RelationshipChanges != null && choice.RelationshipChanges.Count > 0)
        {
            Debug.Log($"[DialogueRunner] Applying {choice.RelationshipChanges.Count} relationship change(s) from choice '{choice.Text}'");
            foreach (var change in choice.RelationshipChanges)
            {
                ApplyRelationshipChange(change, _currentNode);
            }
        }

        // Navigate to next node
        GoToNode(choice.NextNodeGuid);
    }

    private void ApplyRelationshipChange(RelationshipChange change, DialogueNode contextNode)
    {
        if (RelationshipSystem.Instance == null) return;

        VNCharacter from, to;

        if (change.AutoBetweenCurrentSpeakers)
        {
            from = _dialogueData.LeftCharacter;
            to = _dialogueData.RightCharacter;
            
            if (from == null || to == null)
            {
                Debug.LogWarning($"[DialogueRunner] AutoBetweenCurrentSpeakers enabled but DialogueData participants are missing. LeftCharacter: {from?.name ?? "null"}, RightCharacter: {to?.name ?? "null"}");
                return;
            }
        }
        else
        {
            from = change.From;
            to = change.To;
            
            if (from == null || to == null)
            {
                Debug.LogWarning($"[DialogueRunner] RelationshipChange has null From or To character.");
                return;
            }
        }

        RelationshipSystem.Instance.ModifyRelationship(from, to, change.Delta);

        if (change.Mutual)
        {
            RelationshipSystem.Instance.ModifyRelationship(to, from, change.Delta);
        }
    }

    private System.Collections.Generic.List<DialogueChoice> FilterChoicesByRelationshipTier(DialogueNode node)
    {
        var filtered = new System.Collections.Generic.List<DialogueChoice>();

        if (node.Choices == null || node.Choices.Count == 0)
            return filtered;

        VNCharacter speaker = node.Speaker;
        VNCharacter other = GetOtherActiveCharacter(speaker);

        foreach (var choice in node.Choices)
        {
            bool allowed = true;

            // Check legacy tier requirement system
            if (choice.RequiresRelationshipTier)
            {
                allowed = CheckLegacyTierRequirement(choice, speaker, other);
                if (!allowed)
                {
                    Debug.Log($"[DialogueRunner] Choice '{choice.Text}' filtered out by legacy tier requirement.");
                    continue;
                }
            }

            // Check new tier condition system
            if (choice.RequiresTierCondition)
            {
                allowed = CheckTierCondition(choice, speaker, other);
                if (!allowed)
                {
                    Debug.Log($"[DialogueRunner] Choice '{choice.Text}' filtered out by tier condition.");
                    continue;
                }
            }

            if (allowed)
                filtered.Add(choice);
        }

        return filtered;
    }

    private bool CheckLegacyTierRequirement(DialogueChoice choice, VNCharacter speaker, VNCharacter other)
    {
        // Fail-safe: if RelationshipSystem missing → disallow
        if (RelationshipSystem.Instance == null)
        {
            Debug.LogError($"[DialogueRunner] Choice '{choice.Text}' requires relationship tier but RelationshipSystem is missing. Disallowing choice.");
            return false;
        }

        // Always use dialogue participants
        VNCharacter from = _dialogueData.LeftCharacter;
        VNCharacter to = _dialogueData.RightCharacter;

        if (from == null || to == null)
        {
            Debug.LogError($"[DialogueRunner] Choice '{choice.Text}' requires relationship tier but DialogueData participants are missing. LeftCharacter: {from?.name ?? "null"}, RightCharacter: {to?.name ?? "null"}. Disallowing choice.");
            return false;
        }

        // Get current tier
        string currentTier = RelationshipSystem.Instance.GetRelationshipTier(from, to);

        // Check if current tier is in allowed list
        return choice.AllowedTiers != null && choice.AllowedTiers.Contains(currentTier);
    }

    private bool CheckTierCondition(DialogueChoice choice, VNCharacter speaker, VNCharacter other)
    {
        // Fail-safe: if RelationshipSystem missing → disallow
        if (RelationshipSystem.Instance == null)
        {
            Debug.LogError($"[DialogueRunner] Choice '{choice.Text}' requires tier condition but RelationshipSystem is missing. Disallowing choice.");
            return false;
        }

        // Always use dialogue participants
        VNCharacter from = _dialogueData.LeftCharacter;
        VNCharacter to = _dialogueData.RightCharacter;

        if (from == null || to == null)
        {
            Debug.LogError($"[DialogueRunner] Choice '{choice.Text}' requires tier condition but DialogueData participants are missing. LeftCharacter: {from?.name ?? "null"}, RightCharacter: {to?.name ?? "null"}. Disallowing choice.");
            return false;
        }

        // No conditions → allow
        if (choice.TierConditions == null || choice.TierConditions.Count == 0)
            return true;

        // Get current tier
        int currentValue = RelationshipSystem.Instance.GetRelationship(from, to);
        string currentTier = RelationshipSystem.Instance.GetRelationshipTier(from, to);
        int currentIndex = GetTierIndex(currentTier);

        string fromName = !string.IsNullOrEmpty(from.DisplayName) ? from.DisplayName : from.name;
        string toName = !string.IsNullOrEmpty(to.DisplayName) ? to.DisplayName : to.name;

        Debug.Log($"[DialogueRunner] Evaluating tier condition for choice '{choice.Text}': {fromName} -> {toName} | Value: {currentValue} | Tier: {currentTier}");

        if (currentIndex == -1)
        {
            Debug.LogError($"[DialogueRunner] Could not resolve current tier index for '{currentTier}'. Disallowing choice '{choice.Text}'.");
            return false;
        }

        // Evaluate each condition
        var results = new System.Collections.Generic.List<bool>();
        foreach (var condition in choice.TierConditions)
        {
            int requiredIndex = GetTierIndex(condition.RequiredTierName);

            if (requiredIndex == -1)
            {
                Debug.LogError($"[DialogueRunner] Could not resolve required tier index for '{condition.RequiredTierName}'. Treating as failed condition.");
                results.Add(false);
                continue;
            }

            bool conditionPassed = condition.ComparisonMode switch
            {
                TierComparisonMode.GreaterOrEqual => currentIndex >= requiredIndex,
                TierComparisonMode.LessOrEqual => currentIndex <= requiredIndex,
                _ => true
            };

            results.Add(conditionPassed);
        }

        // Combine results using logic operator
        bool finalResult = choice.LogicOperator switch
        {
            TierLogicOperator.AND => results.All(r => r),
            TierLogicOperator.OR => results.Any(r => r),
            _ => true
        };

        return finalResult;
    }

    private int GetTierIndex(string tierName)
    {
        if (RelationshipSystem.Instance == null)
            return -1;

        return RelationshipSystem.Instance.GetTierIndex(tierName);
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
