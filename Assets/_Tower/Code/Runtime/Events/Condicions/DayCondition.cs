using System;
using UnityEngine;

/// <summary>
/// Condition that checks whether the current in-game day meets or exceeds a required day threshold.
///
/// This condition passes when <c>GameClock.Instance.CurrentDay >= RequiredDay</c>.
/// Useful for time-gated events that unlock after a specific day is reached.
///
/// Requires a <c>GameClock</c> singleton that exposes <c>int CurrentDay</c>.
/// </summary>
[Serializable]
public sealed class DayCondition : EventCondition
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("The minimum day (inclusive) at which this condition passes.\n" +
             "Condition is true when CurrentDay >= RequiredDay.")]
    [Min(1)]
    public int RequiredDay = 1;

    // -------------------------------------------------------------------------
    // Evaluate
    // -------------------------------------------------------------------------

    protected override bool Evaluate()
    {
        // Safe null check for GameClock singleton
        if (GameClock.Instance == null)
        {
            Debug.LogWarning("[DayCondition] GameClock.Instance is null. Day check cannot be evaluated. Returning false.");
            return false;
        }

        // Check if current day meets or exceeds required day
        return GameClock.Instance.CurrentDay >= RequiredDay;
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string Describe() =>
        $"Day >= {RequiredDay}";
}
