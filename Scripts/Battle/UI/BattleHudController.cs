using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using CardChessDemo.Battle.Cards;
using CardChessDemo.Battle.State;
using CardChessDemo.Battle.Turn;

namespace CardChessDemo.Battle.UI;

public partial class BattleHudController : CanvasLayer
{
	private const string BattleButtonFolderPath = "res://Assets/UI/Battle/Buttons";

	[Signal] public delegate void EndTurnRequestedEventHandler();
	[Signal] public delegate void AttackRequestedEventHandler();
	[Signal] public delegate void DefendRequestedEventHandler();
	[Signal] public delegate void RetreatRequestedEventHandler();
	[Signal] public delegate void ArakawaWheelRequestedEventHandler();
	[Signal] public delegate void ArakawaAbilityRequestedEventHandler(string abilityId);
	[Signal] public delegate void ArakawaCancelRequestedEventHandler();
	[Signal] public delegate void MeditateRequestedEventHandler();
	[Signal] public delegate void CardRequestedEventHandler(string cardInstanceId);

	private const float HoverOffsetX = 10.0f;
	private const float HoverOffsetY = -8.0f;
	private const float ScreenMargin = 4.0f;
	private const float SelectedCardLift = 8.0f;
	private const float CardWidth = 56.0f;
	private const float CardHeight = 54.0f;
	private const float PilePreviewCardWidth = 58.0f;
	private const float PilePreviewCardHeight = 96.0f;
	private const int MaxPilePreviewColumns = 3;
	private const float SpriteButtonSize = 28.0f;
	private const float SpriteIconSize = 16.0f;
	private const float WindowCloseButtonSize = 16.0f;
	private const float WindowCloseIconSize = 8.0f;
	private const float PileOverscrollPixels = 18.0f;

	private static readonly Dictionary<string, Texture2D> BattleButtonTextures = new(StringComparer.Ordinal);
	private readonly PackedScene _cardViewScene = GD.Load<PackedScene>("res://Scene/Battle/UI/BattleCardView.tscn");
	private readonly StyleBoxFlat _compactButtonStyle = new()
	{
		BgColor = new Color(0.18f, 0.18f, 0.22f, 0.88f),
		BorderColor = new Color(0.72f, 0.72f, 0.78f, 0.95f),
		BorderWidthLeft = 1,
		BorderWidthTop = 1,
		BorderWidthRight = 1,
		BorderWidthBottom = 1,
		CornerRadiusTopLeft = 2,
		CornerRadiusTopRight = 2,
		CornerRadiusBottomLeft = 2,
		CornerRadiusBottomRight = 2,
		ContentMarginLeft = 0,
		ContentMarginTop = -2,
		ContentMarginRight = 0,
		ContentMarginBottom = -2,
	};

	private PanelContainer _hoveredUnitPanel = null!;
	private Label _hoveredUnitTitle = null!;
	private Label _hoveredUnitStats = null!;
	private PanelContainer _hoveredCardPanel = null!;
	private Label _hoveredCardTitle = null!;
	private Label _hoveredCardStats = null!;
	private Button _actionLogDismissButton = null!;
	private PanelContainer _actionLogPopup = null!;
	private Label _actionLogTitle = null!;
	private Button _actionLogCloseButton = null!;
	private RichTextLabel _actionLogBodyText = null!;
	private Button _pileDismissButton = null!;
	private PanelContainer _pilePopup = null!;
	private Label _pilePopupTitle = null!;
	private Button _pilePopupCloseButton = null!;
	private TabContainer _pileTabs = null!;
	private Label _drawPileEmptyLabel = null!;
	private ScrollContainer _drawPileScroll = null!;
	private VBoxContainer _drawPileContent = null!;
	private GridContainer _drawPileGrid = null!;
	private Control _drawPileOverscrollSpacer = null!;
	private Label _discardPileEmptyLabel = null!;
	private ScrollContainer _discardPileScroll = null!;
	private VBoxContainer _discardPileContent = null!;
	private GridContainer _discardPileGrid = null!;
	private Control _discardPileOverscrollSpacer = null!;
	private Label _exhaustPileEmptyLabel = null!;
	private ScrollContainer _exhaustPileScroll = null!;
	private VBoxContainer _exhaustPileContent = null!;
	private GridContainer _exhaustPileGrid = null!;
	private Control _exhaustPileOverscrollSpacer = null!;
	private Label _turnLabel = null!;
	private Label _resourceLabel = null!;
	private Label _arakawaEnergyLabel = null!;
	private Button _arakawaButton = null!;
	private Button _actionLogButton = null!;
	private Button _pileButton = null!;
	private Button _attackButton = null!;
	private Button _defendButton = null!;
	private Button _meditateButton = null!;
	private Button _retreatButton = null!;
	private Button _endTurnButton = null!;
	private Control _arakawaWheel = null!;
	private Button _arakawaBuildButton = null!;
	private Button _arakawaEnhanceButton = null!;
	private Button _arakawaWeaponButton = null!;
	private Button _arakawaCancelButton = null!;
	private Control _bottomHand = null!;
	private Control _handArea = null!;
	private Control _cardFxRoot = null!;

	private readonly Dictionary<string, BattleCardView> _cardViews = new(StringComparer.Ordinal);
	private readonly List<BattleCardView> _pileCardViews = new();
	private readonly Dictionary<Button, TextureRect> _buttonIconRects = new();
	private readonly Dictionary<Button, TextureRect> _buttonBackgroundRects = new();
	private readonly Dictionary<Button, TextureRect> _windowCloseIconRects = new();
	private readonly Dictionary<Button, TextureRect> _windowCloseBackgroundRects = new();
	private readonly Dictionary<ScrollContainer, Control> _pileOverscrollSpacers = new();
	private readonly Dictionary<ScrollContainer, Tween> _pileOverscrollTweens = new();

	private TurnActionState? _turnState;
	private BattleObjectState? _hoveredUnitState;
	private Vector2 _hoveredUnitScreenPosition;
	private BattleCardInstance? _hoveredCard;
	private Vector2 _hoveredCardScreenPosition;
	private BattleCardInstance[] _handCards = Array.Empty<BattleCardInstance>();
	private BattleCardInstance[] _drawPileCards = Array.Empty<BattleCardInstance>();
	private BattleCardInstance[] _discardPileCards = Array.Empty<BattleCardInstance>();
	private BattleCardInstance[] _exhaustPileCards = Array.Empty<BattleCardInstance>();
	private string _lastPileSignature = string.Empty;
	private string[] _currentTurnActionLogEntries = Array.Empty<string>();
	private string[] _previousTurnActionLogEntries = Array.Empty<string>();
	private int _currentTurnActionLogTurnIndex;
	private int _previousTurnActionLogTurnIndex;
	private int _currentEnergy;
	private int _maxEnergy;
	private int _arakawaCurrentEnergy;
	private int _arakawaMaxEnergy;
	private bool _canUseArakawa;
	private bool _showRetreatButton;
	private bool _isArakawaWheelOpen;
	private bool _isActionLogOpen;
	private string _selectedArakawaAbilityId = string.Empty;
	private int _energyRechargeProgress;
	private int _energyRechargeInterval = 3;
	private string _selectedCardInstanceId = string.Empty;
	private string _lastHandSignature = string.Empty;
	private bool _signalsHooked;

	public override void _Ready()
	{
		if (!EnsureNodes())
		{
			return;
		}

		HookSignals();
		Refresh();
	}

