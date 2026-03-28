using System.Collections.Generic;

namespace CardChessDemo.Battle.Cards;

public sealed class BattleDeckValidationResult
{
	public bool IsValid => Errors.Count == 0;

	public List<string> Errors { get; } = new();

	public List<string> Warnings { get; } = new();

	public int TotalCardCount { get; set; }

	public int TotalBuildPoints { get; set; }

	public int EffectiveMinDeckSize { get; set; }

	public int EffectiveMaxDeckSize { get; set; }

	public int EffectivePointBudget { get; set; }

	public int EffectiveMaxCopiesPerCard { get; set; }

	public int EffectiveCycleCardLimit { get; set; }

	public int EffectiveQuickCycleCardLimit { get; set; }

	public int EffectiveEnergyPositiveCardLimit { get; set; }

	public int EffectiveOverlimitCarrySlots { get; set; }

	public int UsedOverlimitCarrySlots { get; set; }

	public List<BattleCardTemplate> ResolvedTemplates { get; } = new();

	public List<BattleDeckResolvedCard> ResolvedCards { get; } = new();
}
