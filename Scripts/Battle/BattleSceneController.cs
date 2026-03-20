using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.Data;
using CardChessDemo.Battle.Presentation;
using CardChessDemo.Battle.Rooms;
using CardChessDemo.Battle.Shared;
using CardChessDemo.Battle.State;
using CardChessDemo.Battle.Turn;
using CardChessDemo.Battle.UI;
using CardChessDemo.Battle.Visual;

namespace CardChessDemo.Battle;

public partial class BattleSceneController : Node2D
{
	[Export] public PackedScene? ForcedBattleRoomScene { get; set; }
	[Export] public PackedScene[] BattleRoomScenes { get; set; } = Array.Empty<PackedScene>();
	[Export] public BattleRoomPoolDefinition? BattleRoomPools { get; set; }
	[Export] public BattlePrefabLibrary? BattlePrefabLibrary { get; set; }
	[Export] public string[] EncounterEnemyTypeIds { get; set; } = { "grunt" };
	[Export] public int RandomSeed { get; set; } = 1337;
	[Export] public float CameraZoom { get; set; } = 1.0f;
	[Export] public int CameraTopMarginPixels { get; set; } = 8;

	public BoardState? BoardState { get; private set; }
	public BoardObjectRegistry? Registry { get; private set; }
	public BoardQueryService? QueryService { get; private set; }
	public TurnActionState? TurnState { get; private set; }
	public BattleRoomTemplate? CurrentRoom { get; private set; }
	public GlobalGameSession? GlobalSession { get; private set; }
	public BattleObjectStateManager? StateManager { get; private set; }

	private RandomNumberGenerator _rng = new();
	private BattlePieceViewManager? _pieceViewManager;
	private BattleHudController? _hud;

	public override void _Ready()
	{
		_rng.Seed = (ulong)Math.Max(RandomSeed, 1);

		GlobalSession = GetNodeOrNull<GlobalGameSession>("/root/GlobalGameSession");
		BattlePrefabLibrary ??= GD.Load<BattlePrefabLibrary>("res://Resources/Battle/Presentation/DefaultBattlePrefabLibrary.tres");

		BoardState = new BoardState();
		Registry = new BoardObjectRegistry();
		QueryService = new BoardQueryService(BoardState, Registry);
		TurnState = new TurnActionState();
		TurnState.StartNewTurn(1);

		CurrentRoom = InstantiateSelectedRoom();
		Node2D roomContainer = GetNode<Node2D>("RoomContainer");
		roomContainer.AddChild(CurrentRoom);
		roomContainer.MoveChild(CurrentRoom, 0);

		RoomLayoutDefinition layout = CurrentRoom.BuildLayoutDefinition();
		BoardInitializer initializer = new(BoardState, Registry);
		initializer.InitializeFromLayout(layout);

		if (GlobalSession == null || BattlePrefabLibrary == null)
		{
			throw new InvalidOperationException("BattleSceneController: GlobalSession or BattlePrefabLibrary is missing.");
		}

		StateManager = new BattleObjectStateManager(Registry, BattlePrefabLibrary, GlobalSession);
		StateManager.Initialize();
		StateManager.SyncAllFromRegistry();

		_pieceViewManager = new BattlePieceViewManager(GetNode<Node>("RoomContainer/PieceRoot"), BattlePrefabLibrary);
		_pieceViewManager.Rebuild(Registry, StateManager, CurrentRoom);

		BattleBoardOverlay? overlay = GetNodeOrNull<BattleBoardOverlay>("RoomContainer/BoardOverlay");
		overlay?.Bind(CurrentRoom);

		_hud = GetNodeOrNull<BattleHudController>("BattleHud");
		if (_hud != null)
		{
			_hud.Bind(StateManager, GlobalSession, TurnState);
			_hud.EndTurnRequested += OnEndTurnRequested;
			_hud.MovePointDeltaRequested += OnMovePointDeltaRequested;
		}

		ConfigureCameraForBattle();

		GlobalSession.PlayerRuntimeChanged += OnPlayerRuntimeChanged;

		GD.Print($"BattleSceneController: layout={layout.LayoutId}, size={layout.BoardSize}, objects={Registry.Count}");
	}

	public override void _ExitTree()
	{
		if (GlobalSession != null)
		{
			GlobalSession.PlayerRuntimeChanged -= OnPlayerRuntimeChanged;
		}

		if (_hud != null)
		{
			_hud.EndTurnRequested -= OnEndTurnRequested;
			_hud.MovePointDeltaRequested -= OnMovePointDeltaRequested;
		}
	}