	public override void _ExitTree()
	{
		if (!_signalsHooked)
		{
			return;
		}

		_attackButton.Pressed -= OnAttackPressed;
		_defendButton.Pressed -= OnDefendPressed;
		_retreatButton.Pressed -= OnRetreatPressed;
		_arakawaButton.Pressed -= OnArakawaButtonPressed;
		_actionLogButton.Pressed -= OnActionLogPressed;
		_arakawaBuildButton.Pressed -= OnArakawaBuildPressed;
		_arakawaEnhanceButton.Pressed -= OnArakawaEnhancePressed;
		_arakawaWeaponButton.Pressed -= OnArakawaWeaponPressed;
		_arakawaCancelButton.Pressed -= OnArakawaCancelPressed;
		_meditateButton.Pressed -= OnMeditatePressed;
		_endTurnButton.Pressed -= OnEndTurnPressed;
		_pileButton.Pressed -= OnPileButtonPressed;
		_actionLogDismissButton.Pressed -= OnActionLogClosePressed;
		_actionLogCloseButton.Pressed -= OnActionLogClosePressed;
		_pileDismissButton.Pressed -= OnPilePopupClosePressed;
		_pilePopupCloseButton.Pressed -= OnPilePopupClosePressed;
		_handArea.Resized -= OnHandAreaResized;
		_signalsHooked = false;
	}

	public void Bind(TurnActionState turnState)
	{
		_turnState = turnState;
		if (IsNodeReady())
		{
			Refresh();
		}
	}

	public void SetCardState(
		int currentEnergy,
		int maxEnergy,
		int energyRechargeProgress,
		int energyRechargeInterval,
		IReadOnlyList<BattleCardInstance> handCards,
		string selectedCardInstanceId,
		IReadOnlyList<BattleCardInstance> drawPileCards,
		IReadOnlyList<BattleCardInstance> discardPileCards,
		IReadOnlyList<BattleCardInstance> exhaustPileCards)
	{
		_currentEnergy = currentEnergy;
		_maxEnergy = maxEnergy;
		_energyRechargeProgress = energyRechargeProgress;
		_energyRechargeInterval = Math.Max(1, energyRechargeInterval);
		_handCards = handCards.ToArray();
		_selectedCardInstanceId = selectedCardInstanceId ?? string.Empty;
		_drawPileCards = drawPileCards.ToArray();
		_discardPileCards = discardPileCards.ToArray();
		_exhaustPileCards = exhaustPileCards.ToArray();
	}

	public void SetArakawaState(int currentEnergy, int maxEnergy, bool canUse, bool isWheelOpen, string selectedAbilityId)
	{
		_arakawaCurrentEnergy = currentEnergy;
		_arakawaMaxEnergy = maxEnergy;
		_canUseArakawa = canUse;
		_isArakawaWheelOpen = isWheelOpen;
		_selectedArakawaAbilityId = selectedAbilityId ?? string.Empty;
	}

	public void SetRetreatActionState(bool visible)
	{
		_showRetreatButton = visible;
	}

	public void SetActionLogState(
		int currentTurnIndex,
		IReadOnlyList<string> currentTurnEntries,
		int previousTurnIndex,
		IReadOnlyList<string> previousTurnEntries)
	{
		_currentTurnActionLogTurnIndex = currentTurnIndex;
		_previousTurnActionLogTurnIndex = previousTurnIndex;
		_currentTurnActionLogEntries = currentTurnEntries?.ToArray() ?? Array.Empty<string>();
		_previousTurnActionLogEntries = previousTurnEntries?.ToArray() ?? Array.Empty<string>();
	}

	public void SetHoveredUnitState(BattleObjectState? hoveredUnitState, Vector2 screenPosition)
	{
		_hoveredUnitState = hoveredUnitState;
		_hoveredUnitScreenPosition = screenPosition;
		if (IsNodeReady())
		{
			RefreshHoveredUnit();
		}
	}

	public void PlayCardUseEffect(BattleCardInstance cardInstance)
	{
		if (!EnsureNodes() || !_cardViews.TryGetValue(cardInstance.InstanceId, out BattleCardView? sourceView))
		{
			return;
		}

		BattleCardView fxView = _cardViewScene.Instantiate<BattleCardView>();
		_cardFxRoot.AddChild(fxView);
		fxView.Bind(cardInstance, false, true);
		fxView.Size = new Vector2(CardWidth, CardHeight);
		fxView.Position = _cardFxRoot.GetGlobalTransformWithCanvas().AffineInverse() * sourceView.GlobalPosition;
		fxView.Disabled = true;
		fxView.MouseFilter = Control.MouseFilterEnum.Ignore;
		fxView.ZIndex = 30;

		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		Vector2 targetPosition = new(viewportSize.X * 0.5f - CardWidth * 0.5f, viewportSize.Y * 0.45f - CardHeight * 0.5f);
		Vector2 localTarget = _cardFxRoot.GetGlobalTransformWithCanvas().AffineInverse() * targetPosition;

		Tween tween = CreateTween();
		tween.SetParallel();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(fxView, "position", localTarget, 0.18f);
		tween.TweenProperty(fxView, "scale", new Vector2(1.12f, 1.12f), 0.18f);
		tween.TweenProperty(fxView, "modulate:a", 0.0f, 0.14f).SetDelay(0.08f);
		tween.Finished += fxView.QueueFree;
	}

	public void PlayCardEnhancementEffect(string cardInstanceId)
	{
		if (_cardViews.TryGetValue(cardInstanceId, out BattleCardView? cardView))
		{
			cardView.PlayEnhancementPulse();
		}
	}

	public override void _Process(double delta)
	{
		Refresh();
	}

	public override void _Input(InputEvent @event)
	{
		if (_pilePopup != null && _pilePopup.Visible && @event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				Vector2 clickPosition = mouseButton.Position;
				if (_pilePopupCloseButton != null && _pilePopupCloseButton.GetGlobalRect().HasPoint(clickPosition))
				{
					ClosePilePopup();
					GetViewport().SetInputAsHandled();
					return;
				}

				if (!_pilePopup.GetGlobalRect().HasPoint(clickPosition))
				{
					ClosePilePopup();
					GetViewport().SetInputAsHandled();
					return;
				}
			}

			if (mouseButton.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
			{
				OnPilePopupGuiInput(@event);
				return;
			}
		}
	}

	private void Refresh()
	{
		if (_turnState == null || !IsNodeReady() || !EnsureNodes())
		{
			return;
		}

		_turnLabel.Text = BuildTurnLabel();
		_resourceLabel.Text = $"E{_currentEnergy}/{_maxEnergy} R{_energyRechargeProgress}/{_energyRechargeInterval}";
		_arakawaEnergyLabel.Text = $"荒川 {_arakawaCurrentEnergy}/{_arakawaMaxEnergy}";
		_arakawaButton.Disabled = !_canUseArakawa && !_isArakawaWheelOpen;
		_arakawaButton.Text = string.IsNullOrWhiteSpace(_selectedArakawaAbilityId) ? "Ark" : "Ark*";
		_arakawaWheel.Visible = _isArakawaWheelOpen;
		_arakawaBuildButton.Disabled = !_canUseArakawa || _arakawaCurrentEnergy <= 0;
		_arakawaEnhanceButton.Disabled = !_canUseArakawa || _arakawaCurrentEnergy <= 0;
		_arakawaWeaponButton.Disabled = !_canUseArakawa || _arakawaCurrentEnergy <= 0;
		_actionLogPopup.Visible = _isActionLogOpen;
		_actionLogDismissButton.Visible = _isActionLogOpen;
		_actionLogButton.Text = string.Empty;
		_actionLogButton.TooltipText = "战斗日志";
		_pileButton.Text = string.Empty;
		_pileButton.TooltipText = $"牌堆  抽:{_drawPileCards.Length} 弃:{_discardPileCards.Length} 消:{_exhaustPileCards.Length}";
		_attackButton.Visible = _turnState.CanEnterAttackTargeting || _turnState.IsAttackTargeting;
		_attackButton.Text = string.Empty;
		_attackButton.TooltipText = _turnState.IsAttackTargeting ? "取消攻击" : "攻击";
		_attackButton.Disabled = !_turnState.CanEnterAttackTargeting && !_turnState.IsAttackTargeting;
		_defendButton.Text = string.Empty;
		_defendButton.TooltipText = "防御";
		_defendButton.Disabled = !_turnState.CanSelectCard;
		_meditateButton.Text = string.Empty;
		_meditateButton.TooltipText = "冥想";
		_meditateButton.Disabled = !_turnState.CanSelectCard;
		_retreatButton.Visible = _showRetreatButton;
		_retreatButton.Text = string.Empty;
		_retreatButton.TooltipText = "逃跑";
		_retreatButton.Disabled = !_showRetreatButton || !_turnState.CanRetreat;
		_endTurnButton.Disabled = !_turnState.IsPlayerTurn && !_turnState.IsAttackTargeting && !_turnState.IsCardTargeting;

		UpdateSpriteButtonLayouts();
		UpdateWindowCloseButtonLayouts();
		RefreshPilePopup();
		RefreshHandViews();
		RefreshHoveredUnit();
		RefreshHoveredCard();
		RefreshActionLogPopup();
	}

