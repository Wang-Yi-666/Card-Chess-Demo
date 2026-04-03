using System;
using System.Collections.Generic;
using Godot;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.Data;

namespace CardChessDemo.Battle.Rooms;

[Tool]
public partial class BattleRoomTemplate : Node2D
{
	private static readonly StringName PlayerTag = new("player");
	private static readonly StringName EnemyTag = new("enemy");
	private static readonly StringName ObstacleTag = new("obstacle");
	private static readonly StringName DestructibleObstacleTag = new("destructible");
	private static readonly StringName IndestructibleObstacleTag = new("indestructible");
	private static readonly StringName SlowPassObstacleTag = new("slow_pass");

	[Export] public string LayoutId { get; set; } = "battle_room_debug";
	[Export] public Vector2I BoardSize { get; set; } = new(16, 8);
	[Export(PropertyHint.Range, "16,16,1")] public int CellSizePixels { get; set; } = 16;
	[Export] public string DefaultTerrainId { get; set; } = "battle_floor";
	[Export] public int DefaultMoveCost { get; set; } = 1;
	[Export] public string[] SupportedEnemyTypeIds { get; set; } = Array.Empty<string>();
	[Export] public string[] RoomTags { get; set; } = Array.Empty<string>();
	[Export] public Vector2I DefaultPlayerCell { get; set; } = new(2, 6);
	[Export] public Godot.Collections.Array<Vector2I> DefaultEnemyCells { get; set; } = new() { new Vector2I(11, 2), new Vector2I(13, 5) };
	[Export] public Godot.Collections.Array<Vector2I> DefaultDestructibleObstacleCells { get; set; } = new() { new Vector2I(6, 2) };
	[Export] public Godot.Collections.Array<Vector2I> DefaultIndestructibleObstacleCells { get; set; } = new() { new Vector2I(7, 5) };
	[Export] public Godot.Collections.Array<Vector2I> DefaultSlowPassObstacleCells { get; set; } = new() { new Vector2I(9, 3) };
	[Export] public Godot.Collections.Array<Vector2I> DefaultEscapeCells { get; set; } = new();
	[Export] public int FloorSourceId { get; set; } = 0;
	[Export] public Vector2I DefaultFloorAtlasCoords { get; set; } = Vector2I.Zero;
	[Export] public int MarkerSourceId { get; set; } = 1;
	[Export] public int EscapeSourceId { get; set; } = 1;
	[Export] public int PlayerMarkerTileId { get; set; } = 1;
	[Export] public int EnemyMarkerTileId { get; set; } = 2;
	[Export] public int DestructibleObstacleMarkerTileId { get; set; } = 3;
	[Export] public int IndestructibleObstacleMarkerTileId { get; set; } = 4;
	[Export] public int SlowPassObstacleMarkerTileId { get; set; } = 5;
	[Export] public int EscapeMarkerTileId { get; set; } = 6;

	private TileMapLayer _floorLayer = null!;
	private TileMapLayer _markerLayer = null!;
	private TileMapLayer _escapeLayer = null!;
	private BoardTopology _topology = null!;

	public BoardTopology Topology => _topology;

	public override void _EnterTree()
	{
		EnsureReferences();
		EnsureTopology();
		EnsureEditableTileSet();
		EnsureDefaultPaint();
	}

	public override void _Ready()
	{
		EnsureReferences();
		EnsureTopology();
		if (!Engine.IsEditorHint())
		{
			_markerLayer.Visible = false;
			_escapeLayer.Visible = false;
		}
	}

	public IReadOnlyList<Vector2I> GetEscapeCells()
	{
		EnsureReferences();
		EnsureTopology();

		List<Vector2I> cells = new();
		foreach (Vector2I cell in _escapeLayer.GetUsedCells())
		{
			if (!_topology.IsInsideBoard(cell))
			{
				continue;
			}

			int sourceId = _escapeLayer.GetCellSourceId(cell);
			if (sourceId != EscapeSourceId)
			{
				continue;
			}

			cells.Add(cell);
		}

		return cells;
	}

