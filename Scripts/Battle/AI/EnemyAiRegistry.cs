using System;
using System.Collections.Generic;
using CardChessDemo.Battle.AI.Strategies;

namespace CardChessDemo.Battle.AI;

public sealed class EnemyAiRegistry
{
    private readonly Dictionary<string, IEnemyAiStrategy> _strategies = new(StringComparer.Ordinal);
    private readonly IEnemyAiStrategy _fallbackStrategy = new WaitEnemyAiStrategy();

    public EnemyAiRegistry()
    {
        Register(_fallbackStrategy);
        Register(new MeleeBasicEnemyAiStrategy());
    }

    public void Register(IEnemyAiStrategy strategy)
    {
        _strategies[strategy.AiId] = strategy;
    }

    public IEnemyAiStrategy Resolve(string aiId)
    {
        if (string.IsNullOrWhiteSpace(aiId))
        {
            return _fallbackStrategy;
        }

        return _strategies.GetValueOrDefault(aiId, _fallbackStrategy);
    }
}