	private void RefreshHoveredUnit()
	{
		if (_hoveredUnitState == null)
		{
			_hoveredUnitPanel.Visible = false;
			return;
		}

		_hoveredUnitPanel.Visible = true;
		_hoveredUnitTitle.Text = _hoveredUnitState.DisplayName;
		string hpText = _hoveredUnitState.MaxHp > 0 ? $"{_hoveredUnitState.CurrentHp}/{_hoveredUnitState.MaxHp}" : "-";
		string defenseText = _hoveredUnitState.HasDefenseStance ? $" DF {_hoveredUnitState.DefenseDamageReductionPercent}%" : string.Empty;
		_hoveredUnitStats.Text = $"HP {hpText} SH {_hoveredUnitState.CurrentShield}{defenseText}";
		_hoveredUnitTitle.ResetSize();
		_hoveredUnitStats.ResetSize();
		_hoveredUnitPanel.ResetSize();
		PositionFloatingPanel(_hoveredUnitPanel, _hoveredUnitScreenPosition);
	}

	private void RefreshHoveredCard()
	{
		if (_hoveredCard == null)
		{
			_hoveredCardPanel.Visible = false;
			return;
		}

		StringBuilder line = new();
		line.Append(_hoveredCard.Definition.Description);
		if (_hoveredCard.Definition.IsQuick)
		{
			line.Append(" Quick");
		}

		if (_hoveredCard.Definition.ExhaustsOnPlay)
		{
			line.Append(" Exhaust");
		}

		_hoveredCardPanel.Visible = true;
		_hoveredCardTitle.Text = $"{_hoveredCard.Definition.DisplayName} C{_hoveredCard.Definition.Cost}";
		_hoveredCardStats.Text = line.ToString();
		PositionFloatingPanel(_hoveredCardPanel, _hoveredCardScreenPosition);
	}

	private void RefreshActionLogPopup()
	{
		if (!_isActionLogOpen)
		{
			return;
		}

		List<string> lines = new();
		lines.Add($"[b]本回合 T{_currentTurnActionLogTurnIndex}[/b]");
		if (_currentTurnActionLogEntries.Length > 0)
		{
			lines.AddRange(_currentTurnActionLogEntries);
		}
		else
		{
			lines.Add("暂无动作记录");
		}

		lines.Add(string.Empty);
		lines.Add($"[b]上一回合 T{_previousTurnActionLogTurnIndex}[/b]");
		if (_previousTurnActionLogEntries.Length > 0)
		{
			lines.AddRange(_previousTurnActionLogEntries);
		}
		else
		{
			lines.Add("暂无动作记录");
		}

		_actionLogTitle.Text = "战斗记录";
		_actionLogBodyText.Text = string.Join('\n', lines);
		_actionLogBodyText.ScrollToLine(Math.Max(0, _actionLogBodyText.GetLineCount() - 1));
	}

	private void RefreshPilePopup(bool forceRebuild = false)
	{
		if (!_pilePopup.Visible)
		{
			return;
		}

		string pileSignature = string.Join("|", _drawPileCards.Select(card => card.InstanceId))
			+ "#"
			+ string.Join("|", _discardPileCards.Select(card => card.InstanceId))
			+ "#"
			+ string.Join("|", _exhaustPileCards.Select(card => card.InstanceId));
		if (!forceRebuild && pileSignature == _lastPileSignature)
		{
			return;
		}

		foreach (BattleCardView cardView in _pileCardViews)
		{
			if (IsInstanceValid(cardView))
			{
				cardView.QueueFree();
			}
		}

		_pileCardViews.Clear();
		PopulatePileTab(_drawPileGrid, _drawPileScroll, _drawPileEmptyLabel, _drawPileCards, reverseOrder: true);
		PopulatePileTab(_discardPileGrid, _discardPileScroll, _discardPileEmptyLabel, _discardPileCards, reverseOrder: true);
		PopulatePileTab(_exhaustPileGrid, _exhaustPileScroll, _exhaustPileEmptyLabel, _exhaustPileCards, reverseOrder: true);

		_pilePopupTitle.Text = "牌堆查看";
		_pileTabs.SetTabTitle(0, $"抽牌堆 {_drawPileCards.Length}");
		_pileTabs.SetTabTitle(1, $"弃牌堆 {_discardPileCards.Length}");
		_pileTabs.SetTabTitle(2, $"消耗堆 {_exhaustPileCards.Length}");
		_lastPileSignature = pileSignature;
	}

	private void PositionFloatingPanel(Control panel, Vector2 screenPosition)
	{
		panel.ResetSize();
		Vector2 panelSize = panel.GetCombinedMinimumSize();
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		panelSize = new Vector2(
			Mathf.Min(panelSize.X, Math.Max(1.0f, viewportSize.X - ScreenMargin * 2.0f)),
			Mathf.Min(panelSize.Y, Math.Max(1.0f, viewportSize.Y - ScreenMargin * 2.0f)));
		panel.Size = panelSize;
		Vector2 desiredPosition = screenPosition + new Vector2(HoverOffsetX, HoverOffsetY);

		if (desiredPosition.X + panelSize.X > viewportSize.X - ScreenMargin)
		{
			desiredPosition.X = screenPosition.X - panelSize.X - HoverOffsetX;
		}

		if (desiredPosition.Y + panelSize.Y > viewportSize.Y - ScreenMargin)
		{
			desiredPosition.Y = screenPosition.Y - panelSize.Y - 8.0f;
		}

		float maxX = Mathf.Max(ScreenMargin, viewportSize.X - panelSize.X - ScreenMargin);
		float maxY = Mathf.Max(ScreenMargin, viewportSize.Y - panelSize.Y - ScreenMargin);
		desiredPosition.X = Mathf.Clamp(desiredPosition.X, ScreenMargin, maxX);
		desiredPosition.Y = Mathf.Clamp(desiredPosition.Y, ScreenMargin, maxY);
		panel.Position = desiredPosition;
	}

