using Godot;

public partial class Chest : StaticBody2D, IInteractable
{

	[Export] public string ChestName = "Sword Chest";

	[Export] public string ItemDescription = "你发现了一把生锈的剑。";
	private bool _isOpened = false;
	private bool _isOpening = false;
	private Sprite2D _sprite;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
	}

	public string GetInteractText(Player player)
	{
		return _isOpened ? "已打开" : "打开宝箱";
	}

	public bool CanInteract(Player player)
	{
		return !_isOpened && !_isOpening;
	}

	// 实现接口方法
	public void Interact(Player player)
	{
		if (!CanInteract(player)) return;

		OpenChest();
	}

	private void OpenChest()
	{
		_isOpening = true;
		_isOpened = true;
		GD.Print("宝箱：咔哒！你发现了一把生锈的剑。");

		if (HasNode("AnimationPlayer"))
		{
			GetNode<AnimationPlayer>("AnimationPlayer").Play("open");
		}

		Vector2 baseScale = Scale;
		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(this, "scale", baseScale * 1.08f, 0.08f);
		tween.TweenProperty(this, "scale", baseScale, 0.10f);
		tween.Parallel().TweenProperty(_sprite, "modulate", new Color(0.75f, 0.75f, 0.75f), 0.15f);
		tween.Finished += () => _isOpening = false;
	}
}
