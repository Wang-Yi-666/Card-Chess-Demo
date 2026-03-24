using Godot;
using CardChessDemo.Battle.Boundary;
using CardChessDemo.Battle.Shared;

public partial class SceneDoor : InteractableTemplate
{
	[Export] public PackedScene? NextScene;
	[Export(PropertyHint.File, "*.tscn")] public string NextScenePath = string.Empty;
	[Export] public bool StartsBattle = false;
	[Export] public PackedScene? BattleScene;
	[Export(PropertyHint.File, "*.tscn")] public string BattleScenePath = "res://Scene/Battle/Battle.tscn";
	[Export] public string BattleEncounterId = "grunt_debug";
	[Export] public string BusyText = "切换中...";

	private bool _isTransitioning;

	public override void _Ready()
	{
		if (!HasValidDestination())
		{
			GD.PushWarning("SceneDoor: no valid destination is configured.");
		}
	}

	public override string GetInteractText(Player player)
	{
		if (_isTransitioning)
		{
			return BusyText;
		}

		if (!CanInteract(player))
		{
			return "无法进入";
		}

		if (!string.IsNullOrWhiteSpace(PromptText))
		{
			return PromptText;
		}

		return StartsBattle ? "进入战斗" : "进入下一场景";
	}

	public override bool CanInteract(Player player)
	{
		if (_isTransitioning)
		{
			return false;
		}

		return base.CanInteract(player) && HasValidDestination();
	}

	protected override void OnInteract(Player player)
	{
		if (StartsBattle)
		{
			EnterBattle(player);
			return;
		}

		ChangeToScene();
	}

	private bool HasValidDestination()
	{
		if (StartsBattle)
		{
			return BattleScene != null || !string.IsNullOrWhiteSpace(BattleScenePath);
		}

		return NextScene != null || !string.IsNullOrWhiteSpace(NextScenePath);
	}

	private void ChangeToScene()
	{
		if (NextScene == null && string.IsNullOrWhiteSpace(NextScenePath))
		{
			GD.PushError("SceneDoor: target scene is not configured.");
			return;
		}

		_isTransitioning = true;
		Error result = NextScene != null
			? GetTree().ChangeSceneToPacked(NextScene)
			: GetTree().ChangeSceneToFile(NextScenePath.Trim());

		if (result != Error.Ok)
		{
			_isTransitioning = false;
			GD.PushError($"SceneDoor: scene change failed, error={result}");
		}
	}

	private void EnterBattle(Player player)
	{
		if (BattleScene == null && string.IsNullOrWhiteSpace(BattleScenePath))
		{
			GD.PushError("SceneDoor: battle scene is not configured.");
			return;
		}

		GlobalGameSession? globalSession = GetNodeOrNull<GlobalGameSession>("/root/GlobalGameSession");
		if (globalSession == null)
		{
			GD.PushError("SceneDoor: GlobalGameSession is missing.");
			return;
		}

		string currentScenePath = GetTree().CurrentScene?.SceneFilePath ?? string.Empty;
		globalSession.BeginBattle(BattleRequest.FromSession(globalSession));
		globalSession.SetPendingBattleEncounterId(BattleEncounterId);
		globalSession.SetPendingMapResumeContext(new MapResumeContext(currentScenePath, player.GlobalPosition));

		_isTransitioning = true;
		Error result = BattleScene != null
			? GetTree().ChangeSceneToPacked(BattleScene)
			: GetTree().ChangeSceneToFile(BattleScenePath.Trim());

		if (result != Error.Ok)
		{
			_isTransitioning = false;
			GD.PushError($"SceneDoor: battle scene change failed, error={result}");
		}
	}
}
