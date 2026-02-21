using System;
using UnityEngine;

/// <summary>
/// Condition that checks whether the current in-game hour falls within
/// an inclusive [<see cref="MinHour"/>, <see cref="MaxHour"/>] window.
///
/// Hours are expected to be in the range 0–23 (24-hour clock).
/// A window that wraps midnight (e.g. 22–06) is supported: if
/// <see cref="MinHour"/> is greater than <see cref="MaxHour"/> the check
/// treats the range as crossing midnight.
///
/// Requires a <c>GameClock</c> singleton (or equivalent service) that exposes
/// <c>int CurrentHour</c>. Wire this up when the time system is implemented.
/// </summary>
[Serializable]
public sealed class TimeCondition : EventCondition
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("Earliest hour (inclusive, 0–23) at which this condition passes.")]
    [Range(0, 23)]
    public int MinHour;

    [Tooltip("Latest hour (inclusive, 0–23) at which this condition passes.\n" +
             "If less than MinHour the window wraps past midnight (e.g. 22–06).")]
    [Range(0, 23)]
    public int MaxHour = 23;

    // -------------------------------------------------------------------------
    // Evaluate
    // -------------------------------------------------------------------------

    protected override bool Evaluate()
    {
        // Safe null check for GameClock singleton
        if (GameClock.Instance == null)
        {
            Debug.LogWarning("[TimeCondition] GameClock.Instance is null. Time check cannot be evaluated. Returning false.");
            return false;
        }

        // Read current hour from GameClock and evaluate using helper
        int currentHour = GameClock.Instance.CurrentHour;
        return IsHourInWindow(currentHour, MinHour, MaxHour);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns true when <paramref name="hour"/> falls inside the window defined
    /// by <paramref name="min"/> and <paramref name="max"/> (inclusive, wrap-aware).
    /// Extracted so the logic can be unit-tested independently.
    /// </summary>
    public static bool IsHourInWindow(int hour, int min, int max)
    {
        // Normal window: e.g. 08–20
        if (min <= max)
            return hour >= min && hour <= max;

        // Midnight-wrapping window: e.g. 22–06
        return hour >= min || hour <= max;
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string Describe() =>
        MinHour <= MaxHour
            ? $"Time {MinHour:D2}:00 – {MaxHour:D2}:59"
            : $"Time {MinHour:D2}:00 – {MaxHour:D2}:59 (wraps midnight)";
}
