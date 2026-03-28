using CardChessDemo.Battle.Data;
using Godot;

namespace CardChessDemo.Battle.AI;

public enum EnemyAiDecisionType
{
    Wait = 0,
    Move = 1,
    Attack = 2,
    Spawn = 3,
}

public sealed class EnemyAiDecision
{
    private EnemyAiDecision(EnemyAiDecisionType decisionType, Vector2I moveCell, string targetObjectId, BoardObjectSpawnDefinition? spawnDefinition)
    {
        DecisionType = decisionType;
        MoveCell = moveCell;
        TargetObjectId = targetObjectId;
        SpawnDefinition = spawnDefinition;
    }

    public EnemyAiDecisionType DecisionType { get; }
    public Vector2I MoveCell { get; }
    public string TargetObjectId { get; }
    public BoardObjectSpawnDefinition? SpawnDefinition { get; }

    public static EnemyAiDecision Wait()
    {
        return new EnemyAiDecision(EnemyAiDecisionType.Wait, Vector2I.Zero, string.Empty, null);
    }

    public static EnemyAiDecision Move(Vector2I targetCell)
    {
        return new EnemyAiDecision(EnemyAiDecisionType.Move, targetCell, string.Empty, null);
    }

    public static EnemyAiDecision Attack(string targetObjectId)
    {
        return new EnemyAiDecision(EnemyAiDecisionType.Attack, Vector2I.Zero, targetObjectId, null);
    }

    public static EnemyAiDecision Spawn(BoardObjectSpawnDefinition spawnDefinition)
    {
        return new EnemyAiDecision(EnemyAiDecisionType.Spawn, Vector2I.Zero, string.Empty, spawnDefinition);
    }
}
