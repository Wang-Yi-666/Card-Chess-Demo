using Godot;
using CardChessDemo.Battle.Board;

namespace CardChessDemo.Battle.State;

public sealed class BattleObjectState
{
    public BattleObjectState(string objectId, string definitionId, string displayName, BoardObjectType objectType, BoardObjectFaction faction)
    {
        ObjectId = objectId;
        DefinitionId = definitionId;
        DisplayName = displayName;
        ObjectType = objectType;
        Faction = faction;
    }

    public string ObjectId { get; }
    public string DefinitionId { get; }
    public string DisplayName { get; set; }
    public BoardObjectType ObjectType { get; }
    public BoardObjectFaction Faction { get; }
    public Vector2I Cell { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int MovePointsPerTurn { get; set; }
    public string CurrentAnimation { get; set; } = "idle";
    public bool IsPlayer { get; set; }
}
