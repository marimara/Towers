using System;
using UnityEngine;

/// <summary>
/// Abstract base for every consequence that an <see cref="EventData"/> applies
/// after it completes.
///
/// Mirrors the <see cref="EventCondition"/> pattern exactly:
///   - <see cref="Negate"/> is the shared opt-out flag.
///   - <see cref="Apply"/> is the public entry point; handles negation, then
///     delegates to <see cref="Execute"/> in subclasses.
///   - <see cref="Execute"/> is the raw side-effect; never check <see cref="Negate"/>
///     there — <see cref="Apply"/> owns that.
///   - <see cref="Describe"/> returns a human-readable summary for logs and tooling.
///
/// Design rules:
///   - No game logic beyond writing state to existing runtime systems.
///   - Subclasses are serialised via <see cref="SerializeReference"/> on the
///     <see cref="EventData"/> asset — no extra ScriptableObjects needed.
///   - All fields in subclasses must be serialisable Unity types.
/// </summary>
[Serializable]
public abstract class EventConsequence
{
    // -------------------------------------------------------------------------
    // Shared options
    // -------------------------------------------------------------------------

    /// <summary>
    /// When enabled, <see cref="Apply"/> skips execution entirely.
    /// Use to temporarily disable a consequence in the Editor without
    /// removing it from the list, or to build mutually exclusive consequence
    /// sets in more complex event configurations.
    /// </summary>
    [Tooltip("When enabled this consequence is skipped — useful for temporarily " +
             "disabling it in the Editor without deleting it.")]
    public bool Negate;

    // -------------------------------------------------------------------------
    // Public contract
    // -------------------------------------------------------------------------

    /// <summary>
    /// Apply this consequence to the current game state.
    /// If <see cref="Negate"/> is true the consequence is skipped silently.
    /// Called by <see cref="EventManager"/> after event completion.
    /// </summary>
    public void Apply()
    {
        if (Negate)
            return;

        Execute();
    }

    // -------------------------------------------------------------------------
    // Subclass contract
    // -------------------------------------------------------------------------

    /// <summary>
    /// Perform the raw side-effect. <see cref="Apply"/> handles negation
    /// before this is called — do not check <see cref="Negate"/> here.
    /// </summary>
    protected abstract void Execute();

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    /// <summary>
    /// Human-readable summary used in logs and Editor tooling.
    /// Override in each subclass for meaningful output.
    /// </summary>
    public virtual string Describe() => GetType().Name;
}
