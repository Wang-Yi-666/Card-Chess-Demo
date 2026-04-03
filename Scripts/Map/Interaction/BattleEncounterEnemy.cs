using Godot;

namespace CardChessDemo.Map;

public partial class BattleEncounterEnemy : InteractableTemplate
{
	[Export] public string EnemyDisplayName = "Wanderer";
	[Export] public string BattleEncounterId = "grunt_debug";
	[Export] public PackedScene? BattleScene;
	[Export(PropertyHint.File, "*.tscn")] public string BattleScenePath = "res://Scene/Battle/Battle.tscn";
	[Export] public string BusyText = "战斗中...";

	private bool _isTransitioning;

	public override string GetInteractText(Player player)
	{
		if (_isTransitioning)
		{
			return BusyText;
		}

		if (!CanInteract(player))
		{
			return "无法交战";
		}

		return string.IsNullOrWhiteSpace(PromptText) ? $"挑战 {EnemyDisplayName}" : PromptText;
	}

	public override bool CanInteract(Player player)
	{
		if (_isTransitioning)
		{
			return false;
		}

		return base.CanInteract(player)
			&& (BattleScene != null || !string.IsNullOrWhiteSpace(BattleScenePath))
			&& !string.IsNullOrWhiteSpace(BattleEncounterId);
	}

	protected override void OnInteract(Player player)
	{
		_isTransitioning = true;
		PromptText = BusyText;
		if (!MapBattleTransitionHelper.TryEnterBattle(this, player, BattleScene, BattleScenePath, BattleEncounterId, out string failureReason, HandleDeferredBattleFailure))
		{
			_isTransitioning = false;
			PromptText = $"失败: {failureReason}";
			GD.PushError($"BattleEncounterEnemy: {failureReason}");
			return;
		}
	}

	private void HandleDeferredBattleFailure(string failureReason)
	{
		_isTransitioning = false;
		PromptText = $"失败: {failureReason}";
		GD.PushError($"BattleEncounterEnemy: {failureReason}");
	}
}
