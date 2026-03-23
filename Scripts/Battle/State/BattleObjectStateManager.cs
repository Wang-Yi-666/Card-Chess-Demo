using System;
using System.Collections.Generic;
using System.Linq;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.Presentation;
using CardChessDemo.Battle.Shared;

namespace CardChessDemo.Battle.State;

// 把 board 层对象整理成更适合 HUD / prefab 读取的只读快照。
// 当前状态仍然比较轻量，还没有 buff、行动点、敌人意图等完整战斗字段。
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
            ApplyRuntimeCombatData(boardObject);
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
        // registry 是逻辑真源，表现层状态每帧向它靠拢。
        HashSet<string> activeObjectIds = _registry.AllObjects
            .Select(boardObject => boardObject.ObjectId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string staleObjectId in _states.Keys.Where(objectId => !activeObjectIds.Contains(objectId)).ToArray())
        {
            _states.Remove(staleObjectId);
        }

        foreach (BoardObject boardObject in _registry.AllObjects)
        {
            if (!_states.TryGetValue(boardObject.ObjectId, out BattleObjectState? state))
            {
                ApplyRuntimeCombatData(boardObject);
                state = CreateState(boardObject);
                _states[state.ObjectId] = state;
            }

            ApplyRuntimeCombatData(boardObject);
            BattlePrefabEntry? prefabEntry = _prefabLibrary.FindEntry(boardObject.DefinitionId);
            state.Cell = boardObject.Cell;
            state.MaxHp = boardObject.MaxHp > 0 ? boardObject.MaxHp : state.MaxHp;
            state.MaxShield = boardObject.MaxShield;
            state.CurrentHp = ResolveCurrentHp(boardObject, prefabEntry);
            state.CurrentShield = ResolveCurrentShield(boardObject);
            state.HasDefenseStance = boardObject.HasDefenseStance;
            state.DefenseDamageReductionPercent = boardObject.DefenseDamageReductionPercent;
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

        // 玩家单位的显示名、生命和移动力以全局 session 为准，
        // 这样 HUD 调试改动能立即反馈到战斗表现。
        playerState.DisplayName = _session.PlayerDisplayName;
        playerState.MaxHp = _session.PlayerMaxHp;
        playerState.CurrentHp = _session.PlayerCurrentHp;
        playerState.MovePointsPerTurn = _session.PlayerMovePointsPerTurn;
        playerState.AttackRange = _session.PlayerAttackRange;
        playerState.AttackDamage = _session.PlayerAttackDamage;
    }

    private BattleObjectState CreateState(BoardObject boardObject)
    {
        BattlePrefabEntry? prefabEntry = _prefabLibrary.FindEntry(boardObject.DefinitionId);
        bool isPlayer = boardObject.HasTag("player");

        BattleObjectState state = new(
            boardObject.ObjectId,
            boardObject.DefinitionId,
            boardObject.AiId,
            prefabEntry?.DisplayName ?? boardObject.DefinitionId,
            boardObject.ObjectType,
            boardObject.Faction)
        {
            Cell = boardObject.Cell,
            MaxHp = boardObject.MaxHp > 0 ? boardObject.MaxHp : prefabEntry?.DefaultMaxHp ?? 0,
            MaxShield = boardObject.MaxShield,
            CurrentHp = ResolveCurrentHp(boardObject, prefabEntry),
            CurrentShield = ResolveCurrentShield(boardObject),
            HasDefenseStance = boardObject.HasDefenseStance,
            DefenseDamageReductionPercent = boardObject.DefenseDamageReductionPercent,
            MovePointsPerTurn = prefabEntry?.DefaultMovePointsPerTurn ?? 0,
            AttackRange = prefabEntry?.DefaultAttackRange ?? 1,
            AttackDamage = prefabEntry?.DefaultAttackDamage ?? 1,
            IsPlayer = isPlayer,
        };

        if (isPlayer)
        {
            // 玩家是当前唯一会被外部全局状态覆盖的对象。
            // 敌人和障碍物暂时没有独立持久化来源。
            state.DisplayName = _session.PlayerDisplayName;
            state.MaxHp = _session.PlayerMaxHp;
            state.CurrentHp = _session.PlayerCurrentHp;
            state.MovePointsPerTurn = _session.PlayerMovePointsPerTurn;
            state.AttackRange = _session.PlayerAttackRange;
            state.AttackDamage = _session.PlayerAttackDamage;
        }

        return state;
    }

    private void ApplyRuntimeCombatData(BoardObject boardObject)
    {
        BattlePrefabEntry? prefabEntry = _prefabLibrary.FindEntry(boardObject.DefinitionId);
        bool isPlayer = boardObject.HasTag("player");

        if (isPlayer)
        {
            boardObject.SyncCombatStats(
                _session.PlayerMaxHp,
                _session.PlayerCurrentHp,
                boardObject.MaxShield,
                boardObject.CurrentShield);
            return;
        }

        int resolvedMaxHp = boardObject.MaxHp > 0 ? boardObject.MaxHp : prefabEntry?.DefaultMaxHp ?? 0;
        int resolvedCurrentHp = boardObject.CurrentHp > 0
            ? boardObject.CurrentHp
            : prefabEntry?.DefaultCurrentHp > 0
                ? prefabEntry.DefaultCurrentHp
                : resolvedMaxHp;
        int resolvedMaxShield = boardObject.MaxShield;
        int resolvedCurrentShield = boardObject.CurrentShield;

        boardObject.ApplyCombatDefaults(resolvedMaxHp, resolvedCurrentHp, resolvedMaxShield, resolvedCurrentShield);
    }

    private static int ResolveCurrentHp(BoardObject boardObject, BattlePrefabEntry? prefabEntry)
    {
        if (boardObject.CurrentHp > 0)
        {
            return boardObject.CurrentHp;
        }

        if (boardObject.MaxHp > 0)
        {
            return boardObject.MaxHp;
        }

        if (prefabEntry?.DefaultCurrentHp > 0)
        {
            return prefabEntry.DefaultCurrentHp;
        }

        return prefabEntry?.DefaultMaxHp ?? 0;
    }

    private static int ResolveCurrentShield(BoardObject boardObject)
    {
        return boardObject.CurrentShield;
    }
}
