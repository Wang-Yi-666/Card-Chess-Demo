using System.Collections.Generic;
using Godot;

namespace CardChessDemo.Battle.Board;

public sealed class BoardCellState
{
    private readonly HashSet<string> _residentObjectIds = new();
    private readonly HashSet<string> _blockingObjectIds = new();
    private readonly HashSet<string> _tags = new();

    public BoardCellState(Vector2I cell, string terrainId, int baseMoveCost)
    {
        Cell = cell;
        TerrainId = terrainId;
        BaseMoveCost = baseMoveCost;
    }

    public Vector2I Cell { get; }

    public string TerrainId { get; set; }

    public int BaseMoveCost { get; set; }

    public string? UnitObjectId { get; private set; }

    public IReadOnlyCollection<string> ResidentObjectIds => _residentObjectIds;

    public IReadOnlyCollection<string> BlockingObjectIds => _blockingObjectIds;

    public IReadOnlyCollection<string> Tags => _tags;

    public void SetUnitOccupant(string objectId)
    {
        UnitObjectId = objectId;
    }

    public void ClearUnitOccupant()
    {
        UnitObjectId = null;
    }

    public void AddResidentObject(string objectId)
    {
        _residentObjectIds.Add(objectId);
    }

    public void RemoveResidentObject(string objectId)
    {
        _residentObjectIds.Remove(objectId);
    }

    public void AddBlockingObject(string objectId)
    {
        _blockingObjectIds.Add(objectId);
    }

    public void RemoveBlockingObject(string objectId)
    {
        _blockingObjectIds.Remove(objectId);
    }

    public void AddTag(string tag)
    {
        _tags.Add(tag);
    }
}
