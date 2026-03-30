using Godot;

public abstract partial class InteractableTemplate : StaticBody2D, IInteractable
{
	[Export] public string DisplayName = "交互物";
	[Export] public string PromptText = "交互";
	[Export] public float CooldownSeconds = 0.0f;
	[Export] public bool IsDisabled = false;
	[Export] public string CompletedFlowNodeId = "";
	[Export] public string NextActiveFlowNodeId = "";

	protected ulong _nextAvailableTimeMs = 0;

	public virtual string GetInteractText(Player player)
	{
		if (IsDisabled)
		{
			return "（已禁用）";
		}

		if (!CanInteract(player))
		{
			return "（冷却中）";
		}

		return PromptText;
	}

	public virtual bool CanInteract(Player player)
	{
		if (IsDisabled)
		{
			return false;
		}

		return Time.GetTicksMsec() >= _nextAvailableTimeMs;
	}

	public void Interact(Player player)
	{
		if (!CanInteract(player))
		{
			return;
		}

		OnInteract(player);
		ApplyCooldown();
		ApplyFlowProgressState();
	}

	protected abstract void OnInteract(Player player);

	protected virtual void ApplyCooldown()
	{
		if (CooldownSeconds > 0.0f)
		{
			_nextAvailableTimeMs = Time.GetTicksMsec() + (ulong)(CooldownSeconds * 1000.0f);
		}
	}

	private void ApplyFlowProgressState()
	{
		GameSession? session = GetNodeOrNull<GameSession>("/root/GameSession");
		if (session == null)
		{
			return;
		}

		if (!string.IsNullOrWhiteSpace(CompletedFlowNodeId))
		{
			session.set_flag(new StringName($"flow.completed.{CompletedFlowNodeId.Trim()}"), true);
		}

		if (!string.IsNullOrWhiteSpace(NextActiveFlowNodeId))
		{
			session.set_flag(new StringName("flow.active_node"), NextActiveFlowNodeId.Trim());
		}
	}
}
