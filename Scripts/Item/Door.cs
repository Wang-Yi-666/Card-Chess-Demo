using Godot;

public partial class Door : InteractableTemplate
{
	[Export] public PackedScene NextScene;
	[Export(PropertyHint.File, "*.tscn")] public string NextScenePath = "";
	[Export] public string BusyText = "切换中...";

	private bool _isTransitioning = false;

	public override void _Ready()
	{
		if (NextScene == null && string.IsNullOrWhiteSpace(NextScenePath))
		{
			GD.PushWarning("Door: 未配置 NextScene 或 NextScenePath，按 E 不会触发切换。");
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

		return string.IsNullOrWhiteSpace(PromptText) ? "进入下一场景" : PromptText;
	}

	public override bool CanInteract(Player player)
	{
		if (_isTransitioning)
		{
			return false;
		}

		if (!base.CanInteract(player))
		{
			return false;
		}

		return NextScene != null || !string.IsNullOrWhiteSpace(NextScenePath);
	}

	protected override void OnInteract(Player player)
	{
		if (NextScene == null && string.IsNullOrWhiteSpace(NextScenePath))
		{
			GD.PushError("Door: 目标场景未配置，请在 Inspector 设置 NextScene 或 NextScenePath。");
			return;
		}

		_isTransitioning = true;

		Error result;
		if (NextScene != null)
		{
			result = GetTree().ChangeSceneToPacked(NextScene);
		}
		else
		{
			result = GetTree().ChangeSceneToFile(NextScenePath.Trim());
		}

		if (result != Error.Ok)
		{
			_isTransitioning = false;
			GD.PushError($"Door: 切换场景失败，错误码={result}");
		}
	}
}
