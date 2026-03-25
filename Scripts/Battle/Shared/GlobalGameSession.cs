using System;
using Godot;
using CardChessDemo.Battle.Boundary;

namespace CardChessDemo.Battle.Shared;

// 当前全局状态只保存玩家最小运行时信息。
// 它还不是完整存档系统，也不承载地图进度、牌组或战斗结果。
public partial class GlobalGameSession : Node
{
	[Signal] public delegate void PlayerRuntimeChangedEventHandler();

	[Export] public string PlayerDisplayName { get; set; } = "Traveler";
	[Export] public int PlayerMaxHp { get; set; } = 12;
	[Export] public int PlayerCurrentHp { get; set; } = 12;
	[Export] public int PlayerMovePointsPerTurn { get; set; } = 4;
	[Export] public int PlayerAttackRange { get; set; } = 1;
	[Export] public int PlayerAttackDamage { get; set; } = 2;

	public BattleRequest? PendingBattleRequest { get; private set; }
	public BattleResult? LastBattleResult { get; private set; }

	public void SetPlayerCurrentHp(int value)
	{
		PlayerCurrentHp = Mathf.Clamp(value, 0, Math.Max(PlayerMaxHp, 0));
		EmitSignal(SignalName.PlayerRuntimeChanged);
	}

	public void SetPlayerMovePointsPerTurn(int value)
	{
		PlayerMovePointsPerTurn = Mathf.Max(0, value);
		EmitSignal(SignalName.PlayerRuntimeChanged);
	}

	public void ApplyMovePointDelta(int delta)
	{
		SetPlayerMovePointsPerTurn(PlayerMovePointsPerTurn + delta);
	}

	public void BeginBattle(BattleRequest? request = null)
	{
		PendingBattleRequest = request ?? BattleRequest.FromSession(this);
		LastBattleResult = null;
	}

	public BattleRequest? ConsumePendingBattleRequest()
	{
		BattleRequest? request = PendingBattleRequest;
		PendingBattleRequest = null;
		return request;
	}

	public void CompleteBattle(BattleResult result)
	{
		LastBattleResult = result;
		result.ApplyToSession(this);
	}

	public BattleResult? ConsumeLastBattleResult()
	{
		BattleResult? result = LastBattleResult;
		LastBattleResult = null;
		return result;
	}

	public Godot.Collections.Dictionary BuildPlayerSnapshot()
	{
		// 这里保留 Dictionary 形式，方便当前原型期快速序列化/回灌。
		return new Godot.Collections.Dictionary
		{
			["display_name"] = PlayerDisplayName,
			["max_hp"] = PlayerMaxHp,
			["current_hp"] = PlayerCurrentHp,
			["move_points_per_turn"] = PlayerMovePointsPerTurn,
			["attack_range"] = PlayerAttackRange,
			["attack_damage"] = PlayerAttackDamage,
		};
	}

	public void ApplyPlayerSnapshot(Godot.Collections.Dictionary snapshot)
	{
		// 当前只恢复玩家基础字段；更复杂的战斗上下文尚未接入。
		if (snapshot.TryGetValue("display_name", out Variant displayName))
		{
			PlayerDisplayName = displayName.AsString();
		}

		if (snapshot.TryGetValue("max_hp", out Variant maxHp))
		{
			PlayerMaxHp = maxHp.AsInt32();
		}

		if (snapshot.TryGetValue("current_hp", out Variant currentHp))
		{
			PlayerCurrentHp = currentHp.AsInt32();
		}

		if (snapshot.TryGetValue("move_points_per_turn", out Variant movePoints))
		{
			PlayerMovePointsPerTurn = movePoints.AsInt32();
		}

		if (snapshot.TryGetValue("attack_range", out Variant attackRange))
		{
			PlayerAttackRange = attackRange.AsInt32();
		}

		if (snapshot.TryGetValue("attack_damage", out Variant attackDamage))
		{
			PlayerAttackDamage = attackDamage.AsInt32();
		}

		EmitSignal(SignalName.PlayerRuntimeChanged);
	}

	public void SetPendingEncounterContext(string encounterId, string returnScenePath, Vector2 returnPlayerPosition)
	{
		PendingEncounterId = encounterId ?? string.Empty;
		PendingReturnScenePath = returnScenePath ?? string.Empty;
		PendingReturnPlayerPosition = returnPlayerPosition;
	}

	public bool TryConsumePendingEncounterContext(out string encounterId, out string returnScenePath, out Vector2 returnPlayerPosition)
	{
		encounterId = PendingEncounterId;
		returnScenePath = PendingReturnScenePath;
		returnPlayerPosition = PendingReturnPlayerPosition;

		if (string.IsNullOrWhiteSpace(encounterId))
		{
			return false;
		}

		PendingEncounterId = string.Empty;
		PendingReturnScenePath = string.Empty;
		PendingReturnPlayerPosition = Vector2.Zero;
		return true;
	}
}
