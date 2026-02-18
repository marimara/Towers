using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Object pool for dialogue choice buttons.
/// Eliminates Destroy/Instantiate per node transition, reducing GC pressure.
///
/// Usage:
///   var pool = new ChoiceButtonPool(prefab, container);
///   pool.Present(node.Choices, guid => runner.GoToNode(guid));
///   pool.ReturnAll(); // called automatically at the start of next Present()
/// </summary>
public sealed class ChoiceButtonPool
{
    private readonly Button _prefab;
    private readonly Transform _container;

    private readonly List<Button> _active = new();
    private readonly Stack<Button> _pool  = new();

    public ChoiceButtonPool(Button prefab, Transform container)
    {
        _prefab    = prefab;
        _container = container;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Return all active buttons to the pool and present the new set of choices.
    /// </summary>
    public void Present(List<DialogueChoice> choices, Action<string> onChosen)
    {
        ReturnAll();

        foreach (var choice in choices)
        {
            var btn = Rent();

            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = choice.Text;

            btn.onClick.RemoveAllListeners();
            string guid = choice.NextNodeGuid; // capture for closure
            btn.onClick.AddListener(() => onChosen(guid));

            btn.gameObject.SetActive(true);
            _active.Add(btn);
        }
    }

    /// <summary>Deactivate all active buttons and return them to the pool.</summary>
    public void ReturnAll()
    {
        foreach (var btn in _active)
        {
            btn.onClick.RemoveAllListeners();
            btn.gameObject.SetActive(false);
            _pool.Push(btn);
        }
        _active.Clear();
    }

    // -------------------------------------------------------------------------
    // Internal
    // -------------------------------------------------------------------------

    private Button Rent()
    {
        if (_pool.Count > 0)
        {
            var pooled = _pool.Pop();
            pooled.gameObject.SetActive(true);
            return pooled;
        }
        return UnityEngine.Object.Instantiate(_prefab, _container);
    }
}
