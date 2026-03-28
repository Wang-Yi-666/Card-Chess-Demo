using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
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
    private readonly Node _awaitHost;

    public double PreActionDelaySeconds { get; set; } = 0.08d;
    public double PostActionDelaySeconds { get; set; } = 0.16d;

    public EnemyTurnResolver(
        BoardObjectRegistry registry,
        BattleObjectStateManager stateManager,
        BoardPathfinder pathfinder,
        BoardTargetingService targetingService,
        BattleActionService actionService,
        EnemyAiRegistry aiRegistry,
        Node awaitHost)
    {
        _registry = registry;
        _stateManager = stateManager;
        _pathfinder = pathfinder;
        _targetingService = targetingService;
        _actionService = actionService;
        _aiRegistry = aiRegistry;
        _awaitHost = awaitHost;
    }

    public async Task ResolveTurnAsync()
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
            await WaitSeconds(PreActionDelaySeconds);
            await ExecuteDecisionAsync(enemyId, decision);
            await WaitSeconds(PostActionDelaySeconds);

            if (_actionService.IsPlayerDefeated)
            {
                break;
            }
        }
    }

    private async Task ExecuteDecisionAsync(string enemyId, EnemyAiDecision decision)
    {
        switch (decision.DecisionType)
        {
            case EnemyAiDecisionType.Move:
                await _actionService.TryMoveObjectAsync(enemyId, decision.MoveCell);
                break;

            case EnemyAiDecisionType.Attack:
                await _actionService.TryAttackObjectAsync(enemyId, decision.TargetObjectId);
                break;

            case EnemyAiDecisionType.Spawn:
                if (decision.SpawnDefinition != null)
                {
                    await _actionService.TrySpawnBoardObjectAsync(decision.SpawnDefinition);
                }
                break;
        }
    }

    private async Task WaitSeconds(double seconds)
    {
        if (seconds <= 0.0d)
        {
            return;
        }

        await _awaitHost.ToSignal(_awaitHost.GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }
}
