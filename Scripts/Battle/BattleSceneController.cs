using Godot;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.Data;
using CardChessDemo.Battle.Turn;

namespace CardChessDemo.Battle;

public partial class BattleSceneController : Node2D
{
    [Export] public RoomLayoutDefinition? LayoutDefinition { get; set; }

    public BoardState? BoardState { get; private set; }

    public BoardObjectRegistry? Registry { get; private set; }

    public BoardQueryService? QueryService { get; private set; }

    public TurnActionState? TurnState { get; private set; }

    public override void _Ready()
    {
        BoardState = new BoardState();
        Registry = new BoardObjectRegistry();
        QueryService = new BoardQueryService(BoardState, Registry);
        TurnState = new TurnActionState();
        TurnState.StartNewTurn(1);

        RoomLayoutDefinition layout = LayoutDefinition ?? CreateFallbackLayout();
        BoardInitializer initializer = new(BoardState, Registry);
        initializer.InitializeFromLayout(layout);

        BattleBoardDebugView? debugView = GetNodeOrNull<BattleBoardDebugView>("BoardRoot/DebugView");
        debugView?.Bind(this);

        GD.Print($"BattleSceneController: layout={layout.LayoutId}, size={layout.BoardSize}, objects={Registry.Count}");
    }

    public bool TryMoveObject(string objectId, Vector2I targetCell, out string failureReason)
    {
        failureReason = "BoardQueryService has not been initialized.";

        if (QueryService == null)
        {
            return false;
        }

        return QueryService.TryMoveObject(objectId, targetCell, out failureReason);
    }

    private static RoomLayoutDefinition CreateFallbackLayout()
    {
        return new RoomLayoutDefinition
        {
            LayoutId = "debug_bootstrap_layout",
            BoardSize = new Vector2I(8, 6),
            DefaultTerrainId = "metal_floor",
            DefaultMoveCost = 1,
            Tags = new[] { "debug" },
            PlayerSpawnCells = new[] { new Vector2I(1, 4) },
            EnemySpawnCells = new[] { new Vector2I(6, 1) },
            ObjectSpawns = new BoardObjectSpawnDefinition[]
            {
                new()
                {
                    ObjectId = "player_unit",
                    DefinitionId = "player_debug_unit",
                    ObjectType = BoardObjectType.Unit,
                    Cell = new Vector2I(1, 4),
                    Faction = BoardObjectFaction.Player,
                    Tags = new[] { "player" },
                    StackableWithUnit = false,
                },
                new()
                {
                    ObjectId = "enemy_unit",
                    DefinitionId = "enemy_debug_unit",
                    ObjectType = BoardObjectType.Unit,
                    Cell = new Vector2I(6, 1),
                    Faction = BoardObjectFaction.Enemy,
                    Tags = new[] { "enemy" },
                    StackableWithUnit = false,
                },
                new()
                {
                    ObjectId = "cover_box_a",
                    DefinitionId = "destructible_cover",
                    ObjectType = BoardObjectType.Obstacle,
                    Cell = new Vector2I(3, 2),
                    Faction = BoardObjectFaction.World,
                    Tags = new[] { "destructible", "cover" },
                    MaxHp = 3,
                    CurrentHp = 3,
                    BlocksMovement = true,
                    BlocksLineOfSight = true,
                    StackableWithUnit = false,
                },
                new()
                {
                    ObjectId = "electro_floor_a",
                    DefinitionId = "electro_floor",
                    ObjectType = BoardObjectType.Field,
                    Cell = new Vector2I(2, 4),
                    Faction = BoardObjectFaction.World,
                    Tags = new[] { "hazard", "passable" },
                    StackableWithUnit = true,
                    MoveCostModifier = 1,
                },
            },
        };
    }
}
