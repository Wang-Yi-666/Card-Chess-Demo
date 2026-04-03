using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CardChessDemo.Battle.Actions;
using CardChessDemo.Battle.AI;
using CardChessDemo.Battle.Arakawa;
using CardChessDemo.Battle.Board;
using CardChessDemo.Battle.Boundary;
using CardChessDemo.Battle.Cards;
using CardChessDemo.Battle.Data;
using CardChessDemo.Battle.Encounters;
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
	private const double PlayerActionResolveBufferSeconds = 0.24d;
	private const string BattleBackgroundTexturePath = "res://Assets/Background/94180512_p2_master1200.jpg";
	private const string BattleReturnTransitionOverlayScenePath = "res://Scene/Transitions/BattleReturnTransitionOverlay.tscn";
	private static readonly ArakawaAbilityDefinition BuildWallAbility = new("build_wall", "造墙", 1);
	private static readonly ArakawaAbilityDefinition EnhanceCardAbility = new("enhance_card", "强化", 1);
	private static readonly IReadOnlyDictionary<string, BattleCardEnhancementDefinition> PrototypeCardEnhancements =
		new Dictionary<string, BattleCardEnhancementDefinition>(StringComparer.Ordinal)
		{
			["cross_slash"] = new BattleCardEnhancementDefinition("+", "伤害+2", damageDelta: 2),
			["quick_cut"] = new BattleCardEnhancementDefinition("+", "伤害+1", damageDelta: 1),
			["line_shot"] = new BattleCardEnhancementDefinition("+", "伤害+2", damageDelta: 2),
			["heavy_shot"] = new BattleCardEnhancementDefinition("+", "伤害+2", damageDelta: 2),
			["battle_read"] = new BattleCardEnhancementDefinition("+", "抽牌+1", drawCountDelta: 1),
			["meditate"] = new BattleCardEnhancementDefinition("+", "抽牌+1", drawCountDelta: 1),
			["surge"] = new BattleCardEnhancementDefinition("+", "能量+1", energyGainDelta: 1),
			["draw_spark"] = new BattleCardEnhancementDefinition("+", "抽牌+1", drawCountDelta: 1),
			["quick_plan"] = new BattleCardEnhancementDefinition("+", "抽牌+1", drawCountDelta: 1),
			["burning_edge"] = new BattleCardEnhancementDefinition("+", "伤害+2", damageDelta: 2),
			["hook_shot"] = new BattleCardEnhancementDefinition("+", "伤害+2", damageDelta: 2),
			["deep_focus"] = new BattleCardEnhancementDefinition("+", "抽牌+1", drawCountDelta: 1),
			["spark_charge"] = new BattleCardEnhancementDefinition("+", "能量+1", energyGainDelta: 1),
			["burst_drive"] = new BattleCardEnhancementDefinition("+", "能量+1", energyGainDelta: 1),
			["guard_up"] = new BattleCardEnhancementDefinition("+", "护盾+2", shieldGainDelta: 2),
			["brace"] = new BattleCardEnhancementDefinition("+", "护盾+3", shieldGainDelta: 3),
			["quick_guard"] = new BattleCardEnhancementDefinition("+", "护盾+2", shieldGainDelta: 2),
		};
	[Export] public PackedScene? ForcedBattleRoomScene { get; set; }
	[Export] public PackedScene[] BattleRoomScenes { get; set; } = Array.Empty<PackedScene>();
	[Export] public BattleRoomPoolDefinition? BattleRoomPools { get; set; }
	[Export] public BattlePrefabLibrary? BattlePrefabLibrary { get; set; }
	[Export] public BattleEncounterLibrary? EncounterLibrary { get; set; }
	[Export] public BattleCardLibrary? BattleCardLibrary { get; set; }
	[Export] public BattleDeckBuildRules? BattleDeckBuildRules { get; set; }
	[Export] public string EncounterId { get; set; } = string.Empty;
	[Export] public string[] EncounterEnemyTypeIds { get; set; } = { "grunt" };
	[Export] public string EncounterEnemyDefinitionId { get; set; } = "battle_enemy";
	[Export] public int RandomSeed { get; set; } = 1337;
	[Export] public float CameraZoom { get; set; } = 1.0f;
	[Export] public int CameraTopMarginPixels { get; set; } = 8;
	[Export] public int CameraBottomMarginPixels { get; set; } = 52;
	[Export(PropertyHint.Range, "4,64,1")] public int CameraEdgePanMarginPixels { get; set; } = 22;
	[Export(PropertyHint.Range, "16,320,1")] public int CameraResetDurationMs { get; set; } = 180;
	[Export(PropertyHint.Range, "20,480,1")] public float CameraPanPixelsPerSecond { get; set; } = 160.0f;
	[Export(PropertyHint.Range, "0.1,1.0,0.05")] public float CameraMinBoardVisibleRatio { get; set; } = 0.8f;
	[Export(PropertyHint.Range, "1,4,1")] public float CameraFocusZoomMultiplier { get; set; } = 2.0f;
	[Export(PropertyHint.Range, "0.02,1.2,0.01")] public float CameraFocusHoldSeconds { get; set; } = 1.8f;
	[Export(PropertyHint.Range, "0.05,3.0,0.05")] public float AttackFocusHoldSeconds { get; set; } = 2.0f;
	[Export(PropertyHint.Range, "0.05,3.0,0.05")] public float ArakawaBuildFocusHoldSeconds { get; set; } = 0.8f;
	[Export] public bool ShowBattleFloorLayer { get; set; } = true;
	[Export] public int PlayerHandSize { get; set; } = 7;
	[Export] public int PlayerEnergyPerTurn { get; set; } = 3;

	public BoardState? BoardState { get; private set; }
	public BoardObjectRegistry? Registry { get; private set; }
	public BoardQueryService? QueryService { get; private set; }
	public BoardPathfinder? Pathfinder { get; private set; }
	public BoardTargetingService? TargetingService { get; private set; }
	public TurnActionState? TurnState { get; private set; }
	public BattleRoomTemplate? CurrentRoom { get; private set; }
	public GlobalGameSession? GlobalSession { get; private set; }
	public BattleObjectStateManager? StateManager { get; private set; }

	private RandomNumberGenerator _rng = new();
	private BattlePieceViewManager? _pieceViewManager;
	private BattleFloatingTextLayer? _floatingTextLayer;
	private BattleActionService? _actionService;
	private EnemyTurnResolver? _enemyTurnResolver;
	private BattleHudController? _hud;
	private BattleDeckState? _playerDeck;
	private Control? _battleFailOverlay;
	private ColorRect? _battleFailFlash;
	private Label? _battleFailLabel;
	private bool _battleFailureSequenceStarted;
	private bool _battleResultCommitted;
	private bool _isArakawaWheelOpen;
	private ArakawaAbilityMode _arakawaAbilityMode = ArakawaAbilityMode.None;
	private BattleRequest? _activeBattleRequest;
	private bool _retreatPending;
	private int _retreatTurnIndex = -1;
	private int _retreatStartHp = -1;
	private bool _isPlayerMoveResolving;
	private Camera2D? _battleCamera;
	private Sprite2D? _battleBackground;
	private Rect2 _cameraPanBounds = new();
	private Vector2 _cameraRestPosition = Vector2.Zero;
	private Tween? _cameraResetTween;
	private Tween? _cameraCinematicTween;
	private bool _isCameraCinematicBusy;
	private readonly List<string> _currentTurnActionLogEntries = new();
	private readonly List<string> _previousTurnActionLogEntries = new();
	private int _currentTurnActionLogTurnIndex = 1;
	private int _previousTurnActionLogTurnIndex;

	public override void _Ready()
	{
		_rng.Seed = (ulong)Math.Max(RandomSeed, 1);

		GlobalSession = GetNodeOrNull<GlobalGameSession>("/root/GlobalGameSession");
		ApplyPendingBattleRequest();
		ApplyPendingEncounterId();
		BattlePrefabLibrary ??= GD.Load<BattlePrefabLibrary>("res://Resources/Battle/Presentation/DefaultBattlePrefabLibrary.tres");
		EncounterLibrary ??= GD.Load<BattleEncounterLibrary>("res://Resources/Battle/Encounters/DebugBattleEncounterLibrary.tres");
		BattleCardLibrary ??= GD.Load<BattleCardLibrary>("res://Resources/Battle/Cards/DefaultBattleCardLibrary.tres");
		BattleDeckBuildRules ??= GD.Load<BattleDeckBuildRules>("res://Resources/Battle/Cards/DefaultBattleDeckBuildRules.tres");
		GlobalSession?.EnsureDeckBuildInitialized(BattleCardLibrary);
		ResolveEncounterConfiguration();

		BoardState = new BoardState();
		Registry = new BoardObjectRegistry();
		QueryService = new BoardQueryService(BoardState, Registry);
		TurnState = new TurnActionState();
		TurnState.StartNewTurn(1);
		_currentTurnActionLogTurnIndex = TurnState.TurnIndex;
		IReadOnlyList<BattleCardDefinition> prototypeDeck = BuildAvailableCardCatalog();
		BattleDeckRuntimeInit? deckRuntimeInit = BuildDeckRuntimeInit(prototypeDeck);
		IReadOnlyList<BattleCardDefinition> battleDeckSource = ResolveBattleDeckSource(prototypeDeck, deckRuntimeInit);
		_playerDeck = new BattleDeckState(
			battleDeckSource,
			(ulong)Math.Max(RandomSeed, 1),
			PlayerHandSize,
			PlayerEnergyPerTurn,
			deckRuntimeInit);
		TurnState.ConfigureEnergyRechargeInterval(_playerDeck.EnergyRegenIntervalTurns);
		_playerDeck.DrawCards(ResolveOpeningDrawCount(deckRuntimeInit, _playerDeck.HandSize));
		_playerDeck.StartPlayerTurn();

		CurrentRoom = InstantiateSelectedRoom();
		Node2D roomContainer = GetNode<Node2D>("RoomContainer");
		roomContainer.AddChild(CurrentRoom);
		roomContainer.MoveChild(CurrentRoom, 0);
		_battleBackground = EnsureBattleBackground(roomContainer);
		AttachBattleBackgroundToRoom();
		ConfigureBattleBackground(roomContainer);
		ConfigureBattleFloorLayerVisibility();

		RoomLayoutDefinition layout = CurrentRoom.BuildLayoutDefinition(EncounterEnemyDefinitionId);
		BoardInitializer initializer = new(BoardState, Registry);
		initializer.InitializeFromLayout(layout);
		Pathfinder = new BoardPathfinder(CurrentRoom.Topology, QueryService);
		TargetingService = new BoardTargetingService(CurrentRoom.Topology, Registry, QueryService);

		if (GlobalSession == null || BattlePrefabLibrary == null)
		{
			throw new InvalidOperationException("BattleSceneController: GlobalSession or BattlePrefabLibrary is missing.");
		}

		StateManager = new BattleObjectStateManager(Registry, BattlePrefabLibrary, GlobalSession);
		StateManager.Initialize();
		StateManager.SyncAllFromRegistry();

		Node pieceRoot = GetNode<Node>("RoomContainer/PieceRoot");
		Node killFxRoot = EnsureRoomLayerRoot("KillFxRoot", false);
		_pieceViewManager = new BattlePieceViewManager(
			pieceRoot,
			killFxRoot,
			BattlePrefabLibrary);
		_pieceViewManager.Rebuild(Registry, StateManager, CurrentRoom);
		_floatingTextLayer = GetNodeOrNull<BattleFloatingTextLayer>("RoomContainer/FloatingTextLayer");
		_actionService = new BattleActionService(BoardState, Registry, QueryService, Pathfinder, StateManager, _pieceViewManager, CurrentRoom, GlobalSession, _floatingTextLayer);
		_actionService.ActionLogged += OnBattleActionLogged;
		_enemyTurnResolver = new EnemyTurnResolver(
			Registry,
			StateManager,
			Pathfinder,
			TargetingService,
			_actionService,
			new EnemyAiRegistry(),
			this);

		BattleBoardOverlay? overlay = GetNodeOrNull<BattleBoardOverlay>("RoomContainer/BoardOverlay");
		overlay?.Bind(CurrentRoom);

		_hud = GetNodeOrNull<BattleHudController>("BattleHud");
		if (_hud != null)
		{
			_hud.Bind(TurnState);
			_hud.AttackRequested += OnAttackRequested;
			_hud.DefendRequested += OnDefendRequested;
			_hud.RetreatRequested += OnRetreatRequested;
			_hud.ArakawaWheelRequested += OnArakawaWheelRequested;
			_hud.ArakawaAbilityRequested += OnArakawaAbilityRequested;
			_hud.ArakawaCancelRequested += OnArakawaCancelRequested;
			_hud.MeditateRequested += OnMeditateRequested;
			_hud.CardRequested += OnCardRequested;
			_hud.EndTurnRequested += OnEndTurnRequested;
			_hud.SetActionLogState(_currentTurnActionLogTurnIndex, _currentTurnActionLogEntries, _previousTurnActionLogTurnIndex, _previousTurnActionLogEntries);
		}

		_battleFailOverlay = GetNodeOrNull<Control>("BattleFailOverlay");
		_battleFailFlash = GetNodeOrNull<ColorRect>("BattleFailOverlay/Flash");
		_battleFailLabel = GetNodeOrNull<Label>("BattleFailOverlay/DefeatLabel");

		GlobalSession.PlayerRuntimeChanged += OnPlayerRuntimeChanged;
		GlobalSession.ArakawaRuntimeChanged += OnArakawaRuntimeChanged;
		ConfigureCameraForBattle();

		GD.Print($"BattleSceneController: layout={layout.LayoutId}, size={layout.BoardSize}, objects={Registry.Count}");
	}

	private Sprite2D? EnsureBattleBackground(Node2D roomContainer)
	{
		Sprite2D? existing = roomContainer.GetNodeOrNull<Sprite2D>("BattleBackground");
		if (existing != null)
		{
			return existing;
		}

		Texture2D? texture = GD.Load<Texture2D>(BattleBackgroundTexturePath);
		if (texture == null)
		{
			GD.Print($"BattleSceneController: failed to load background texture at {BattleBackgroundTexturePath}.");
			return null;
		}

		Sprite2D background = new()
		{
			Name = "BattleBackground",
			Texture = texture,
			Centered = true,
			ZIndex = 0,
		};
		roomContainer.AddChild(background);
		GD.Print($"BattleSceneController: battle background created at runtime from {BattleBackgroundTexturePath}.");
		return background;
	}

	private Node EnsureRoomLayerRoot(string nodeName, bool ySortEnabled)
	{
		Node2D roomContainer = GetNode<Node2D>("RoomContainer");
		if (roomContainer.GetNodeOrNull<Node>(nodeName) is Node existing)
		{
			return existing;
		}

		Node2D created = new()
		{
			Name = nodeName,
			YSortEnabled = ySortEnabled,
		};
		roomContainer.AddChild(created);
		GD.Print($"BattleSceneController: created missing room layer root '{nodeName}' at runtime.");
		return created;
	}

	private void ConfigureBattleBackground(Node2D roomContainer)
	{
		if (_battleBackground == null || CurrentRoom == null || _battleBackground.Texture == null)
		{
			GD.Print("BattleSceneController: battle background missing or texture unresolved.");
			return;
		}

		Vector2 boardPixelSize = new(
			CurrentRoom.BoardSize.X * CurrentRoom.CellSizePixels,
			CurrentRoom.BoardSize.Y * CurrentRoom.CellSizePixels);
		Vector2 textureSize = _battleBackground.Texture.GetSize();
		if (textureSize.X <= 0.0f || textureSize.Y <= 0.0f)
		{
			return;
		}

		const float horizontalPadding = 128.0f;
		const float verticalPadding = 128.0f;
		float scale = Math.Max(
			(boardPixelSize.X + horizontalPadding) / textureSize.X,
			(boardPixelSize.Y + verticalPadding) / textureSize.Y);

		_battleBackground.Scale = new Vector2(scale, scale);
		_battleBackground.Position = boardPixelSize * 0.5f;
		GD.Print(
			$"BattleSceneController: background configured parent={_battleBackground.GetParent()?.Name}, " +
			$"pos={_battleBackground.Position}, scale={_battleBackground.Scale}, " +
			$"size={textureSize}, visible={_battleBackground.Visible}, z={_battleBackground.ZIndex}");
	}

	private void AttachBattleBackgroundToRoom()
	{
		if (_battleBackground == null || CurrentRoom == null)
		{
			GD.Print("BattleSceneController: battle background node or current room missing before attach.");
			return;
		}

		_battleBackground.Reparent(CurrentRoom, false);

		int insertionIndex = 0;
		if (CurrentRoom.GetNodeOrNull<Node>("FloorLayer") is Node floorLayer)
		{
			insertionIndex = floorLayer.GetIndex();
		}

		CurrentRoom.MoveChild(_battleBackground, insertionIndex);
		GD.Print(
			$"BattleSceneController: background attached to room={CurrentRoom.Name}, " +
			$"childIndex={_battleBackground.GetIndex()}, insertionIndex={insertionIndex}");
	}

	private void ConfigureBattleFloorLayerVisibility()
	{
		if (CurrentRoom?.GetNodeOrNull<TileMapLayer>("FloorLayer") is not TileMapLayer floorLayer)
		{
			GD.Print("BattleSceneController: floor layer not found when configuring floor visibility.");
			return;
		}

		floorLayer.Visible = ShowBattleFloorLayer;
		GD.Print($"BattleSceneController: floor layer visible={floorLayer.Visible}");
	}

	public override void _ExitTree()
	{
		if (GlobalSession != null)
		{
			GlobalSession.PlayerRuntimeChanged -= OnPlayerRuntimeChanged;
			GlobalSession.ArakawaRuntimeChanged -= OnArakawaRuntimeChanged;
		}

		if (_hud != null)
		{
			_hud.AttackRequested -= OnAttackRequested;
			_hud.DefendRequested -= OnDefendRequested;
			_hud.RetreatRequested -= OnRetreatRequested;
			_hud.ArakawaWheelRequested -= OnArakawaWheelRequested;
			_hud.ArakawaAbilityRequested -= OnArakawaAbilityRequested;
			_hud.ArakawaCancelRequested -= OnArakawaCancelRequested;
			_hud.MeditateRequested -= OnMeditateRequested;
			_hud.CardRequested -= OnCardRequested;
			_hud.EndTurnRequested -= OnEndTurnRequested;
		}

		if (_actionService != null)
		{
			_actionService.ActionLogged -= OnBattleActionLogged;
		}
	}

	public override void _Process(double delta)
	{
		if (Registry == null || CurrentRoom == null || StateManager == null)
		{
			return;
		}

		StateManager.SyncAllFromRegistry();
		if (!_battleFailureSequenceStarted && GlobalSession?.PlayerCurrentHp <= 0)
		{
			StartBattleFailureSequence();
		}

		bool hasHoveredCell = CurrentRoom.TryScreenToCell(GetGlobalMousePosition(), out Vector2I hoveredCell);
		Vector2 hoverScreenPosition = GetViewport().GetMousePosition();
		_hud?.SetHoveredUnitState(hasHoveredCell ? GetHoveredObjectState(hoveredCell) : null, hoverScreenPosition);
		if (_playerDeck != null)
		{
			_hud?.SetCardState(
				_playerDeck.CurrentEnergy,
				_playerDeck.MaxEnergyPerTurn,
				TurnState?.EnergyRechargeTurnProgress ?? 0,
				TurnState?.EnergyRechargeTurnInterval ?? 3,
				_playerDeck.Hand,
				TurnState?.SelectedCardInstanceId ?? string.Empty,
				_playerDeck.DrawPileCards,
				_playerDeck.DiscardPileCards,
				_playerDeck.ExhaustPileCards);
		}
		BattleObjectState? playerState = StateManager.GetPrimaryPlayerState();
		if (_hud != null && GlobalSession != null)
		{
			bool canUseArakawa = CanUseArakawaThisTurn();
			if (!canUseArakawa)
			{
				_isArakawaWheelOpen = false;
				if (_arakawaAbilityMode != ArakawaAbilityMode.None)
				{
					CancelArakawaAbilityMode();
				}
			}

			_hud.SetArakawaState(
				GlobalSession.ArakawaCurrentEnergy,
				GlobalSession.ArakawaMaxEnergy,
				canUseArakawa,
				_isArakawaWheelOpen,
				GetCurrentArakawaAbilityId());
			_hud.SetRetreatActionState(playerState != null && IsPlayerStandingOnEscapeCell(playerState.Cell) && IsRetreatFeatureAvailable());
			_hud.SetActionLogState(_currentTurnActionLogTurnIndex, _currentTurnActionLogEntries, _previousTurnActionLogTurnIndex, _previousTurnActionLogEntries);
		}

		UpdateBattleCameraPan(delta);

		BattleBoardOverlay? overlay = GetNodeOrNull<BattleBoardOverlay>("RoomContainer/BoardOverlay");
		if (overlay == null)
		{
			return;
		}

		overlay.SetEscapeCells(CurrentRoom.GetEscapeCells());

		if (_isPlayerMoveResolving)
		{
			overlay.SetReachableCells(Array.Empty<Vector2I>());
			overlay.SetAttackTargetCells(Array.Empty<Vector2I>());
			overlay.SetSupportTargetCells(Array.Empty<Vector2I>());
			overlay.SetPreviewPath(Array.Empty<Vector2I>());
			return;
		}

		if (_battleFailureSequenceStarted)
		{
			overlay.SetReachableCells(Array.Empty<Vector2I>());
			overlay.SetAttackTargetCells(Array.Empty<Vector2I>());
			overlay.SetSupportTargetCells(Array.Empty<Vector2I>());
			overlay.SetPreviewPath(Array.Empty<Vector2I>());
			return;
		}

		if (playerState == null)
		{
			overlay.SetReachableCells(Array.Empty<Vector2I>());
			overlay.SetAttackTargetCells(Array.Empty<Vector2I>());
			overlay.SetSupportTargetCells(Array.Empty<Vector2I>());
			overlay.SetPreviewPath(Array.Empty<Vector2I>());
			return;
		}

		if (_arakawaAbilityMode == ArakawaAbilityMode.BuildWall)
		{
			overlay.SetReachableCells(Array.Empty<Vector2I>());
			overlay.SetAttackTargetCells(Array.Empty<Vector2I>());
			overlay.SetSupportTargetCells(BuildArakawaWallTargetCells(), playerState.Cell);
			overlay.SetPreviewPath(Array.Empty<Vector2I>());
			return;
		}

		if (TurnState?.IsCardTargeting == true)
		{
			overlay.SetReachableCells(Array.Empty<Vector2I>());
			BattleCardDefinition? selectedDefinition = GetSelectedCardDefinition();
			if (selectedDefinition?.TargetingMode == BattleCardTargetingMode.FriendlyUnit)
			{
				overlay.SetAttackTargetCells(Array.Empty<Vector2I>());
				overlay.SetSupportTargetCells(BuildSelectedCardTargetCells(playerState.ObjectId), playerState.Cell);
			}
			else
			{
				overlay.SetAttackTargetCells(BuildSelectedCardTargetCells(playerState.ObjectId), playerState.Cell);
				overlay.SetSupportTargetCells(Array.Empty<Vector2I>());
			}
			overlay.SetPreviewPath(Array.Empty<Vector2I>());
			return;
		}

		if (TurnState?.IsAttackTargeting == true)
		{
			overlay.SetReachableCells(Array.Empty<Vector2I>());
			overlay.SetAttackTargetCells(BuildAttackTargetCells(playerState.ObjectId, playerState.Cell, playerState.AttackRange), playerState.Cell);
			overlay.SetSupportTargetCells(Array.Empty<Vector2I>());
			overlay.SetPreviewPath(Array.Empty<Vector2I>());
			return;
		}

		if (!CanPlayerMoveThisTurn())
		{
			overlay.SetReachableCells(Array.Empty<Vector2I>());
			overlay.SetAttackTargetCells(Array.Empty<Vector2I>());
			overlay.SetSupportTargetCells(Array.Empty<Vector2I>());
			overlay.SetPreviewPath(Array.Empty<Vector2I>());
			return;
		}

		List<Vector2I> reachableCells = BuildReachableCells(playerState.ObjectId, playerState.Cell, playerState.MovePointsPerTurn);
		overlay.SetReachableCells(reachableCells, playerState.Cell);
		overlay.SetAttackTargetCells(Array.Empty<Vector2I>());
		overlay.SetSupportTargetCells(Array.Empty<Vector2I>());

		if (hasHoveredCell && reachableCells.Contains(hoveredCell))
		{
			overlay.SetPreviewPath(BuildPreviewPath(playerState.ObjectId, playerState.Cell, hoveredCell, playerState.MovePointsPerTurn));
		}
		else
		{
			overlay.SetPreviewPath(Array.Empty<Vector2I>());
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_battleFailureSequenceStarted)
		{
			return;
		}

		if (_isPlayerMoveResolving)
		{
			return;
		}

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
				EndPlayerTurn();
				return;
			}

			if (keyEvent.Keycode == Key.Y)
			{
				TryResetBattleCamera();
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
			if (TurnState?.IsCardTargeting == true || TurnState?.IsAttackTargeting == true)
			{
				TurnState.CancelTargeting();
			}

			return;
		}

		if (_arakawaAbilityMode == ArakawaAbilityMode.BuildWall)
		{
			TryExecuteArakawaBuildWall(targetCell);
			return;
		}

		if (TurnState?.IsCardTargeting == true && _playerDeck != null)
		{
			if (!_playerDeck.TryGetHandCard(TurnState.SelectedCardInstanceId, out BattleCardInstance? selectedCard) || selectedCard == null)
			{
				TurnState.CancelTargeting();
				return;
			}

			if (!selectedCard.Definition.RequiresTarget)
			{
				TurnState.CancelTargeting();
				return;
			}

			BoardObject? cardTarget = GetCardTargetAtCell(playerState.ObjectId, targetCell, selectedCard.Definition);
			if (cardTarget == null)
			{
				TurnState.CancelTargeting();
				return;
			}

			TryPlayCard(playerState.ObjectId, selectedCard.InstanceId, cardTarget.ObjectId, out _);
			return;
		}

		if (TurnState?.IsAttackTargeting == true)
		{
			BoardObject? attackTarget = GetAttackableObjectAtCell(playerState.ObjectId, targetCell);
			if (attackTarget == null)
			{
				TurnState.CancelTargeting();
				return;
			}

			TryAttackObject(playerState.ObjectId, attackTarget.ObjectId, out _);
			return;
		}

		if (!CanPlayerMoveThisTurn())
		{
			return;
		}

		if (!BuildReachableCells(playerState.ObjectId, playerState.Cell, playerState.MovePointsPerTurn).Contains(targetCell))
		{
			return;
		}

		_ = TryMoveObjectAsync(playerState.ObjectId, targetCell);
	}

	public bool TryMoveObject(string objectId, Vector2I targetCell, out string failureReason)
	{
		failureReason = "BoardQueryService has not been initialized.";

		if (_actionService == null || StateManager == null)
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

		bool moved = _actionService.TryMoveObject(objectId, targetCell, out failureReason);
		if (moved)
		{
			if (isPlayerObject)
			{
				TurnState?.MarkMoved();
			}
		}

		return moved;
	}

	public async System.Threading.Tasks.Task<bool> TryMoveObjectAsync(string objectId, Vector2I targetCell)
	{
		if (_actionService == null || StateManager == null)
		{
			return false;
		}

		BattleObjectState? playerState = StateManager.GetPrimaryPlayerState();
		bool isPlayerObject = playerState?.ObjectId == objectId;
		if (isPlayerObject && !CanPlayerMoveThisTurn())
		{
			return false;
		}

		if (isPlayerObject)
		{
			_isPlayerMoveResolving = true;
		}

		try
		{
			bool moved = await _actionService.TryMoveObjectAsync(objectId, targetCell);
			if (moved && isPlayerObject)
			{
				TurnState?.MarkMoved();
			}

			return moved;
		}
		finally
		{
			if (isPlayerObject)
			{
				_isPlayerMoveResolving = false;
			}
		}
	}

	private void OnPlayerRuntimeChanged()
	{
		StateManager?.SyncPlayerFromSession();
	}

	private void OnBattleActionLogged(string line)
	{
		AppendBattleActionLog(line);
	}

	private void AppendBattleActionLog(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return;
		}

		_currentTurnActionLogEntries.Add(line);
	}

	private void AdvanceBattleActionLogTurn(int nextTurnIndex)
	{
		_previousTurnActionLogEntries.Clear();
		_previousTurnActionLogEntries.AddRange(_currentTurnActionLogEntries);
		_previousTurnActionLogTurnIndex = _currentTurnActionLogTurnIndex;
		_currentTurnActionLogEntries.Clear();
		_currentTurnActionLogTurnIndex = nextTurnIndex;
	}

	private string ResolveObjectDisplayName(string objectId)
	{
		return StateManager?.Get(objectId)?.DisplayName ?? objectId;
	}

	private static int SumImpactAmount(DamageApplicationResult result, params CombatImpactType[] impactTypes)
	{
		if (impactTypes == null || impactTypes.Length == 0)
		{
			return 0;
		}

		return result.Impacts
			.Where(impact => impactTypes.Contains(impact.ImpactType))
			.Sum(impact => impact.Amount);
	}

	private void OnArakawaRuntimeChanged()
	{
	}

	private DefenseActionDefinition BuildPlayerDefenseActionDefinition()
	{
		int reductionPercent = GlobalSession?.GetResolvedPlayerDefenseDamageReductionPercent() ?? 50;
		int shieldGain = GlobalSession?.GetResolvedPlayerDefenseShieldGain() ?? 0;
		return new DefenseActionDefinition(reductionPercent, shieldGain);
	}

	private void OnEndTurnRequested()
	{
		if (_battleFailureSequenceStarted)
		{
			return;
		}

		if (_isPlayerMoveResolving)
		{
			return;
		}

		EndPlayerTurn();
	}

	private void OnAttackRequested()
	{
		if (_battleFailureSequenceStarted || TurnState == null)
		{
			return;
		}

		if (_isPlayerMoveResolving)
		{
			return;
		}

		if (TurnState.IsAttackTargeting)
		{
			TurnState.CancelTargeting();
			return;
		}

		if (!TurnState.CanEnterAttackTargeting)
		{
			return;
		}

		TurnState.EnterAttackTargeting();
	}

	private void OnCardRequested(string cardInstanceId)
	{
		if (_battleFailureSequenceStarted || TurnState == null || _playerDeck == null || StateManager == null)
		{
			return;
		}

		if (_isPlayerMoveResolving)
		{
			return;
		}

		if (_arakawaAbilityMode == ArakawaAbilityMode.EnhanceCard)
		{
			TryExecuteArakawaEnhanceCard(cardInstanceId);
			return;
		}

		if (!_playerDeck.TryGetHandCard(cardInstanceId, out BattleCardInstance? cardInstance) || cardInstance == null)
		{
			return;
		}

		if (TurnState.IsCardTargeting && string.Equals(TurnState.SelectedCardInstanceId, cardInstanceId, StringComparison.Ordinal))
		{
			if (cardInstance.Definition.RequiresTarget)
			{
				return;
			}

			BattleObjectState? selectedPlayerState = StateManager.GetPrimaryPlayerState();
			if (selectedPlayerState == null)
			{
				return;
			}

			TryPlayCard(selectedPlayerState.ObjectId, cardInstanceId, null, out _);
			return;
		}

		if (!_playerDeck.CanPlay(cardInstanceId, out _))
		{
			return;
		}

		BattleObjectState? playerState = StateManager.GetPrimaryPlayerState();
		if (playerState == null)
		{
			return;
		}

		TurnState.EnterCardTargeting(cardInstanceId);
	}

	private void OnMeditateRequested()
	{
		if (_battleFailureSequenceStarted || _playerDeck == null || TurnState == null)
		{
			return;
		}

		if (_isPlayerMoveResolving)
		{
			return;
		}

		if (!TurnState.CanSelectCard)
		{
			return;
		}

		_playerDeck.DiscardHand();
		_playerDeck.DrawToHandSize();
		if (StateManager?.GetPrimaryPlayerState() is BattleObjectState playerState)
		{
			AppendBattleActionLog($"{playerState.DisplayName}->{playerState.DisplayName} 冥想");
		}
		TurnState.MarkActed();
		ResolveTurnPostPhase();
	}

	private async void OnDefendRequested()
	{
		if (_battleFailureSequenceStarted || TurnState == null || StateManager == null || _actionService == null)
		{
			return;
		}

		if (_isPlayerMoveResolving)
		{
			return;
		}

		if (!TurnState.CanSelectCard)
		{
			return;
		}

		if (TurnState.IsAttackTargeting || TurnState.IsCardTargeting)
		{
			TurnState.CancelTargeting();
		}

		BattleObjectState? playerState = StateManager.GetPrimaryPlayerState();
		if (playerState == null)
		{
			return;
		}

		await _actionService.ApplyDefenseActionAsync(playerState.ObjectId, BuildPlayerDefenseActionDefinition(), TurnState.TurnIndex);
		int defenseShieldGain = GlobalSession?.GetResolvedPlayerDefenseShieldGain() ?? 0;
		AppendBattleActionLog(defenseShieldGain > 0
			? $"{playerState.DisplayName}->{playerState.DisplayName} 护盾{defenseShieldGain}"
			: $"{playerState.DisplayName}->{playerState.DisplayName} 防御");
		TurnState.MarkActed();
		ResolveTurnPostPhase();
	}

	private void OnRetreatRequested()
	{
		if (_battleFailureSequenceStarted || TurnState == null || GlobalSession == null)
		{
			return;
		}

		if (_isPlayerMoveResolving)
		{
			return;
		}

		if (!CanAttemptRetreatThisTurn())
		{
			return;
		}

		if (TurnState.IsAttackTargeting || TurnState.IsCardTargeting)
		{
			TurnState.CancelTargeting();
		}

		CancelArakawaAbilityMode();
		_isArakawaWheelOpen = false;
		_retreatPending = false;
		_retreatTurnIndex = -1;
		_retreatStartHp = -1;
		if (StateManager?.GetPrimaryPlayerState() is BattleObjectState playerState)
		{
			AppendBattleActionLog($"{playerState.DisplayName}->{playerState.DisplayName} 立即脱离战斗");
		}
		CommitBattleResult(BattleOutcome.Retreat);
	}

	private void OnArakawaWheelRequested()
	{
		if (!CanUseArakawaThisTurn())
		{
			_isArakawaWheelOpen = false;
			CancelArakawaAbilityMode();
			return;
		}

		if (_arakawaAbilityMode != ArakawaAbilityMode.None)
		{
			CancelArakawaAbilityMode();
			_isArakawaWheelOpen = false;
			return;
		}

		_isArakawaWheelOpen = !_isArakawaWheelOpen;
	}

	private void OnArakawaAbilityRequested(string abilityId)
	{
		if (!CanUseArakawaThisTurn())
		{
			_isArakawaWheelOpen = false;
			return;
		}

		_isArakawaWheelOpen = false;
		switch (abilityId)
		{
			case "build_wall":
				BeginArakawaAbilityMode(ArakawaAbilityMode.BuildWall);
				break;

			case "enhance_card":
				BeginArakawaAbilityMode(ArakawaAbilityMode.EnhanceCard);
				break;
		}
	}

	private void OnArakawaCancelRequested()
	{
		_isArakawaWheelOpen = false;
		CancelArakawaAbilityMode();
	}

	private bool CanPlayerMoveThisTurn()
	{
		return TurnState?.CanMove != false;
	}

	private bool CanUseArakawaThisTurn()
	{
		return !_battleFailureSequenceStarted
			&& TurnState?.IsPlayerTurn == true
			&& GlobalSession != null
			&& GlobalSession.ArakawaCurrentEnergy > 0;
	}

	private bool CanAttemptRetreatThisTurn()
	{
		if (TurnState?.CanRetreat != true || GlobalSession == null || StateManager?.GetPrimaryPlayerState() is not BattleObjectState playerState)
		{
			return false;
		}

		if (!IsPlayerStandingOnEscapeCell(playerState.Cell))
		{
			return false;
		}

		return IsRetreatFeatureAvailable();
	}

	private bool IsRetreatFeatureAvailable()
	{
		if (GlobalSession == null)
		{
			return false;
		}

		if (_activeBattleRequest != null
			&& _activeBattleRequest.RuntimeModifiers.TryGetValue("allow_retreat", out Variant allowRetreatVariant)
			&& allowRetreatVariant.VariantType == Variant.Type.Bool)
		{
			return allowRetreatVariant.AsBool();
		}

		return true;
	}

	private bool IsPlayerStandingOnEscapeCell(Vector2I playerCell)
	{
		return CurrentRoom != null && CurrentRoom.GetEscapeCells().Contains(playerCell);
	}

	private void BeginArakawaAbilityMode(ArakawaAbilityMode abilityMode)
	{
		if (TurnState?.IsAttackTargeting == true || TurnState?.IsCardTargeting == true)
		{
			TurnState.CancelTargeting();
		}

		_arakawaAbilityMode = abilityMode;
	}

	private void CancelArakawaAbilityMode()
	{
		_arakawaAbilityMode = ArakawaAbilityMode.None;
	}

	private string GetCurrentArakawaAbilityId()
	{
		return _arakawaAbilityMode switch
		{
			ArakawaAbilityMode.BuildWall => BuildWallAbility.AbilityId,
			ArakawaAbilityMode.EnhanceCard => EnhanceCardAbility.AbilityId,
			_ => string.Empty,
		};
	}

	private void EndPlayerTurn()
	{
		if (_battleFailureSequenceStarted || TurnState == null)
		{
			return;
		}

		if (_isPlayerMoveResolving)
		{
			return;
		}

		if (!TurnState.IsPlayerTurn && !TurnState.IsAttackTargeting)
		{
			return;
		}

		TurnState.MarkEndedTurn();
		ResolveTurnPostPhase();
	}

	private BattleObjectState? GetHoveredObjectState(Vector2I hoveredCell)
	{
		if (QueryService == null || StateManager == null)
		{
			return null;
		}

		foreach (BoardObject boardObject in QueryService.GetObjectsAtCell(hoveredCell))
		{
			return StateManager.Get(boardObject.ObjectId);
		}

		return null;
	}

	private BoardObject? GetCardTargetAtCell(string sourceObjectId, Vector2I targetCell, BattleCardDefinition cardDefinition)
	{
		if (Registry == null || !Registry.TryGet(sourceObjectId, out BoardObject? sourceObject) || sourceObject == null)
		{
			return null;
		}

		return cardDefinition.TargetingMode switch
		{
			BattleCardTargetingMode.EnemyUnit => GetAttackableObjectAtCell(sourceObjectId, targetCell) is BoardObject enemyTarget
				? GetManhattanTarget(sourceObject, enemyTarget, cardDefinition.Range)
				: null,
			BattleCardTargetingMode.StraightLineEnemy => GetAttackableObjectAtCell(sourceObjectId, targetCell) is BoardObject lineEnemyTarget
				? GetStraightLineTarget(sourceObjectId, lineEnemyTarget, cardDefinition.Range)
				: null,
			BattleCardTargetingMode.FriendlyUnit => GetFriendlyUnitAtCell(sourceObjectId, targetCell) is BoardObject friendlyTarget
				? GetManhattanTarget(sourceObject, friendlyTarget, cardDefinition.Range)
				: null,
			_ => null,
		};
	}

	private BoardObject? GetAttackableObjectAtCell(string sourceObjectId, Vector2I targetCell)
	{
		if (_actionService == null)
		{
			return null;
		}

		return _actionService.GetAttackableObjectAtCell(sourceObjectId, targetCell);
	}

	private BoardObject? GetFriendlyUnitAtCell(string sourceObjectId, Vector2I targetCell)
	{
		if (Registry == null || !Registry.TryGet(sourceObjectId, out BoardObject? sourceObject) || sourceObject == null || QueryService == null)
		{
			return null;
		}

		foreach (BoardObject boardObject in QueryService.GetObjectsAtCell(targetCell))
		{
			if (boardObject.ObjectType == BoardObjectType.Unit && boardObject.Faction == sourceObject.Faction)
			{
				return boardObject;
			}
		}

		return null;
	}

	private bool TryPlayCard(string attackerId, string cardInstanceId, string? targetId, out string failureReason)
	{
		failureReason = string.Empty;

		if (Registry == null || BoardState == null || StateManager == null || _pieceViewManager == null || TurnState == null || CurrentRoom == null || _playerDeck == null)
		{
			failureReason = "Battle systems are not initialized.";
			return false;
		}

		if (!Registry.TryGet(attackerId, out BoardObject? attacker) || attacker == null)
		{
			failureReason = $"Attacker {attackerId} was not found.";
			return false;
		}

		if (!_playerDeck.TryGetHandCard(cardInstanceId, out BattleCardInstance? cardInstance) || cardInstance == null)
		{
			failureReason = $"Card {cardInstanceId} was not found in hand.";
			return false;
		}

		if (!_playerDeck.CanPlay(cardInstanceId, out failureReason))
		{
			return false;
		}

		if (!TryResolveCardTarget(attackerId, targetId, cardInstance.Definition, out BoardObject? targetObject, out failureReason))
		{
			return false;
		}

		if (!_playerDeck.CommitPlayedCard(cardInstanceId, out _, out failureReason))
		{
			return false;
		}

		_hud?.PlayCardUseEffect(cardInstance);
		_pieceViewManager.PlayAction(attackerId);

		if (targetObject != null && cardInstance.Definition.Damage > 0)
		{
			if (_actionService == null)
			{
				failureReason = "Battle action service is not initialized.";
				return false;
			}

			Vector2 knockbackDirection = Vector2.Zero;
			if (Registry != null && Registry.TryGet(attackerId, out BoardObject? attackerObject) && attackerObject != null)
			{
				knockbackDirection = new Vector2(
					targetObject.Cell.X - attackerObject.Cell.X,
					targetObject.Cell.Y - attackerObject.Cell.Y);
			}

			DamageApplicationResult damageResult = _actionService.ApplyDamageToTarget(
				targetObject.ObjectId,
				cardInstance.Definition.Damage,
				knockbackDirection,
				out bool wasDestroyed,
				out string damageFailureReason);
			if (!string.IsNullOrWhiteSpace(damageFailureReason))
			{
				failureReason = damageFailureReason;
				return false;
			}

			int damageAmount = SumImpactAmount(damageResult, CombatImpactType.HealthDamage, CombatImpactType.ShieldDamage);
			if (damageAmount > 0)
			{
				if (wasDestroyed && targetObject.ObjectType == BoardObjectType.Unit && targetObject.Faction == BoardObjectFaction.Enemy)
				{
					Vector2I attackerCell = Registry != null && Registry.TryGet(attackerId, out BoardObject? attackerObjectForFocus) && attackerObjectForFocus != null
						? attackerObjectForFocus.Cell
						: targetObject.Cell;
					TriggerBattleCameraFocusForCells(attackerCell, targetObject.Cell);
				}
				AppendBattleActionLog($"{ResolveObjectDisplayName(attackerId)}->{ResolveObjectDisplayName(targetObject.ObjectId)} 閺€璇插毊{damageAmount}");
			}
		}

		if (cardInstance.Definition.EnergyGain > 0)
		{
			_playerDeck.GainEnergy(cardInstance.Definition.EnergyGain);
		}

		if (cardInstance.Definition.ShieldGain > 0)
		{
			if (_actionService == null)
			{
				failureReason = "Battle action service is not initialized.";
				return false;
			}

			DamageApplicationResult shieldResult = _actionService.ApplyShieldGainToTarget(attackerId, cardInstance.Definition.ShieldGain, out string shieldFailureReason);
			if (!string.IsNullOrWhiteSpace(shieldFailureReason))
			{
				failureReason = shieldFailureReason;
				return false;
			}

			int shieldGain = SumImpactAmount(shieldResult, CombatImpactType.ShieldGain);
			if (shieldGain > 0)
			{
				AppendBattleActionLog($"{ResolveObjectDisplayName(attackerId)}->{ResolveObjectDisplayName(attackerId)} 护盾{shieldGain}");
			}
		}

		if (cardInstance.Definition.HealingAmount > 0)
		{
			if (_actionService == null)
			{
				failureReason = "Battle action service is not initialized.";
				return false;
			}

			string healingTargetId = targetObject?.ObjectId ?? attackerId;
			DamageApplicationResult healingResult = _actionService.ApplyHealingToTarget(healingTargetId, cardInstance.Definition.HealingAmount, out string healingFailureReason);
			if (!string.IsNullOrWhiteSpace(healingFailureReason))
			{
				failureReason = healingFailureReason;
				return false;
			}

			int healAmount = SumImpactAmount(healingResult, CombatImpactType.HealthHeal);
			if (healAmount > 0)
			{
				AppendBattleActionLog($"{ResolveObjectDisplayName(attackerId)}->{ResolveObjectDisplayName(healingTargetId)} 治疗{healAmount}");
			}
		}

		if (cardInstance.Definition.DrawCount > 0)
		{
			_playerDeck.DrawCards(cardInstance.Definition.DrawCount);
		}

		StateManager.SyncAllFromRegistry();
		_pieceViewManager.Sync(Registry, StateManager, CurrentRoom);
		TurnState.MarkActed(!cardInstance.Definition.IsQuick);

		if (!cardInstance.Definition.IsQuick)
		{
			ResolveTurnPostPhase();
		}

		return true;
	}

	private bool TryAttackObject(string attackerId, string targetId, out string failureReason)
	{
		failureReason = string.Empty;

		if (_actionService == null || TurnState == null)
		{
			failureReason = "Battle systems are not initialized.";
			return false;
		}

		Vector2I attackerCell = Vector2I.Zero;
		Vector2I targetCell = Vector2I.Zero;
		BoardObjectFaction targetFaction = BoardObjectFaction.World;
		BoardObjectType targetType = BoardObjectType.Obstacle;
		if (Registry != null)
		{
			if (Registry.TryGet(attackerId, out BoardObject? attackerObject) && attackerObject != null)
			{
				attackerCell = attackerObject.Cell;
			}

			if (Registry.TryGet(targetId, out BoardObject? targetObject) && targetObject != null)
			{
				targetCell = targetObject.Cell;
				targetFaction = targetObject.Faction;
				targetType = targetObject.ObjectType;
			}
		}

		if (!_actionService.TryAttackObject(attackerId, targetId, out bool wasDestroyed, out failureReason))
		{
			return false;
		}

		if (wasDestroyed && targetType == BoardObjectType.Unit && targetFaction == BoardObjectFaction.Enemy)
		{
			TriggerBattleCameraFocusForCells(attackerCell, targetCell);
		}

		TurnState.MarkActed();
		ResolveTurnPostPhase();
		return true;
	}

	private async void ResolveTurnPostPhase()
	{
		if (TurnState == null)
		{
			return;
		}

		if (TurnState.Phase != TurnPhase.TurnPost)
		{
			return;
		}

		if (TurnState.HasActed)
		{
			double resolveDelay = Math.Max(
				PlayerActionResolveBufferSeconds,
				_actionService?.LastImpactPresentationDurationSeconds ?? 0.0d);
			await ToSignal(GetTree().CreateTimer(resolveDelay), SceneTreeTimer.SignalName.Timeout);
			if (_battleFailureSequenceStarted || TurnState.Phase != TurnPhase.TurnPost)
			{
				return;
			}
		}

		_playerDeck?.EndPlayerTurn();
		StateManager?.SyncAllFromRegistry();
		TurnState.BeginEnemyTurn();
		_actionService?.ResolveTurnStart(BoardObjectFaction.Enemy, TurnState.TurnIndex);
		if (_enemyTurnResolver != null)
		{
			await _enemyTurnResolver.ResolveTurnAsync();
		}

		if (_actionService?.IsPlayerDefeated == true)
		{
			StartBattleFailureSequence();
			return;
		}

		if (TryResolveRetreatSuccess())
		{
			return;
		}

		TurnState.AdvanceToNextTurn();
		AdvanceBattleActionLogTurn(TurnState.TurnIndex);
		if (_playerDeck != null)
		{
			TurnState.ConfigureEnergyRechargeInterval(_playerDeck.EnergyRegenIntervalTurns);
			if (TurnState.AdvanceEnergyRechargeProgress())
			{
				_playerDeck.GainEnergy(_playerDeck.EnergyRegenAmount);
			}
		}

		_playerDeck?.StartPlayerTurn();
		_actionService?.ResolveTurnStart(BoardObjectFaction.Player, TurnState.TurnIndex);
	}

	private bool TryResolveRetreatSuccess()
	{
		if (!_retreatPending || TurnState == null || GlobalSession == null)
		{
			return false;
		}

		bool isSameTurnRetreatWindow = _retreatTurnIndex == TurnState.TurnIndex;
		bool hpWasPreserved = _retreatStartHp >= 0 && GlobalSession.PlayerCurrentHp >= _retreatStartHp;
		_retreatPending = false;
		_retreatTurnIndex = -1;
		_retreatStartHp = -1;

		if (!isSameTurnRetreatWindow || !hpWasPreserved)
		{
			return false;
		}

		CommitBattleResult(BattleOutcome.Retreat);
		return true;
	}

	private void ApplyPendingBattleRequest()
	{
		if (GlobalSession == null)
		{
			return;
		}

		_activeBattleRequest = GlobalSession.ConsumePendingBattleRequest();
		_activeBattleRequest?.ApplyToSession(GlobalSession);
		if (_activeBattleRequest == null)
		{
			return;
		}

		if (!string.IsNullOrWhiteSpace(_activeBattleRequest.EncounterId))
		{
			EncounterId = _activeBattleRequest.EncounterId;
		}

		if (_activeBattleRequest.RandomSeed > 0)
		{
			RandomSeed = _activeBattleRequest.RandomSeed;
			_rng.Seed = (ulong)_activeBattleRequest.RandomSeed;
		}
	}

	private void ApplyPendingEncounterId()
	{
		if (GlobalSession == null)
		{
			return;
		}

		string encounterId = GlobalSession.ConsumePendingBattleEncounterId();
		if (!string.IsNullOrWhiteSpace(encounterId))
		{
			EncounterId = encounterId;
		}
	}

	private void StartBattleFailureSequence()
	{
		if (_battleFailureSequenceStarted || GlobalSession == null)
		{
			return;
		}

		_battleFailureSequenceStarted = true;
		BattleObjectState? playerState = StateManager?.GetPrimaryPlayerState();
		if (playerState != null)
		{
			_pieceViewManager?.PlayDefeat(playerState.ObjectId);
		}

		if (_battleFailOverlay == null || _battleFailFlash == null || _battleFailLabel == null)
		{
			CommitBattleResult(BattleOutcome.Defeat);
			return;
		}

		_battleFailOverlay.Visible = true;
		_battleFailOverlay.Modulate = Colors.White;
		_battleFailFlash.Color = new Color(0.45f, 0.04f, 0.06f, 0.0f);
		_battleFailLabel.Visible = true;
		_battleFailLabel.Modulate = new Color(1.0f, 0.92f, 0.92f, 0.0f);
		_battleFailLabel.Scale = new Vector2(0.9f, 0.9f);

		Tween tween = CreateTween();
		tween.SetParallel();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(_battleFailFlash, "color", new Color(0.45f, 0.04f, 0.06f, 0.82f), 0.18f);
		tween.TweenProperty(_battleFailLabel, "modulate:a", 1.0f, 0.16f).SetDelay(0.08f);
		tween.TweenProperty(_battleFailLabel, "scale", Vector2.One, 0.22f).SetDelay(0.08f);
		tween.Finished += () => CommitBattleResult(BattleOutcome.Defeat);
	}

	private void CommitBattleResult(BattleOutcome outcome)
	{
		if (_battleResultCommitted || GlobalSession == null)
		{
			return;
		}

		_battleResultCommitted = true;
		GlobalSession.CompleteBattle(BattleResult.FromSession(
			GlobalSession,
			outcome,
			_activeBattleRequest?.RequestId ?? string.Empty,
			EncounterId,
			outcome == BattleOutcome.Victory ? EncounterId : string.Empty));
		ReturnToPendingMapSceneIfAny();
	}

	private BattleDeckRuntimeInit? BuildDeckRuntimeInit(IReadOnlyList<BattleCardDefinition> prototypeDeck)
	{
		if (_activeBattleRequest == null)
		{
			return null;
		}

		Dictionary<string, BattleCardDefinition> definitionMap = prototypeDeck
			.GroupBy(definition => definition.CardId, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

		BattleCardDefinition[] buildCards = ResolveCardDefinitionsFromSnapshot(_activeBattleRequest.DeckBuildSnapshot, "card_ids", definitionMap);
		if (BattleCardLibrary != null && BattleDeckBuildRules != null)
		{
			DeckBuildSnapshot deckBuildSnapshot = DeckBuildSnapshot.FromDictionary(_activeBattleRequest.DeckBuildSnapshot);
			ProgressionSnapshot progressionSnapshot = ProgressionSnapshot.FromDictionary(_activeBattleRequest.ProgressionSnapshot);
			BattleDeckConstructionService constructionService = new(BattleCardLibrary, BattleDeckBuildRules);
			BattleDeckValidationResult validationResult;
			BattleCardDefinition[] resolvedBuildCards = constructionService.BuildRuntimeDefinitions(deckBuildSnapshot, progressionSnapshot, out validationResult);
			if (validationResult.IsValid && resolvedBuildCards.Length > 0)
			{
				buildCards = resolvedBuildCards;
				definitionMap = resolvedBuildCards
					.GroupBy(definition => definition.CardId, StringComparer.Ordinal)
					.ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
			}
		else if (!validationResult.IsValid)
		{
			GD.PushWarning($"BattleSceneController: deck build snapshot failed validation. {string.Join(" | ", validationResult.Errors)}");
		}
	}
		if (definitionMap.TryGetValue("debug_finisher", out BattleCardDefinition? debugFinisher)
			&& !buildCards.Any(definition => string.Equals(definition.CardId, "debug_finisher", StringComparison.Ordinal)))
		{
			buildCards = new[] { debugFinisher }.Concat(buildCards).ToArray();
		}
		BattleCardDefinition[] startingHandCards = ResolveCardDefinitionsFromSnapshot(_activeBattleRequest.DeckRuntimeInitOverrides, "starting_hand_card_ids", definitionMap);
		BattleCardDefinition[] startingDrawPileCards = ResolveCardDefinitionsFromSnapshot(_activeBattleRequest.DeckRuntimeInitOverrides, "starting_draw_pile_card_ids", definitionMap);
		BattleCardDefinition[] startingDiscardPileCards = ResolveCardDefinitionsFromSnapshot(_activeBattleRequest.DeckRuntimeInitOverrides, "starting_discard_pile_card_ids", definitionMap);
		BattleCardDefinition[] startingExhaustPileCards = ResolveCardDefinitionsFromSnapshot(_activeBattleRequest.DeckRuntimeInitOverrides, "starting_exhaust_pile_card_ids", definitionMap);
		int handSizeOverride = ReadIntOverride(_activeBattleRequest.DeckRuntimeInitOverrides, "hand_size_override");
		int maxEnergyOverride = ReadIntOverride(_activeBattleRequest.DeckRuntimeInitOverrides, "max_energy_override");
		int initialEnergy = ReadIntOverride(_activeBattleRequest.DeckRuntimeInitOverrides, "initial_energy");
		int openingDrawCount = ReadIntOverride(_activeBattleRequest.DeckRuntimeInitOverrides, "opening_draw_count");

		if (buildCards.Length == 0
			&& startingHandCards.Length == 0
			&& startingDrawPileCards.Length == 0
			&& startingDiscardPileCards.Length == 0
			&& startingExhaustPileCards.Length == 0
			&& handSizeOverride < 0
			&& maxEnergyOverride < 0
			&& initialEnergy < 0
			&& openingDrawCount < 0)
		{
			return null;
		}

		return new BattleDeckRuntimeInit
		{
			BuildCards = buildCards,
			StartingHandCards = startingHandCards,
			StartingDrawPileCards = startingDrawPileCards,
			StartingDiscardPileCards = startingDiscardPileCards,
			StartingExhaustPileCards = startingExhaustPileCards,
			HandSizeOverride = handSizeOverride,
			MaxEnergyOverride = maxEnergyOverride,
			InitialEnergy = initialEnergy,
			OpeningDrawCount = openingDrawCount,
		};
	}

	private static IReadOnlyList<BattleCardDefinition> ResolveBattleDeckSource(
		IReadOnlyList<BattleCardDefinition> prototypeDeck,
		BattleDeckRuntimeInit? runtimeInit)
	{
		if (runtimeInit == null || runtimeInit.BuildCards.Length == 0)
		{
			return prototypeDeck;
		}

		return runtimeInit.BuildCards;
	}

	private IReadOnlyList<BattleCardDefinition> BuildAvailableCardCatalog()
	{
		if (BattleCardLibrary != null && BattleCardLibrary.Entries.Length > 0)
		{
			return BattleCardLibrary.Entries
				.Where(template => template != null)
				.Select(template => template.BuildRuntimeDefinition())
				.ToArray();
		}

		return BuildPrototypePlayerDeck();
	}

	private static int ResolveOpeningDrawCount(BattleDeckRuntimeInit? runtimeInit, int defaultCount)
	{
		if (runtimeInit == null)
		{
			return defaultCount;
		}

		if (runtimeInit.OpeningDrawCount >= 0)
		{
			return runtimeInit.OpeningDrawCount;
		}

		return runtimeInit.HasExplicitStartingHand ? 0 : defaultCount;
	}

	private static BattleCardDefinition[] ResolveCardDefinitionsFromSnapshot(
		Godot.Collections.Dictionary snapshot,
		string key,
		IReadOnlyDictionary<string, BattleCardDefinition> definitionMap)
	{
		if (!snapshot.TryGetValue(key, out Variant value) || value.Obj is not Godot.Collections.Array rawArray)
		{
			return Array.Empty<BattleCardDefinition>();
		}

		List<BattleCardDefinition> resolved = new();
		foreach (Variant item in rawArray)
		{
			string cardId = item.AsString();
			if (!string.IsNullOrWhiteSpace(cardId) && definitionMap.TryGetValue(cardId, out BattleCardDefinition? definition))
			{
				resolved.Add(definition);
			}
		}

		return resolved.ToArray();
	}

	private static int ReadIntOverride(Godot.Collections.Dictionary snapshot, string key)
	{
		return snapshot.TryGetValue(key, out Variant value) ? value.AsInt32() : -1;
	}

	private async void ReturnToPendingMapSceneIfAny()
	{
		if (GlobalSession?.PeekPendingMapResumeContext() is not MapResumeContext resumeContext
			|| string.IsNullOrWhiteSpace(resumeContext.ScenePath))
		{
			return;
		}

		if (GD.Load<PackedScene>(BattleReturnTransitionOverlayScenePath) is PackedScene overlayScene
			&& overlayScene.Instantiate() is CardChessDemo.Map.BattleReturnTransitionOverlay overlay)
		{
			GetTree().Root.AddChild(overlay);
			await overlay.PlayAsync();
			overlay.QueueFree();
		}

		Error result = GetTree().ChangeSceneToFile(resumeContext.ScenePath);
		if (result != Error.Ok)
		{
			GD.PushError($"BattleSceneController: return to map failed, error={result}");
		}
	}
	private void ConfigureCameraForBattle()
	{
		if (CurrentRoom == null)
		{
			return;
		}

		_battleCamera = GetNodeOrNull<Camera2D>("Camera2D");
		if (_battleCamera == null)
		{
			return;
		}

		Vector2 viewportSize = GetViewportRect().Size;
		float boardWidth = CurrentRoom.BoardSize.X * CurrentRoom.CellSizePixels;
		float boardHeight = CurrentRoom.BoardSize.Y * CurrentRoom.CellSizePixels;
		Vector2 boardOrigin = CurrentRoom.ToGlobal(Vector2.Zero);
		float topMargin = Mathf.Max(0.0f, CameraTopMarginPixels);
		float bottomMargin = Mathf.Max(0.0f, CameraBottomMarginPixels);
		float usableViewportHeight = Mathf.Max(1.0f, viewportSize.Y - topMargin - bottomMargin);
		float centeredTopInset = Mathf.Max(0.0f, (usableViewportHeight - boardHeight) * 0.5f);
		float targetBoardTop = topMargin + centeredTopInset;
		float currentBoardTop = boardOrigin.Y;
		float cameraYOffset = currentBoardTop - targetBoardTop;
		Vector2 boardCenter = boardOrigin + new Vector2(boardWidth * 0.5f, boardHeight * 0.5f);
		_cameraRestPosition = boardCenter + new Vector2(0.0f, cameraYOffset);

		_battleCamera.Enabled = true;
		_battleCamera.Zoom = new Vector2(CameraZoom, CameraZoom);
		_battleCamera.Position = _cameraRestPosition;
		_cameraPanBounds = BuildBattleCameraPanBounds(boardOrigin, boardWidth, boardHeight, _battleCamera.Zoom);
	}

	private Rect2 BuildBattleCameraPanBounds(Vector2 boardOrigin, float boardWidth, float boardHeight, Vector2 cameraZoom)
	{
		Vector2 viewportWorldSize = GetViewportRect().Size * cameraZoom;
		float minVisibleRatio = Mathf.Clamp(CameraMinBoardVisibleRatio, 0.1f, 1.0f);
		float minVisibleBoardWidth = boardWidth * minVisibleRatio;
		float minVisibleBoardHeight = boardHeight * minVisibleRatio;

		float minCenterX = boardOrigin.X + minVisibleBoardWidth - viewportWorldSize.X * 0.5f;
		float maxCenterX = boardOrigin.X + boardWidth - minVisibleBoardWidth + viewportWorldSize.X * 0.5f;
		float minCenterY = boardOrigin.Y + minVisibleBoardHeight - viewportWorldSize.Y * 0.5f;
		float maxCenterY = boardOrigin.Y + boardHeight - minVisibleBoardHeight + viewportWorldSize.Y * 0.5f;

		if (minCenterX > maxCenterX)
		{
			float centerX = boardOrigin.X + boardWidth * 0.5f;
			minCenterX = centerX;
			maxCenterX = centerX;
		}

		if (minCenterY > maxCenterY)
		{
			float centerY = boardOrigin.Y + boardHeight * 0.5f;
			minCenterY = centerY;
			maxCenterY = centerY;
		}

		return new Rect2(
			new Vector2(minCenterX, minCenterY),
			new Vector2(maxCenterX - minCenterX, maxCenterY - minCenterY));
	}

	private void UpdateBattleCameraPan(double delta)
	{
		if (_battleCamera == null || _isCameraCinematicBusy)
		{
			return;
		}

		float horizontalFactor = 0.0f;
		float verticalFactor = 0.0f;

		if (Input.IsKeyPressed(Key.Left))
		{
			horizontalFactor -= 1.0f;
		}

		if (Input.IsKeyPressed(Key.Right))
		{
			horizontalFactor += 1.0f;
		}

		if (Input.IsKeyPressed(Key.Up))
		{
			verticalFactor -= 1.0f;
		}

		if (Input.IsKeyPressed(Key.Down))
		{
			verticalFactor += 1.0f;
		}

		if (Mathf.IsZeroApprox(horizontalFactor) && Mathf.IsZeroApprox(verticalFactor))
		{
			return;
		}

		_cameraResetTween?.Kill();
		Vector2 deltaMove = new(horizontalFactor, verticalFactor);
		if (deltaMove.LengthSquared() > 1.0f)
		{
			deltaMove = deltaMove.Normalized();
		}

		Vector2 nextPosition = _battleCamera.Position + deltaMove * CameraPanPixelsPerSecond * (float)delta;
		_battleCamera.Position = ClampBattleCameraPosition(nextPosition);
	}

	private void TryResetBattleCamera()
	{
		if (_battleCamera == null || _isCameraCinematicBusy)
		{
			return;
		}

		_cameraResetTween?.Kill();
		_cameraResetTween = CreateTween();
		_cameraResetTween.SetEase(Tween.EaseType.Out);
		_cameraResetTween.SetTrans(Tween.TransitionType.Cubic);
		_cameraResetTween.TweenProperty(
			_battleCamera,
			"position",
			ClampBattleCameraPosition(_cameraRestPosition),
			Math.Max(0.06d, CameraResetDurationMs / 1000.0d));
	}

	private Vector2 ClampBattleCameraPosition(Vector2 targetPosition)
	{
		if (_cameraPanBounds.Size == Vector2.Zero)
		{
			return targetPosition;
		}

		return new Vector2(
			Mathf.Clamp(targetPosition.X, _cameraPanBounds.Position.X, _cameraPanBounds.End.X),
			Mathf.Clamp(targetPosition.Y, _cameraPanBounds.Position.Y, _cameraPanBounds.End.Y));
	}

	private void TriggerBattleCameraFocusForCell(Vector2I cell)
	{
		if (CurrentRoom == null)
		{
			return;
		}

		double holdDuration = Math.Max(ArakawaBuildFocusHoldSeconds, BattleActionService.UtilityPresentationDurationSeconds);
		_ = PlayBattleCameraFocusAsync(GetBattleWorldPositionForCell(cell), holdDuration);
	}

	private void TriggerBattleCameraFocusForCells(Vector2I firstCell, Vector2I secondCell)
	{
		Vector2 focusPosition = (GetBattleWorldPositionForCell(firstCell) + GetBattleWorldPositionForCell(secondCell)) * 0.5f;
		double holdDuration = Math.Max(
			AttackFocusHoldSeconds,
			Math.Max(
				BattleActionService.AttackPresentationDurationSeconds,
				_actionService?.LastImpactPresentationDurationSeconds ?? 0.0d));
		_ = PlayBattleCameraFocusAsync(focusPosition, holdDuration);
	}

	private void TriggerBattleCameraFocusForObjects(string firstObjectId, string secondObjectId)
	{
		if (Registry == null
			|| !Registry.TryGet(firstObjectId, out BoardObject? firstObject) || firstObject == null
			|| !Registry.TryGet(secondObjectId, out BoardObject? secondObject) || secondObject == null)
		{
			return;
		}

		Vector2 focusPosition = (GetBattleWorldPositionForCell(firstObject.Cell) + GetBattleWorldPositionForCell(secondObject.Cell)) * 0.5f;
		double holdDuration = Math.Max(
			AttackFocusHoldSeconds,
			Math.Max(
				BattleActionService.AttackPresentationDurationSeconds,
				_actionService?.LastImpactPresentationDurationSeconds ?? 0.0d));
		_ = PlayBattleCameraFocusAsync(focusPosition, holdDuration);
	}

	private Vector2 GetBattleWorldPositionForCell(Vector2I cell)
	{
		if (CurrentRoom == null)
		{
			return Vector2.Zero;
		}

		return CurrentRoom.ToGlobal(CurrentRoom.CellToLocalCenter(cell));
	}

	private async System.Threading.Tasks.Task PlayBattleCameraFocusAsync(Vector2 focusPosition, double holdDuration = -1.0d)
	{
		if (_battleCamera == null || _isCameraCinematicBusy)
		{
			return;
		}

		_isCameraCinematicBusy = true;
		_cameraResetTween?.Kill();
		_cameraCinematicTween?.Kill();

		Vector2 previousPosition = _battleCamera.Position;
		Vector2 previousZoom = _battleCamera.Zoom;
		Vector2 clampedFocus = ClampBattleCameraPosition(focusPosition);
		float zoomMultiplier = Mathf.Clamp(Mathf.Round(CameraFocusZoomMultiplier), 1.0f, 4.0f);
		// 像素表现要求整数缩放，运行时强制取整，避免非整数倍率导致的像素畸变。
		Vector2 focusZoom = new(previousZoom.X * zoomMultiplier, previousZoom.Y * zoomMultiplier);

		_battleCamera.Position = clampedFocus;
		_battleCamera.Zoom = focusZoom;

		double resolvedHold = holdDuration >= 0.0d ? holdDuration : CameraFocusHoldSeconds;
		if (resolvedHold > 0.0d)
		{
			await ToSignal(GetTree().CreateTimer(resolvedHold), SceneTreeTimer.SignalName.Timeout);
		}

		_battleCamera.Position = ClampBattleCameraPosition(previousPosition);
		_battleCamera.Zoom = previousZoom;
		_isCameraCinematicBusy = false;
	}

	private BattleRoomTemplate InstantiateSelectedRoom()
	{
		PackedScene roomScene = SelectRoomScene();
		return roomScene.Instantiate<BattleRoomTemplate>();
	}

	private void ResolveEncounterConfiguration()
	{
		if (EncounterLibrary == null || string.IsNullOrWhiteSpace(EncounterId))
		{
			return;
		}

		BattleEncounterProfile? encounterProfile = EncounterLibrary.FindEntry(EncounterId);
		if (encounterProfile == null)
		{
			GD.PushWarning($"BattleSceneController: encounter '{EncounterId}' was not found in EncounterLibrary.");
			return;
		}

		if (!string.IsNullOrWhiteSpace(encounterProfile.PrimaryEnemyDefinitionId))
		{
			EncounterEnemyDefinitionId = encounterProfile.PrimaryEnemyDefinitionId;
		}

		string[] configuredEnemyTypeIds = encounterProfile.EnemyTypeIds
			.Where(enemyTypeId => !string.IsNullOrWhiteSpace(enemyTypeId))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		if (configuredEnemyTypeIds.Length > 0)
		{
			EncounterEnemyTypeIds = configuredEnemyTypeIds;
		}
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
				bool entryMatchesEncounter = string.IsNullOrWhiteSpace(entry.EnemyTypeId)
					|| EncounterEnemyTypeIds.Length == 0
					|| EncounterEnemyTypeIds.Any(enemyTypeId =>
						string.Equals(enemyTypeId, entry.EnemyTypeId, StringComparison.OrdinalIgnoreCase));

				if (!entryMatchesEncounter)
				{
					continue;
				}

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

	private List<Vector2I> BuildReachableCells(string objectId, Vector2I origin, int moveRange)
	{
		List<Vector2I> cells = new();
		if (Pathfinder == null)
		{
			return cells;
		}

		return Pathfinder.FindReachableCells(objectId, origin, moveRange).ToList();
	}

	private List<Vector2I> BuildArakawaWallTargetCells()
	{
		List<Vector2I> cells = new();
		if (BoardState == null || QueryService == null)
		{
			return cells;
		}

		foreach (BoardCellState cellState in BoardState.EnumerateCells())
		{
			if (QueryService.GetObjectsAtCell(cellState.Cell).Count == 0)
			{
				cells.Add(cellState.Cell);
			}
		}

		return cells;
	}

	private List<Vector2I> BuildAttackTargetCells(string objectId, Vector2I origin, int attackRange)
	{
		List<Vector2I> cells = new();
		if (CurrentRoom == null || attackRange <= 0)
		{
			return cells;
		}

		for (int y = origin.Y - attackRange; y <= origin.Y + attackRange; y++)
		{
			for (int x = origin.X - attackRange; x <= origin.X + attackRange; x++)
			{
				Vector2I cell = new(x, y);
				if (cell == origin || !CurrentRoom.Topology.IsInsideBoard(cell))
				{
					continue;
				}

				int distance = Mathf.Abs(cell.X - origin.X) + Mathf.Abs(cell.Y - origin.Y);
				if (distance <= attackRange)
				{
					cells.Add(cell);
				}
			}
		}

		return cells;
	}

	private List<Vector2I> BuildSelectedCardTargetCells(string sourceObjectId)
	{
		if (_playerDeck == null || TurnState == null)
		{
			return new List<Vector2I>();
		}

		if (!_playerDeck.TryGetHandCard(TurnState.SelectedCardInstanceId, out BattleCardInstance? selectedCard) || selectedCard == null)
		{
			return new List<Vector2I>();
		}

		return BuildCardTargetCells(sourceObjectId, selectedCard.Definition);
	}

	private async void TryExecuteArakawaBuildWall(Vector2I targetCell)
	{
		if (_actionService == null || GlobalSession == null || !CanUseArakawaThisTurn())
		{
			return;
		}

		if (!BuildArakawaWallTargetCells().Contains(targetCell))
		{
			return;
		}

		if (!GlobalSession.TrySpendArakawaEnergy(BuildWallAbility.EnergyCost))
		{
			return;
		}

		bool created = await _actionService.TryCreateArakawaBarrierAsync(targetCell);
		if (!created)
		{
			GlobalSession.RestoreArakawaEnergy(BuildWallAbility.EnergyCost);
			return;
		}
		TriggerBattleCameraFocusForCell(targetCell);

		AppendBattleActionLog($"荒川->({targetCell.X},{targetCell.Y}) 造墙");

		CancelArakawaAbilityMode();
	}

	private void TryExecuteArakawaEnhanceCard(string cardInstanceId)
	{
		if (_playerDeck == null || GlobalSession == null || _hud == null || !CanUseArakawaThisTurn())
		{
			return;
		}

		if (!_playerDeck.TryGetHandCard(cardInstanceId, out BattleCardInstance? cardInstance) || cardInstance == null)
		{
			return;
		}

		if (cardInstance.IsEnhanced || !PrototypeCardEnhancements.TryGetValue(cardInstance.BaseDefinition.CardId, out BattleCardEnhancementDefinition? enhancement))
		{
			return;
		}

		if (!GlobalSession.TrySpendArakawaEnergy(EnhanceCardAbility.EnergyCost))
		{
			return;
		}

		if (!cardInstance.TryApplyEnhancement(enhancement))
		{
			GlobalSession.RestoreArakawaEnergy(EnhanceCardAbility.EnergyCost);
			return;
		}

		_hud.PlayCardEnhancementEffect(cardInstanceId);
		AppendBattleActionLog($"荒川->{cardInstance.Definition.DisplayName} 强化");
		CancelArakawaAbilityMode();
	}

	private List<Vector2I> BuildCardTargetCells(string sourceObjectId, BattleCardDefinition cardDefinition)
	{
		if (Registry == null || !Registry.TryGet(sourceObjectId, out BoardObject? sourceObject) || sourceObject == null)
		{
			return new List<Vector2I>();
		}

		return cardDefinition.TargetingMode switch
		{
			BattleCardTargetingMode.EnemyUnit => BuildAttackTargetCells(sourceObjectId, sourceObject.Cell, cardDefinition.Range),
			BattleCardTargetingMode.StraightLineEnemy => BuildStraightLineTargetCells(sourceObject.Cell, cardDefinition.Range),
			BattleCardTargetingMode.FriendlyUnit => BuildFriendlyTargetCells(sourceObject.Cell, cardDefinition.Range),
			_ => new List<Vector2I>(),
		};
	}

	private List<Vector2I> BuildFriendlyTargetCells(Vector2I origin, int range)
	{
		List<Vector2I> cells = new();
		if (Registry == null || CurrentRoom == null)
		{
			return cells;
		}

		foreach (BoardObject boardObject in Registry.AllObjects)
		{
			if (boardObject.ObjectType != BoardObjectType.Unit || boardObject.Faction != BoardObjectFaction.Player)
			{
				continue;
			}

			int distance = Mathf.Abs(boardObject.Cell.X - origin.X) + Mathf.Abs(boardObject.Cell.Y - origin.Y);
			if (distance <= range)
			{
				cells.Add(boardObject.Cell);
			}
		}

		return cells;
	}

	private List<Vector2I> BuildStraightLineTargetCells(Vector2I origin, int range)
	{
		List<Vector2I> cells = new();
		if (CurrentRoom == null || QueryService == null || range <= 0)
		{
			return cells;
		}

		foreach (Vector2I direction in BoardTopology.CardinalDirections)
		{
			Vector2I currentCell = origin;
			for (int step = 0; step < range; step++)
			{
				currentCell += direction;
				if (!CurrentRoom.Topology.IsInsideBoard(currentCell))
				{
					break;
				}

				cells.Add(currentCell);

				bool shouldStop = false;
				foreach (BoardObject boardObject in QueryService.GetObjectsAtCell(currentCell))
				{
					if (boardObject.ObjectType == BoardObjectType.Unit || boardObject.BlocksLineOfSight)
					{
						shouldStop = true;
						break;
					}
				}

				if (shouldStop)
				{
					break;
				}
			}
		}

		return cells.Distinct().ToList();
	}

	private bool TryResolveCardTarget(
		string attackerId,
		string? targetId,
		BattleCardDefinition cardDefinition,
		out BoardObject? targetObject,
		out string failureReason)
	{
		targetObject = null;
		failureReason = string.Empty;

		if (!cardDefinition.RequiresTarget)
		{
			return true;
		}

		if (Registry == null || !Registry.TryGet(attackerId, out BoardObject? attacker) || attacker == null)
		{
			failureReason = $"Attacker {attackerId} was not found.";
			return false;
		}

		if (string.IsNullOrWhiteSpace(targetId) || !Registry.TryGet(targetId, out targetObject) || targetObject == null)
		{
			failureReason = "Card target was not found.";
			return false;
		}

		switch (cardDefinition.TargetingMode)
		{
			case BattleCardTargetingMode.EnemyUnit:
				if (!BattleActionService.IsAttackable(attacker, targetObject))
				{
					failureReason = "This target cannot be targeted by an enemy card.";
					return false;
				}

				if (GetManhattanTarget(attacker, targetObject, cardDefinition.Range) == null)
				{
					failureReason = $"Target is out of range. Range={cardDefinition.Range}.";
					return false;
				}

				return true;

			case BattleCardTargetingMode.StraightLineEnemy:
				if (!BattleActionService.IsAttackable(attacker, targetObject))
				{
					failureReason = "This target cannot be targeted by a straight-line enemy card.";
					return false;
				}

				if (GetStraightLineTarget(attackerId, targetObject, cardDefinition.Range) == null)
				{
					failureReason = $"Target is not in a valid straight line. Range={cardDefinition.Range}.";
					return false;
				}

				return true;

			case BattleCardTargetingMode.FriendlyUnit:
				if (targetObject.ObjectType != BoardObjectType.Unit || attacker.Faction != targetObject.Faction)
				{
					failureReason = "Friendly unit target is invalid.";
					return false;
				}

				if (GetManhattanTarget(attacker, targetObject, cardDefinition.Range) == null)
				{
					failureReason = $"Friendly target is out of range. Range={cardDefinition.Range}.";
					return false;
				}

				return true;

			default:
				failureReason = "Unsupported card targeting mode.";
				return false;
		}
	}

	private static BoardObject? GetManhattanTarget(BoardObject attacker, BoardObject target, int range)
	{
		int distance = Mathf.Abs(attacker.Cell.X - target.Cell.X) + Mathf.Abs(attacker.Cell.Y - target.Cell.Y);
		return distance <= range ? target : null;
	}

	private BattleCardDefinition? GetSelectedCardDefinition()
	{
		if (_playerDeck == null || TurnState == null)
		{
			return null;
		}

		return _playerDeck.TryGetHandCard(TurnState.SelectedCardInstanceId, out BattleCardInstance? selectedCard) && selectedCard != null
			? selectedCard.Definition
			: null;
	}

	private BoardObject? GetStraightLineTarget(string attackerId, BoardObject target, int range)
	{
		if (TargetingService == null)
		{
			return null;
		}

		IReadOnlyDictionary<Vector2I, BoardObject> lineTargets = TargetingService.FindEnemiesInStraightLines(attackerId, range);
		foreach (BoardObject candidate in lineTargets.Values)
		{
			if (candidate.ObjectId == target.ObjectId)
			{
				return candidate;
			}
		}

		return null;
	}

	private List<Vector2I> BuildPreviewPath(string objectId, Vector2I start, Vector2I end, int moveRange)
	{
		if (Pathfinder == null)
		{
			return new List<Vector2I>();
		}

		IReadOnlyList<Vector2I> path;
		int totalCost;
		if (Pathfinder.TryFindPath(objectId, start, end, moveRange, out path, out totalCost))
		{
			return path.ToList();
		}

		return new List<Vector2I>();
	}

	private static IReadOnlyList<BattleCardDefinition> BuildPrototypePlayerDeck()
	{
		return new[]
		{
			new BattleCardDefinition(
				"debug_finisher",
				"澶勫喅",
				"杩戦偦 99 浼ゅ",
				1,
				BattleCardCategory.Attack,
				BattleCardTargetingMode.EnemyUnit,
				range: 1,
				damage: 99,
				exhaustsOnPlay: true),
			new BattleCardDefinition(
				"cross_slash",
				"浜ゆ柀",
				"杩戦偦 3 浼ゅ",
				1,
				BattleCardCategory.Attack,
				BattleCardTargetingMode.EnemyUnit,
				range: 1,
				damage: 3),
			new BattleCardDefinition(
				"quick_cut",
				"鐤炬柀",
				"杩戦偦 2 浼ゅ",
				0,
				BattleCardCategory.Attack,
				BattleCardTargetingMode.EnemyUnit,
				range: 1,
				damage: 2,
				isQuick: true),
			new BattleCardDefinition(
				"line_shot",
				"璐皠",
				"鐩寸嚎棣栨晫 2 浼ゅ",
				1,
				BattleCardCategory.Attack,
				BattleCardTargetingMode.StraightLineEnemy,
				range: 4,
				damage: 2),
			new BattleCardDefinition(
				"heavy_shot",
				"閲嶉摮",
				"鐩寸嚎棣栨晫 5 浼ゅ",
				2,
				BattleCardCategory.Attack,
				BattleCardTargetingMode.StraightLineEnemy,
				range: 5,
				damage: 5,
				exhaustsOnPlay: true),
			new BattleCardDefinition(
				"battle_read",
				"整备",
				"抽 2 张",
				1,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				drawCount: 2),
			new BattleCardDefinition(
				"meditate",
				"璋冩伅",
				"鎶?1 寮犲苟鍥?1 鑳介噺",
				0,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				drawCount: 1,
				energyGain: 1,
				isQuick: true),
			new BattleCardDefinition(
				"surge",
				"钃勮兘",
				"鍥?2 鑳介噺",
				1,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				energyGain: 2,
				exhaustsOnPlay: true),
			new BattleCardDefinition(
				"draw_spark",
				"鐏垫劅",
				"鎶?1 寮犲苟鍥?1 鑳介噺",
				1,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				drawCount: 1,
				energyGain: 1),
			new BattleCardDefinition(
				"quick_plan",
				"快谋",
				"抽 2 张",
				0,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				drawCount: 2,
				isQuick: true),
			new BattleCardDefinition(
				"burning_edge",
				"鐕冨垉",
				"杩戦偦 4 浼ゅ",
				1,
				BattleCardCategory.Attack,
				BattleCardTargetingMode.EnemyUnit,
				range: 1,
				damage: 4,
				exhaustsOnPlay: true),
			new BattleCardDefinition(
				"hook_shot",
				"閽╁皠",
				"鐩寸嚎棣栨晫 3 浼ゅ",
				1,
				BattleCardCategory.Attack,
				BattleCardTargetingMode.StraightLineEnemy,
				range: 4,
				damage: 3),
			new BattleCardDefinition(
				"deep_focus",
				"沉念",
				"抽 3 张",
				2,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				drawCount: 3),
			new BattleCardDefinition(
				"spark_charge",
				"火花",
				"获得 1 能量并抽 1 张",
				0,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				drawCount: 1,
				energyGain: 1,
				isQuick: true),
			new BattleCardDefinition(
				"burst_drive",
				"鐖嗛┍",
				"鍥?2 鑳介噺",
				0,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				energyGain: 2,
				exhaustsOnPlay: true),
			new BattleCardDefinition(
				"guard_up",
				"涓剧浘",
				"鑾峰緱 3 鎶ょ浘",
				1,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				shieldGain: 3),
			new BattleCardDefinition(
				"brace",
				"鏋跺娍",
				"鑾峰緱 5 鎶ょ浘",
				2,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				shieldGain: 5),
			new BattleCardDefinition(
				"quick_guard",
				"鐬畧",
				"鑾峰緱 2 鎶ょ浘",
				0,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				shieldGain: 2,
				isQuick: true),
			new BattleCardDefinition(
				"field_patch",
				"鐜板満鍖呮墡",
				"2 鏍煎唴鍙嬫柟鎭㈠ 3 鐢熷懡",
				1,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.FriendlyUnit,
				range: 2,
				healingAmount: 3),
		};
	}
}
