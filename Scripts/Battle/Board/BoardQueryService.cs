using System;
using System.Collections.Generic;
using Godot;

namespace CardChessDemo.Battle.Board;

public sealed class BoardQueryService
{
    private readonly BoardState _boardState;
    private readonly BoardObjectRegistry _registry;

    public BoardQueryService(BoardState boardState, BoardObjectRegistry registry)
    {
        _boardState = boardState;
        _registry = registry;
    }

    public IReadOnlyList<BoardObject> GetObjectsAtCell(Vector2I cell)
    {
        if (!_boardState.TryGetCell(cell, out BoardCellState? cellState) || cellState == null)
        {
            return Array.Empty<BoardObject>();
        }

        List<BoardObject> objects = new();

        if (!string.IsNullOrWhiteSpace(cellState.UnitObjectId) && _registry.TryGet(cellState.UnitObjectId, out BoardObject? unitObject) && unitObject != null)
        {
            objects.Add(unitObject);
        }

        foreach (string objectId in cellState.BlockingObjectIds)
        {
            if (_registry.TryGet(objectId, out BoardObject? boardObject) && boardObject != null)
            {
                objects.Add(boardObject);
            }
        }

        foreach (string objectId in cellState.ResidentObjectIds)
        {
            if (_registry.TryGet(objectId, out BoardObject? boardObject) && boardObject != null)
            {
                objects.Add(boardObject);
            }
        }

        return objects;
    }

    public int GetMoveCost(Vector2I cell)
    {
        BoardCellState cellState = _boardState.GetCell(cell);
        int moveCost = cellState.BaseMoveCost;

        foreach (BoardObject boardObject in GetObjectsAtCell(cell))
        {
            moveCost += boardObject.MoveCostModifier;
        }

        return Math.Max(0, moveCost);
    }

    public bool TryMoveObject(string objectId, Vector2I targetCell, out string failureReason)
    {
        failureReason = string.Empty;

        if (!_registry.TryGet(objectId, out BoardObject? boardObject) || boardObject == null)
        {
            failureReason = $"Object {objectId} was not found in the registry.";
            return false;
        }

        if (boardObject.Cell == targetCell)
        {
            return true;
        }

        if (!OccupancyRules.CanPlaceObject(_boardState, _registry, boardObject, targetCell, out failureReason))
        {
            return false;
        }

        _boardState.RemoveObject(boardObject);
        boardObject.SetCell(targetCell);
        _boardState.PlaceObject(boardObject);
        return true;
    }
}
