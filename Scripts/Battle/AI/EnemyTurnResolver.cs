using System;
using System.Linq;
using CardChessDemo.Battle.Actions;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.State;

namespace CardChessDemo.Battle.AI;

public sealed class EnemyTurnResolver
{
    private readonly BoardObjectRegistry _registry;
    private readonly BattleObjectStateManager _stateManager;
    private readonly BoardPathfinder _pathfinder;
    private readonly BoardTargetingService _targetingService;
    private readonly BattleActionService _actionService;
    private readonly EnemyAiRegistry _aiRegistry;

    public EnemyTurnResolver(
        BoardObjectRegistry registry,
        BattleObjectStateManager stateManager,
        BoardPathfinder pathfinder,
        BoardTargetingService targetingService,
        BattleActionService actionService,
        EnemyAiRegistry aiRegistry)
    {
        _registry = registry;
        _stateManager = stateManager;
        _pathfinder = pathfinder;
        _targetingService = targetingService;
        _actionService = actionService;
        _aiRegistry = aiRegistry;
    }

    public void ResolveTurn()
    {
        string[] enemyIds = _registry.AllObjects
            .Where(boardObject => boardObject.ObjectType == BoardObjectType.Unit && boardObject.Faction == BoardObjectFaction.Enemy)
            .Select(boardObject => boardObject.ObjectId)
            .OrderBy(objectId => objectId, StringComparer.Ordinal)
            .ToArray();

        foreach (string enemyId in enemyIds)
        {
            if (!_registry.TryGet(enemyId, out BoardObject? enemyObject) || enemyObject == null)
            {
                continue;
            }

            BattleObjectState? enemyState = _stateManager.Get(enemyId);
            if (enemyState == null)
            {
                continue;
            }

            EnemyAiContext context = new(
                enemyObject,
                enemyState,
                _registry,
                _stateManager,
                _pathfinder,
                _targetingService,
                _actionService);

            IEnemyAiStrategy strategy = _aiRegistry.Resolve(enemyState.AiId);
            EnemyAiDecision decision = strategy.Decide(context);
            ExecuteDecision(enemyId, decision);

            if (_actionService.IsPlayerDefeated)
            {
                break;
            }
        }
    }

    private void ExecuteDecision(string enemyId, EnemyAiDecision decision)
    {
        switch (decision.DecisionType)
        {
            case EnemyAiDecisionType.Move:
                _actionService.TryMoveObject(enemyId, decision.MoveCell, out _);
                break;

            case EnemyAiDecisionType.Attack:
                _actionService.TryAttackObject(enemyId, decision.TargetObjectId, out _);
                break;
        }
    }
}
