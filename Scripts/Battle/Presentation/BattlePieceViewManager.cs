using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.Rooms;
using CardChessDemo.Battle.State;

namespace CardChessDemo.Battle.Presentation;

public sealed class BattlePieceViewManager
{
    private readonly Node _pieceRoot;
    private readonly Node _killFxRoot;
    private readonly BattlePrefabLibrary _prefabLibrary;
    private readonly Dictionary<string, BattleAnimatedViewBase> _views = new(StringComparer.Ordinal);
    public BattlePieceViewManager(Node pieceRoot, Node killFxRoot, BattlePrefabLibrary prefabLibrary)
    {
        _pieceRoot = pieceRoot;
        _killFxRoot = killFxRoot;
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

    public async Task<bool> PlayMovePathAsync(
        string objectId,
        IReadOnlyList<Vector2I> cellPath,
        BattleRoomTemplate room,
        double secondsPerCell)
    {
        if (!_views.TryGetValue(objectId, out BattleAnimatedViewBase? view) || cellPath.Count == 0)
        {
            return false;
        }

        view.PlayMove();
        Vector2I previousCell = cellPath[0];
        foreach (Vector2I cell in cellPath.Skip(1))
        {
            view.FaceDirection(cell - previousCell);
            await view.TweenBoardPositionAsync(room.CellToLocalCenter(cell), secondsPerCell);
            previousCell = cell;
        }

        view.PlayIdle();
        return true;
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
            attackerView.FaceDirection(normalizedDirection);
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

    public async Task PlayKillSequenceAsync(
        string objectId,
        Vector2 knockbackDirection,
        float knockbackDistance,
        double knockbackDuration,
        double whiteFlashDuration,
        double shatterDuration)
    {
        if (!_views.TryGetValue(objectId, out BattleAnimatedViewBase? view))
        {
            return;
        }

        Vector2 spriteAnchor = view.CaptureSpriteLocalPosition();

        Node2D killGhost = new()
        {
            Name = $"{view.Name}_KillGhost",
            Position = view.Position + spriteAnchor,
            Scale = view.Scale,
            ZIndex = 999,
        };

        Polygon2D baseSquare = new()
        {
            Polygon = new[]
            {
                new Vector2(-8.0f, -8.0f),
                new Vector2(8.0f, -8.0f),
                new Vector2(8.0f, 8.0f),
                new Vector2(-8.0f, 8.0f),
            },
            Color = Colors.White,
        };
        killGhost.AddChild(baseSquare);

        Vector2[] shardDirections =
        {
            new Vector2(-0.8f, -0.9f),
            new Vector2(0.9f, -0.7f),
            new Vector2(-0.7f, 0.8f),
            new Vector2(0.85f, 0.95f),
        };

        Vector2[] shardOrigins =
        {
            new Vector2(-8.0f, -8.0f),
            new Vector2(0.0f, -8.0f),
            new Vector2(-8.0f, 0.0f),
            new Vector2(0.0f, 0.0f),
        };

        List<Polygon2D> shardSprites = new();
        for (int index = 0; index < 4; index++)
        {
            Polygon2D shard = new()
            {
                Polygon = new[]
                {
                    Vector2.Zero,
                    new Vector2(8.0f, 0.0f),
                    new Vector2(8.0f, 8.0f),
                    new Vector2(0.0f, 8.0f),
                },
                Position = shardOrigins[index],
                Color = new Color(1.0f, 1.0f, 1.0f, 0.0f),
            };
            killGhost.AddChild(shard);
            shardSprites.Add(shard);
        }

        _killFxRoot.AddChild(killGhost);

        _views.Remove(objectId);
        view.QueueFree();

        Vector2 resolvedDirection = knockbackDirection == Vector2.Zero ? Vector2.Right : knockbackDirection.Normalized();
        Tween rootTween = killGhost.CreateTween();
        rootTween.SetEase(Tween.EaseType.Out);
        rootTween.SetTrans(Tween.TransitionType.Cubic);
        rootTween.TweenProperty(killGhost, "position", killGhost.Position + resolvedDirection * knockbackDistance, knockbackDuration);

        double shardDelay = Math.Max(whiteFlashDuration * 0.35d, 0.02d);
        Tween flashTween = killGhost.CreateTween();
        flashTween.SetEase(Tween.EaseType.Out);
        flashTween.SetTrans(Tween.TransitionType.Cubic);
        flashTween.TweenProperty(baseSquare, "color", Colors.White, whiteFlashDuration * 0.45d);
        flashTween.TweenProperty(baseSquare, "color", new Color(1.0f, 1.0f, 1.0f, 0.0f), shatterDuration).SetDelay(shardDelay);

        for (int index = 0; index < shardSprites.Count; index++)
        {
            Polygon2D shard = shardSprites[index];
            Vector2 shardDrift = (resolvedDirection * 0.8f + shardDirections[index]).Normalized() * (knockbackDistance * 0.95f + 6.0f);
            Tween shardTween = killGhost.CreateTween();
            shardTween.SetParallel();
            shardTween.SetEase(Tween.EaseType.Out);
            shardTween.SetTrans(Tween.TransitionType.Cubic);
            shardTween.TweenProperty(shard, "color", Colors.White, 0.01d).SetDelay(shardDelay);
            shardTween.TweenProperty(shard, "position", shard.Position + shardDrift, shatterDuration).SetDelay(shardDelay);
            shardTween.TweenProperty(shard, "rotation_degrees", (index % 2 == 0 ? -1.0f : 1.0f) * (20.0f + index * 12.0f), shatterDuration).SetDelay(shardDelay);
            shardTween.TweenProperty(shard, "color", new Color(1.0f, 1.0f, 1.0f, 0.0f), shatterDuration).SetDelay(shardDelay);
        }

        double totalDuration = Math.Max(knockbackDuration, Math.Max(whiteFlashDuration, shardDelay + shatterDuration));
        await killGhost.ToSignal(killGhost.GetTree().CreateTimer(totalDuration), SceneTreeTimer.SignalName.Timeout);
        killGhost.QueueFree();
    }

    public void PlayCue(string objectId, StringName animationName)
    {
        if (_views.TryGetValue(objectId, out BattleAnimatedViewBase? view))
        {
            view.PlayCue(animationName);
        }
    }

    public void PlayTintPulse(string objectId, Color tintColor)
    {
        if (_views.TryGetValue(objectId, out BattleAnimatedViewBase? view))
        {
            view.PlayTintPulse(tintColor);
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
