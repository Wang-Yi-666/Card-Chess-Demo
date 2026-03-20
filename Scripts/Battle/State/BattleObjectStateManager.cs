using System;
using System.Collections.Generic;
using System.Linq;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.Presentation;
using CardChessDemo.Battle.Shared;

namespace CardChessDemo.Battle.State;

public sealed class BattleObjectStateManager
{
    private readonly Dictionary<string, BattleObjectState> _states = new(StringComparer.Ordinal);
    private readonly BoardObjectRegistry _registry;
    private readonly BattlePrefabLibrary _prefabLibrary;
    private readonly GlobalGameSession _session;

    public BattleObjectStateManager(BoardObjectRegistry registry, BattlePrefabLibrary prefabLibrary, GlobalGameSession session)
    {
        _registry = registry;
        _prefabLibrary = prefabLibrary;
        _session = session;
    }

    public IEnumerable<BattleObjectState> AllStates => _states.Values.OrderBy(state => state.ObjectType).ThenBy(state => state.ObjectId);

    public void Initialize()
    {
        _states.Clear();

        foreach (BoardObject boardObject in _registry.AllObjects)
        {
            BattleObjectState state = CreateState(boardObject);
            _states[state.ObjectId] = state;
        }
    }

    public BattleObjectState? Get(string objectId)
    {
        return _states.GetValueOrDefault(objectId);
    }

    public BattleObjectState? GetPrimaryPlayerState()
    {
        return _states.Values.FirstOrDefault(state => state.IsPlayer);
    }

    public void SyncAllFromRegistry()
    {
        foreach (BoardObject boardObject in _registry.AllObjects)
        {
            if (!_states.TryGetValue(boardObject.ObjectId, out BattleObjectState? state))
            {
                state = CreateState(boardObject);
                _states[state.ObjectId] = state;
            }

            state.Cell = boardObject.Cell;
            state.CurrentHp = boardObject.CurrentHp;
            state.MaxHp = boardObject.MaxHp > 0 ? boardObject.MaxHp : state.MaxHp;
        }

        SyncPlayerFromSession();
    }

    public void SyncPlayerFromSession()
    {
        BattleObjectState? playerState = GetPrimaryPlayerState();
        if (playerState == null)
        {
            return;
        }

        playerState.DisplayName = _session.PlayerDisplayName;
        playerState.MaxHp = _session.PlayerMaxHp;
        playerState.CurrentHp = _session.PlayerCurrentHp;
        playerState.MovePointsPerTurn = _session.PlayerMovePointsPerTurn;
    }

    private BattleObjectState CreateState(BoardObject boardObject)
    {
        BattlePrefabEntry? prefabEntry = _prefabLibrary.FindEntry(boardObject.DefinitionId);
        bool isPlayer = boardObject.HasTag("player");

        BattleObjectState state = new(
            boardObject.ObjectId,
            boardObject.DefinitionId,
            prefabEntry?.DisplayName ?? boardObject.DefinitionId,
            boardObject.ObjectType,
            boardObject.Faction)
        {
            Cell = boardObject.Cell,
            MaxHp = boardObject.MaxHp > 0 ? boardObject.MaxHp : prefabEntry?.DefaultMaxHp ?? 0,
            CurrentHp = boardObject.CurrentHp > 0 ? boardObject.CurrentHp : prefabEntry?.DefaultCurrentHp ?? 0,
            MovePointsPerTurn = prefabEntry?.DefaultMovePointsPerTurn ?? 0,
            IsPlayer = isPlayer,
        };

        if (isPlayer)
        {
            state.DisplayName = _session.PlayerDisplayName;
            state.MaxHp = _session.PlayerMaxHp;
            state.CurrentHp = _session.PlayerCurrentHp;
            state.MovePointsPerTurn = _session.PlayerMovePointsPerTurn;
        }

        return state;
    }
}
