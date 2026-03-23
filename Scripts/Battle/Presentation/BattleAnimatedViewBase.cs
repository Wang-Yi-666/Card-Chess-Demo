using Godot;
using CardChessDemo.Battle.State;

namespace CardChessDemo.Battle.Presentation;

public partial class BattleAnimatedViewBase : Node2D
{
	private AnimatedSprite2D? _animatedSprite;
	// 有些调用会在节点 _Ready 前发生，所以先缓存“想播什么动画”。
	private string _pendingAnimation = "idle";
	private Vector2 _boardAnchor;
	private Vector2 _motionOffset;
	private Tween? _motionTween;

	public BattleObjectState? State { get; private set; }

	public override void _Ready()
	{
		_animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_animatedSprite == null)
		{
			GD.PushError($"{nameof(BattleAnimatedViewBase)} requires an AnimatedSprite2D child on {GetPath()}.");
			return;
		}

		if (_animatedSprite.SpriteFrames == null)
		{
			// 运行期允许 prefab 没配美术资源时降级成内建帧，
			// 这样战斗原型不会因为缺贴图直接失效。
			_animatedSprite.SpriteFrames = BuildFallbackFrames();
		}

		PlayNamedAnimation(_pendingAnimation);
	}

	public virtual void Bind(BattleObjectState state)
	{
		State = state;
		Name = state.ObjectId;
	}

	public void SetBoardPosition(Vector2 localCenter)
	{
		_boardAnchor = localCenter;
		ApplyVisualPosition();
	}

	public virtual void PlayIdle() => PlayCue("idle");
	public virtual void PlayMove() => PlayCue("move");
	public virtual void PlayAction() => PlayCue("action");
	public virtual void PlayHit() => PlayCue("hit");
	public virtual void PlayDefeat() => PlayCue("defeat");

	public virtual void PlayCue(StringName animationName)
	{
		PlayNamedAnimation(animationName.ToString());
	}

	public virtual void PlayMotionOffset(Vector2 targetOffset, double outDuration, double returnDuration, double returnDelay = 0.0d)
	{
		_motionTween?.Kill();
		_motionTween = CreateTween();
		_motionTween.SetEase(Tween.EaseType.Out);
		_motionTween.SetTrans(Tween.TransitionType.Cubic);
		_motionTween.TweenProperty(this, nameof(MotionOffset), targetOffset, outDuration);
		_motionTween.TweenProperty(this, nameof(MotionOffset), Vector2.Zero, returnDuration).SetDelay(returnDelay);
	}

	public Vector2 MotionOffset
	{
		get => _motionOffset;
		set
		{
			_motionOffset = value;
			ApplyVisualPosition();
		}
	}

	protected void PlayNamedAnimation(string animationName)
	{
		_pendingAnimation = animationName;

		// 如果视图还没 ready，就只记录状态，不立刻访问 AnimatedSprite2D。
		if (_animatedSprite?.SpriteFrames == null)
		{
			if (State != null)
			{
				State.CurrentAnimation = animationName;
			}

			return;
		}

		if (!_animatedSprite.SpriteFrames.HasAnimation(animationName))
		{
			// 当前原型允许调用比资源更多的动画名，缺失时统一回退到 idle。
			animationName = "idle";
		}

		_pendingAnimation = animationName;
		_animatedSprite.Play(animationName);
		if (State != null)
		{
			State.CurrentAnimation = animationName;
		}
	}

	protected virtual SpriteFrames BuildFallbackFrames()
	{
		return CreateFrames(new Color(0.9f, 0.9f, 0.9f), new Color(1.0f, 1.0f, 1.0f));
	}

	protected SpriteFrames CreateFrames(Color primary, Color secondary)
	{
		SpriteFrames frames = new();
		AddAnimation(frames, "idle", new[] { primary, secondary }, 3.0f, true);
		AddAnimation(frames, "move", new[] { primary.Lightened(0.1f), secondary, primary.Darkened(0.1f) }, 7.0f, true);
		AddAnimation(frames, "action", new[] { secondary, primary, secondary }, 10.0f, false);
		AddAnimation(frames, "hit", new[] { new Color(1.0f, 1.0f, 1.0f), primary.Darkened(0.3f) }, 8.0f, false);
		AddAnimation(frames, "defeat", new[] { primary.Darkened(0.2f), new Color(0.26f, 0.0f, 0.0f), Colors.Transparent }, 6.0f, false);
		// defend 目前只是视觉占位动画，正式防御规则还没有落地。
		AddAnimation(frames, "defend", new[] { primary, new Color(0.75f, 0.95f, 1.0f), primary }, 6.0f, false);
		return frames;
	}

	private static void AddAnimation(SpriteFrames frames, string name, Color[] colors, double fps, bool loop)
	{
		frames.AddAnimation(name);
		frames.SetAnimationSpeed(name, fps);
		frames.SetAnimationLoop(name, loop);

		foreach (Color color in colors)
		{
			frames.AddFrame(name, CreateFrameTexture(color));
		}
	}

	private static Texture2D CreateFrameTexture(Color fillColor)
	{
		Image image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
		image.Fill(Colors.Transparent);

		for (int y = 2; y < 14; y++)
		{
			for (int x = 2; x < 14; x++)
			{
				bool border = x == 2 || x == 13 || y == 2 || y == 13;
				image.SetPixel(x, y, border ? Colors.Black : fillColor);
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	private void ApplyVisualPosition()
	{
		Position = _boardAnchor + _motionOffset;
	}
}
