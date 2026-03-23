using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.Presentation;
using CardChessDemo.Battle.Rooms;
using CardChessDemo.Battle.Shared;
using CardChessDemo.Battle.State;
using CardChessDemo.Battle.Visual;

namespace CardChessDemo.Battle.Actions;

public sealed class BattleActionService
{
    private readonly BoardState _boardState;
    private readonly BoardObjectRegistry _registry;
    private readonly BoardQueryService _queryService;
    private readonly BattleObjectStateManager _stateManager;
    private readonly BattlePieceViewManager _pieceViewManager;
    private readonly BattleRoomTemplate _room;
    private readonly GlobalGameSession _session;
    private readonly BattleFloatingTextLayer? _floatingTextLayer;
    private readonly SceneTree _sceneTree;

    public const double MovePresentationDurationSeconds = 0.16d;
    public const double AttackPresentationDurationSeconds = 0.24d;
    public const double ImpactPresentationDurationSeconds = 0.18d;
    public const double DefensePresentationDurationSeconds = 0.22d;

    public BattleActionService(
        BoardState boardState,
        BoardObjectRegistry registry,
        BoardQueryService queryService,
        BattleObjectStateManager stateManager,
        BattlePieceViewManager pieceViewManager,
        BattleRoomTemplate room,
        GlobalGameSession session,
        BattleFloatingTextLayer? floatingTextLayer = null,
        SceneTree? sceneTree = null)
    {
        _boardState = boardState;
        _registry = registry;
        _queryService = queryService;
        _stateManager = stateManager;
        _pieceViewManager = pieceViewManager;
        _room = room;
        _session = session;
        _floatingTextLayer = floatingTextLayer;
        _sceneTree = sceneTree ?? room.GetTree();
    }

    public bool IsPlayerDefeated => _session.PlayerCurrentHp <= 0;

    public bool TryMoveObject(string objectId, Vector2I targetCell, out string failureReason)
    {
        failureReason = string.Empty;

        bool moved = _queryService.TryMoveObject(objectId, targetCell, out failureReason);
        if (!moved)
        {
            return false;
        }

        SyncPresentation();
        _pieceViewManager.PlayMove(objectId);
        return true;
    }

    public async Task<bool> TryMoveObjectAsync(string objectId, Vector2I targetCell)
    {
        bool moved = TryMoveObject(objectId, targetCell, out _);
        if (!moved)
        {
            return false;
        }

        await WaitSeconds(MovePresentationDurationSeconds);
        return true;
    }

    public bool TryAttackObject(string attackerId, string targetId, out string failureReason)
    {
        failureReason = string.Empty;

        if (!_registry.TryGet(attackerId, out BoardObject? attacker) || attacker == null)
        {
            failureReason = $"Attacker {attackerId} was not found.";
            return false;
        }

        if (!_registry.TryGet(targetId, out BoardObject? target) || target == null)
        {
            failureReason = $"Target {targetId} was not found.";
            return false;
        }

        BattleObjectState? attackerState = _stateManager.Get(attackerId);
        if (attackerState == null)
        {
            failureReason = $"Attacker state {attackerId} was not found.";
            return false;
        }

        if (!CanAttack(attacker, target, attackerState.AttackRange, out failureReason))
        {
            return false;
        }

        PlayAttackPresentation(attacker, target);
        ApplyDamageToTarget(target, attackerState.AttackDamage);
        SyncPresentation();
        return true;
    }

    public async Task<bool> TryAttackObjectAsync(string attackerId, string targetId)
    {
        bool attacked = TryAttackObject(attackerId, targetId, out _);
        if (!attacked)
        {
            return false;
        }

        await WaitSeconds(Math.Max(AttackPresentationDurationSeconds, ImpactPresentationDurationSeconds));
        return true;
    }