	public override void _Process(double delta)
	{
		if (Registry == null || CurrentRoom == null || StateManager == null)
		{
			return;
		}

		StateManager.SyncAllFromRegistry();

		BattleBoardOverlay? overlay = GetNodeOrNull<BattleBoardOverlay>("RoomContainer/BoardOverlay");
		if (overlay == null)
		{
			return;
		}

		BattleObjectState? playerState = StateManager.GetPrimaryPlayerState();
		if (playerState == null)
		{
			overlay.SetReachableCells(Array.Empty<Vector2I>());
			overlay.SetPreviewPath(Array.Empty<Vector2I>());
			return;
		}

		if (!CanPlayerMoveThisTurn())
		{
			overlay.SetReachableCells(Array.Empty<Vector2I>());
			overlay.SetPreviewPath(Array.Empty<Vector2I>());
			return;
		}

		overlay.SetReachableCells(BuildReachableCells(playerState.Cell, playerState.MovePointsPerTurn));

		if (CurrentRoom.TryScreenToCell(GetGlobalMousePosition(), out Vector2I hoveredCell))
		{
			overlay.SetPreviewPath(BuildPreviewPath(playerState.Cell, hoveredCell, playerState.MovePointsPerTurn));
		}
		else
		{
			overlay.SetPreviewPath(Array.Empty<Vector2I>());
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && GlobalSession != null)
		{
			if (keyEvent.Keycode == Key.Pageup)
			{
				GlobalSession.ApplyMovePointDelta(1);
				return;
			}

			if (keyEvent.Keycode == Key.Pagedown)
			{
				GlobalSession.ApplyMovePointDelta(-1);
				return;
			}

			if (keyEvent.Keycode == Key.T || keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.KpEnter)
			{
				AdvanceTurn();
				return;
			}
		}

		if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed || mouseButton.ButtonIndex != MouseButton.Left)
		{
			return;
		}

		if (CurrentRoom == null || StateManager == null)
		{
			return;
		}

		BattleObjectState? playerState = StateManager.GetPrimaryPlayerState();
		if (playerState == null)
		{
			return;
		}

		if (!CurrentRoom.TryScreenToCell(GetGlobalMousePosition(), out Vector2I targetCell))
		{
			return;
		}

		if (!CanPlayerMoveThisTurn())
		{
			return;
		}

		if (!BuildReachableCells(playerState.Cell, playerState.MovePointsPerTurn).Contains(targetCell))
		{
			return;
		}

