namespace CardChessDemo.Battle.Arakawa;

public sealed class ArakawaAbilityDefinition
{
    public ArakawaAbilityDefinition(string abilityId, string displayName, int energyCost)
    {
        AbilityId = abilityId;
        DisplayName = displayName;
        EnergyCost = energyCost < 0 ? 0 : energyCost;
    }

    public string AbilityId { get; }
    public string DisplayName { get; }
    public int EnergyCost { get; }
}
