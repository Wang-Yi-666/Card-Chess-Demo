using Godot;
using System;

public partial class BattleQuickExitController : Node2D
{
	private GlobalBattleContext _battleContext;
	private GameSession _gameSession;

	public override void _Ready()
	{
		_battleContext = GetNodeOrNull<GlobalBattleContext>("/root/GlobalBattleContext");
		_gameSession = GetNodeOrNull<GameSession>("/root/GameSession");

		if (_battleContext == null || _gameSession == null)
		{
			GD.PushError("BattleQuickExitController: 无法找到必要的 AutoLoad 节点。");
			return;
		}

		// 检查是否有待处理的战斗
		if (!_battleContext.has_pending_battle)
		{
			GD.PushWarning("BattleQuickExitController: 没有待处理的战斗。");
			return;
		}

		// 进行快速退出：构建零伤害的战斗结果并应用
		var battleRequest = _battleContext.pending_battle_request;
		
		// 快速退出路径按零增量处理，不修改任何资源状态。
		ApplyZeroDeltaResult();

		// 获取返回场景和玩家位置
		string returnScenePath = _battleContext.return_scene_path;
		Vector2 returnPlayerPosition = _battleContext.return_player_position;

		// 保存玩家位置到 GameSession，待场景加载后使用
		_gameSession.pending_restore_player_position = returnPlayerPosition;
		_gameSession.should_restore_player_position = true;

		// 清除战斗上下文
		_battleContext.clear_pending_battle();

		GD.Print($"BattleQuickExitController: 战斗快速退出。返回场景: {returnScenePath}，玩家位置: {returnPlayerPosition}");

		// 返回到原始场景
		if (!string.IsNullOrEmpty(returnScenePath))
		{
			GetTree().ChangeSceneToFile(returnScenePath);
		}
		else
		{
			GD.PushWarning("BattleQuickExitController: 返回场景路径为空。");
		}
	}

	private void ApplyZeroDeltaResult()
	{
		if (_gameSession == null)
		{
			return;
		}

		GD.Print("BattleQuickExitController: 应用战斗结果 - HP变化: 0, 能量变化: 0");
	}
}