    public DamageApplicationResult ApplyDamageToTarget(string targetId, int amount, out bool wasDestroyed, out string failureReason)
    {
        failureReason = string.Empty;
        wasDestroyed = false;

        if (!_registry.TryGet(targetId, out BoardObject? target) || target == null)
        {
            failureReason = $"Target {targetId} was not found.";
            return new DamageApplicationResult();
        }

        DamageApplicationResult result = ApplyDamageToTarget(target, amount);
        wasDestroyed = target.IsDestroyed;
        return result;
    }

    public DamageApplicationResult ApplyShieldGainToTarget(string targetId, int amount, out string failureReason)
    {
        failureReason = string.Empty;

        if (!_registry.TryGet(targetId, out BoardObject? target) || target == null)
        {
            failureReason = $"Target {targetId} was not found.";
            return new DamageApplicationResult();
        }

        DamageApplicationResult result = target.GainShield(amount);
        OnNonDamageImpactsApplied(target, result);
        SyncPresentation();
        return result;
    }

    public DamageApplicationResult ApplyHealingToTarget(string targetId, int amount, out string failureReason)
    {
        failureReason = string.Empty;

        if (!_registry.TryGet(targetId, out BoardObject? target) || target == null)
        {
            failureReason = $"Target {targetId} was not found.";
            return new DamageApplicationResult();
        }

        DamageApplicationResult result = target.RestoreHealth(amount);
        OnNonDamageImpactsApplied(target, result);
        SyncPresentation();
        return result;
    }

    public DamageApplicationResult ApplyDefenseAction(string objectId, DefenseActionDefinition definition, int currentTurnIndex, out string failureReason)
    {
        failureReason = string.Empty;

        if (!_registry.TryGet(objectId, out BoardObject? target) || target == null)
        {
            failureReason = $"Target {objectId} was not found.";
            return new DamageApplicationResult();
        }

        DamageApplicationResult result = target.EnterDefenseStance(currentTurnIndex, definition.DamageReductionPercent, definition.ShieldGain);
        _pieceViewManager.PlayCue(objectId, "defend");
        OnNonDamageImpactsApplied(target, result);
        SyncPresentation();
        return result;
    }

    public async Task ApplyDefenseActionAsync(string objectId, DefenseActionDefinition definition, int currentTurnIndex)
    {
        ApplyDefenseAction(objectId, definition, currentTurnIndex, out _);
        await WaitSeconds(DefensePresentationDurationSeconds);
    }

    public void ResolveTurnStart(BoardObjectFaction activeFaction, int activeTurnIndex)
    {
        foreach (BoardObject boardObject in _registry.AllObjects)
        {
            if (boardObject.Faction == activeFaction)
            {
                boardObject.ResolveTurnStart(activeFaction, activeTurnIndex);
            }
        }

        SyncPresentation();
    }

    public BoardObject? GetAttackableObjectAtCell(string sourceObjectId, Vector2I targetCell)
    {
        if (!_registry.TryGet(sourceObjectId, out BoardObject? sourceObject) || sourceObject == null)
        {
            return null;
        }

        foreach (BoardObject boardObject in _queryService.GetObjectsAtCell(targetCell))
        {
            if (boardObject.ObjectId == sourceObjectId)
            {
                continue;
            }

            if (IsAttackable(sourceObject, boardObject))
            {
                return boardObject;
            }
        }

        return null;
    }

    public IReadOnlyList<BoardObject> FindAttackableTargetsInRange(string attackerId, Vector2I origin, int attackRange)
    {
        if (!_registry.TryGet(attackerId, out BoardObject? attacker) || attacker == null)
        {
            return Array.Empty<BoardObject>();
        }

        return _registry.AllObjects
            .Where(boardObject => boardObject.ObjectId != attackerId && IsAttackable(attacker, boardObject))
            .Where(boardObject => GetManhattanDistance(origin, boardObject.Cell) <= attackRange)
            .OrderBy(boardObject => GetManhattanDistance(origin, boardObject.Cell))
            .ThenBy(boardObject => boardObject.ObjectId, StringComparer.Ordinal)
            .ToArray();
    }

