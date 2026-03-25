using Godot;

/// <summary>
/// 通用交互物基类，所有交互物（宝箱、NPC、回血站等）推荐继承此类。
/// 只需实现 OnInteract 方法即可，冷却、禁用等逻辑自动处理。
/// </summary>
public abstract partial class InteractableTemplate : StaticBody2D, IInteractable
{
	[Export] public string DisplayName = "交互物";
	[Export] public string PromptText = "交互";
	[Export] public float CooldownSeconds = 0.0f;
	[Export] public bool IsDisabled = false;

	protected ulong _nextAvailableTimeMs = 0;

	/// <summary>
	/// 获取交互提示文本。
	/// 默认返回 PromptText，禁用或冷却中会返回对应提示。
	/// 可被子类覆盖以实现自定义逻辑（如 "已打开" / "打开宝箱"）。
	/// </summary>
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

	/// <summary>
	/// 判断是否可以交互。
	/// 默认检查禁用状态和冷却状态。
	/// 可被子类覆盖以实现额外逻辑（如 "已打开的箱子不能再开"）。
	/// </summary>
	public virtual bool CanInteract(Player player)
	{
		if (IsDisabled)
		{
			return false;
		}

		return Time.GetTicksMsec() >= _nextAvailableTimeMs;
	}

	/// <summary>
	/// 接口实现：交互入口。
	/// 自动检查可交互性，然后调用子类的 OnInteract，最后应用冷却。
	/// 子类不应覆盖此方法，应覆盖 OnInteract。
	/// </summary>
	public void Interact(Player player)
	{
		if (!CanInteract(player))
		{
			return;
		}

		OnInteract(player);
		ApplyCooldown();
	}

	/// <summary>
	/// 子类实现具体交互行为。
	/// 例如：开箱、对话、回血等。
	/// </summary>
	protected abstract void OnInteract(Player player);

	/// <summary>
	/// 应用冷却。
	/// 可被子类覆盖以自定义冷却逻辑。
	/// </summary>
	protected virtual void ApplyCooldown()
	{
		if (CooldownSeconds > 0.0f)
		{
			_nextAvailableTimeMs = Time.GetTicksMsec() + (ulong)(CooldownSeconds * 1000.0f);
		}
	}
}
