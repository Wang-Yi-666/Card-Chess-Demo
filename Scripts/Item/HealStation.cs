using Godot;

public partial class HealStation : StaticBody2D, IInteractable
{
	[Export] public int HealAmount = 30;
	[Export] public float CooldownSeconds = 1.5f;

	private ulong _nextAvailableTimeMs = 0;

	public string GetInteractText(Player player)
	{
		return CanInteract(player) ? "治疗" : "冷却中";
	}

	public bool CanInteract(Player player)
	{
		return Time.GetTicksMsec() >= _nextAvailableTimeMs;
	}

	public void Interact(Player player)
	{
		if (!CanInteract(player))
		{
			return;
		}

		GameSession session = GetNodeOrNull<GameSession>("/root/GameSession");
		if (session == null)
		{
			GD.PushWarning("治疗站：未找到 GameSession，无法恢复状态。");
			return;
		}

		int playerHpMax = Mathf.Max(1, session.player_runtime.hp_max);
		int playerHpDelta = playerHpMax - session.player_runtime.hp_current;
		if (playerHpDelta != 0)
		{
			session.apply_resource_delta("player_hp", playerHpDelta, 0, playerHpMax);
		}

		int partnerEnergyCap = Mathf.Max(1, session.arakawa_state.energy_cap);
		int partnerEnergyDelta = partnerEnergyCap - session.arakawa_state.energy_current;
		if (partnerEnergyDelta != 0)
		{
			session.apply_resource_delta("arakawa_energy", partnerEnergyDelta, 0, partnerEnergyCap);
		}

		GD.Print($"治疗站：已回满主角 HP({session.player_runtime.hp_current}/{playerHpMax}) 与伙伴 EN({session.arakawa_state.energy_current}/{partnerEnergyCap})。");

		_nextAvailableTimeMs = Time.GetTicksMsec() + (ulong)(CooldownSeconds * 1000.0f);

		Vector2 baseScale = Scale;
		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(this, "scale", baseScale * 1.08f, 0.08f);
		tween.TweenProperty(this, "scale", baseScale, 0.10f);
	}
}


// using Godot;

// public partial class HealingStation : StaticBody2D, IInteractable
// {
//     // 永远可以回血，或者你可以加个冷却变量
//     public bool CanInteract(Player player) => true;

//     public string GetInteractText(Player player) => "恢复生命值";

//     public void Interact(Player player)
//     {
//         GD.Print("回血站激活：玩家状态已恢复！");
//         // 视觉反馈：闪烁一下绿色
//         var sprite = GetNode<Sprite2D>("Sprite2D");
//         sprite.Modulate = Colors.Green;
        
//         // 1秒后恢复原色
//         GetTree().CreateTimer(1.0).Timeout += () => sprite.Modulate = Colors.White;
//     }
// }