using Godot;
using CardChessDemo.Battle.State;

namespace CardChessDemo.Battle.Presentation;

public partial class BattleAnimatedViewBase : Node2D
{
	private AnimatedSprite2D? _animatedSprite;
	private string _pendingAnimation = "idle";

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
		Position = localCenter;
	}

	public virtual void PlayIdle() => PlayNamedAnimation("idle");
	public virtual void PlayMove() => PlayNamedAnimation("move");
	public virtual void PlayAction() => PlayNamedAnimation("action");
	public virtual void PlayHit() => PlayNamedAnimation("hit");

	protected void PlayNamedAnimation(string animationName)
	{
		_pendingAnimation = animationName;

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
}
