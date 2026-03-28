using System;
using System.Linq;
using Godot;
using CardChessDemo.Battle.Boundary;

namespace CardChessDemo.Battle.Cards;

[GlobalClass]
public partial class BattleCardTemplate : Resource
{
	[Export] public string CardId { get; set; } = "card_id";
	[Export] public string DisplayName { get; set; } = "新卡牌";
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
	[Export] public BattleCardCategory Category { get; set; } = BattleCardCategory.Skill;
	[Export] public BattleCardTargetingMode TargetingMode { get; set; } = BattleCardTargetingMode.None;
	[Export] public int Cost { get; set; }
	[Export] public int Range { get; set; }
	[Export] public int Damage { get; set; }
	[Export] public int HealingAmount { get; set; }
	[Export] public int DrawCount { get; set; }
	[Export] public int EnergyGain { get; set; }
	[Export] public int ShieldGain { get; set; }
	[Export] public bool IsQuick { get; set; }
	[Export] public bool ExhaustsOnPlay { get; set; }
	[Export] public int BuildPoints { get; set; } = 1;
	[Export] public int MaxCopiesInDeck { get; set; } = 3;
	[Export] public int DefaultStarterCopies { get; set; } = 0;
	[Export] public bool UnlockedByDefault { get; set; } = true;
	[Export] public int RequiredPlayerLevel { get; set; } = 1;
	[Export] public string[] RequiredTalentIds { get; set; } = Array.Empty<string>();
	[Export] public string[] RequiredBranchTags { get; set; } = Array.Empty<string>();
	[Export] public string[] CycleTags { get; set; } = Array.Empty<string>();
	[Export] public bool IsLearnedCard { get; set; }
	[Export] public bool DisallowOverlimitCarry { get; set; }
	[Export] public int OverlimitCostPenalty { get; set; } = 1;
	[Export(PropertyHint.Range, "0,1,0.05")] public float OverlimitEffectMultiplier { get; set; } = 0.8f;
	[Export] public int OverlimitExtraBuildPoints { get; set; } = 1;

	public BattleCardDefinition BuildRuntimeDefinition(bool applyOverlimitPenalty = false)
	{
		int cost = Cost;
		int damage = Damage;
		int healingAmount = HealingAmount;
		int drawCount = DrawCount;
		int energyGain = EnergyGain;
		int shieldGain = ShieldGain;
		string displayName = DisplayName;
		string description = Description;

		if (applyOverlimitPenalty)
		{
			float multiplier = Mathf.Clamp(OverlimitEffectMultiplier, 0.0f, 1.0f);
			cost += Math.Max(0, OverlimitCostPenalty);
			damage = ScalePositiveValue(damage, multiplier);
			healingAmount = ScalePositiveValue(healingAmount, multiplier);
			drawCount = ScalePositiveValue(drawCount, multiplier);
			energyGain = ScalePositiveValue(energyGain, multiplier);
			shieldGain = ScalePositiveValue(shieldGain, multiplier);
			displayName = string.IsNullOrWhiteSpace(displayName) ? "超规卡牌" : $"{displayName} [超规]";
			description = string.IsNullOrWhiteSpace(description)
				? "以超规方式携带，费用或效果已受惩罚。"
				: $"{description} / 超规携带：费用提高或效果衰减";
		}

		return new BattleCardDefinition(
			cardId: CardId,
			displayName: displayName,
			description: description,
			cost: cost,
			category: Category,
			targetingMode: TargetingMode,
			range: Range,
			damage: damage,
			healingAmount: healingAmount,
			drawCount: drawCount,
			energyGain: energyGain,
			shieldGain: shieldGain,
			isQuick: IsQuick,
			exhaustsOnPlay: ExhaustsOnPlay);
	}

	public bool IsOwned(ProgressionSnapshot snapshot)
	{
		return UnlockedByDefault || snapshot.UnlockedCardIds.Contains(CardId, StringComparer.Ordinal);
	}

	public bool MeetsCarryRequirements(ProgressionSnapshot snapshot)
	{
		if (snapshot.PlayerLevel < Math.Max(1, RequiredPlayerLevel))
		{
			return false;
		}

		if (RequiredTalentIds.Any(requiredTalentId => !snapshot.TalentIds.Contains(requiredTalentId, StringComparer.Ordinal)))
		{
			return false;
		}

		if (RequiredBranchTags.Any(requiredBranchTag => !snapshot.TalentBranchTags.Contains(requiredBranchTag, StringComparer.Ordinal)))
		{
			return false;
		}

		return true;
	}

	public bool CanCarryNormally(ProgressionSnapshot snapshot)
	{
		if (!MeetsCarryRequirements(snapshot))
		{
			return false;
		}

		return IsOwned(snapshot) || HasExplicitCarryRequirements();
	}

	public bool CanCarryOverlimit(ProgressionSnapshot snapshot)
	{
		if (DisallowOverlimitCarry)
		{
			return false;
		}

		return IsOwned(snapshot) && !CanCarryNormally(snapshot);
	}

	public bool IsUnlocked(ProgressionSnapshot snapshot)
	{
		return CanCarryNormally(snapshot);
	}

	public string[] GetNormalizedCycleTags()
	{
		return CycleTags
			.Where(tag => !string.IsNullOrWhiteSpace(tag))
			.Select(tag => tag.Trim().ToLowerInvariant())
			.Distinct(StringComparer.Ordinal)
			.ToArray();
	}

	private bool HasExplicitCarryRequirements()
	{
		return RequiredPlayerLevel > 1 || RequiredTalentIds.Length > 0 || RequiredBranchTags.Length > 0;
	}

	private static int ScalePositiveValue(int value, float multiplier)
	{
		if (value <= 0)
		{
			return 0;
		}

		return Math.Max(1, Mathf.FloorToInt(value * multiplier));
	}
}
