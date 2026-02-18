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

    [Header("Config")]
    [SerializeField] private SpeakerConfig _speakerConfig;

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

        _presenter.PresentNode(node, _speakerConfig);

        if (!node.IsLinear)
        {
            // Branching: show choices and let the presenter call back with a GUID
            _presenter.ShowChoices(node.Choices, GoToNode);
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

        // _dialogueData and _speakerConfig are optional at Awake — StartDialogue() validates data
        return ok;
    }
}