	private void RefreshHandViews()
	{
		string handSignature = string.Join(
			"|",
			_handCards.Select(card =>
				$"{card.InstanceId}:{card.Definition.DisplayName}:{card.Definition.Cost}:{card.Definition.IsQuick}:{card.Definition.ExhaustsOnPlay}"))
			+ $"|sel:{_selectedCardInstanceId}|en:{_currentEnergy}";

		if (handSignature == _lastHandSignature)
		{
			LayoutCardViews();
			return;
		}

		foreach (Node child in _handArea.GetChildren())
		{
			child.QueueFree();
		}

		_cardViews.Clear();

		foreach (BattleCardInstance card in _handCards)
		{
			BattleCardView cardView = _cardViewScene.Instantiate<BattleCardView>();
			_handArea.AddChild(cardView);

			bool isPlayable = _turnState?.CanSelectCard == true && card.Definition.Cost <= _currentEnergy;
			bool isSelected = string.Equals(card.InstanceId, _selectedCardInstanceId, StringComparison.Ordinal);
			cardView.Bind(card, isSelected, isPlayable);
			cardView.Pressed += () => EmitSignal(SignalName.CardRequested, card.InstanceId);
			cardView.MouseEntered += () => OnCardMouseEntered(card);
			cardView.MouseExited += OnCardMouseExited;
			_cardViews[card.InstanceId] = cardView;
		}

		_lastHandSignature = handSignature;
		LayoutCardViews();
	}

	private void LayoutCardViews()
	{
		if (_cardViews.Count == 0)
		{
			return;
		}

		int cardCount = _handCards.Length;
		float availableWidth = Mathf.Max(CardWidth, _handArea.Size.X);
		float spacing = cardCount <= 1
			? 0.0f
			: Mathf.Clamp((availableWidth - CardWidth * cardCount) / Math.Max(1, cardCount - 1), -22.0f, 4.0f);

		float totalWidth = CardWidth * cardCount + spacing * Mathf.Max(0, cardCount - 1);
		float startX = Mathf.Max(0.0f, (availableWidth - totalWidth) * 0.5f);

		for (int index = 0; index < _handCards.Length; index++)
		{
			BattleCardInstance card = _handCards[index];
			if (!_cardViews.TryGetValue(card.InstanceId, out BattleCardView? cardView))
			{
				continue;
			}

			bool isPlayable = _turnState?.CanSelectCard == true && card.Definition.Cost <= _currentEnergy;
			bool isSelected = string.Equals(card.InstanceId, _selectedCardInstanceId, StringComparison.Ordinal);
			cardView.Bind(card, isSelected, isPlayable);
			cardView.Size = new Vector2(CardWidth, CardHeight);
			cardView.Position = new Vector2(startX + index * (CardWidth + spacing), isSelected ? 0.0f : SelectedCardLift);
			cardView.ZIndex = isSelected ? 10 : index;
		}
	}

	private void PopulatePileTab(
		GridContainer grid,
		ScrollContainer scroll,
		Label emptyLabel,
		IReadOnlyList<BattleCardInstance> cards,
		bool reverseOrder)
	{
		int columns = Mathf.Clamp(cards.Count >= MaxPilePreviewColumns ? MaxPilePreviewColumns : Math.Max(1, cards.Count), 1, MaxPilePreviewColumns);
		grid.Columns = columns;
		grid.AddThemeConstantOverride("h_separation", 10);
		grid.AddThemeConstantOverride("v_separation", 10);

		GetActiveOverscrollSpacerForGrid(grid).CustomMinimumSize = Vector2.Zero;
		scroll.GetVScrollBar().Value = 0.0f;

		if (cards.Count == 0)
		{
			emptyLabel.Visible = true;
			scroll.Visible = false;
			return;
		}

		emptyLabel.Visible = false;
		scroll.Visible = true;
		IEnumerable<BattleCardInstance> sourceCards = reverseOrder ? cards.Reverse() : cards;
		foreach (BattleCardInstance card in sourceCards)
		{
			BattleCardView cardView = _cardViewScene.Instantiate<BattleCardView>();
			grid.AddChild(cardView);
			cardView.Bind(card, false, true);
			cardView.CustomMinimumSize = new Vector2(PilePreviewCardWidth, PilePreviewCardHeight);
			cardView.Size = new Vector2(PilePreviewCardWidth, PilePreviewCardHeight);
			cardView.Disabled = true;
			cardView.FocusMode = Control.FocusModeEnum.None;
			cardView.MouseFilter = Control.MouseFilterEnum.Ignore;
			_pileCardViews.Add(cardView);
		}
	}

	private void OpenPilePopup()
	{
		CloseActionLogPopup();
		SetBattleUiInputBlocked(true);
		_pileDismissButton.Visible = true;
		_pilePopup.Visible = true;
		_pileDismissButton.MoveToFront();
		_pilePopup.MoveToFront();
		RefreshPilePopup(true);
	}

	private void ClosePilePopup()
	{
		foreach (BattleCardView cardView in _pileCardViews)
		{
			if (IsInstanceValid(cardView))
			{
				cardView.QueueFree();
			}
		}

		_pileCardViews.Clear();
		_pileDismissButton.Visible = false;
		_pilePopup.Visible = false;
		SetBattleUiInputBlocked(false);
		_lastPileSignature = string.Empty;
	}

	private void OpenActionLogPopup()
	{
		ClosePilePopup();
		_isActionLogOpen = true;
		RefreshActionLogPopup();
		_actionLogDismissButton.Visible = true;
		_actionLogPopup.Visible = true;
	}

	private void CloseActionLogPopup()
	{
		_isActionLogOpen = false;
		_actionLogDismissButton.Visible = false;
		_actionLogPopup.Visible = false;
	}

