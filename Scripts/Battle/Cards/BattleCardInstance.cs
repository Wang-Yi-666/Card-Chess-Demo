namespace CardChessDemo.Battle.Cards;

public sealed class BattleCardInstance
{
    public BattleCardInstance(string instanceId, BattleCardDefinition definition)
    {
        InstanceId = instanceId;
        BaseDefinition = definition;
        Definition = definition;
    }

    public string InstanceId { get; }

    public BattleCardDefinition BaseDefinition { get; }

    public BattleCardDefinition Definition { get; private set; }

    public bool IsEnhanced { get; private set; }

    public bool TryApplyEnhancement(BattleCardEnhancementDefinition enhancement)
    {
        if (IsEnhanced)
        {
            return false;
        }

        Definition = BaseDefinition.CreateEnhanced(enhancement);
        IsEnhanced = true;
        return true;
    }
}
