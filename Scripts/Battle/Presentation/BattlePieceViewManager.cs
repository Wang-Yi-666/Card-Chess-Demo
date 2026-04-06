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

        Texture2D? capturedTexture = view.CaptureCurrentFrameTexture()
            ?? view.CaptureAnimationFrameTexture("idle", 0);
        if (capturedTexture == null)
        {
            _views.Remove(objectId);
            view.QueueFree();
            return;
        }

        Texture2D snapshotTexture = CreateSnapshotTexture(capturedTexture, view.CaptureSpriteFlipH());
        Texture2D whiteOverlayTexture = CreateWhiteOverlayTexture(snapshotTexture);
        Vector2 textureSize = snapshotTexture.GetSize();
        Vector2 halfSize = textureSize * 0.5f;
        Vector2 spriteAnchor = view.CaptureSpriteLocalPosition();
        bool spriteCentered = view.CaptureSpriteCentered();
        Vector2 spriteTopLeft = spriteCentered ? spriteAnchor - halfSize : spriteAnchor;

        Node2D killGhost = new()
        {
            Name = $"{view.Name}_KillGhost",
            Position = view.Position,
            Scale = view.Scale,
            ZIndex = 999,
        };

        Sprite2D baseSprite = new()
        {
            Texture = snapshotTexture,
            Centered = false,
            Position = spriteTopLeft,
            Modulate = Colors.White,
            ZIndex = 0,
        };
        killGhost.AddChild(baseSprite);

        Sprite2D whiteOverlaySprite = new()
        {
            Texture = whiteOverlayTexture,
            Centered = false,
            Position = spriteTopLeft,
            Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f),
            ZIndex = 1,
        };
        killGhost.AddChild(whiteOverlaySprite);

        Vector2[] shardDirections =
        {
            new Vector2(-0.85f, -0.95f),
            new Vector2(0.95f, -0.75f),
            new Vector2(-0.75f, 0.95f),
            new Vector2(0.9f, 1.0f),
        };

        List<Sprite2D> shardSprites = new();
        for (int index = 0; index < 4; index++)
        {
            Vector2 regionPosition = new(
                index % 2 == 0 ? 0.0f : halfSize.X,
                index < 2 ? 0.0f : halfSize.Y);
            AtlasTexture shardTexture = new()
            {
                Atlas = snapshotTexture,
                Region = new Rect2(regionPosition, halfSize),
            };

            Sprite2D shard = new()
            {
                Texture = shardTexture,
                Centered = false,
                Position = spriteTopLeft + regionPosition,
                Modulate = Colors.White,
            };
            killGhost.AddChild(shard);
            shardSprites.Add(shard);
        }

        _killFxRoot.AddChild(killGhost);

        _views.Remove(objectId);
        view.QueueFree();

        Vector2 knockDirection = knockbackDirection == Vector2.Zero ? Vector2.Right : knockbackDirection.Normalized();
        Tween rootTween = killGhost.CreateTween();
        rootTween.SetParallel();
        rootTween.SetEase(Tween.EaseType.Out);
        rootTween.SetTrans(Tween.TransitionType.Cubic);
        rootTween.TweenProperty(killGhost, "position", killGhost.Position + knockDirection * knockbackDistance, knockbackDuration);
        rootTween.TweenProperty(whiteOverlaySprite, "modulate", Colors.White, knockbackDuration);
        await killGhost.ToSignal(rootTween, Tween.SignalName.Finished);

        await killGhost.ToSignal(killGhost.GetTree().CreateTimer(0.18d), SceneTreeTimer.SignalName.Timeout);

        baseSprite.Visible = false;
        whiteOverlaySprite.Visible = false;

        for (int index = 0; index < shardSprites.Count; index++)
        {
            Sprite2D shard = shardSprites[index];
            Vector2 shardDrift = (knockDirection * 0.28f + shardDirections[index]).Normalized() * (Mathf.Max(textureSize.X, textureSize.Y) * 0.7f + 8.0f);
            Tween shardTween = killGhost.CreateTween();
            shardTween.SetParallel();
            shardTween.SetEase(Tween.EaseType.Out);
            shardTween.SetTrans(Tween.TransitionType.Cubic);
            shardTween.TweenProperty(shard, "position", shard.Position + shardDrift, shatterDuration);
            shardTween.TweenProperty(shard, "rotation_degrees", (index % 2 == 0 ? -1.0f : 1.0f) * (28.0f + index * 16.0f), shatterDuration);
            shardTween.TweenProperty(shard, "modulate", new Color(1.0f, 1.0f, 1.0f, 0.0f), shatterDuration);
        }

        double totalDuration = knockbackDuration + 0.18d + shatterDuration;
        await killGhost.ToSignal(killGhost.GetTree().CreateTimer(totalDuration), SceneTreeTimer.SignalName.Timeout);
        killGhost.QueueFree();
    }

    public async Task PlayObstacleBreakSequenceAsync(
        string objectId,
        double whiteFlashDuration,
        double shatterDuration)
    {
        if (!_views.TryGetValue(objectId, out BattleAnimatedViewBase? view))
        {
            return;
        }

        Texture2D? capturedTexture = view.CaptureCurrentFrameTexture()
            ?? view.CaptureAnimationFrameTexture("idle", 0);
        if (capturedTexture == null)
        {
            _views.Remove(objectId);
            view.QueueFree();
            return;
        }

        Texture2D snapshotTexture = CreateSnapshotTexture(capturedTexture, view.CaptureSpriteFlipH());
        Vector2 textureSize = snapshotTexture.GetSize();
        Vector2 halfSize = textureSize * 0.5f;
        Vector2 spriteAnchor = view.CaptureSpriteLocalPosition();
        bool spriteCentered = view.CaptureSpriteCentered();
        Vector2 spriteTopLeft = spriteCentered ? spriteAnchor - halfSize : spriteAnchor;

        Node2D breakGhost = new()
        {
            Name = $"{view.Name}_ObstacleBreakGhost",
            Position = view.Position,
            Scale = view.Scale,
            ZIndex = 999,
        };

        Sprite2D baseSprite = new()
        {
            Texture = snapshotTexture,
            Centered = false,
            Position = spriteTopLeft,
            Modulate = Colors.White,
        };
        breakGhost.AddChild(baseSprite);

        Vector2[] shardDirections =
        {
            new Vector2(-1.0f, -0.65f),
            new Vector2(1.0f, -0.55f),
            new Vector2(-0.8f, 0.95f),
            new Vector2(0.85f, 1.0f),
        };

        List<Sprite2D> shardSprites = new();
        for (int index = 0; index < 4; index++)
        {
            Vector2 regionPosition = new(
                index % 2 == 0 ? 0.0f : halfSize.X,
                index < 2 ? 0.0f : halfSize.Y);
            AtlasTexture shardTexture = new()
            {
                Atlas = snapshotTexture,
                Region = new Rect2(regionPosition, halfSize),
            };

            Sprite2D shard = new()
            {
                Texture = shardTexture,
                Centered = false,
                Position = spriteTopLeft + regionPosition,
                Modulate = Colors.White,
            };
            breakGhost.AddChild(shard);
            shardSprites.Add(shard);
        }

        _killFxRoot.AddChild(breakGhost);

        _views.Remove(objectId);
        view.QueueFree();

        await breakGhost.ToSignal(breakGhost.GetTree().CreateTimer(Math.Max(whiteFlashDuration, 0.16d)), SceneTreeTimer.SignalName.Timeout);

        baseSprite.Visible = false;

        for (int index = 0; index < shardSprites.Count; index++)
        {
            Sprite2D shard = shardSprites[index];
            Vector2 shardDrift = shardDirections[index].Normalized() * (Mathf.Max(textureSize.X, textureSize.Y) * 0.75f + 6.0f);
            Tween shardTween = breakGhost.CreateTween();
            shardTween.SetParallel();
            shardTween.SetEase(Tween.EaseType.Out);
            shardTween.SetTrans(Tween.TransitionType.Cubic);
            shardTween.TweenProperty(shard, "position", shard.Position + shardDrift, shatterDuration);
            shardTween.TweenProperty(shard, "rotation_degrees", (index % 2 == 0 ? -1.0f : 1.0f) * (24.0f + index * 14.0f), shatterDuration);
            shardTween.TweenProperty(shard, "modulate", new Color(1.0f, 1.0f, 1.0f, 0.0f), shatterDuration);
        }

        double totalDuration = Math.Max(whiteFlashDuration, 0.16d) + shatterDuration;
        await breakGhost.ToSignal(breakGhost.GetTree().CreateTimer(totalDuration), SceneTreeTimer.SignalName.Timeout);
        breakGhost.QueueFree();
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

    private static Texture2D CreateSnapshotTexture(Texture2D sourceTexture, bool flipHorizontally)
    {
        Image image = sourceTexture.GetImage();
        if (image == null || image.IsEmpty())
        {
            return sourceTexture;
        }

        if (flipHorizontally)
        {
            image.FlipX();
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static Texture2D CreateWhiteOverlayTexture(Texture2D sourceTexture)
    {
        Image image = sourceTexture.GetImage();
        if (image == null || image.IsEmpty())
        {
            return sourceTexture;
        }

        for (int y = 0; y < image.GetHeight(); y++)
        {
            for (int x = 0; x < image.GetWidth(); x++)
            {
                Color source = image.GetPixel(x, y);
                image.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, source.A));
            }
        }

        return ImageTexture.CreateFromImage(image);
    }
}