	private bool EnsureNodes()
	{
		if (_turnLabel != null)
		{
			return true;
		}

		_hoveredUnitPanel = GetNodeOrNull<PanelContainer>("HoverPanel");
		_hoveredUnitTitle = GetNodeOrNull<Label>("HoverPanel/Margin/VBox/HoverTitle");
		_hoveredUnitStats = GetNodeOrNull<Label>("HoverPanel/Margin/VBox/HoverStats");
		_hoveredCardPanel = GetNodeOrNull<PanelContainer>("CardHoverPanel");
		_hoveredCardTitle = GetNodeOrNull<Label>("CardHoverPanel/Margin/VBox/HoverTitle");
		_hoveredCardStats = GetNodeOrNull<Label>("CardHoverPanel/Margin/VBox/HoverStats");
		_actionLogDismissButton = GetNodeOrNull<Button>("ActionLogDismissButton");
		_actionLogPopup = GetNodeOrNull<PanelContainer>("ActionLogPopup");
		_actionLogTitle = GetNodeOrNull<Label>("ActionLogPopup/Margin/VBox/Header/TitleLabel");
		_actionLogCloseButton = GetNodeOrNull<Button>("ActionLogPopup/Margin/VBox/Header/CloseButton");
		_actionLogBodyText = GetNodeOrNull<RichTextLabel>("ActionLogPopup/Margin/VBox/BodyScroll/BodyText");
		_pileDismissButton = GetNodeOrNull<Button>("PileDismissButton");
		_pilePopup = GetNodeOrNull<PanelContainer>("PilePopup");
		_pilePopupTitle = GetNodeOrNull<Label>("PilePopup/Margin/VBox/Header/TitleLabel");
		_pilePopupCloseButton = GetNodeOrNull<Button>("PilePopup/Margin/VBox/Header/CloseButton");
		_pileTabs = GetNodeOrNull<TabContainer>("PilePopup/Margin/VBox/PileTabs");
		_drawPileEmptyLabel = GetNodeOrNull<Label>("PilePopup/Margin/VBox/PileTabs/DrawTab/EmptyLabel");
		_drawPileScroll = GetNodeOrNull<ScrollContainer>("PilePopup/Margin/VBox/PileTabs/DrawTab/PileScroll");
		_drawPileContent = GetNodeOrNull<VBoxContainer>("PilePopup/Margin/VBox/PileTabs/DrawTab/PileScroll/PileContent");
		_drawPileGrid = GetNodeOrNull<GridContainer>("PilePopup/Margin/VBox/PileTabs/DrawTab/PileScroll/PileContent/PileGrid");
		_drawPileOverscrollSpacer = GetNodeOrNull<Control>("PilePopup/Margin/VBox/PileTabs/DrawTab/PileScroll/PileContent/OverscrollSpacer");
		_discardPileEmptyLabel = GetNodeOrNull<Label>("PilePopup/Margin/VBox/PileTabs/DiscardTab/EmptyLabel");
		_discardPileScroll = GetNodeOrNull<ScrollContainer>("PilePopup/Margin/VBox/PileTabs/DiscardTab/PileScroll");
		_discardPileContent = GetNodeOrNull<VBoxContainer>("PilePopup/Margin/VBox/PileTabs/DiscardTab/PileScroll/PileContent");
		_discardPileGrid = GetNodeOrNull<GridContainer>("PilePopup/Margin/VBox/PileTabs/DiscardTab/PileScroll/PileContent/PileGrid");
		_discardPileOverscrollSpacer = GetNodeOrNull<Control>("PilePopup/Margin/VBox/PileTabs/DiscardTab/PileScroll/PileContent/OverscrollSpacer");
		_exhaustPileEmptyLabel = GetNodeOrNull<Label>("PilePopup/Margin/VBox/PileTabs/ExhaustTab/EmptyLabel");
		_exhaustPileScroll = GetNodeOrNull<ScrollContainer>("PilePopup/Margin/VBox/PileTabs/ExhaustTab/PileScroll");
		_exhaustPileContent = GetNodeOrNull<VBoxContainer>("PilePopup/Margin/VBox/PileTabs/ExhaustTab/PileScroll/PileContent");
		_exhaustPileGrid = GetNodeOrNull<GridContainer>("PilePopup/Margin/VBox/PileTabs/ExhaustTab/PileScroll/PileContent/PileGrid");
		_exhaustPileOverscrollSpacer = GetNodeOrNull<Control>("PilePopup/Margin/VBox/PileTabs/ExhaustTab/PileScroll/PileContent/OverscrollSpacer");
		_turnLabel = GetNodeOrNull<Label>("TopBar/LeftInfo/TurnLabel");
		_resourceLabel = GetNodeOrNull<Label>("TopBar/LeftInfo/ResourceLabel");
		_arakawaEnergyLabel = GetNodeOrNull<Label>("TopBar/ArakawaInfo/ArakawaEnergyLabel");
		_arakawaButton = GetNodeOrNull<Button>("TopBar/ArakawaInfo/ArakawaButton");
		_actionLogButton = GetNodeOrNull<Button>("RightControls/ActionLogButton");
		_pileButton = GetNodeOrNull<Button>("RightControls/PileButton");
		_attackButton = GetNodeOrNull<Button>("RightControls/AttackButton");
		_defendButton = GetNodeOrNull<Button>("RightControls/DefendButton");
		_meditateButton = GetNodeOrNull<Button>("RightControls/MeditateButton");
		_retreatButton = GetNodeOrNull<Button>("RightControls/RetreatButton");
		_endTurnButton = GetNodeOrNull<Button>("RightControls/EndTurnButton");
		_arakawaWheel = GetNodeOrNull<Control>("ArakawaWheel");
		_arakawaBuildButton = GetNodeOrNull<Button>("ArakawaWheel/BuildButton");
		_arakawaEnhanceButton = GetNodeOrNull<Button>("ArakawaWheel/EnhanceButton");
		_arakawaWeaponButton = GetNodeOrNull<Button>("ArakawaWheel/WeaponButton");
		_arakawaCancelButton = GetNodeOrNull<Button>("ArakawaWheel/CancelButton");
		_bottomHand = GetNodeOrNull<Control>("BottomHand");
		_handArea = GetNodeOrNull<Control>("BottomHand/HandArea");
		_cardFxRoot = GetNodeOrNull<Control>("CardFxRoot");

		if (_drawPileScroll != null && _drawPileOverscrollSpacer != null)
		{
			_pileOverscrollSpacers[_drawPileScroll] = _drawPileOverscrollSpacer;
		}

		if (_discardPileScroll != null && _discardPileOverscrollSpacer != null)
		{
			_pileOverscrollSpacers[_discardPileScroll] = _discardPileOverscrollSpacer;
		}

		if (_exhaustPileScroll != null && _exhaustPileOverscrollSpacer != null)
		{
			_pileOverscrollSpacers[_exhaustPileScroll] = _exhaustPileOverscrollSpacer;
		}

		return _hoveredUnitPanel != null
			&& _hoveredUnitTitle != null
			&& _hoveredUnitStats != null
			&& _hoveredCardPanel != null
			&& _hoveredCardTitle != null
			&& _hoveredCardStats != null
			&& _actionLogDismissButton != null
			&& _actionLogPopup != null
			&& _actionLogTitle != null
			&& _actionLogCloseButton != null
			&& _actionLogBodyText != null
			&& _pileDismissButton != null
			&& _pilePopup != null
			&& _pilePopupTitle != null
			&& _pilePopupCloseButton != null
			&& _pileTabs != null
			&& _drawPileEmptyLabel != null
			&& _drawPileScroll != null
			&& _drawPileContent != null
			&& _drawPileGrid != null
			&& _drawPileOverscrollSpacer != null
			&& _discardPileEmptyLabel != null
			&& _discardPileScroll != null
			&& _discardPileContent != null
			&& _discardPileGrid != null
			&& _discardPileOverscrollSpacer != null
			&& _exhaustPileEmptyLabel != null
			&& _exhaustPileScroll != null
			&& _exhaustPileContent != null
			&& _exhaustPileGrid != null
			&& _exhaustPileOverscrollSpacer != null
			&& _turnLabel != null
			&& _resourceLabel != null
			&& _arakawaEnergyLabel != null
			&& _arakawaButton != null
			&& _actionLogButton != null
			&& _pileButton != null
			&& _attackButton != null
			&& _defendButton != null
			&& _meditateButton != null
			&& _retreatButton != null
			&& _endTurnButton != null
			&& _arakawaWheel != null
			&& _arakawaBuildButton != null
			&& _arakawaEnhanceButton != null
			&& _arakawaWeaponButton != null
			&& _arakawaCancelButton != null
			&& _bottomHand != null
			&& _handArea != null
			&& _cardFxRoot != null;
	}

	private void HookSignals()
	{
		if (_signalsHooked || !EnsureNodes())
		{
			return;
		}

		ApplyBattleSpriteButton(_pileButton, "pile", "牌堆");
		ApplyBattleSpriteButton(_actionLogButton, "log", "战斗日志");
		ApplyBattleSpriteButton(_attackButton, "attack", "攻击");
		ApplyBattleSpriteButton(_defendButton, "defend", "防御");
		ApplyBattleSpriteButton(_retreatButton, "retreat", "逃跑");
		ApplyBattleSpriteButton(_meditateButton, "meditate", "冥想");
		ApplyCompactButtonStyle(_arakawaButton);
		ApplyCompactButtonStyle(_arakawaBuildButton);
		ApplyCompactButtonStyle(_arakawaEnhanceButton);
		ApplyCompactButtonStyle(_arakawaWeaponButton);
		ApplyCompactButtonStyle(_arakawaCancelButton);
		ApplyCompactButtonStyle(_endTurnButton);
		ApplyWindowCloseButtonStyle(_actionLogCloseButton);
		ApplyWindowCloseButtonStyle(_pilePopupCloseButton);
		DisableButtonFocus(_actionLogDismissButton);
		DisableButtonFocus(_pileDismissButton);
		SetupPileScrollBounce(_drawPileScroll, _drawPileOverscrollSpacer);
		SetupPileScrollBounce(_discardPileScroll, _discardPileOverscrollSpacer);
		SetupPileScrollBounce(_exhaustPileScroll, _exhaustPileOverscrollSpacer);

		_attackButton.Pressed += OnAttackPressed;
		_defendButton.Pressed += OnDefendPressed;
		_retreatButton.Pressed += OnRetreatPressed;
		_arakawaButton.Pressed += OnArakawaButtonPressed;
		_actionLogButton.Pressed += OnActionLogPressed;
		_arakawaBuildButton.Pressed += OnArakawaBuildPressed;
		_arakawaEnhanceButton.Pressed += OnArakawaEnhancePressed;
		_arakawaWeaponButton.Pressed += OnArakawaWeaponPressed;
		_arakawaCancelButton.Pressed += OnArakawaCancelPressed;
		_meditateButton.Pressed += OnMeditatePressed;
		_endTurnButton.Pressed += OnEndTurnPressed;
		_pileButton.Pressed += OnPileButtonPressed;
		_actionLogDismissButton.Pressed += OnActionLogClosePressed;
		_actionLogCloseButton.Pressed += OnActionLogClosePressed;
		_pileDismissButton.Pressed += OnPilePopupClosePressed;
		_pilePopupCloseButton.Pressed += OnPilePopupClosePressed;
		_handArea.Resized += OnHandAreaResized;
		_signalsHooked = true;
	}

