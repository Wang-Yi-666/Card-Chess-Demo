using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.Presentation;
using CardChessDemo.Battle.Rooms;
using CardChessDemo.Battle.Shared;
using CardChessDemo.Battle.State;

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

    public BattleActionService(
        BoardState boardState,
        BoardObjectRegistry registry,
        BoardQueryService queryService,
        BattleObjectStateManager stateManager,
        BattlePieceViewManager pieceViewManager,
        BattleRoomTemplate room,
        GlobalGameSession session)
    {
        _boardState = boardState;
        _registry = registry;
        _queryService = queryService;
        _stateManager = stateManager;
        _pieceViewManager = pieceViewManager;
        _room = room;
        _session = session;
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

        _pieceViewManager.PlayAction(attackerId);
        bool isPlayerTarget = target.HasTag("player");
        target.ApplyDamage(attackerState.AttackDamage);

        if (isPlayerTarget)
        {
            _session.SetPlayerCurrentHp(target.CurrentHp);
        }

        if (target.IsDestroyed)
        {
            if (!isPlayerTarget)
            {
                _boardState.RemoveObject(target);
                _registry.Remove(target.ObjectId);
            }
        }
        else
        {
            _pieceViewManager.PlayHit(targetId);
        }

        SyncPresentation();
        return true;
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

    private static int GetManhattanDistance(Vector2I a, Vector2I b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
    }
}
