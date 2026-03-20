using Godot;
using CardChessDemo.Battle.Data;

namespace CardChessDemo.Battle.Board;

public sealed class BoardInitializer
{
    private readonly BoardState _boardState;
    private readonly BoardObjectRegistry _registry;

    public BoardInitializer(BoardState boardState, BoardObjectRegistry registry)
    {
        _boardState = boardState;
        _registry = registry;
    }

    public void InitializeFromLayout(RoomLayoutDefinition layout)
    {
        _registry.Clear();
        _boardState.Initialize(layout.BoardSize, layout.DefaultTerrainId, layout.DefaultMoveCost);
        _boardState.SetRoomTags(layout.Tags);

        foreach (BoardObjectSpawnDefinition spawn in layout.ObjectSpawns)
        {
            BoardObject boardObject = BoardObject.FromSpawn(spawn);

            if (!_registry.Register(boardObject))
            {
                GD.PushWarning($"BoardInitializer: duplicate object id '{boardObject.ObjectId}' was skipped.");
                continue;
            }

            if (!OccupancyRules.CanPlaceObject(_boardState, _registry, boardObject, boardObject.Cell, out string failureReason))
            {
                _registry.Remove(boardObject.ObjectId);
                GD.PushWarning($"BoardInitializer: failed to place '{boardObject.ObjectId}' at {boardObject.Cell}. {failureReason}");
                continue;
            }

            _boardState.PlaceObject(boardObject);
        }
    }
}
