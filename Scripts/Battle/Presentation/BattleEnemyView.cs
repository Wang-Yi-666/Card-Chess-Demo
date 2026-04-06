using Godot;

namespace CardChessDemo.Battle.Presentation;

public partial class BattleEnemyView : BattleAnimatedViewBase
{
	private const string DefaultGruntFolderPath = "res://Assets/Character/Battle/Enemy/Grunt";

	[Export] public Texture2D? IdleSpriteSheet { get; set; }
	[Export(PropertyHint.Range, "1,256,1")] public int FrameWidth { get; set; } = 32;
	[Export(PropertyHint.Range, "1,256,1")] public int FrameHeight { get; set; } = 32;
	[Export(PropertyHint.Range, "1,64,1")] public int FrameCount { get; set; } = 3;
	[Export(PropertyHint.Range, "0,32,1")] public int CellFootOffsetY { get; set; } = 8;
	[Export] public Vector2 SpriteDrawOffset { get; set; } = new(-16.0f, -28.0f);

	protected override SpriteFrames BuildFallbackFrames()
	{
		Texture2D? spriteSheet = ResolveSpriteSheet();
		if (spriteSheet != null)
		{
			return BuildSheetFrames(spriteSheet);
		}

		return CreateFrames(new Color(1.0f, 0.38f, 0.32f), new Color(1.0f, 0.72f, 0.65f));
	}

	protected override void ConfigureAnimatedSprite(AnimatedSprite2D sprite)
	{
		sprite.Centered = false;
		sprite.Position = SpriteDrawOffset;
	}

	protected override Vector2 GetBoardAnchorOffset()
	{
		return new Vector2(0.0f, CellFootOffsetY);
	}

	protected override int GetSourceArtFacingSign()
	{
		return -1;
	}

	protected override int GetDefaultFacingSign()
	{
		return -1;
	}

	private Texture2D? ResolveSpriteSheet()
	{
		if (IdleSpriteSheet != null)
		{
			return IdleSpriteSheet;
		}

		foreach (string fileName in DirAccess.GetFilesAt(DefaultGruntFolderPath))
		{
			if (!fileName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			IdleSpriteSheet = GD.Load<Texture2D>($"{DefaultGruntFolderPath}/{fileName}");
			if (IdleSpriteSheet != null)
			{
				break;
			}
		}

		return IdleSpriteSheet;
	}

	private SpriteFrames BuildSheetFrames(Texture2D spriteSheet)
	{
		SpriteFrames frames = new();
		AddSheetAnimation(frames, spriteSheet, "idle", 5.0d, true);
		AddSheetAnimation(frames, spriteSheet, "move", 6.0d, true);
		AddSheetAnimation(frames, spriteSheet, "action", 7.0d, false);
		AddSheetAnimation(frames, spriteSheet, "hit", 8.0d, false, 0, 1);
		AddSheetAnimation(frames, spriteSheet, "defeat", 6.0d, false, FrameCount - 1, 1);
		return frames;
	}

	private void AddSheetAnimation(
		SpriteFrames frames,
		Texture2D spriteSheet,
		string animationName,
		double fps,
		bool loop,
		int startFrame = 0,
		int? frameLimit = null)
	{
		frames.AddAnimation(animationName);
		frames.SetAnimationSpeed(animationName, fps);
		frames.SetAnimationLoop(animationName, loop);

		if (FrameCount <= 0)
		{
			return;
		}

		int safeStart = Mathf.Clamp(startFrame, 0, FrameCount - 1);
		int resolvedLength = Mathf.Clamp(frameLimit ?? FrameCount, 1, FrameCount - safeStart);
		for (int frameIndex = safeStart; frameIndex < safeStart + resolvedLength; frameIndex++)
		{
			AtlasTexture atlas = new()
			{
				Atlas = spriteSheet,
				Region = new Rect2(frameIndex * FrameWidth, 0, FrameWidth, FrameHeight),
			};
			frames.AddFrame(animationName, atlas);
		}
	}
}
