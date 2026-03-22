namespace CardChessDemo.Battle.AI.Strategies;

public sealed class WaitEnemyAiStrategy : IEnemyAiStrategy
{
    public string AiId => "wait";

    public EnemyAiDecision Decide(EnemyAiContext context)
    {
        return EnemyAiDecision.Wait();
    }
}
