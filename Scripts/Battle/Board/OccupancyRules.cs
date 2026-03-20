using System.Collections.Generic;
using Godot;

namespace CardChessDemo.Battle.Board;

public static class OccupancyRules
{
    public static bool CanPlaceObject(
        BoardState boardState,
        BoardObjectRegistry registry,
        BoardObject boardObject,
        Vector2I targetCell,
        out string failureReason)
    {
        failureReason = string.Empty;

        if (!boardState.TryGetCell(targetCell, out BoardCellState? cellState) || cellState == null)
        {
            failureReason = $"Cell {targetCell} is outside of the board.";
            return false;
        }

        List<BoardObject> occupants = GetOccupants(registry, cellState);

        if (boardObject.ObjectType == BoardObjectType.Unit)
        {
            if (!string.IsNullOrWhiteSpace(cellState.UnitObjectId) && cellState.UnitObjectId != boardObject.ObjectId)
            {
                failureReason = $"Cell {targetCell} already contains another unit.";
                return false;
            }

            foreach (BoardObject occupant in occupants)
            {
                if (occupant.ObjectId == boardObject.ObjectId)
                {
                    continue;
                }

                if (!occupant.StackableWithUnit)
                {
                    failureReason = $"Object {occupant.ObjectId} does not allow unit stacking at {targetCell}.";
                    return false;
                }
            }

            return true;
        }

        if (!boardObject.StackableWithUnit && !string.IsNullOrWhiteSpace(cellState.UnitObjectId) && cellState.UnitObjectId != boardObject.ObjectId)
        {
            failureReason = $"Object {boardObject.ObjectId} cannot share cell {targetCell} with a unit.";
            return false;
        }

        return true;
    }

    private static List<BoardObject> GetOccupants(BoardObjectRegistry registry, BoardCellState cellState)
    {
        List<BoardObject> occupants = new();

        if (!string.IsNullOrWhiteSpace(cellState.UnitObjectId) && registry.TryGet(cellState.UnitObjectId, out BoardObject? unitObject) && unitObject != null)
        {
            occupants.Add(unitObject);
        }

        foreach (string objectId in cellState.ResidentObjectIds)
        {
            if (registry.TryGet(objectId, out BoardObject? boardObject) && boardObject != null)
            {
                occupants.Add(boardObject);
            }
        }

        foreach (string objectId in cellState.BlockingObjectIds)
        {
            if (registry.TryGet(objectId, out BoardObject? boardObject) && boardObject != null)
            {
                occupants.Add(boardObject);
            }
        }

        return occupants;
    }
}