	public RoomLayoutDefinition BuildLayoutDefinition(string enemyDefinitionId = "battle_enemy")
	{
		EnsureReferences();
		EnsureTopology();

		List<BoardObjectSpawnDefinition> spawns = new();
		List<Vector2I> playerSpawnCells = new();
		List<Vector2I> enemySpawnCells = new();
		int playerCounter = 0;
		int enemyCounter = 0;
		int obstacleCounter = 0;

		foreach (Vector2I cell in _markerLayer.GetUsedCells())
		{
			int sourceId = _markerLayer.GetCellSourceId(cell);
			if (sourceId != MarkerSourceId)
			{
				continue;
			}

			int tileId = _markerLayer.GetCellAlternativeTile(cell);
			if (tileId == PlayerMarkerTileId)
			{
				playerCounter++;
				playerSpawnCells.Add(cell);
				spawns.Add(CreatePlayerSpawn(playerCounter, cell));
				continue;
			}

			if (tileId == EnemyMarkerTileId)
			{
				enemyCounter++;
				enemySpawnCells.Add(cell);
				spawns.Add(CreateEnemySpawn(enemyCounter, cell, enemyDefinitionId));
				continue;
			}

		}

		foreach (Vector2I cell in DefaultDestructibleObstacleCells)
		{
			obstacleCounter++;
			spawns.Add(CreateDestructibleObstacleSpawn(obstacleCounter, cell));
		}

		foreach (Vector2I cell in DefaultIndestructibleObstacleCells)
		{
			obstacleCounter++;
			spawns.Add(CreateIndestructibleObstacleSpawn(obstacleCounter, cell));
		}

		foreach (Vector2I cell in DefaultSlowPassObstacleCells)
		{
			obstacleCounter++;
			spawns.Add(CreateSlowPassObstacleSpawn(obstacleCounter, cell));
		}

		return new RoomLayoutDefinition
		{
			LayoutId = LayoutId,
			BoardSize = BoardSize,
			DefaultTerrainId = DefaultTerrainId,
			DefaultMoveCost = DefaultMoveCost,
			Tags = RoomTags,
			PlayerSpawnCells = playerSpawnCells.ToArray(),
			EnemySpawnCells = enemySpawnCells.ToArray(),
			ObjectSpawns = spawns.ToArray(),
		};
	}

	public void SyncMarkersFromBoard(BoardObjectRegistry registry)
	{
		EnsureReferences();
		EnsureEditableTileSet();

		_markerLayer.Clear();

		foreach (BoardObject boardObject in registry.AllObjects)
		{
			int tileId = boardObject.ObjectType switch
			{
				BoardObjectType.Unit when boardObject.HasTag(PlayerTag.ToString()) => PlayerMarkerTileId,
				BoardObjectType.Unit => EnemyMarkerTileId,
				_ => ResolveObstacleMarkerTileId(boardObject),
			};

			_markerLayer.SetCell(boardObject.Cell, MarkerSourceId, Vector2I.Zero, tileId);
		}

		_markerLayer.NotifyRuntimeTileDataUpdate();
	}

	public bool TryScreenToCell(Vector2 globalPosition, out Vector2I cell)
	{
		Vector2 floorLocalPosition = _floorLayer.ToLocal(globalPosition);
		cell = _floorLayer.LocalToMap(floorLocalPosition);
		return _topology.IsInsideBoard(cell);
	}

	public Vector2 CellToLocalCenter(Vector2I cell)
	{
		Vector2 floorLocalCenter = _floorLayer.MapToLocal(cell);
		Vector2 floorGlobalCenter = _floorLayer.ToGlobal(floorLocalCenter);
		return ToLocal(floorGlobalCenter);
	}

	public Rect2 GetCellRect(Vector2I cell)
	{
		Vector2 localCenter = CellToLocalCenter(cell);
		Vector2 cellSize = new(CellSizePixels, CellSizePixels);
		return new Rect2(localCenter - cellSize * 0.5f, cellSize);
	}

