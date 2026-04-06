using Godot;
using CardChessDemo.Battle.Board;

namespace CardChessDemo.Battle.Data;

[GlobalClass]
public partial class BoardObjectSpawnDefinition : Resource
{
    [Export] public string ObjectId { get; set; } = string.Empty;
    [Export] public string DefinitionId { get; set; } = "undefined";
    [Export] public string AiId { get; set; } = string.Empty;
    [Export] public BoardObjectType ObjectType { get; set; } = BoardObjectType.Obstacle;
    [Export] public Vector2I Cell { get; set; } = Vector2I.Zero;
    [Export] public BoardObjectFaction Faction { get; set; } = BoardObjectFaction.None;
    [Export] public string[] Tags { get; set; } = System.Array.Empty<string>();
    [Export] public int MaxHp { get; set; } = 0;
    [Export] public int CurrentHp { get; set; } = 0;
    [Export] public int MaxShield { get; set; } = 0;
    [Export] public int CurrentShield { get; set; } = 0;
    [Export] public bool BlocksMovement { get; set; } = false;
    [Export] public bool BlocksLineOfSight { get; set; } = false;
    [Export] public bool StackableWithUnit { get; set; } = true;
    [Export] public int MoveCostModifier { get; set; } = 0;
    [Export] public Vector2I InitialFacing { get; set; } = Vector2I.Right;
    [Export] public Godot.Collections.Dictionary InitialStatePayload { get; set; } = new();
}
