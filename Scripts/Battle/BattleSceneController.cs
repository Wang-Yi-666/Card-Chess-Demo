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
	private static readonly DefenseActionDefinition BasicDefenseAction = new(damageReductionPercent: 50);
	private const double PlayerActionResolveBufferSeconds = 0.24d;
	private static readonly ArakawaAbilityDefinition BuildWallAbility = new("build_wall", "造墙", 1);
	private static readonly ArakawaAbilityDefinition EnhanceCardAbility = new("enhance_card", "强化", 1);
	private static readonly IReadOnlyDictionary<string, BattleCardEnhancementDefinition> PrototypeCardEnhancements =
		new Dictionary<string, BattleCardEnhancementDefinition>(StringComparer.Ordinal)
		{
			["cross_slash"] = new BattleCardEnhancementDefinition("+", "伤害 +2", damageDelta: 2),
			["quick_cut"] = new BattleCardEnhancementDefinition("+", "伤害 +1", damageDelta: 1),
			["line_shot"] = new BattleCardEnhancementDefinition("+", "伤害 +2", damageDelta: 2),
			["heavy_shot"] = new BattleCardEnhancementDefinition("+", "伤害 +2", damageDelta: 2),
			["battle_read"] = new BattleCardEnhancementDefinition("+", "额外抽 1", drawCountDelta: 1),
			["meditate"] = new BattleCardEnhancementDefinition("+", "额外抽 1", drawCountDelta: 1),
			["surge"] = new BattleCardEnhancementDefinition("+", "额外获得 1 能量", energyGainDelta: 1),
			["draw_spark"] = new BattleCardEnhancementDefinition("+", "额外抽 1", drawCountDelta: 1),
			["quick_plan"] = new BattleCardEnhancementDefinition("+", "额外抽 1", drawCountDelta: 1),
			["burning_edge"] = new BattleCardEnhancementDefinition("+", "伤害 +2", damageDelta: 2),
			["hook_shot"] = new BattleCardEnhancementDefinition("+", "伤害 +2", damageDelta: 2),
			["deep_focus"] = new BattleCardEnhancementDefinition("+", "额外抽 1", drawCountDelta: 1),
			["spark_charge"] = new BattleCardEnhancementDefinition("+", "额外获得 1 能量", energyGainDelta: 1),
			["burst_drive"] = new BattleCardEnhancementDefinition("+", "额外获得 1 能量", energyGainDelta: 1),
			["guard_up"] = new BattleCardEnhancementDefinition("+", "获得额外 2 护盾", shieldGainDelta: 2),
			["brace"] = new BattleCardEnhancementDefinition("+", "获得额外 3 护盾", shieldGainDelta: 3),
			["quick_guard"] = new BattleCardEnhancementDefinition("+", "获得额外 2 护盾", shieldGainDelta: 2),
		};
	[Export] public PackedScene? ForcedBattleRoomScene { get; set; }
	[Export] public PackedScene[] BattleRoomScenes { get; set; } = Array.Empty<PackedScene>();
	[Export] public BattleRoomPoolDefinition? BattleRoomPools { get; set; }
	[Export] public BattlePrefabLibrary? BattlePrefabLibrary { get; set; }
	[Export] public BattleEncounterLibrary? EncounterLibrary { get; set; }
	[Export] public string EncounterId { get; set; } = string.Empty;
	[Export] public string[] EncounterEnemyTypeIds { get; set; } = { "grunt" };
	[Export] public string EncounterEnemyDefinitionId { get; set; } = "battle_enemy";
	[Export] public int RandomSeed { get; set; } = 1337;
	[Export] public float CameraZoom { get; set; } = 1.0f;
	[Export] public int CameraTopMarginPixels { get; set; } = 8;
	[Export] public int CameraBottomMarginPixels { get; set; } = 52;
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

	public override void _Ready()
	{
		_rng.Seed = (ulong)Math.Max(RandomSeed, 1);

		GlobalSession = GetNodeOrNull<GlobalGameSession>("/root/GlobalGameSession");
		ApplyPendingBattleRequest();
		ApplyPendingEncounterId();
		BattlePrefabLibrary ??= GD.Load<BattlePrefabLibrary>("res://Resources/Battle/Presentation/DefaultBattlePrefabLibrary.tres");
		EncounterLibrary ??= GD.Load<BattleEncounterLibrary>("res://Resources/Battle/Encounters/DebugBattleEncounterLibrary.tres");
		ResolveEncounterConfiguration();

		BoardState = new BoardState();
		Registry = new BoardObjectRegistry();
		QueryService = new BoardQueryService(BoardState, Registry);
		TurnState = new TurnActionState();
		TurnState.StartNewTurn(1);
		_playerDeck = new BattleDeckState(
			BuildPrototypePlayerDeck(),
			(ulong)Math.Max(RandomSeed, 1),
			PlayerHandSize,
			PlayerEnergyPerTurn);
		TurnState.ConfigureEnergyRechargeInterval(_playerDeck.EnergyRegenIntervalTurns);
		_playerDeck.DrawCards(PlayerHandSize);
		_playerDeck.StartPlayerTurn();

		CurrentRoom = InstantiateSelectedRoom();
		Node2D roomContainer = GetNode<Node2D>("RoomContainer");
		roomContainer.AddChild(CurrentRoom);
		roomContainer.MoveChild(CurrentRoom, 0);

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

		_pieceViewManager = new BattlePieceViewManager(GetNode<Node>("RoomContainer/PieceRoot"), BattlePrefabLibrary);
		_pieceViewManager.Rebuild(Registry, StateManager, CurrentRoom);
		_floatingTextLayer = GetNodeOrNull<BattleFloatingTextLayer>("RoomContainer/FloatingTextLayer");
		_actionService = new BattleActionService(BoardState, Registry, QueryService, StateManager, _pieceViewManager, CurrentRoom, GlobalSession, _floatingTextLayer);
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
			_hud.ArakawaWheelRequested += OnArakawaWheelRequested;
			_hud.ArakawaAbilityRequested += OnArakawaAbilityRequested;
			_hud.ArakawaCancelRequested += OnArakawaCancelRequested;
			_hud.MeditateRequested += OnMeditateRequested;
			_hud.CardRequested += OnCardRequested;
			_hud.EndTurnRequested += OnEndTurnRequested;
		}

		_battleFailOverlay = GetNodeOrNull<Control>("BattleFailOverlay");
		_battleFailFlash = GetNodeOrNull<ColorRect>("BattleFailOverlay/Flash");
		_battleFailLabel = GetNodeOrNull<Label>("BattleFailOverlay/DefeatLabel");

		GlobalSession.PlayerRuntimeChanged += OnPlayerRuntimeChanged;
		GlobalSession.ArakawaRuntimeChanged += OnArakawaRuntimeChanged;
		ConfigureCameraForBattle();

		GD.Print($"BattleSceneController: layout={layout.LayoutId}, size={layout.BoardSize}, objects={Registry.Count}");
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
			_hud.ArakawaWheelRequested -= OnArakawaWheelRequested;
			_hud.ArakawaAbilityRequested -= OnArakawaAbilityRequested;
			_hud.ArakawaCancelRequested -= OnArakawaCancelRequested;
			_hud.MeditateRequested -= OnMeditateRequested;
			_hud.CardRequested -= OnCardRequested;
			_hud.EndTurnRequested -= OnEndTurnRequested;
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
		}

		BattleBoardOverlay? overlay = GetNodeOrNull<BattleBoardOverlay>("RoomContainer/BoardOverlay");
		if (overlay == null)
		{
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

		BattleObjectState? playerState = StateManager.GetPrimaryPlayerState();
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
			overlay.SetAttackTargetCells(BuildSelectedCardTargetCells(playerState.ObjectId), playerState.Cell);
			overlay.SetSupportTargetCells(Array.Empty<Vector2I>());
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

		TryMoveObject(playerState.ObjectId, targetCell, out _);
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

	private void OnPlayerRuntimeChanged()
	{
		StateManager?.SyncPlayerFromSession();
	}

	private void OnArakawaRuntimeChanged()
	{
	}

	private void OnEndTurnRequested()
	{
		if (_battleFailureSequenceStarted)
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

		if (!TurnState.CanSelectCard)
		{
			return;
		}

		_playerDeck.DiscardHand();
		_playerDeck.DrawToHandSize();
		TurnState.MarkActed();
		ResolveTurnPostPhase();
	}

	private async void OnDefendRequested()
	{
		if (_battleFailureSequenceStarted || TurnState == null || StateManager == null || _actionService == null)
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

		await _actionService.ApplyDefenseActionAsync(playerState.ObjectId, BasicDefenseAction, TurnState.TurnIndex);
		TurnState.MarkActed();
		ResolveTurnPostPhase();
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
		BoardObject? enemyTarget = GetEnemyUnitAtCell(sourceObjectId, targetCell);
		if (enemyTarget == null || Registry == null || !Registry.TryGet(sourceObjectId, out BoardObject? sourceObject) || sourceObject == null)
		{
			return null;
		}

		return cardDefinition.TargetingMode switch
		{
			BattleCardTargetingMode.EnemyUnit => GetManhattanTarget(sourceObject, enemyTarget, cardDefinition.Range),
			BattleCardTargetingMode.StraightLineEnemy => GetStraightLineTarget(sourceObjectId, enemyTarget, cardDefinition.Range),
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

	private BoardObject? GetEnemyUnitAtCell(string sourceObjectId, Vector2I targetCell)
	{
		BoardObject? attackableObject = GetAttackableObjectAtCell(sourceObjectId, targetCell);
		return attackableObject?.ObjectType == BoardObjectType.Unit ? attackableObject : null;
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

			_actionService.ApplyDamageToTarget(targetObject.ObjectId, cardInstance.Definition.Damage, out _, out string damageFailureReason);
			if (!string.IsNullOrWhiteSpace(damageFailureReason))
			{
				failureReason = damageFailureReason;
				return false;
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

			_actionService.ApplyShieldGainToTarget(attackerId, cardInstance.Definition.ShieldGain, out string shieldFailureReason);
			if (!string.IsNullOrWhiteSpace(shieldFailureReason))
			{
				failureReason = shieldFailureReason;
				return false;
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

		if (!_actionService.TryAttackObject(attackerId, targetId, out failureReason))
		{
			return false;
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
			await ToSignal(GetTree().CreateTimer(PlayerActionResolveBufferSeconds), SceneTreeTimer.SignalName.Timeout);
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

		TurnState.AdvanceToNextTurn();
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

	private void ApplyPendingBattleRequest()
	{
		if (GlobalSession == null)
		{
			return;
		}

		BattleRequest? request = GlobalSession.ConsumePendingBattleRequest();
		request?.ApplyToSession(GlobalSession);
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
			CommitBattleResult(true);
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
		tween.Finished += () => CommitBattleResult(true);
	}

	private void CommitBattleResult(bool didPlayerFail)
	{
		if (_battleResultCommitted || GlobalSession == null)
		{
			return;
		}

		_battleResultCommitted = true;
		GlobalSession.CompleteBattle(BattleResult.FromSession(GlobalSession, didPlayerFail));
		ReturnToPendingMapSceneIfAny();
	}

	private void ReturnToPendingMapSceneIfAny()
	{
		if (GlobalSession?.PeekPendingMapResumeContext() is not MapResumeContext resumeContext
			|| string.IsNullOrWhiteSpace(resumeContext.ScenePath))
		{
			return;
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

		Camera2D? camera = GetNodeOrNull<Camera2D>("Camera2D");
		if (camera == null)
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

		camera.Enabled = true;
		camera.Zoom = new Vector2(CameraZoom, CameraZoom);
		camera.Position = boardCenter + new Vector2(0.0f, cameraYOffset);
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

		bool created = await _actionService.TryCreateIndestructibleObstacleAsync(targetCell);
		if (!created)
		{
			GlobalSession.RestoreArakawaEnergy(BuildWallAbility.EnergyCost);
			return;
		}

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
			_ => new List<Vector2I>(),
		};
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

		if (targetObject.ObjectType != BoardObjectType.Unit)
		{
			failureReason = "Only unit targets can be targeted by cards.";
			return false;
		}

		if (attacker.Faction == targetObject.Faction)
		{
			failureReason = "Friendly targets cannot be targeted by this card.";
			return false;
		}

		switch (cardDefinition.TargetingMode)
		{
			case BattleCardTargetingMode.EnemyUnit:
				if (GetManhattanTarget(attacker, targetObject, cardDefinition.Range) == null)
				{
					failureReason = $"Target is out of range. Range={cardDefinition.Range}.";
					return false;
				}

				return true;

			case BattleCardTargetingMode.StraightLineEnemy:
				if (GetStraightLineTarget(attackerId, targetObject, cardDefinition.Range) == null)
				{
					failureReason = $"Target is not in a valid straight line. Range={cardDefinition.Range}.";
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
				"cross_slash",
				"交斩",
				"邻近 3 伤",
				1,
				BattleCardCategory.Attack,
				BattleCardTargetingMode.EnemyUnit,
				range: 1,
				damage: 3),
			new BattleCardDefinition(
				"quick_cut",
				"疾斩",
				"邻近 2 伤",
				0,
				BattleCardCategory.Attack,
				BattleCardTargetingMode.EnemyUnit,
				range: 1,
				damage: 2,
				isQuick: true),
			new BattleCardDefinition(
				"line_shot",
				"贯射",
				"直线首敌 2 伤",
				1,
				BattleCardCategory.Attack,
				BattleCardTargetingMode.StraightLineEnemy,
				range: 4,
				damage: 2),
			new BattleCardDefinition(
				"heavy_shot",
				"重铳",
				"直线首敌 5 伤",
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
				"调息",
				"抽 1 回 1 能",
				0,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				drawCount: 1,
				energyGain: 1,
				isQuick: true),
			new BattleCardDefinition(
				"surge",
				"蓄能",
				"回 2 能",
				1,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				energyGain: 2,
				exhaustsOnPlay: true),
			new BattleCardDefinition(
				"draw_spark",
				"灵感",
				"抽 1 回 1 能",
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
				"燃刃",
				"邻近 4 伤",
				1,
				BattleCardCategory.Attack,
				BattleCardTargetingMode.EnemyUnit,
				range: 1,
				damage: 4,
				exhaustsOnPlay: true),
			new BattleCardDefinition(
				"hook_shot",
				"钩射",
				"直线首敌 3 伤",
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
				"回 1 能 抽 1 张",
				0,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				drawCount: 1,
				energyGain: 1,
				isQuick: true),
			new BattleCardDefinition(
				"burst_drive",
				"爆驱",
				"回 2 能",
				0,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				energyGain: 2,
				exhaustsOnPlay: true),
			new BattleCardDefinition(
				"guard_up",
				"举盾",
				"得 3 盾",
				1,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				shieldGain: 3),
			new BattleCardDefinition(
				"brace",
				"架势",
				"得 5 盾",
				2,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				shieldGain: 5),
			new BattleCardDefinition(
				"quick_guard",
				"瞬守",
				"得 2 盾",
				0,
				BattleCardCategory.Skill,
				BattleCardTargetingMode.None,
				shieldGain: 2,
				isQuick: true),
		};
	}
}
