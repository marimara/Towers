/// <summary>
/// Enumeration of in-game time periods based on hour of day.
///
/// Divides the 24-hour day into meaningful periods for narrative and gameplay.
/// Each period is associated with a range of hours.
///
/// Usage:
///   TimePeriod period = GameClock.Instance.CurrentPeriod;
///   if (period == TimePeriod.Night)
///       Debug.Log("It's nighttime!");
/// </summary>
public enum TimePeriod
{
    /// <summary>
    /// Early morning hours before sunrise.
    /// Hours: 3–5 (3 AM to 5:59 AM)
    /// Perfect for: Night owls, bakers, guards changing shift
    /// </summary>
    LateNight = 0,

    /// <summary>
    /// Morning hours: sunrise to midday.
    /// Hours: 6–11 (6 AM to 11:59 AM)
    /// Perfect for: Work, training, exploration
    /// </summary>
    Morning = 1,

    /// <summary>
    /// Afternoon hours: midday to late afternoon.
    /// Hours: 12–17 (12 PM to 5:59 PM)
    /// Perfect for: Peak activity, social time, quests
    /// </summary>
    Afternoon = 2,

    /// <summary>
    /// Evening hours: sunset to night.
    /// Hours: 18–21 (6 PM to 9:59 PM)
    /// Perfect for: Dinner, gatherings, wind-down
    /// </summary>
    Evening = 3,

    /// <summary>
    /// Night hours: late evening to near midnight.
    /// Hours: 22–2 (10 PM to 2:59 AM)
    /// Perfect for: Sleep, secrets, supernatural events
    /// </summary>
    Night = 4,
}