	public bool SupportsEnemyTypes(IReadOnlyList<string> enemyTypeIds, out bool exactMatch)
	{
		exactMatch = false;

		if (enemyTypeIds.Count == 0)
		{
			return true;
		}

		HashSet<string> supported = new(SupportedEnemyTypeIds, StringComparer.OrdinalIgnoreCase);
		if (supported.Count == 0)
		{
			return false;
		}

		int matchCount = 0;
		foreach (string enemyTypeId in enemyTypeIds)
		{
			if (supported.Contains(enemyTypeId))
			{
				matchCount++;
			}
		}

		exactMatch = matchCount == enemyTypeIds.Count;
		return matchCount > 0;
	}

	private void EnsureReferences()
	{
		_floorLayer ??= GetNode<TileMapLayer>("FloorLayer");
		_markerLayer ??= GetNode<TileMapLayer>("MarkerLayer");
		_escapeLayer ??= GetNode<TileMapLayer>("EscapeLayer");
	}

	private void EnsureTopology()
	{
		_topology = new BoardTopology(BoardSize, CellSizePixels);
	}

	private void EnsureEditableTileSet()
	{
		if (_floorLayer.TileSet != null && _markerLayer.TileSet != null)
		{
			if (_escapeLayer.TileSet == null)
			{
				_escapeLayer.TileSet = _floorLayer.TileSet;
			}
			return;
		}

		Texture2D floorTexture = GD.Load<Texture2D>("res://Assets/Tilemap/CosmicLegacy_PetricakeGamesPNG.png");
		PackedScene playerScene = GD.Load<PackedScene>("res://Scene/Battle/Tiles/BattlePlayerToken.tscn");
		PackedScene enemyScene = GD.Load<PackedScene>("res://Scene/Battle/Tiles/BattleEnemyToken.tscn");
		PackedScene obstacleScene = GD.Load<PackedScene>("res://Scene/Battle/Tiles/BattleObstacleToken.tscn");
		PackedScene escapeScene = GD.Load<PackedScene>("res://Scene/Battle/Tiles/BattleEscapeTile.tscn");

		TileSet tileSet = BattleRoomTileSetFactory.CreateTileSet(
			floorTexture,
			playerScene,
			enemyScene,
			obstacleScene,
			escapeScene,
			CellSizePixels);

		_floorLayer.TileSet = tileSet;
		_markerLayer.TileSet = tileSet;
		_escapeLayer.TileSet = tileSet;
	}

	private void EnsureDefaultPaint()
	{
		if (_floorLayer.GetUsedCells().Count == 0)
		{
			for (int y = 0; y < BoardSize.Y; y++)
			{
				for (int x = 0; x < BoardSize.X; x++)
				{
					_floorLayer.SetCell(new Vector2I(x, y), FloorSourceId, DefaultFloorAtlasCoords, 0);
				}
			}
		}

		if (_markerLayer.GetUsedCells().Count == 0)
		{
			PaintMarker(DefaultPlayerCell, PlayerMarkerTileId);

			foreach (Vector2I cell in DefaultEnemyCells)
			{
				PaintMarker(cell, EnemyMarkerTileId);
			}

			foreach (Vector2I cell in DefaultDestructibleObstacleCells)
			{
				PaintMarker(cell, DestructibleObstacleMarkerTileId);
			}

			foreach (Vector2I cell in DefaultIndestructibleObstacleCells)
			{
				PaintMarker(cell, IndestructibleObstacleMarkerTileId);
			}

			foreach (Vector2I cell in DefaultSlowPassObstacleCells)
			{
				PaintMarker(cell, SlowPassObstacleMarkerTileId);
			}
		}

		if (_escapeLayer.GetUsedCells().Count == 0)
		{
			foreach (Vector2I cell in DefaultEscapeCells)
			{
				PaintEscapeMarker(cell);
			}
		}
	}

	private void PaintMarker(Vector2I cell, int tileId)
	{
		if (!_topology.IsInsideBoard(cell))
		{
			return;
		}

		_markerLayer.SetCell(cell, MarkerSourceId, Vector2I.Zero, tileId);
	}

