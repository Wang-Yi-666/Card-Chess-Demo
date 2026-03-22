using CardChessDemo.Battle.Actions;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.State;

namespace CardChessDemo.Battle.AI;

public sealed class EnemyAiContext
{
    public EnemyAiContext(
        BoardObject self,
        BattleObjectState selfState,
        BoardObjectRegistry registry,
        BattleObjectStateManager stateManager,
        BoardPathfinder pathfinder,
        BoardTargetingService targetingService,
        BattleActionService actionService)
    {
        Self = self;
        SelfState = selfState;
        Registry = registry;
        StateManager = stateManager;
        Pathfinder = pathfinder;
        TargetingService = targetingService;
        ActionService = actionService;
    }

    public BoardObject Self { get; }
    public BattleObjectState SelfState { get; }
    public BoardObjectRegistry Registry { get; }
    public BattleObjectStateManager StateManager { get; }
    public BoardPathfinder Pathfinder { get; }
    public BoardTargetingService TargetingService { get; }
    public BattleActionService ActionService { get; }
}
