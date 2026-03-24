namespace CardChessDemo.Battle.Cards;

public sealed class BattleCardEnhancementDefinition
{
    public BattleCardEnhancementDefinition(
        string displaySuffix,
        string descriptionSuffix,
        int costDelta = 0,
        int rangeDelta = 0,
        int damageDelta = 0,
        int drawCountDelta = 0,
        int energyGainDelta = 0,
        int shieldGainDelta = 0,
        bool? isQuickOverride = null,
        bool? exhaustsOnPlayOverride = null)
    {
        DisplaySuffix = displaySuffix ?? string.Empty;
        DescriptionSuffix = descriptionSuffix ?? string.Empty;
        CostDelta = costDelta;
        RangeDelta = rangeDelta;
        DamageDelta = damageDelta;
        DrawCountDelta = drawCountDelta;
        EnergyGainDelta = energyGainDelta;
        ShieldGainDelta = shieldGainDelta;
        IsQuickOverride = isQuickOverride;
        ExhaustsOnPlayOverride = exhaustsOnPlayOverride;
    }

    public string DisplaySuffix { get; }
    public string DescriptionSuffix { get; }
    public int CostDelta { get; }
    public int RangeDelta { get; }
    public int DamageDelta { get; }
    public int DrawCountDelta { get; }
    public int EnergyGainDelta { get; }
    public int ShieldGainDelta { get; }
    public bool? IsQuickOverride { get; }
    public bool? ExhaustsOnPlayOverride { get; }
}
