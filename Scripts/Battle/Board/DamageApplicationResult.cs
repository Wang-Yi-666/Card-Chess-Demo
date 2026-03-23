using System;
using System.Collections.Generic;
using System.Linq;

namespace CardChessDemo.Battle.Board;

public enum CombatImpactType
{
    HealthDamage = 0,
    ShieldDamage = 1,
    HealthHeal = 2,
    ShieldGain = 3,
}

public readonly record struct CombatImpact(CombatImpactType ImpactType, int Amount);

public sealed class DamageApplicationResult
{
    private static readonly CombatImpact[] EmptyImpacts = Array.Empty<CombatImpact>();

    public DamageApplicationResult(IEnumerable<CombatImpact>? impacts = null)
    {
        Impacts = impacts is null
            ? EmptyImpacts
            : impacts.Where(impact => impact.Amount > 0).ToArray();
    }

    public IReadOnlyList<CombatImpact> Impacts { get; }

    public bool HasAnyImpact => Impacts.Count > 0;
}
