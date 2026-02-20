/// <summary>
/// Numeric comparison operations used by conditions that check integer values
/// (e.g. stat thresholds, relationship scores).
///
/// Append new values at the end only — serialised as int by Unity.
/// </summary>
public enum ComparisonOperator
{
    /// <summary>The runtime value must equal the threshold exactly.</summary>
    Equal,

    /// <summary>The runtime value must be greater than or equal to the threshold.</summary>
    GreaterOrEqual,

    /// <summary>The runtime value must be less than or equal to the threshold.</summary>
    LessOrEqual,
}
