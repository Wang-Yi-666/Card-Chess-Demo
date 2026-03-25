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

		Error result = ChangeToConfiguredScene();

		if (result != Error.Ok)
		{
			_isTransitioning = false;
			GD.PushError($"Door: 切换场景失败，错误码={result}，NextScenePath='{NextScenePath}'");
		}
	}

	private Error ChangeToConfiguredScene()
	{
		if (NextScene != null)
		{
			return GetTree().ChangeSceneToPacked(NextScene);
		}

		string rawPath = NextScenePath?.Trim() ?? string.Empty;
		if (string.IsNullOrEmpty(rawPath))
		{
			return Error.InvalidParameter;
		}

		if (rawPath.StartsWith("uid://"))
		{
			PackedScene byUid = ResourceLoader.Load<PackedScene>(rawPath);
			if (byUid == null)
			{
				return Error.CantOpen;
			}

			return GetTree().ChangeSceneToPacked(byUid);
		}

		string normalizedPath = NormalizeLegacyScenePath(rawPath);
		if (!ResourceLoader.Exists(normalizedPath))
		{
			return Error.CantOpen;
		}

		return GetTree().ChangeSceneToFile(normalizedPath);
	}

	private static string NormalizeLegacyScenePath(string path)
	{
		if (path.StartsWith("res://Scene(garbage)/"))
		{
			return path.Replace("res://Scene(garbage)/", "res://Scene/");
		}

		return path;
	}
}