	private void ApplyCompactButtonStyle(Button button)
	{
		DisableButtonFocus(button);

		StyleBoxFlat normal = _compactButtonStyle.Duplicate() as StyleBoxFlat ?? _compactButtonStyle;
		StyleBoxFlat hover = _compactButtonStyle.Duplicate() as StyleBoxFlat ?? _compactButtonStyle;
		StyleBoxFlat pressed = _compactButtonStyle.Duplicate() as StyleBoxFlat ?? _compactButtonStyle;
		StyleBoxFlat disabled = _compactButtonStyle.Duplicate() as StyleBoxFlat ?? _compactButtonStyle;
		hover.BgColor = hover.BgColor.Lightened(0.08f);
		pressed.BgColor = pressed.BgColor.Darkened(0.08f);
		disabled.BgColor = disabled.BgColor.Darkened(0.18f);
		disabled.BorderColor = disabled.BorderColor.Darkened(0.20f);

		button.AddThemeStyleboxOverride("normal", normal);
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("disabled", disabled);
	}

	private void ApplyWindowCloseButtonStyle(Button button)
	{
		DisableButtonFocus(button);
		button.Flat = true;
		button.Text = string.Empty;
		button.CustomMinimumSize = new Vector2(WindowCloseButtonSize, WindowCloseButtonSize);
		button.Size = button.CustomMinimumSize;
		button.MouseFilter = Control.MouseFilterEnum.Stop;
		button.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		button.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
		button.AddThemeStyleboxOverride("hover", new StyleBoxEmpty());
		button.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
		button.AddThemeStyleboxOverride("disabled", new StyleBoxEmpty());

		TextureRect backgroundRect;
		if (button.GetNodeOrNull<TextureRect>("CloseBgRect") is TextureRect existingBackground)
		{
			backgroundRect = existingBackground;
		}
		else
		{
			backgroundRect = new TextureRect
			{
				Name = "CloseBgRect",
				MouseFilter = Control.MouseFilterEnum.Ignore,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				Position = Vector2.Zero,
				Size = new Vector2(WindowCloseButtonSize, WindowCloseButtonSize),
				ZIndex = -1,
			};
			button.AddChild(backgroundRect);
			button.MoveChild(backgroundRect, 0);
		}

		TextureRect iconRect;
		if (button.GetNodeOrNull<TextureRect>("CloseIconRect") is TextureRect existingIcon)
		{
			iconRect = existingIcon;
		}
		else
		{
			iconRect = new TextureRect
			{
				Name = "CloseIconRect",
				MouseFilter = Control.MouseFilterEnum.Ignore,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				Position = Vector2.Zero,
				Size = new Vector2(WindowCloseIconSize, WindowCloseIconSize),
				ZIndex = 1,
			};
			button.AddChild(iconRect);
		}

		_windowCloseBackgroundRects[button] = backgroundRect;
		_windowCloseIconRects[button] = iconRect;
		LayoutWindowCloseButton(button);
	}

	private void ApplyBattleSpriteButton(Button button, string iconKey, string tooltip)
	{
		DisableButtonFocus(button);
		button.Text = string.Empty;
		button.TooltipText = tooltip;
		button.Flat = true;
		button.CustomMinimumSize = new Vector2(SpriteButtonSize, SpriteButtonSize);
		button.Size = new Vector2(SpriteButtonSize, SpriteButtonSize);
		button.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		button.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
		button.AddThemeStyleboxOverride("hover", new StyleBoxEmpty());
		button.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
		button.AddThemeStyleboxOverride("disabled", new StyleBoxEmpty());

		TextureRect backgroundRect;
		if (button.GetNodeOrNull<TextureRect>("BgRect") is TextureRect existingBackground)
		{
			backgroundRect = existingBackground;
		}
		else
		{
			backgroundRect = new TextureRect
			{
				Name = "BgRect",
				MouseFilter = Control.MouseFilterEnum.Ignore,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				Position = Vector2.Zero,
				Size = new Vector2(SpriteButtonSize, SpriteButtonSize),
				ZIndex = -1,
			};
			button.AddChild(backgroundRect);
			button.MoveChild(backgroundRect, 0);
		}

		TextureRect iconRect;
		if (_buttonIconRects.TryGetValue(button, out TextureRect? existingRect) && IsInstanceValid(existingRect))
		{
			iconRect = existingRect;
		}
		else
		{
			iconRect = new TextureRect
			{
				Name = "IconRect",
				MouseFilter = Control.MouseFilterEnum.Ignore,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				Position = Vector2.Zero,
				Size = new Vector2(SpriteIconSize, SpriteIconSize),
			};
			button.AddChild(iconRect);
			_buttonIconRects[button] = iconRect;
		}

		backgroundRect.Texture = ResolvePixelButtonBackgroundTexture();
		iconRect.Texture = ResolveBattleButtonTextureByName(iconKey);
		_buttonBackgroundRects[button] = backgroundRect;
		LayoutSpriteButton(button);
	}