	private void PaintEscapeMarker(Vector2I cell)
	{
		if (!_topology.IsInsideBoard(cell))
		{
			return;
		}

		_escapeLayer.SetCell(cell, EscapeSourceId, Vector2I.Zero, EscapeMarkerTileId);
	}

	private int ResolveObstacleMarkerTileId(BoardObject boardObject)
	{
		if (boardObject.HasTag(SlowPassObstacleTag.ToString()))
		{
			return SlowPassObstacleMarkerTileId;
		}

		if (boardObject.HasTag(IndestructibleObstacleTag.ToString()))
		{
			return IndestructibleObstacleMarkerTileId;
		}

		return DestructibleObstacleMarkerTileId;
	}

	private static BoardObjectSpawnDefinition CreatePlayerSpawn(int index, Vector2I cell)
	{
		return new BoardObjectSpawnDefinition
		{
			ObjectId = $"player_{index:00}",
			DefinitionId = "battle_player",
			ObjectType = BoardObjectType.Unit,
			Cell = cell,
			Faction = BoardObjectFaction.Player,
			Tags = new[] { PlayerTag.ToString() },
			StackableWithUnit = false,
		};
	}

	private static BoardObjectSpawnDefinition CreateEnemySpawn(int index, Vector2I cell, string definitionId)
	{
		string resolvedDefinitionId = string.IsNullOrWhiteSpace(definitionId) ? "battle_enemy" : definitionId;

		return new BoardObjectSpawnDefinition
		{
			ObjectId = $"enemy_{index:00}",
			DefinitionId = resolvedDefinitionId,
			AiId = "melee_basic",
			ObjectType = BoardObjectType.Unit,
			Cell = cell,
			Faction = BoardObjectFaction.Enemy,
			Tags = new[] { EnemyTag.ToString(), resolvedDefinitionId },
			MaxShield = 2,
			CurrentShield = 2,
			StackableWithUnit = false,
		};
	}

	private static BoardObjectSpawnDefinition CreateDestructibleObstacleSpawn(int index, Vector2I cell)
	{
		return new BoardObjectSpawnDefinition
		{
			ObjectId = $"obstacle_destructible_{index:00}",
			DefinitionId = "battle_obstacle_destructible",
			ObjectType = BoardObjectType.Obstacle,
			Cell = cell,
			Faction = BoardObjectFaction.World,
			Tags = new[] { ObstacleTag.ToString(), DestructibleObstacleTag.ToString() },
			MaxHp = 3,
			CurrentHp = 3,
			BlocksMovement = true,
			BlocksLineOfSight = true,
			StackableWithUnit = false,
		};
	}

	private static BoardObjectSpawnDefinition CreateIndestructibleObstacleSpawn(int index, Vector2I cell)
	{
		return new BoardObjectSpawnDefinition
		{
			ObjectId = $"obstacle_wall_{index:00}",
			DefinitionId = "battle_obstacle_wall",
			ObjectType = BoardObjectType.Obstacle,
			Cell = cell,
			Faction = BoardObjectFaction.World,
			Tags = new[] { ObstacleTag.ToString(), IndestructibleObstacleTag.ToString() },
			BlocksMovement = true,
			BlocksLineOfSight = true,
			StackableWithUnit = false,
		};
	}

	private static BoardObjectSpawnDefinition CreateSlowPassObstacleSpawn(int index, Vector2I cell)
	{
		return new BoardObjectSpawnDefinition
		{
			ObjectId = $"obstacle_slow_{index:00}",
			DefinitionId = "battle_obstacle_slow",
			ObjectType = BoardObjectType.Obstacle,
			Cell = cell,
			Faction = BoardObjectFaction.World,
			Tags = new[] { ObstacleTag.ToString(), SlowPassObstacleTag.ToString() },
			BlocksMovement = false,
			BlocksLineOfSight = false,
			StackableWithUnit = true,
			MoveCostModifier = 1,
		};
	}
}
