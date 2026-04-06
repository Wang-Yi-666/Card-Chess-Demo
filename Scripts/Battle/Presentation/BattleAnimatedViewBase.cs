using Godot;
using CardChessDemo.Battle.State;

using System;

namespace CardChessDemo.Battle.Presentation;

public partial class BattleAnimatedViewBase : Node2D
{
	private static readonly Shader KillShatterShader = GD.Load<Shader>("res://Shaders/Battle/KillShatter.gdshader");
	private AnimatedSprite2D? _animatedSprite;
	// 有些调用会在节点 _Ready 前发生，所以先缓存“想播什么动画”。
	private string _pendingAnimation = "idle";
	private int _horizontalFacing = 1;
	private Vector2 _boardAnchor;
	private Vector2 _motionOffset;
	private Tween? _motionTween;
	private Tween? _pulseTween;
	private Tween? _boardMoveTween;
	private Tween? _killTween;
	private ShaderMaterial? _killShaderMaterial;

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

		if (!_animatedSprite.IsConnected(AnimatedSprite2D.SignalName.AnimationFinished, Callable.From(OnAnimationFinished)))
		{
			_animatedSprite.AnimationFinished += OnAnimationFinished;
		}

		if (State == null)
		{
			_horizontalFacing = GetDefaultFacingSign();
		}
		ConfigureAnimatedSprite(_animatedSprite);
		ApplySpriteFacing();
		PlayNamedAnimation(_pendingAnimation);
	}

	public virtual void Bind(BattleObjectState state)
	{
		State = state;
		Name = state.ObjectId;
		FaceDirection(state.InitialFacing == Vector2.Zero
			? new Vector2(GetDefaultFacingSign(), 0.0f)
			: state.InitialFacing);
	}

	public void SetBoardPosition(Vector2 localCenter)
	{
		_boardMoveTween?.Kill();
		_boardAnchor = localCenter;
		ApplyVisualPosition();
	}

	public async System.Threading.Tasks.Task TweenBoardPositionAsync(Vector2 localCenter, double duration)
	{
		if (duration <= 0.0d)
		{
			SetBoardPosition(localCenter);
			return;
		}

		_boardMoveTween?.Kill();
		_boardMoveTween = CreateTween();
		_boardMoveTween.SetEase(Tween.EaseType.InOut);
		_boardMoveTween.SetTrans(Tween.TransitionType.Sine);
		_boardMoveTween.TweenProperty(this, nameof(BoardAnchor), localCenter, duration);
		await ToSignal(_boardMoveTween, Tween.SignalName.Finished);
	}

	public virtual void PlayIdle() => PlayCue("idle");
	public virtual void PlayMove() => PlayCue("move");
	public virtual void PlayAction() => PlayCue("action");
	public virtual void PlayHit() => PlayCue("hit");
	public virtual void PlayDefeat() => PlayCue("defeat");

	public Texture2D? CaptureCurrentFrameTexture()
	{
		_animatedSprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_animatedSprite == null)
		{
			return null;
		}

		if (_animatedSprite.SpriteFrames == null)
		{
			_animatedSprite.SpriteFrames = BuildFallbackFrames();
			ConfigureAnimatedSprite(_animatedSprite);
			ApplySpriteFacing();
		}

		string animationName = _animatedSprite.Animation.ToString();
		if (string.IsNullOrWhiteSpace(animationName))
		{
			animationName = "idle";
		}

		SpriteFrames? frames = _animatedSprite.SpriteFrames;
		if (frames == null || !frames.HasAnimation(animationName))
		{
			return null;
		}

		int frameCount = frames.GetFrameCount(animationName);
		if (frameCount <= 0)
		{
			return null;
		}

		int frame = Math.Clamp(_animatedSprite.Frame, 0, frameCount - 1);
		Texture2D? frameTexture = frames.GetFrameTexture(animationName, frame);
		return CloneFrameTexture(frameTexture);
	}

	public Texture2D? CaptureAnimationFrameTexture(string animationName, int frameIndex)
	{
		_animatedSprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_animatedSprite == null)
		{
			return null;
		}

		if (_animatedSprite.SpriteFrames == null)
		{
			_animatedSprite.SpriteFrames = BuildFallbackFrames();
			ConfigureAnimatedSprite(_animatedSprite);
			ApplySpriteFacing();
		}

		SpriteFrames? frames = _animatedSprite.SpriteFrames;
		if (frames == null || !frames.HasAnimation(animationName))
		{
			return null;
		}

		int frameCount = frames.GetFrameCount(animationName);
		if (frameCount <= 0)
		{
			return null;
		}

		int safeFrameIndex = Math.Clamp(frameIndex, 0, frameCount - 1);
		Texture2D? frameTexture = frames.GetFrameTexture(animationName, safeFrameIndex);
		return CloneFrameTexture(frameTexture);
	}

	private static Texture2D? CloneFrameTexture(Texture2D? frameTexture)
	{
		if (frameTexture == null)
		{
			return null;
		}

		Image image = frameTexture.GetImage();
		if (image == null || image.IsEmpty())
		{
			return frameTexture;
		}

		return ImageTexture.CreateFromImage(image);
	}

	public Vector2 CaptureSpriteLocalPosition()
	{
		_animatedSprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		return _animatedSprite?.Position ?? Vector2.Zero;
	}

	public bool CaptureSpriteFlipH()
	{
		_animatedSprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		return _animatedSprite?.FlipH ?? false;
	}

	public bool CaptureSpriteCentered()
	{
		_animatedSprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		return _animatedSprite?.Centered ?? true;
	}

	public virtual async System.Threading.Tasks.Task PlayKillSequenceAsync(
		Vector2 knockbackDirection,
		float knockbackDistance,
		double knockbackDuration,
		double whiteFlashDuration,
		double shatterDuration)
	{
		_animatedSprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_animatedSprite == null)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			_animatedSprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		}

		if (_animatedSprite == null)
		{
			return;
		}

		if (_animatedSprite.SpriteFrames == null)
		{
			_animatedSprite.SpriteFrames = BuildFallbackFrames();
			ConfigureAnimatedSprite(_animatedSprite);
			ApplySpriteFacing();
		}

		_motionTween?.Kill();
		_pulseTween?.Kill();
		_killTween?.Kill();

		Vector2 resolvedDirection = knockbackDirection == Vector2.Zero
			? Vector2.Right
			: knockbackDirection.Normalized();

		FaceDirection(resolvedDirection);
		PlayDefeat();
		_animatedSprite.Stop();
		_animatedSprite.Pause();

		_killShaderMaterial = new ShaderMaterial
		{
			Shader = KillShatterShader,
		};
		_killShaderMaterial.SetShaderParameter("white_mix", 0.0f);
		_killShaderMaterial.SetShaderParameter("shatter_progress", 0.0f);
		_animatedSprite.Material = _killShaderMaterial;

		Vector2 targetOffset = resolvedDirection * knockbackDistance;
		MotionOffset = Vector2.Zero;

		_killTween = CreateTween();
		_killTween.SetParallel();
		_killTween.SetEase(Tween.EaseType.Out);
		_killTween.SetTrans(Tween.TransitionType.Cubic);
		_killTween.TweenProperty(this, nameof(MotionOffset), targetOffset, knockbackDuration);
		_killTween.TweenMethod(Callable.From<float>(SetKillWhiteMix), 0.0f, 1.0f, whiteFlashDuration);
		_killTween.TweenMethod(Callable.From<float>(SetKillShatterProgress), 0.0f, 1.0f, shatterDuration)
			.SetDelay(Math.Max(whiteFlashDuration * 0.4d, 0.02d));

		await ToSignal(_killTween, Tween.SignalName.Finished);
	}

	public virtual void PlayCue(StringName animationName)
	{
		PlayNamedAnimation(animationName.ToString());
	}

	public virtual void FaceDirection(Vector2 direction)
	{
		if (Mathf.Abs(direction.X) < 0.01f)
		{
			return;
		}

		_horizontalFacing = direction.X < 0.0f ? -1 : 1;
		ApplySpriteFacing();
	}

	public virtual void PlayTintPulse(Color tintColor)
	{
		_pulseTween?.Kill();
		_pulseTween = CreateTween();
		_pulseTween.SetParallel();
		_pulseTween.SetEase(Tween.EaseType.Out);
		_pulseTween.SetTrans(Tween.TransitionType.Cubic);
		_pulseTween.TweenProperty(this, "modulate", tintColor, 0.08d);
		_pulseTween.TweenProperty(this, "scale", new Vector2(1.06f, 1.06f), 0.08d);
		_pulseTween.TweenProperty(this, "modulate", Colors.White, 0.20d).SetDelay(0.08d);
		_pulseTween.TweenProperty(this, "scale", Vector2.One, 0.18d).SetDelay(0.08d);
		PlayMotionOffset(new Vector2(0.0f, -1.5f), 0.04d, 0.12d);
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

	public Vector2 BoardAnchor
	{
		get => _boardAnchor;
		set
		{
			_boardAnchor = value;
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

	protected virtual void ConfigureAnimatedSprite(AnimatedSprite2D sprite)
	{
	}

	protected virtual Vector2 GetBoardAnchorOffset()
	{
		return Vector2.Zero;
	}

	protected virtual int GetSourceArtFacingSign()
	{
		return 1;
	}

	protected virtual int GetDefaultFacingSign()
	{
		return 1;
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
		Position = _boardAnchor + GetBoardAnchorOffset() + _motionOffset;
	}

	private void ApplySpriteFacing()
	{
		if (_animatedSprite == null)
		{
			return;
		}

		_animatedSprite.FlipH = _horizontalFacing != GetSourceArtFacingSign();
	}

	private void OnAnimationFinished()
	{
		if (_animatedSprite == null)
		{
			return;
		}

		string finishedAnimation = _animatedSprite.Animation.ToString();
		if (string.IsNullOrWhiteSpace(finishedAnimation))
		{
			return;
		}

		// 非循环表现动画播完后要主动回到 idle，
		// 否则 AnimatedSprite2D 会停在最后一帧，看起来像“动画卡住”。
		if (string.Equals(finishedAnimation, "idle", StringComparison.Ordinal)
			|| string.Equals(finishedAnimation, "move", StringComparison.Ordinal)
			|| string.Equals(finishedAnimation, "defeat", StringComparison.Ordinal))
		{
			return;
		}

		PlayIdle();
	}

	private void SetKillWhiteMix(float value)
	{
		_killShaderMaterial?.SetShaderParameter("white_mix", value);
	}

	private void SetKillShatterProgress(float value)
	{
		_killShaderMaterial?.SetShaderParameter("shatter_progress", value);
	}
}
