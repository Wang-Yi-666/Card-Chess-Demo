using System;
using System.Linq;
using Godot;
using CardChessDemo.Battle.Board;

namespace CardChessDemo.Battle.AI.Strategies;

public sealed class MeleeBasicEnemyAiStrategy : IEnemyAiStrategy
{
    public string AiId => "melee_basic";

    public EnemyAiDecision Decide(EnemyAiContext context)
    {
        BoardObject? nearestOpponent = FindNearestOpponent(context);
        if (nearestOpponent == null)
        {
            return EnemyAiDecision.Wait();
        }

        BoardObject? attackTarget = context.ActionService
            .FindAttackableTargetsInRange(context.Self.ObjectId, context.Self.Cell, context.SelfState.AttackRange)
            .FirstOrDefault();
        if (attackTarget != null)
        {
            return EnemyAiDecision.Attack(attackTarget.ObjectId);
        }

        int currentDistance = GetManhattanDistance(context.Self.Cell, nearestOpponent.Cell);
        Vector2I? nextCell = context.Pathfinder
            .FindReachableCells(context.Self.ObjectId, context.Self.Cell, context.SelfState.MovePointsPerTurn)
            .Where(cell => cell != context.Self.Cell)
            .Select(cell => new
            {
                Cell = cell,
                Distance = GetManhattanDistance(cell, nearestOpponent.Cell),
                Progress = currentDistance - GetManhattanDistance(cell, nearestOpponent.Cell),
            })
            .Where(candidate => candidate.Progress > 0)
            .OrderByDescending(candidate => candidate.Progress)
            .ThenBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.Cell.Y)
            .ThenBy(candidate => candidate.Cell.X)
            .Select(candidate => (Vector2I?)candidate.Cell)
            .FirstOrDefault();

        return nextCell.HasValue
            ? EnemyAiDecision.Move(nextCell.Value)
            : EnemyAiDecision.Wait();
    }

    private static BoardObject? FindNearestOpponent(EnemyAiContext context)
    {
        return context.Registry.AllObjects
            .Where(boardObject => boardObject.ObjectType == BoardObjectType.Unit)
            .Where(boardObject => boardObject.ObjectId != context.Self.ObjectId)
            .Where(boardObject => boardObject.Faction != context.Self.Faction)
            .OrderBy(boardObject => GetManhattanDistance(context.Self.Cell, boardObject.Cell))
            .ThenBy(boardObject => boardObject.ObjectId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static int GetManhattanDistance(Vector2I a, Vector2I b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
    }
}
