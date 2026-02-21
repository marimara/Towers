using System;
using UnityEngine;

/// <summary>
/// Condition that checks whether the current in-game time period matches a required period.
///
/// Instead of checking a specific hour range, this condition uses the <see cref="TimePeriod"/>
/// enum to provide a semantic, higher-level check (Morning, Afternoon, Evening, Night, LateNight).
///
/// Requires a <c>GameClock</c> singleton that exposes <c>TimePeriod CurrentPeriod</c>.
/// </summary>
[Serializable]
public sealed class TimePeriodCondition : EventCondition
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("The time period that must be active for this condition to pass.")]
    public TimePeriod RequiredPeriod = TimePeriod.Morning;

    // -------------------------------------------------------------------------
    // Evaluate
    // -------------------------------------------------------------------------

    protected override bool Evaluate()
    {
        // Safe null check for GameClock singleton
        if (GameClock.Instance == null)
        {
            Debug.LogWarning("[TimePeriodCondition] GameClock.Instance is null. Period check cannot be evaluated. Returning false.");
            return false;
        }

        // Read current period from GameClock and compare
        TimePeriod currentPeriod = GameClock.Instance.CurrentPeriod;
        return currentPeriod == RequiredPeriod;
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string Describe() =>
        $"TimePeriod == {RequiredPeriod}";
}
