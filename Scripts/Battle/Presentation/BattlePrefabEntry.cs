using Godot;

namespace CardChessDemo.Battle.Presentation;

[GlobalClass]
public partial class BattlePrefabEntry : Resource
{
    [Export] public string DefinitionId { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = "Unit";
    [Export] public PackedScene? PrefabScene { get; set; }
    [Export] public int DefaultMaxHp { get; set; } = 0;
    [Export] public int DefaultCurrentHp { get; set; } = 0;
    [Export] public int DefaultMovePointsPerTurn { get; set; } = 0;
}
