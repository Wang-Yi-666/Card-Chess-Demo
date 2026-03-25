namespace CardChessDemo.Battle.AI;

public interface IEnemyAiStrategy
{
    string AiId { get; }

    EnemyAiDecision Decide(EnemyAiContext context);
}
