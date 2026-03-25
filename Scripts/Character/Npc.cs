using Godot;

public partial class Npc : InteractableTemplate
{
	[Export] public string NpcName = "村民";
	[Export] public string DialogueText = "你好，旅行者。";

	protected override void OnInteract(Player player)
	{
		GD.Print($"{NpcName}：{DialogueText}");

		Vector2 baseScale = Scale;
		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(this, "scale", baseScale * 1.08f, 0.08f);
		tween.TweenProperty(this, "scale", baseScale, 0.10f);
	}
}

