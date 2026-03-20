using Godot;

namespace CardChessDemo.Battle.Data;

[GlobalClass]
public partial class RoomLayoutDefinition : Resource
{
    [Export] public string LayoutId { get; set; } = "debug_layout";
    [Export] public Vector2I BoardSize { get; set; } = new(8, 6);
    [Export] public string DefaultTerrainId { get; set; } = "floor";
    [Export] public int DefaultMoveCost { get; set; } = 1;
    [Export] public BoardObjectSpawnDefinition[] ObjectSpawns { get; set; } = System.Array.Empty<BoardObjectSpawnDefinition>();
    [Export] public string[] Tags { get; set; } = System.Array.Empty<string>();
    public Vector2I[] PlayerSpawnCells { get; set; } = System.Array.Empty<Vector2I>();
    public Vector2I[] EnemySpawnCells { get; set; } = System.Array.Empty<Vector2I>();
}
