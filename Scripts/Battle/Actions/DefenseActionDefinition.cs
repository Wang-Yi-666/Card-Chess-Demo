namespace CardChessDemo.Battle.Actions;

public sealed class DefenseActionDefinition
{
    public DefenseActionDefinition(int damageReductionPercent, int shieldGain = 0)
    {
        DamageReductionPercent = damageReductionPercent < 0 ? 0 : damageReductionPercent;
        ShieldGain = shieldGain < 0 ? 0 : shieldGain;
    }

    public int DamageReductionPercent { get; }
    public int ShieldGain { get; }
}
