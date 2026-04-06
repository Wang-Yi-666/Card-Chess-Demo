using Godot;
using CardChessDemo.Battle.Board;

namespace CardChessDemo.Battle.State;

public sealed class BattleObjectState
{
    public BattleObjectState(string objectId, string definitionId, string aiId, string displayName, BoardObjectType objectType, BoardObjectFaction faction)
    {
        ObjectId = objectId;
        DefinitionId = definitionId;
        AiId = aiId;
        DisplayName = displayName;
        ObjectType = objectType;
        Faction = faction;
    }

    public string ObjectId { get; }
    public string DefinitionId { get; }
    public string AiId { get; }
    public string DisplayName { get; set; }
    public BoardObjectType ObjectType { get; }
    public BoardObjectFaction Faction { get; }
    public Vector2I Cell { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int MaxShield { get; set; }
    public int CurrentShield { get; set; }
    public bool HasDefenseStance { get; set; }
    public int DefenseDamageReductionPercent { get; set; }
    public int MovePointsPerTurn { get; set; }
    public int AttackRange { get; set; }
    public int AttackDamage { get; set; }
    public Vector2 InitialFacing { get; set; } = Vector2.Right;
    public string CurrentAnimation { get; set; } = "idle";
    public bool IsPlayer { get; set; }
}
