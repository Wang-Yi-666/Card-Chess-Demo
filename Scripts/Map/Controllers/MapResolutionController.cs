using Godot;

namespace CardChessDemo.Map;

public partial class MapResolutionController : Node
{
	[Export] public Vector2I MapContentScaleSize = new Vector2I(640, 360);
	[Export] public Vector2I FallbackContentScaleSize = new Vector2I(320, 180);

	public override void _Ready()
	{
		Window window = GetWindow();
		if (window == null)
		{
			return;
		}

		// Scene-level override: use higher logical size for map exploration.
		window.Set("content_scale_size", MapContentScaleSize);
	}

	public override void _ExitTree()
	{
		Window window = GetWindow();
		if (window == null)
		{
			return;
		}

		// Restore default logical size when leaving map scene.
		window.Set("content_scale_size", FallbackContentScaleSize);
	}
}
