using Godot;

namespace CardChessDemo.Map;

public partial class Enemy : InteractableTemplate
{
	[Export] public string EncounterId = "grunt_debug";
	[Export] public PackedScene? BattleScene;
	[Export(PropertyHint.File, "*.tscn")] public string BattleScenePath = "res://Scene/Battle/Battle.tscn";
	[Export] public string BusyText = "战斗中...";
	[Export] public bool DisableAfterInteract = true;

	private bool _isTransitioning;

	public override string GetInteractText(Player player)
	{
		if (_isTransitioning)
		{
			return BusyText;
		}

		if (!CanInteract(player))
		{
			return "无法接战";
		}

		return string.IsNullOrWhiteSpace(PromptText) ? "发起战斗" : PromptText;
	}

	public override bool CanInteract(Player player)
	{
		if (_isTransitioning)
		{
			return false;
		}

		return base.CanInteract(player)
			&& !string.IsNullOrWhiteSpace(EncounterId)
			&& (BattleScene != null || !string.IsNullOrWhiteSpace(BattleScenePath));
	}

	protected override void OnInteract(Player player)
	{
		_isTransitioning = true;
		if (!MapBattleTransitionHelper.TryEnterBattle(this, player, BattleScene, BattleScenePath, EncounterId, out string failureReason, HandleDeferredBattleFailure))
		{
			_isTransitioning = false;
			GD.PushError($"Enemy: {failureReason}");
			return;
		}

		if (DisableAfterInteract)
		{
			IsDisabled = true;
		}
	}

	private void HandleDeferredBattleFailure(string failureReason)
	{
		_isTransitioning = false;
		if (DisableAfterInteract)
		{
			IsDisabled = false;
		}

		GD.PushError($"Enemy: {failureReason}");
	}
}
