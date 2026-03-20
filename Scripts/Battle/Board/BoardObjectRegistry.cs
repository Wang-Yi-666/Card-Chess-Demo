using System;
using System.Collections.Generic;

namespace CardChessDemo.Battle.Board;

public sealed class BoardObjectRegistry
{
    private readonly Dictionary<string, BoardObject> _objects = new(StringComparer.Ordinal);

    public int Count => _objects.Count;

    public IEnumerable<BoardObject> AllObjects => _objects.Values;

    public void Clear()
    {
        _objects.Clear();
    }

    public bool Register(BoardObject boardObject)
    {
        return _objects.TryAdd(boardObject.ObjectId, boardObject);
    }

    public bool Remove(string objectId)
    {
        return _objects.Remove(objectId);
    }

    public bool TryGet(string objectId, out BoardObject? boardObject)
    {
        return _objects.TryGetValue(objectId, out boardObject);
    }
}
