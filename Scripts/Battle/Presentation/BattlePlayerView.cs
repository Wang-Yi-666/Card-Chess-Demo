using Godot;

namespace CardChessDemo.Battle.Presentation;

public partial class BattlePlayerView : BattleAnimatedViewBase
{
	[Export] public Texture2D? IdleSpriteSheet { get; set; }
	[Export(PropertyHint.Range, "1,256,1")] public int FrameWidth { get; set; } = 48;
	[Export(PropertyHint.Range, "1,256,1")] public int FrameHeight { get; set; } = 64;
	[Export(PropertyHint.Range, "1,64,1")] public int FrameCount { get; set; } = 7;
	[Export(PropertyHint.Range, "0,32,1")] public int CellFootOffsetY { get; set; } = 8;
	[Export] public Vector2 SpriteDrawOffset { get; set; } = new(-24.0f, -48.0f);

	public void PlayDefend()
	{
		// 当前只有防御动画入口，正式的防御结算规则仍由 action service 处理。
		PlayNamedAnimation("defend");
	}

	public void PlayCustom(StringName animationName)
	{
		PlayNamedAnimation(animationName.ToString());
	}

	public override void PlayAction()
	{
		PlayCustom("action");
	}

	protected override SpriteFrames BuildFallbackFrames()
	{
		if (IdleSpriteSheet != null)
		{
			return BuildPlayerSheetFrames();
		}

		return CreateFrames(new Color(0.24f, 0.78f, 1.0f), new Color(0.8f, 0.96f, 1.0f));
	}

	protected override void ConfigureAnimatedSprite(AnimatedSprite2D sprite)
	{
		sprite.Centered = false;
		sprite.Position = SpriteDrawOffset;
	}

	protected override Vector2 GetBoardAnchorOffset()
	{
		// 把 token 原点放到格子底边中心，这样下半身留在格子内，上半身可正常与其他对象按 Y 排序交叠。
		return new Vector2(0.0f, CellFootOffsetY);
	}

	protected override int GetSourceArtFacingSign()
	{
		return 1;
	}

	protected override int GetDefaultFacingSign()
	{
		return 1;
	}

	private SpriteFrames BuildPlayerSheetFrames()
	{
		SpriteFrames frames = new();
		AddSheetAnimation(frames, "idle", 7.0d, true);
		AddSheetAnimation(frames, "move", 8.0d, true);
		AddSheetAnimation(frames, "action", 9.0d, false);
		AddSheetAnimation(frames, "hit", 10.0d, false, 0, 3);
		AddSheetAnimation(frames, "defend", 8.0d, false, 0, 4);
		AddSheetAnimation(frames, "defeat", 6.0d, false, 0, Mathf.Min(2, FrameCount));
		return frames;
	}

	private void AddSheetAnimation(SpriteFrames frames, string animationName, double fps, bool loop, int startFrame = 0, int? frameLimit = null)
	{
		frames.AddAnimation(animationName);
		frames.SetAnimationSpeed(animationName, fps);
		frames.SetAnimationLoop(animationName, loop);

		if (IdleSpriteSheet == null || FrameCount <= 0)
		{
			return;
		}

		int lastFrameExclusive = Mathf.Clamp(frameLimit ?? FrameCount, 1, FrameCount);
		for (int frameIndex = Mathf.Clamp(startFrame, 0, FrameCount - 1); frameIndex < lastFrameExclusive; frameIndex++)
		{
			AtlasTexture atlas = new()
			{
				Atlas = IdleSpriteSheet,
				Region = new Rect2(frameIndex * FrameWidth, 0, FrameWidth, FrameHeight),
			};
			frames.AddFrame(animationName, atlas);
		}
	}
}
