using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Concrete IDialoguePresenter for the visual novel UI layout.
/// Owns all Unity UI references and the choice button pool.
/// Completely decoupled from DialogueRunner's flow logic.
///
/// Assign to DialogueRunner.Presenter in the inspector.
/// </summary>
public class VNDialoguePresenter : MonoBehaviour, IDialoguePresenter
{
    // -------------------------------------------------------------------------
    // Inspector references
    // -------------------------------------------------------------------------

    [Header("Panels")]
    [SerializeField] private GameObject _linearPanel;        // shown for linear nodes
    [SerializeField] private GameObject _choicesPanel;       // shown for branching nodes

    [Header("Linear panel")]
    [SerializeField] private TMP_Text _linearDialogueText;
    [SerializeField] private TMP_Text _linearNameText;
    [SerializeField] private Button   _continueButton;

    [Header("Choices panel")]
    [SerializeField] private TMP_Text   _choicesDialogueText;
    [SerializeField] private TMP_Text   _choicesNameText;
    [SerializeField] private Transform  _choicesContainer;
    [SerializeField] private Button     _choiceButtonPrefab;

    [Header("Actors")]
    [SerializeField] private GameObject _leftActor;
    [SerializeField] private GameObject _rightActor;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private ChoiceButtonPool _buttonPool;
    private bool _initialized;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _initialized = ValidateReferences();
        if (_initialized)
            _buttonPool = new ChoiceButtonPool(_choiceButtonPrefab, _choicesContainer);
    }

    // -------------------------------------------------------------------------
    // IDialoguePresenter
    // -------------------------------------------------------------------------

    public void PresentNode(DialogueNode node, SpeakerConfig config)
    {
        if (!_initialized) return;

        string displayName = config != null
            ? config.GetDisplayName(node.Speaker)
            : node.Speaker.ToString();

        if (_linearNameText  != null) _linearNameText.text  = displayName;
        if (_choicesNameText != null) _choicesNameText.text = displayName;
        if (_linearDialogueText  != null) _linearDialogueText.text  = node.Text;
        if (_choicesDialogueText != null) _choicesDialogueText.text = node.Text;

        UpdateActors(node.Speaker, config);
    }

    public void ShowChoices(List<DialogueChoice> choices, Action<string> onChosen)
    {
        if (!_initialized) return;

        _linearPanel?.SetActive(false);
        _choicesPanel?.SetActive(true);
        _buttonPool.Present(choices, onChosen);
    }

    public void HideChoices()
    {
        if (!_initialized) return;

        _choicesPanel?.SetActive(false);
        _linearPanel?.SetActive(true);
    }

    public void OnDialogueEnd()
    {
        if (!_initialized) return;

        _buttonPool?.ReturnAll();
        _linearPanel?.SetActive(false);
        _choicesPanel?.SetActive(false);
        _leftActor?.SetActive(false);
        _rightActor?.SetActive(false);
        gameObject.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Continue button wiring (called by DialogueRunner)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Wire the continue button to advance to the next linear node.
    /// Called by DialogueRunner when a linear node is presented.
    /// </summary>
    public void SetContinueAction(Action onContinue)
    {
        if (_continueButton == null) return;
        _continueButton.onClick.RemoveAllListeners();
        _continueButton.onClick.AddListener(() => onContinue());
    }

    // -------------------------------------------------------------------------
    // Actors
    // -------------------------------------------------------------------------

    private void UpdateActors(Speaker speaker, SpeakerConfig config)
    {
        _leftActor?.SetActive(speaker == Speaker.Left);
        _rightActor?.SetActive(speaker == Speaker.Right);
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    private bool ValidateReferences()
    {
        bool ok = true;

        Check(ref ok, _linearPanel,        nameof(_linearPanel));
        Check(ref ok, _choicesPanel,       nameof(_choicesPanel));
        Check(ref ok, _linearDialogueText, nameof(_linearDialogueText));
        Check(ref ok, _linearNameText,     nameof(_linearNameText));
        Check(ref ok, _continueButton,     nameof(_continueButton));
        Check(ref ok, _choicesContainer,   nameof(_choicesContainer));
        Check(ref ok, _choiceButtonPrefab, nameof(_choiceButtonPrefab));

        return ok;
    }

    private void Check<T>(ref bool ok, T value, string fieldName) where T : UnityEngine.Object
    {
        if (value != null) return;
        Debug.LogError($"[{name}] {nameof(VNDialoguePresenter)}.{fieldName} is not assigned.", this);
        ok = false;
    }
}
