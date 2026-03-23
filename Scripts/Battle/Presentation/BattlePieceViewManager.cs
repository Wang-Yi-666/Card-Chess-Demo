using System;
using System.Collections.Generic;
using System.Linq;
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
        // 进入房间时直接整表重建，比维护复杂 diff 更适合当前原型阶段。
        foreach (Node child in _pieceRoot.GetChildren())
        {
            child.QueueFree();
        }

        _views.Clear();

        foreach (BoardObject boardObject in registry.AllObjects)
        {
            CreateView(boardObject, stateManager, room);
        }
    }

    public void Sync(BoardObjectRegistry registry, BattleObjectStateManager stateManager, BattleRoomTemplate room)
    {
        HashSet<string> activeObjectIds = registry.AllObjects
            .Select(boardObject => boardObject.ObjectId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string staleObjectId in _views.Keys.Where(objectId => !activeObjectIds.Contains(objectId)).ToArray())
        {
            if (_views.Remove(staleObjectId, out BattleAnimatedViewBase? staleView))
            {
                staleView.QueueFree();
            }
        }

        foreach (BoardObject boardObject in registry.AllObjects)
        {
            if (!_views.TryGetValue(boardObject.ObjectId, out BattleAnimatedViewBase? view))
            {
                view = CreateView(boardObject, stateManager, room);
                if (view == null)
                {
                    continue;
                }
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
            // 当前移动表现只有本地动画切换，没有位移补间或行动时序。
            view.PlayMove();
        }
    }

    public void PlayAction(string objectId)
    {
        if (_views.TryGetValue(objectId, out BattleAnimatedViewBase? view))
        {
            view.PlayAction();
        }
    }

    public void PlayHit(string objectId)
    {
        if (_views.TryGetValue(objectId, out BattleAnimatedViewBase? view))
        {
            view.PlayHit();
        }
    }

    public void PlayAttackExchange(string attackerId, Vector2 directionToTarget, string targetId)
    {
        Vector2 normalizedDirection = directionToTarget == Vector2.Zero ? Vector2.Zero : directionToTarget.Normalized();

        if (_views.TryGetValue(attackerId, out BattleAnimatedViewBase? attackerView))
        {
            attackerView.PlayAction();
            attackerView.PlayMotionOffset(normalizedDirection * 3.0f, 0.05d, 0.10d);
        }

        if (_views.TryGetValue(targetId, out BattleAnimatedViewBase? targetView))
        {
            targetView.PlayHit();
            targetView.PlayMotionOffset(-normalizedDirection * 4.0f, 0.04d, 0.12d, 0.01d);
        }
    }

    public void PlayDefeat(string objectId)
    {
        if (_views.TryGetValue(objectId, out BattleAnimatedViewBase? view))
        {
            view.PlayDefeat();
        }
    }

    public void PlayCue(string objectId, StringName animationName)
    {
        if (_views.TryGetValue(objectId, out BattleAnimatedViewBase? view))
        {
            view.PlayCue(animationName);
        }
    }

    private BattleAnimatedViewBase? CreateView(BoardObject boardObject, BattleObjectStateManager stateManager, BattleRoomTemplate room)
    {
        BattleObjectState? state = stateManager.Get(boardObject.ObjectId);
        if (state == null)
        {
            return null;
        }

        BattlePrefabEntry? entry = _prefabLibrary.FindEntry(boardObject.DefinitionId);
        if (entry?.PrefabScene == null)
        {
            return null;
        }

        BattleAnimatedViewBase? view = entry.PrefabScene.Instantiate<BattleAnimatedViewBase>();
        view.Bind(state);
        _pieceRoot.AddChild(view);
        view.SetBoardPosition(room.CellToLocalCenter(state.Cell));
        view.PlayIdle();
        _views[state.ObjectId] = view;
        return view;
    }
}