    public bool CanAttack(string attackerId, string targetId, out string failureReason)
    {
        failureReason = string.Empty;

        if (!_registry.TryGet(attackerId, out BoardObject? attacker) || attacker == null)
        {
            failureReason = $"Attacker {attackerId} was not found.";
            return false;
        }

        if (!_registry.TryGet(targetId, out BoardObject? target) || target == null)
        {
            failureReason = $"Target {targetId} was not found.";
            return false;
        }

        BattleObjectState? attackerState = _stateManager.Get(attackerId);
        if (attackerState == null)
        {
            failureReason = $"Attacker state {attackerId} was not found.";
            return false;
        }

        return CanAttack(attacker, target, attackerState.AttackRange, out failureReason);
    }

    public static bool IsAttackable(BoardObject sourceObject, BoardObject targetObject)
    {
        if (targetObject.ObjectType == BoardObjectType.Unit)
        {
            return sourceObject.Faction != targetObject.Faction;
        }

        if (targetObject.ObjectType == BoardObjectType.Obstacle)
        {
            return targetObject.HasTag("destructible");
        }

        return false;
    }

    private bool CanAttack(BoardObject attacker, BoardObject target, int attackRange, out string failureReason)
    {
        failureReason = string.Empty;

        if (!IsAttackable(attacker, target))
        {
            failureReason = "This target cannot be attacked.";
            return false;
        }

        int distance = GetManhattanDistance(attacker.Cell, target.Cell);
        if (distance > attackRange)
        {
            failureReason = $"Target is out of range. Range={attackRange}, distance={distance}.";
            return false;
        }

        return true;
    }

    private void SyncPresentation()
    {
        _stateManager.SyncAllFromRegistry();
        _pieceViewManager.Sync(_registry, _stateManager, _room);
    }

    private DamageApplicationResult ApplyDamageToTarget(BoardObject target, int amount)
    {
        bool isPlayerTarget = target.HasTag("player");
        DamageApplicationResult result = target.ApplyDamage(amount);

        ShowImpacts(target, result);

        if (isPlayerTarget)
        {
            _session.SetPlayerCurrentHp(target.CurrentHp);
        }

        if (target.IsDestroyed)
        {
            if (!isPlayerTarget)
            {
                _pieceViewManager.PlayDefeat(target.ObjectId);
                _boardState.RemoveObject(target);
                _registry.Remove(target.ObjectId);
            }
        }
        else if (result.HasAnyImpact)
        {
            _pieceViewManager.PlayHit(target.ObjectId);
        }

        return result;
    }

    private void OnNonDamageImpactsApplied(BoardObject target, DamageApplicationResult result)
    {
        if (!result.HasAnyImpact)
        {
            return;
        }

        if (target.HasTag("player"))
        {
            _session.SetPlayerCurrentHp(target.CurrentHp);
        }

        ShowImpacts(target, result);
    }

    private void ShowImpacts(BoardObject target, DamageApplicationResult result)
    {
        if (!result.HasAnyImpact)
        {
            return;
        }

        _floatingTextLayer?.ShowImpacts(
            target.ObjectId,
            _room.CellToLocalCenter(target.Cell) + new Vector2(0.0f, -6.0f),
            result.Impacts);
    }

    private void PlayAttackPresentation(BoardObject attacker, BoardObject target)
    {
        Vector2 direction = new(target.Cell.X - attacker.Cell.X, target.Cell.Y - attacker.Cell.Y);
        _pieceViewManager.PlayAttackExchange(attacker.ObjectId, direction, target.ObjectId);
    }

    private async Task WaitSeconds(double seconds)
    {
        if (seconds <= 0.0d)
        {
            return;
        }

        await _room.ToSignal(_sceneTree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    private static int GetManhattanDistance(Vector2I a, Vector2I b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
    }
}
