using System;
using System.Collections.Generic;
using System.Linq;
using CardChessDemo.Battle.Boundary;

namespace CardChessDemo.Battle.Cards;

public sealed class BattleDeckConstructionService
{
	private readonly BattleCardLibrary _library;
	private readonly BattleDeckBuildRules _rules;

	public BattleDeckConstructionService(BattleCardLibrary library, BattleDeckBuildRules rules)
	{
		_library = library;
		_rules = rules;
	}

	public IReadOnlyList<BattleCardTemplate> GetAvailableCardPool(ProgressionSnapshot snapshot)
	{
		return _library.Entries
			.Where(template => template != null && (template.CanCarryNormally(snapshot) || template.CanCarryOverlimit(snapshot)))
			.OrderBy(template => template.CardId, StringComparer.Ordinal)
			.ToArray();
	}

	public BattleDeckValidationResult ValidateDeck(DeckBuildSnapshot snapshot, ProgressionSnapshot progression)
	{
		BattleDeckValidationResult result = new()
		{
			EffectiveMinDeckSize = Math.Max(1, _rules.MinDeckSize + progression.DeckMinCardCountDelta),
			EffectivePointBudget = Math.Max(0, _rules.BasePointBudget + progression.DeckPointBudgetBonus),
			EffectiveMaxCopiesPerCard = Math.Max(1, _rules.BaseMaxCopiesPerCard + progression.DeckMaxCopiesPerCardBonus),
			EffectiveCycleCardLimit = Math.Max(0, _rules.BaseCycleCardLimit),
			EffectiveQuickCycleCardLimit = Math.Max(0, _rules.BaseQuickCycleCardLimit),
			EffectiveEnergyPositiveCardLimit = Math.Max(0, _rules.BaseEnergyPositiveCardLimit),
			EffectiveOverlimitCarrySlots = Math.Max(0, _rules.BaseOverlimitCarrySlots),
		};

		Dictionary<string, int> copyCounts = new(StringComparer.Ordinal);
		Dictionary<string, int> cycleCounts = new(StringComparer.Ordinal);
		foreach (string cardId in snapshot.CardIds)
		{
			BattleCardTemplate? template = _library.FindTemplate(cardId);
			if (template == null)
			{
				result.Errors.Add($"Card '{cardId}' was not found in BattleCardLibrary.");
				continue;
			}

			bool canCarryNormally = template.CanCarryNormally(progression);
			bool canCarryOverlimit = !canCarryNormally && template.CanCarryOverlimit(progression);
			if (!canCarryNormally && !canCarryOverlimit)
			{
				result.Errors.Add($"Card '{cardId}' is not available for the current progression snapshot.");
				continue;
			}

			BattleDeckResolvedCard resolvedCard = new(
				template,
				usesOverlimitCarry: canCarryOverlimit,
				appliedBuildPoints: Math.Max(0, template.BuildPoints) + (canCarryOverlimit ? Math.Max(0, template.OverlimitExtraBuildPoints) : 0));
			result.ResolvedCards.Add(resolvedCard);
			result.ResolvedTemplates.Add(template);
			copyCounts.TryGetValue(cardId, out int currentCopies);
			copyCounts[cardId] = currentCopies + 1;
			result.TotalBuildPoints += resolvedCard.AppliedBuildPoints;

			foreach (string cycleTag in resolvedCard.CycleTags)
			{
				cycleCounts.TryGetValue(cycleTag, out int currentCount);
				cycleCounts[cycleTag] = currentCount + 1;
			}

			if (canCarryOverlimit)
			{
				result.UsedOverlimitCarrySlots += 1;
			}
		}

		result.TotalCardCount = result.ResolvedCards.Count;

		if (result.TotalCardCount < result.EffectiveMinDeckSize)
		{
			result.Errors.Add($"Deck size is below minimum. Need {result.EffectiveMinDeckSize}, got {result.TotalCardCount}.");
		}

		if (result.TotalBuildPoints > result.EffectivePointBudget)
		{
			result.Errors.Add($"Deck impact budget exceeded. Budget {result.EffectivePointBudget}, got {result.TotalBuildPoints}.");
		}

		if (result.UsedOverlimitCarrySlots > result.EffectiveOverlimitCarrySlots)
		{
			result.Errors.Add($"Overlimit carry slots exceeded. Slots {result.EffectiveOverlimitCarrySlots}, used {result.UsedOverlimitCarrySlots}.");
		}

		if (result.EffectiveCycleCardLimit > 0
			&& cycleCounts.TryGetValue("cycle", out int cycleCount)
			&& cycleCount > result.EffectiveCycleCardLimit)
		{
			result.Errors.Add($"Cycle-tagged cards exceed limit. Max {result.EffectiveCycleCardLimit}, got {cycleCount}.");
		}

		if (result.EffectiveQuickCycleCardLimit > 0
			&& cycleCounts.TryGetValue("quick_cycle", out int quickCycleCount)
			&& quickCycleCount > result.EffectiveQuickCycleCardLimit)
		{
			result.Errors.Add($"Quick-cycle cards exceed limit. Max {result.EffectiveQuickCycleCardLimit}, got {quickCycleCount}.");
		}

		if (result.EffectiveEnergyPositiveCardLimit > 0
			&& cycleCounts.TryGetValue("energy_positive", out int energyPositiveCount)
			&& energyPositiveCount > result.EffectiveEnergyPositiveCardLimit)
		{
			result.Errors.Add($"Energy-positive cards exceed limit. Max {result.EffectiveEnergyPositiveCardLimit}, got {energyPositiveCount}.");
		}

		foreach ((string cardId, int copies) in copyCounts)
		{
			BattleCardTemplate template = _library.FindTemplate(cardId)!;
			bool usesOverlimitCarry = !template.CanCarryNormally(progression) && template.CanCarryOverlimit(progression);
			int maxCopies = usesOverlimitCarry
				? 1
				: Math.Min(result.EffectiveMaxCopiesPerCard, Math.Max(1, template.MaxCopiesInDeck));
			if (copies > maxCopies)
			{
				result.Errors.Add($"Card '{cardId}' exceeds copy limit. Max {maxCopies}, got {copies}.");
			}
		}

		foreach (BattleDeckResolvedCard resolvedCard in result.ResolvedCards.Where(card => card.UsesOverlimitCarry))
		{
			result.Warnings.Add($"Card '{resolvedCard.Template.DisplayName}' is using overlimit carry penalties.");
		}

		if (!snapshot.TryValidate(out string failureReason))
		{
			result.Errors.Add(failureReason);
		}

		if (!progression.TryValidate(out failureReason))
		{
			result.Errors.Add(failureReason);
		}

		return result;
	}

	public BattleCardDefinition[] BuildRuntimeDefinitions(DeckBuildSnapshot snapshot, ProgressionSnapshot progression, out BattleDeckValidationResult validationResult)
	{
		validationResult = ValidateDeck(snapshot, progression);
		if (!validationResult.IsValid)
		{
			return Array.Empty<BattleCardDefinition>();
		}

		return validationResult.ResolvedCards
			.Select(card => card.BuildRuntimeDefinition())
			.ToArray();
	}
}
