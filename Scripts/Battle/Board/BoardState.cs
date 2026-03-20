using System;
using System.Collections.Generic;
using Godot;

namespace CardChessDemo.Battle.Board;

public sealed class BoardState
{
    private readonly Dictionary<Vector2I, BoardCellState> _cells = new();
    private readonly HashSet<string> _roomTags = new(StringComparer.Ordinal);

    public Vector2I Size { get; private set; } = Vector2I.Zero;

    public IReadOnlyCollection<string> RoomTags => _roomTags;

    public void Initialize(Vector2I size, string defaultTerrainId, int defaultMoveCost)
    {
        _cells.Clear();
        _roomTags.Clear();
        Size = size;

        for (int y = 0; y < size.Y; y++)
        {
            for (int x = 0; x < size.X; x++)
            {
                Vector2I cell = new(x, y);
                _cells[cell] = new BoardCellState(cell, defaultTerrainId, defaultMoveCost);
            }
        }
    }

    public bool ContainsCell(Vector2I cell)
    {
        return _cells.ContainsKey(cell);
    }

    public bool TryGetCell(Vector2I cell, out BoardCellState? cellState)
    {
        bool found = _cells.TryGetValue(cell, out BoardCellState? foundCell);
        cellState = foundCell;
        return found;
    }

    public BoardCellState GetCell(Vector2I cell)
    {
        if (!_cells.TryGetValue(cell, out BoardCellState? cellState))
        {
            throw new InvalidOperationException($"Cell {cell} is outside of the board.");
        }

        return cellState;
    }

    public IEnumerable<BoardCellState> EnumerateCells()
    {
        return _cells.Values;
    }

    public void SetRoomTags(IEnumerable<string> tags)
    {
        _roomTags.Clear();

        foreach (string tag in tags)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                _roomTags.Add(tag);
            }
        }
    }

    public void PlaceObject(BoardObject boardObject)
    {
        BoardCellState cellState = GetCell(boardObject.Cell);

        if (boardObject.ObjectType == BoardObjectType.Unit)
        {
            cellState.SetUnitOccupant(boardObject.ObjectId);
            return;
        }

        if (boardObject.BlocksMovement || boardObject.BlocksLineOfSight || !boardObject.StackableWithUnit)
        {
            cellState.AddBlockingObject(boardObject.ObjectId);
            return;
        }

        cellState.AddResidentObject(boardObject.ObjectId);
    }

    public void RemoveObject(BoardObject boardObject)
    {
        if (!TryGetCell(boardObject.Cell, out BoardCellState? cellState) || cellState == null)
        {
            return;
        }

        if (boardObject.ObjectType == BoardObjectType.Unit)
        {
            if (cellState.UnitObjectId == boardObject.ObjectId)
            {
                cellState.ClearUnitOccupant();
            }

            return;
        }

        cellState.RemoveResidentObject(boardObject.ObjectId);
        cellState.RemoveBlockingObject(boardObject.ObjectId);
    }
}