		TryMoveObject(playerState.ObjectId, targetCell, out _);
	}

	public bool TryMoveObject(string objectId, Vector2I targetCell, out string failureReason)
	{
		failureReason = "BoardQueryService has not been initialized.";

		if (QueryService == null || Registry == null || CurrentRoom == null || StateManager == null || _pieceViewManager == null)
		{
			return false;
		}

		BattleObjectState? playerState = StateManager.GetPrimaryPlayerState();
		bool isPlayerObject = playerState?.ObjectId == objectId;
		if (isPlayerObject && !CanPlayerMoveThisTurn())
		{
			failureReason = TurnState?.HasEndedTurn == true
				? "The current turn has already ended."
				: "The player has already moved this turn.";
			return false;
		}

		bool moved = QueryService.TryMoveObject(objectId, targetCell, out failureReason);
		if (moved)
		{
			if (isPlayerObject)
			{
				TurnState?.MarkMoved();
			}

			StateManager.SyncAllFromRegistry();
			_pieceViewManager.Sync(Registry, StateManager, CurrentRoom);
			_pieceViewManager.PlayMove(objectId);
		}

		return moved;
	}

	private void OnPlayerRuntimeChanged()
	{
		StateManager?.SyncPlayerFromSession();
	}

	private void OnEndTurnRequested()
	{
		AdvanceTurn();
	}

	private void OnMovePointDeltaRequested(int delta)
	{
		GlobalSession?.ApplyMovePointDelta(delta);
	}

	private bool CanPlayerMoveThisTurn()
	{
		return TurnState?.CanMove != false;
	}

	private void AdvanceTurn()
	{
		if (TurnState == null)
		{
			return;
		}

		if (!TurnState.HasEndedTurn)
		{
			TurnState.MarkEndedTurn();
			return;
		}

		TurnState.AdvanceToNextTurn();
	}

	private void ConfigureCameraForBattle()
	{
		if (CurrentRoom == null)
		{
			return;
		}

		Camera2D? camera = GetNodeOrNull<Camera2D>("Camera2D");
		if (camera == null)
		{
			return;
		}

		Vector2 viewportSize = GetViewportRect().Size;
		float boardWidth = CurrentRoom.BoardSize.X * CurrentRoom.CellSizePixels;
		float boardHeight = CurrentRoom.BoardSize.Y * CurrentRoom.CellSizePixels;
		Vector2 boardOrigin = CurrentRoom.ToGlobal(Vector2.Zero);
		Vector2 boardCenter = boardOrigin + new Vector2(boardWidth * 0.5f, boardHeight * 0.5f);
		float initialTopMargin = Mathf.Max(0.0f, (viewportSize.Y - boardHeight) * 0.5f);
		float targetTopMargin = Mathf.Max(0.0f, CameraTopMarginPixels);
		float cameraYOffset = Mathf.Max(0.0f, initialTopMargin - targetTopMargin);

		camera.Enabled = true;
		camera.Zoom = new Vector2(CameraZoom, CameraZoom);
		camera.Position = boardCenter + new Vector2(0.0f, cameraYOffset);
	}

	private BattleRoomTemplate InstantiateSelectedRoom()
	{
		PackedScene roomScene = SelectRoomScene();
		return roomScene.Instantiate<BattleRoomTemplate>();
	}

	private PackedScene SelectRoomScene()
	{
		if (ForcedBattleRoomScene != null)
		{
			return ForcedBattleRoomScene;
		}

		List<PackedScene> exactMatches = new();
		List<PackedScene> partialMatches = new();
		List<PackedScene> fallbackMatches = new();

		foreach (PackedScene scene in ExpandRoomScenePool())
		{
			BattleRoomTemplate previewRoom = scene.Instantiate<BattleRoomTemplate>();

			if (previewRoom.SupportedEnemyTypeIds.Length == 0)
			{
				fallbackMatches.Add(scene);
				previewRoom.Free();
				continue;
			}

			bool matches = previewRoom.SupportsEnemyTypes(EncounterEnemyTypeIds, out bool exactMatch);
			previewRoom.Free();

			if (!matches)
			{
				continue;
			}

			if (exactMatch)
			{
				exactMatches.Add(scene);
			}
			else
			{
				partialMatches.Add(scene);
			}
		}

		List<PackedScene> candidatePool = exactMatches.Count > 0
			? exactMatches
			: partialMatches.Count > 0
				? partialMatches
				: fallbackMatches.Count > 0
					? fallbackMatches
					: BattleRoomScenes.ToList();

		if (candidatePool.Count == 0)
		{
			throw new InvalidOperationException("BattleSceneController: no battle room scenes are configured.");
		}

		return candidatePool[_rng.RandiRange(0, candidatePool.Count - 1)];
	}

	private IEnumerable<PackedScene> ExpandRoomScenePool()
	{
		HashSet<PackedScene> pooledScenes = new();

		if (BattleRoomPools != null)
		{
			foreach (BattleRoomPoolEntry entry in BattleRoomPools.Entries)
			{
				foreach (PackedScene scene in entry.RoomScenes)
				{
					if (scene != null)
					{
						pooledScenes.Add(scene);
					}
				}
			}
		}

		if (pooledScenes.Count > 0)
		{
			return pooledScenes;
		}

		HashSet<PackedScene> directScenes = new();
		foreach (PackedScene scene in BattleRoomScenes)
		{
			if (scene != null)
			{
				directScenes.Add(scene);
			}
		}

		return directScenes;
	}

	private List<Vector2I> BuildReachableCells(Vector2I origin, int moveRange)
	{
		List<Vector2I> cells = new();
		if (BoardState == null)
		{
			return cells;
		}

		for (int y = 0; y < BoardState.Size.Y; y++)
		{
			for (int x = 0; x < BoardState.Size.X; x++)
			{
				Vector2I cell = new(x, y);
				int distance = Mathf.Abs(cell.X - origin.X) + Mathf.Abs(cell.Y - origin.Y);
				if (distance <= moveRange)
				{
					cells.Add(cell);
				}
			}
		}

		return cells;
	}

	private static List<Vector2I> BuildPreviewPath(Vector2I start, Vector2I end, int moveRange)
	{
		List<Vector2I> path = new() { start };
		Vector2I current = start;

		while (current.X != end.X)
		{
			current = new Vector2I(current.X + Math.Sign(end.X - current.X), current.Y);
			path.Add(current);
			if (path.Count - 1 >= moveRange)
			{
				return path;
			}
		}

		while (current.Y != end.Y)
		{
			current = new Vector2I(current.X, current.Y + Math.Sign(end.Y - current.Y));
			path.Add(current);
			if (path.Count - 1 >= moveRange)
			{
				return path;
			}
		}

		return path;
	}
}
