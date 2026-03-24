namespace CardChessDemo.Battle.Cards;

public enum BattleCardCategory
{
    Attack = 0,
    Skill = 1,
}

public enum BattleCardTargetingMode
{
    None = 0,
    EnemyUnit = 1,
    StraightLineEnemy = 2,
}

public sealed class BattleCardDefinition
{
    public BattleCardDefinition(
        string cardId,
        string displayName,
        string description,
        int cost,
        BattleCardCategory category,
        BattleCardTargetingMode targetingMode,
        int range = 0,
        int damage = 0,
        int drawCount = 0,
        int energyGain = 0,
        int shieldGain = 0,
        bool isQuick = false,
        bool exhaustsOnPlay = false)
    {
        CardId = string.IsNullOrWhiteSpace(cardId) ? "card" : cardId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? CardId : displayName;
        Description = description ?? string.Empty;
        Cost = cost < 0 ? 0 : cost;
        Category = category;
        TargetingMode = targetingMode;
        Range = range < 0 ? 0 : range;
        Damage = damage < 0 ? 0 : damage;
        DrawCount = drawCount < 0 ? 0 : drawCount;
        EnergyGain = energyGain < 0 ? 0 : energyGain;
        ShieldGain = shieldGain < 0 ? 0 : shieldGain;
        IsQuick = isQuick;
        ExhaustsOnPlay = exhaustsOnPlay;
    }

    public string CardId { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public int Cost { get; }

    public BattleCardCategory Category { get; }

    public BattleCardTargetingMode TargetingMode { get; }

    public int Range { get; }

    public int Damage { get; }

    public int DrawCount { get; }

    public int EnergyGain { get; }

    public int ShieldGain { get; }

    public bool IsQuick { get; }

    public bool ExhaustsOnPlay { get; }

    public bool RequiresTarget => TargetingMode != BattleCardTargetingMode.None;

    public BattleCardDefinition CreateEnhanced(BattleCardEnhancementDefinition enhancement)
    {
        string suffix = string.IsNullOrWhiteSpace(enhancement.DisplaySuffix) ? "+" : enhancement.DisplaySuffix;
        string enhancedDescription = string.IsNullOrWhiteSpace(enhancement.DescriptionSuffix)
            ? Description
            : $"{Description} / {enhancement.DescriptionSuffix}";

        return new BattleCardDefinition(
            CardId,
            $"{DisplayName}{suffix}",
            enhancedDescription,
            cost: Cost + enhancement.CostDelta,
            category: Category,
            targetingMode: TargetingMode,
            range: Range + enhancement.RangeDelta,
            damage: Damage + enhancement.DamageDelta,
            drawCount: DrawCount + enhancement.DrawCountDelta,
            energyGain: EnergyGain + enhancement.EnergyGainDelta,
            shieldGain: ShieldGain + enhancement.ShieldGainDelta,
            isQuick: enhancement.IsQuickOverride ?? IsQuick,
            exhaustsOnPlay: enhancement.ExhaustsOnPlayOverride ?? ExhaustsOnPlay);
    }
}
