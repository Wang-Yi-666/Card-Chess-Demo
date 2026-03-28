using System;
using System.Linq;

namespace CardChessDemo.Battle.Cards;

public sealed class BattleDeckResolvedCard
{
	public BattleDeckResolvedCard(BattleCardTemplate template, bool usesOverlimitCarry, int appliedBuildPoints)
	{
		Template = template ?? throw new ArgumentNullException(nameof(template));
		UsesOverlimitCarry = usesOverlimitCarry;
		AppliedBuildPoints = Math.Max(0, appliedBuildPoints);
		CycleTags = template.GetNormalizedCycleTags().ToArray();
	}

	public BattleCardTemplate Template { get; }

	public bool UsesOverlimitCarry { get; }

	public int AppliedBuildPoints { get; }

	public string[] CycleTags { get; }

	public BattleCardDefinition BuildRuntimeDefinition()
	{
		return Template.BuildRuntimeDefinition(UsesOverlimitCarry);
	}
}
