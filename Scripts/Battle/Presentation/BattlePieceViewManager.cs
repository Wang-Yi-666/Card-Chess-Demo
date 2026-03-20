using System;
using System.Collections.Generic;
using Godot;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.Rooms;
using CardChessDemo.Battle.State;

namespace CardChessDemo.Battle.Presentation;

public sealed class BattlePieceViewManager
{
    private readonly Node _pieceRoot;
    private readonly BattlePrefabLibrary _prefabLibrary;
    private readonly Dictionary<string, BattleAnimatedViewBase> _views = new(StringComparer.Ordinal);

    public BattlePieceViewManager(Node pieceRoot, BattlePrefabLibrary prefabLibrary)
    {
        _pieceRoot = pieceRoot;
        _prefabLibrary = prefabLibrary;
    }

    public void Rebuild(BoardObjectRegistry registry, BattleObjectStateManager stateManager, BattleRoomTemplate room)
    {
        foreach (Node child in _pieceRoot.GetChildren())
        {
            child.QueueFree();
        }

        _views.Clear();

        foreach (BoardObject boardObject in registry.AllObjects)
        {
            BattleObjectState? state = stateManager.Get(boardObject.ObjectId);
            if (state == null)
            {
                continue;
            }

            BattlePrefabEntry? entry = _prefabLibrary.FindEntry(boardObject.DefinitionId);
            if (entry?.PrefabScene == null)
            {
                continue;
            }

            BattleAnimatedViewBase? view = entry.PrefabScene.Instantiate<BattleAnimatedViewBase>();
            view.Bind(state);
            _pieceRoot.AddChild(view);
            view.SetBoardPosition(room.CellToLocalCenter(state.Cell));
            view.PlayIdle();
            _views[state.ObjectId] = view;
        }
    }

    public void Sync(BoardObjectRegistry registry, BattleObjectStateManager stateManager, BattleRoomTemplate room)
    {
        foreach (BoardObject boardObject in registry.AllObjects)
        {
            if (!_views.TryGetValue(boardObject.ObjectId, out BattleAnimatedViewBase? view))
            {
                continue;
            }

            BattleObjectState? state = stateManager.Get(boardObject.ObjectId);
            if (state == null)
            {
                continue;
            }

            view.SetBoardPosition(room.CellToLocalCenter(state.Cell));
        }
    }

    public void PlayMove(string objectId)
    {
        if (_views.TryGetValue(objectId, out BattleAnimatedViewBase? view))
        {
            view.PlayMove();
        }
    }
}
