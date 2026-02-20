using System;
using UnityEngine;

/// <summary>
/// Consequence that modifies the relationship between two characters by a signed
/// delta after an event completes.
///
/// Delegates directly to <see cref="RelationshipSystem.ModifyRelationship"/>, which
/// is already live — no stub required.
///
/// When <see cref="Mutual"/> is true, the delta is applied in both directions
/// (From→To and To→From), mirroring the behaviour of <see cref="RelationshipChange"/>
/// in the dialogue system.
///
/// Pairs naturally with <see cref="RelationshipTierCondition"/>: an event can shift
/// a relationship as a consequence so that a subsequent tier condition check reflects
/// the new value during the recheck in <see cref="EventManager"/>.
/// </summary>
[Serializable]
public sealed class RelationshipConsequence : EventConsequence
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("The character whose feeling toward To is being modified (the relationship source).")]
    public VNCharacter From;

    [Tooltip("The character the relationship points toward (the relationship target).")]
    public VNCharacter To;

    [Tooltip("Amount to add to the relationship. Positive values improve it; negative values worsen it. " +
             "Result is clamped between 1 and 100 by RelationshipSystem.")]
    public int Delta;

    [Tooltip("When true, the delta is applied in both directions: From→To and To→From.")]
    public bool Mutual;

    // -------------------------------------------------------------------------
    // Execute
    // -------------------------------------------------------------------------

    protected override void Execute()
    {
        if (From == null || To == null)
        {
            Debug.LogWarning("[RelationshipConsequence] From or To character is null — consequence will not modify any relationship.");
            return;
        }

        var system = RelationshipSystem.Instance;

        if (system == null)
        {
            Debug.LogWarning("[RelationshipConsequence] RelationshipSystem instance not found — consequence will not modify any relationship.");
            return;
        }

        system.ModifyRelationship(From, To, Delta);

        if (Mutual)
            system.ModifyRelationship(To, From, Delta);
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string Describe()
    {
        string fromName = From != null
            ? (!string.IsNullOrEmpty(From.DisplayName) ? From.DisplayName : From.name)
            : "?";

        string toName = To != null
            ? (!string.IsNullOrEmpty(To.DisplayName) ? To.DisplayName : To.name)
            : "?";

        string mutual = Mutual ? " (Mutual)" : string.Empty;

        return $"Relationship {fromName} -> {toName} {Delta:+#;-#;0}{mutual}";
    }
}
