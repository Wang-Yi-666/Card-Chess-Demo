using Godot;

namespace CardChessDemo.Map;

/// <summary>
/// 根据 TileMapLayer 的已用格子生成静态碰撞，适配 TileMapLayer 工作流。
/// 把该节点放到场景里并指向 WallLayer 即可自动生效。
/// </summary>
public partial class WallLayerCollisionBuilder : Node
{
	[Export] public NodePath WallLayerPath = "../WallLayer";
	[Export(PropertyHint.Range, "8,128,1")] public int CellSize = 16;
	[Export] public uint CollisionLayer = 2;
	[Export] public uint CollisionMask = 0;
	[Export] public bool RebuildOnReady = true;

	private const string RuntimeBodyName = "RuntimeWallCollisionBody";

	public override void _Ready()
	{
		if (RebuildOnReady)
		{
			Rebuild();
		}
	}

	public void Rebuild()
	{
		TileMapLayer wallLayer = GetNodeOrNull<TileMapLayer>(WallLayerPath);
		if (wallLayer == null)
		{
			GD.PushWarning($"WallLayerCollisionBuilder: 未找到 WallLayer，路径='{WallLayerPath}'");
			return;
		}

		Node oldBody = wallLayer.GetNodeOrNull<Node>(RuntimeBodyName);
		oldBody?.QueueFree();

		StaticBody2D body = new StaticBody2D
		{
			Name = RuntimeBodyName,
			CollisionLayer = CollisionLayer,
			CollisionMask = CollisionMask,
		};
		wallLayer.AddChild(body);

		Godot.Collections.Array<Vector2I> usedCells = wallLayer.GetUsedCells();
		foreach (Vector2I cell in usedCells)
		{
			if (wallLayer.GetCellSourceId(cell) < 0)
			{
				continue;
			}

			RectangleShape2D shape = new RectangleShape2D
			{
				Size = new Vector2(CellSize, CellSize),
			};

			CollisionShape2D shapeNode = new CollisionShape2D
			{
				Shape = shape,
				Position = wallLayer.MapToLocal(cell),
			};

			body.AddChild(shapeNode);
		}
	}
}
