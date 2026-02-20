using System;
using UnityEngine;

/// <summary>
/// Abstract base for every condition that guards an <see cref="EventData"/>.
///
/// Each concrete subclass encapsulates one discrete check and implements
/// <see cref="Evaluate"/> to return whether that check passes at evaluation time.
///
/// Design rules:
///   - No game logic beyond reading state from existing runtime systems.
///   - Subclasses are serialised via <see cref="SerializeReference"/> on the
///     <see cref="EventData"/> asset — no extra ScriptableObjects needed.
///   - All fields in subclasses must be serialisable Unity types.
/// </summary>
[Serializable]
public abstract class EventCondition
{
    // -------------------------------------------------------------------------
    // Shared options
    // -------------------------------------------------------------------------

    [Tooltip("When enabled the result of IsMet() is flipped — the condition passes " +
             "only when the underlying check fails (NOT logic).")]
    public bool Negate;

    // -------------------------------------------------------------------------
    // Public contract
    // -------------------------------------------------------------------------

    /// <summary>
    /// Evaluate this condition against the current game state.
    /// Returns <c>true</c> when satisfied, after applying <see cref="Negate"/>.
    /// </summary>
    public bool IsMet()
    {
        bool result = Evaluate();
        return Negate ? !result : result;
    }

    // -------------------------------------------------------------------------
    // Subclass contract
    // -------------------------------------------------------------------------

    /// <summary>
    /// Perform the raw check. <see cref="IsMet"/> handles negation automatically;
    /// do not apply it here.
    /// </summary>
    protected abstract bool Evaluate();

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    /// <summary>
    /// Human-readable summary used in logs and Editor tooling.
    /// Override in each subclass for meaningful output.
    /// </summary>
    public virtual string Describe() => GetType().Name;
}