	private static Texture2D ResolvePixelButtonBackgroundTexture()
	{
		const string cacheKey = "__pixel_button_bg__";
		if (BattleButtonTextures.TryGetValue(cacheKey, out Texture2D? cached) && cached != null)
		{
			return cached;
		}

		Image image = Image.CreateEmpty((int)SpriteButtonSize, (int)SpriteButtonSize, false, Image.Format.Rgba8);
		image.Fill(Colors.Transparent);
		Color fill = new(0.09f, 0.10f, 0.12f, 0.94f);
		Color border = new(0.86f, 0.88f, 0.92f, 0.98f);
		int width = (int)SpriteButtonSize;
		int height = (int)SpriteButtonSize;
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				bool clippedCorner =
					(x == 0 && y <= 1) || (x <= 1 && y == 0) ||
					(x == width - 1 && y <= 1) || (x >= width - 2 && y == 0) ||
					(x == 0 && y >= height - 2) || (x <= 1 && y == height - 1) ||
					(x == width - 1 && y >= height - 2) || (x >= width - 2 && y == height - 1);
				if (clippedCorner)
				{
					continue;
				}

				bool isBorder = x == 0 || y == 0 || x == width - 1 || y == height - 1
					|| ((x == 1 || x == width - 2) && (y <= 1 || y >= height - 2))
					|| ((y == 1 || y == height - 2) && (x <= 1 || x >= width - 2));
				image.SetPixel(x, y, isBorder ? border : fill);
			}
		}

		ImageTexture texture = ImageTexture.CreateFromImage(image);
		BattleButtonTextures[cacheKey] = texture;
		return texture;
	}

	private static Texture2D ResolveWindowCloseButtonBackgroundTexture(string stateKey)
	{
		string cacheKey = $"__window_close_bg_{stateKey}__";
		if (BattleButtonTextures.TryGetValue(cacheKey, out Texture2D? cached) && cached != null)
		{
			return cached;
		}

		Color fill = stateKey switch
		{
			"hover" => new Color(0.24f, 0.26f, 0.31f, 0.98f),
			"pressed" => new Color(0.42f, 0.16f, 0.16f, 0.98f),
			"disabled" => new Color(0.08f, 0.09f, 0.11f, 0.88f),
			_ => new Color(0.12f, 0.13f, 0.16f, 0.96f),
		};
		Color border = stateKey switch
		{
			"pressed" => new Color(1.0f, 0.82f, 0.82f, 1.0f),
			"disabled" => new Color(0.42f, 0.44f, 0.48f, 0.90f),
			_ => new Color(0.88f, 0.90f, 0.94f, 1.0f),
		};

		Image image = Image.CreateEmpty((int)WindowCloseButtonSize, (int)WindowCloseButtonSize, false, Image.Format.Rgba8);
		for (int y = 0; y < (int)WindowCloseButtonSize; y++)
		{
			for (int x = 0; x < (int)WindowCloseButtonSize; x++)
			{
				bool isBorder = x == 0 || y == 0 || x == (int)WindowCloseButtonSize - 1 || y == (int)WindowCloseButtonSize - 1;
				image.SetPixel(x, y, isBorder ? border : fill);
			}
		}

		ImageTexture texture = ImageTexture.CreateFromImage(image);
		BattleButtonTextures[cacheKey] = texture;
		return texture;
	}

	private static Texture2D ResolveWindowCloseButtonIconTexture(string stateKey)
	{
		string cacheKey = $"__window_close_icon_{stateKey}__";
		if (BattleButtonTextures.TryGetValue(cacheKey, out Texture2D? cached) && cached != null)
		{
			return cached;
		}

		Color color = stateKey switch
		{
			"pressed" => new Color(1.0f, 0.96f, 0.96f, 1.0f),
			"disabled" => new Color(0.58f, 0.60f, 0.64f, 0.92f),
			_ => new Color(0.96f, 0.97f, 1.0f, 1.0f),
		};

		Image image = Image.CreateEmpty((int)WindowCloseIconSize, (int)WindowCloseIconSize, false, Image.Format.Rgba8);
		image.Fill(Colors.Transparent);
		for (int i = 1; i < (int)WindowCloseIconSize - 1; i++)
		{
			image.SetPixel(i, i, color);
			image.SetPixel((int)WindowCloseIconSize - 1 - i, i, color);
		}

		ImageTexture texture = ImageTexture.CreateFromImage(image);
		BattleButtonTextures[cacheKey] = texture;
		return texture;
	}

	private void UpdateSpriteButtonLayouts()
	{
		LayoutSpriteButton(_pileButton);
		LayoutSpriteButton(_actionLogButton);
		LayoutSpriteButton(_attackButton);
		LayoutSpriteButton(_defendButton);
		LayoutSpriteButton(_retreatButton);
		LayoutSpriteButton(_meditateButton);
	}

	private void UpdateWindowCloseButtonLayouts()
	{
		LayoutWindowCloseButton(_actionLogCloseButton);
		LayoutWindowCloseButton(_pilePopupCloseButton);
	}

	private void LayoutSpriteButton(Button button)
	{
		if (!_buttonBackgroundRects.TryGetValue(button, out TextureRect? backgroundRect)
			|| !_buttonIconRects.TryGetValue(button, out TextureRect? iconRect))
		{
			return;
		}

		Vector2 buttonSize = button.Size;
		if (buttonSize.X <= 0.0f || buttonSize.Y <= 0.0f)
		{
			buttonSize = button.CustomMinimumSize;
		}

		float sideLength = Mathf.Round(Mathf.Min(buttonSize.X, buttonSize.Y));
		Vector2 squareOffset = new(
			Mathf.Round((buttonSize.X - sideLength) * 0.5f),
			Mathf.Round((buttonSize.Y - sideLength) * 0.5f));
		backgroundRect.Position = squareOffset;
		backgroundRect.Size = new Vector2(sideLength, sideLength);

		Vector2 iconOffset = new(
			Mathf.Round(squareOffset.X + (sideLength - SpriteIconSize) * 0.5f),
			Mathf.Round(squareOffset.Y + (sideLength - SpriteIconSize) * 0.5f));
		iconRect.Position = iconOffset;
		iconRect.Size = new Vector2(SpriteIconSize, SpriteIconSize);
	}

	private void LayoutWindowCloseButton(Button button)
	{
		if (!_windowCloseBackgroundRects.TryGetValue(button, out TextureRect? backgroundRect)
			|| !_windowCloseIconRects.TryGetValue(button, out TextureRect? iconRect))
		{
			return;
		}

		Vector2 size = button.Size;
		if (size.X <= 0.0f || size.Y <= 0.0f)
		{
			size = button.CustomMinimumSize;
		}

		float sideLength = Mathf.Round(Mathf.Min(size.X, size.Y));
		Vector2 squareOffset = new(
			Mathf.Round((size.X - sideLength) * 0.5f),
			Mathf.Round((size.Y - sideLength) * 0.5f));
		backgroundRect.Position = squareOffset;
		backgroundRect.Size = new Vector2(sideLength, sideLength);

		Vector2 iconOffset = new(
			Mathf.Round(squareOffset.X + (sideLength - WindowCloseIconSize) * 0.5f),
			Mathf.Round(squareOffset.Y + (sideLength - WindowCloseIconSize) * 0.5f));
		iconRect.Position = iconOffset;
		iconRect.Size = new Vector2(WindowCloseIconSize, WindowCloseIconSize);

		string stateKey = button.Disabled
			? "disabled"
			: button.GetDrawMode() is BaseButton.DrawMode.Hover or BaseButton.DrawMode.HoverPressed
				? "hover"
				: button.GetDrawMode() == BaseButton.DrawMode.Pressed
					? "pressed"
					: "normal";
		backgroundRect.Texture = ResolveWindowCloseButtonBackgroundTexture(stateKey);
		iconRect.Texture = ResolveWindowCloseButtonIconTexture(stateKey);
	}

	private static Texture2D? ResolveBattleButtonTextureByName(string iconKey)
	{
		if (BattleButtonTextures.TryGetValue(iconKey, out Texture2D? cached))
		{
			return cached;
		}

		string? fileName = iconKey switch
		{
			"attack" => "攻击.png",
			"defend" => "防御按键.png",
			"meditate" => "冥想.png",
			"retreat" => "逃跑.png",
			"log" => "日志.png",
			"pile" => "牌堆.png",
			_ => null,
		};

		if (string.IsNullOrWhiteSpace(fileName))
		{
			return null;
		}

		Texture2D? texture = GD.Load<Texture2D>($"{BattleButtonFolderPath}/{fileName}");
		if (texture != null)
		{
			BattleButtonTextures[iconKey] = texture;
		}

		return texture;
	}

	private void SetupPileScrollBounce(ScrollContainer scroll, Control overscrollSpacer)
	{
		overscrollSpacer.CustomMinimumSize = Vector2.Zero;
		scroll.GuiInput += @event => OnPileScrollGuiInput(scroll, overscrollSpacer, @event);
	}

	private void OnPilePopupGuiInput(InputEvent @event)
	{
		if (!_pilePopup.Visible || @event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
		{
			return;
		}

		if (mouseButton.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown))
		{
			return;
		}

		HandlePileWheel(mouseButton.ButtonIndex);
	}

	private void OnPileScrollGuiInput(ScrollContainer scroll, Control overscrollSpacer, InputEvent @event)
	{
		if (!_pilePopup.Visible || @event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
		{
			return;
		}

		if (mouseButton.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown))
		{
			return;
		}

		HandlePileWheel(mouseButton.ButtonIndex, scroll, overscrollSpacer);
	}

	private void HandlePileWheel(MouseButton buttonIndex, ScrollContainer? explicitScroll = null, Control? explicitSpacer = null)
	{
		ScrollContainer scroll = explicitScroll ?? GetActivePileScroll();
		Control overscrollSpacer = explicitSpacer ?? GetActivePileOverscrollSpacer();
		VScrollBar scrollBar = scroll.GetVScrollBar();
		float step = 28.0f;

		if (buttonIndex == MouseButton.WheelUp)
		{
			overscrollSpacer.CustomMinimumSize = Vector2.Zero;
			scrollBar.Value = Math.Max(scrollBar.MinValue, scrollBar.Value - step);
			GetViewport().SetInputAsHandled();
			return;
		}

		double maxValue = Math.Max(0.0d, scrollBar.MaxValue - scrollBar.Page);
		if (scrollBar.Value >= maxValue - 0.5d)
		{
			overscrollSpacer.CustomMinimumSize = new Vector2(0.0f, Mathf.Min(PileOverscrollPixels, overscrollSpacer.CustomMinimumSize.Y + 6.0f));
			StartPileOverscrollBounce(scroll, overscrollSpacer, 0.12d);
		}
		else
		{
			scrollBar.Value = Math.Min((float)maxValue, scrollBar.Value + step);
		}

		GetViewport().SetInputAsHandled();
	}

	private void StartPileOverscrollBounce(ScrollContainer scroll, Control overscrollSpacer, double duration)
	{
		if (_pileOverscrollTweens.TryGetValue(scroll, out Tween? existingTween) && IsInstanceValid(existingTween))
		{
			existingTween.Kill();
		}

		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(overscrollSpacer, "custom_minimum_size", Vector2.Zero, duration);
		_pileOverscrollTweens[scroll] = tween;
	}

	private ScrollContainer GetActivePileScroll()
	{
		return _pileTabs.CurrentTab switch
		{
			1 => _discardPileScroll,
			2 => _exhaustPileScroll,
			_ => _drawPileScroll,
		};
	}

	private Control GetActivePileOverscrollSpacer()
	{
		return _pileTabs.CurrentTab switch
		{
			1 => _discardPileOverscrollSpacer,
			2 => _exhaustPileOverscrollSpacer,
			_ => _drawPileOverscrollSpacer,
		};
	}

	private Control GetActiveOverscrollSpacerForGrid(GridContainer grid)
	{
		if (grid == _discardPileGrid)
		{
			return _discardPileOverscrollSpacer;
		}

		if (grid == _exhaustPileGrid)
		{
			return _exhaustPileOverscrollSpacer;
		}

		return _drawPileOverscrollSpacer;
	}

	private static void DisableButtonFocus(Button button)
	{
		button.FocusMode = Control.FocusModeEnum.None;
	}

	private void OnCardMouseEntered(BattleCardInstance card)
	{
		_hoveredCard = card;
		_hoveredCardScreenPosition = GetViewport().GetMousePosition();
		RefreshHoveredCard();
	}

	private void OnCardMouseExited()
	{
		_hoveredCard = null;
		_hoveredCardPanel.Visible = false;
	}

	private void OnHandAreaResized()
	{
		LayoutCardViews();
	}

	private void OnAttackPressed()
	{
		CloseTransientPanels();
		EmitSignal(SignalName.AttackRequested);
	}

	private void OnMeditatePressed()
	{
		CloseTransientPanels();
		EmitSignal(SignalName.MeditateRequested);
	}

	private void OnDefendPressed()
	{
		CloseTransientPanels();
		EmitSignal(SignalName.DefendRequested);
	}

	private void OnRetreatPressed()
	{
		CloseTransientPanels();
		EmitSignal(SignalName.RetreatRequested);
	}

	private void OnEndTurnPressed()
	{
		CloseTransientPanels();
		EmitSignal(SignalName.EndTurnRequested);
	}

	private void OnArakawaButtonPressed()
	{
		CloseTransientPanels();
		EmitSignal(SignalName.ArakawaWheelRequested);
	}

	private void OnActionLogPressed()
	{
		if (_isActionLogOpen)
		{
			CloseActionLogPopup();
			return;
		}

		OpenActionLogPopup();
	}

	private void OnArakawaBuildPressed()
	{
		EmitSignal(SignalName.ArakawaAbilityRequested, "build_wall");
	}

	private void OnArakawaEnhancePressed()
	{
		EmitSignal(SignalName.ArakawaAbilityRequested, "enhance_card");
	}

	private void OnArakawaWeaponPressed()
	{
		EmitSignal(SignalName.ArakawaAbilityRequested, "enhance_weapon");
	}

	private void OnArakawaCancelPressed()
	{
		EmitSignal(SignalName.ArakawaCancelRequested);
	}

	private void OnPileButtonPressed()
	{
		if (_pilePopup.Visible)
		{
			ClosePilePopup();
			return;
		}

		OpenPilePopup();
	}

	private void OnPilePopupClosePressed()
	{
		ClosePilePopup();
	}

	private void OnActionLogClosePressed()
	{
		CloseActionLogPopup();
	}

	private void CloseTransientPanels()
	{
		ClosePilePopup();
		CloseActionLogPopup();
	}

	private void SetBattleUiInputBlocked(bool blocked)
	{
		Control.MouseFilterEnum mouseFilter = blocked
			? Control.MouseFilterEnum.Ignore
			: Control.MouseFilterEnum.Stop;

		_actionLogButton.MouseFilter = mouseFilter;
		_pileButton.MouseFilter = mouseFilter;
		_attackButton.MouseFilter = mouseFilter;
		_defendButton.MouseFilter = mouseFilter;
		_meditateButton.MouseFilter = mouseFilter;
		_retreatButton.MouseFilter = mouseFilter;
		_endTurnButton.MouseFilter = mouseFilter;
		_arakawaButton.MouseFilter = mouseFilter;
		_arakawaBuildButton.MouseFilter = mouseFilter;
		_arakawaEnhanceButton.MouseFilter = mouseFilter;
		_arakawaWeaponButton.MouseFilter = mouseFilter;
		_arakawaCancelButton.MouseFilter = mouseFilter;
		_bottomHand.MouseFilter = blocked
			? Control.MouseFilterEnum.Stop
			: Control.MouseFilterEnum.Ignore;
		_bottomHand.ZIndex = blocked ? 5 : 10;
		_handArea.MouseFilter = blocked
			? Control.MouseFilterEnum.Stop
			: Control.MouseFilterEnum.Ignore;

		foreach (BattleCardView cardView in _cardViews.Values)
		{
			if (!IsInstanceValid(cardView))
			{
				continue;
			}

			cardView.Disabled = blocked;
			cardView.MouseFilter = blocked
				? Control.MouseFilterEnum.Ignore
				: Control.MouseFilterEnum.Stop;
		}
	}

	private string BuildTurnLabel()
	{
		if (_turnState == null)
		{
			return "Turn ?";
		}

		string phaseLabel = _turnState.Phase switch
		{
			TurnPhase.PlayerMove => "Move",
			TurnPhase.PlayerAction => "Act",
			TurnPhase.TurnPost => "Post",
			TurnPhase.EnemyTurn => "Enemy",
			_ => _turnState.Phase.ToString(),
		};

		if (_turnState.IsCardTargeting)
		{
			return $"T{_turnState.TurnIndex} {phaseLabel} Card";
		}

		if (_turnState.IsAttackTargeting)
		{
			return $"T{_turnState.TurnIndex} {phaseLabel} Atk";
		}

		return $"T{_turnState.TurnIndex} {phaseLabel}";
	}
}
