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
	[Signal] public delegate void EndTurnRequestedEventHandler();
	[Signal] public delegate void AttackRequestedEventHandler();
	[Signal] public delegate void MeditateRequestedEventHandler();
	[Signal] public delegate void CardRequestedEventHandler(string cardInstanceId);

	private const float HoverOffsetX = 10.0f;
	private const float HoverOffsetY = -8.0f;
	private const float ScreenMargin = 4.0f;
	private const float SelectedCardLift = 8.0f;
	private const float CardWidth = 56.0f;
	private const float CardHeight = 54.0f;

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
	private PanelContainer _pilePopup = null!;
	private Label _pilePopupTitle = null!;
	private RichTextLabel _pilePopupBody = null!;
	private Label _turnLabel = null!;
	private Label _resourceLabel = null!;
	private Button _drawPileButton = null!;
	private Button _discardPileButton = null!;
	private Button _exhaustPileButton = null!;
	private Button _attackButton = null!;
	private Button _meditateButton = null!;
	private Button _endTurnButton = null!;
	private Control _handArea = null!;
	private Control _cardFxRoot = null!;

	private readonly Dictionary<string, BattleCardView> _cardViews = new(StringComparer.Ordinal);

	private TurnActionState? _turnState;
	private BattleObjectState? _hoveredUnitState;
	private Vector2 _hoveredUnitScreenPosition;
	private BattleCardInstance? _hoveredCard;
	private Vector2 _hoveredCardScreenPosition;
	private BattleCardInstance[] _handCards = Array.Empty<BattleCardInstance>();
	private BattleCardInstance[] _drawPileCards = Array.Empty<BattleCardInstance>();
	private BattleCardInstance[] _discardPileCards = Array.Empty<BattleCardInstance>();
	private BattleCardInstance[] _exhaustPileCards = Array.Empty<BattleCardInstance>();
	private int _currentEnergy;
	private int _maxEnergy;
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
		_meditateButton.Pressed -= OnMeditatePressed;
		_endTurnButton.Pressed -= OnEndTurnPressed;
		_drawPileButton.Pressed -= OnDrawPilePressed;
		_discardPileButton.Pressed -= OnDiscardPilePressed;
		_exhaustPileButton.Pressed -= OnExhaustPilePressed;
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

	public override void _Process(double delta)
	{
		Refresh();
	}

	private void Refresh()
	{
		if (_turnState == null || !IsNodeReady() || !EnsureNodes())
		{
			return;
		}

		_turnLabel.Text = BuildTurnLabel();
		_resourceLabel.Text = $"E{_currentEnergy}/{_maxEnergy} R{_energyRechargeProgress}/{_energyRechargeInterval}";
		_drawPileButton.Text = $"D{_drawPileCards.Length}";
		_discardPileButton.Text = $"G{_discardPileCards.Length}";
		_exhaustPileButton.Text = $"X{_exhaustPileCards.Length}";
		_attackButton.Visible = _turnState.CanEnterAttackTargeting || _turnState.IsAttackTargeting;
		_attackButton.Text = _turnState.IsAttackTargeting ? "Cn" : "Atk";
		_attackButton.Disabled = !_turnState.CanEnterAttackTargeting && !_turnState.IsAttackTargeting;
		_meditateButton.Disabled = !_turnState.CanSelectCard;
		_endTurnButton.Disabled = !_turnState.IsPlayerTurn && !_turnState.IsAttackTargeting && !_turnState.IsCardTargeting;

		RefreshHandViews();
		RefreshHoveredUnit();
		RefreshHoveredCard();
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
		_hoveredUnitStats.Text = $"HP {hpText} SH {_hoveredUnitState.CurrentShield}";
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

	private void PositionFloatingPanel(Control panel, Vector2 screenPosition)
	{
		panel.ResetSize();
		Vector2 panelSize = panel.GetCombinedMinimumSize();
		panel.Size = panelSize;
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		Vector2 desiredPosition = screenPosition + new Vector2(HoverOffsetX, HoverOffsetY);

		if (desiredPosition.X + panelSize.X > viewportSize.X - ScreenMargin)
		{
			desiredPosition.X = screenPosition.X - panelSize.X - HoverOffsetX;
		}

		if (desiredPosition.Y + panelSize.Y > viewportSize.Y - ScreenMargin)
		{
			desiredPosition.Y = screenPosition.Y - panelSize.Y - 8.0f;
		}

		desiredPosition.X = Mathf.Clamp(desiredPosition.X, ScreenMargin, viewportSize.X - panelSize.X - ScreenMargin);
		desiredPosition.Y = Mathf.Clamp(desiredPosition.Y, ScreenMargin, viewportSize.Y - panelSize.Y - ScreenMargin);
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

	private void ShowPilePopup(string title, IReadOnlyList<BattleCardInstance> cards)
	{
		StringBuilder builder = new();
		if (cards.Count == 0)
		{
			builder.Append("Empty");
		}
		else
		{
			for (int index = cards.Count - 1; index >= 0; index--)
			{
				BattleCardInstance card = cards[index];
				builder.Append(card.Definition.DisplayName);
				builder.Append(" C");
				builder.Append(card.Definition.Cost);

				if (card.Definition.IsQuick)
				{
					builder.Append(" Quick");
				}

				if (card.Definition.ExhaustsOnPlay)
				{
					builder.Append(" Exhaust");
				}

				if (index > 0)
				{
					builder.Append('\n');
				}
			}
		}

		_pilePopupTitle.Text = title;
		_pilePopupBody.Text = builder.ToString();
		_pilePopup.Visible = true;
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
		_pilePopup = GetNodeOrNull<PanelContainer>("PilePopup");
		_pilePopupTitle = GetNodeOrNull<Label>("PilePopup/Margin/VBox/TitleLabel");
		_pilePopupBody = GetNodeOrNull<RichTextLabel>("PilePopup/Margin/VBox/BodyLabel");
		_turnLabel = GetNodeOrNull<Label>("TopBar/LeftInfo/TurnLabel");
		_resourceLabel = GetNodeOrNull<Label>("TopBar/LeftInfo/ResourceLabel");
		_drawPileButton = GetNodeOrNull<Button>("RightControls/DrawPileButton");
		_discardPileButton = GetNodeOrNull<Button>("RightControls/DiscardPileButton");
		_exhaustPileButton = GetNodeOrNull<Button>("RightControls/ExhaustPileButton");
		_attackButton = GetNodeOrNull<Button>("RightControls/AttackButton");
		_meditateButton = GetNodeOrNull<Button>("RightControls/MeditateButton");
		_endTurnButton = GetNodeOrNull<Button>("RightControls/EndTurnButton");
		_handArea = GetNodeOrNull<Control>("BottomHand/HandArea");
		_cardFxRoot = GetNodeOrNull<Control>("CardFxRoot");

		return _hoveredUnitPanel != null
			&& _hoveredUnitTitle != null
			&& _hoveredUnitStats != null
			&& _hoveredCardPanel != null
			&& _hoveredCardTitle != null
			&& _hoveredCardStats != null
			&& _pilePopup != null
			&& _pilePopupTitle != null
			&& _pilePopupBody != null
			&& _turnLabel != null
			&& _resourceLabel != null
			&& _drawPileButton != null
			&& _discardPileButton != null
			&& _exhaustPileButton != null
			&& _attackButton != null
			&& _meditateButton != null
			&& _endTurnButton != null
			&& _handArea != null
			&& _cardFxRoot != null;
	}

	private void HookSignals()
	{
		if (_signalsHooked || !EnsureNodes())
		{
			return;
		}

		ApplyCompactButtonStyle(_drawPileButton);
		ApplyCompactButtonStyle(_discardPileButton);
		ApplyCompactButtonStyle(_exhaustPileButton);
		ApplyCompactButtonStyle(_attackButton);
		ApplyCompactButtonStyle(_meditateButton);
		ApplyCompactButtonStyle(_endTurnButton);

		_attackButton.Pressed += OnAttackPressed;
		_meditateButton.Pressed += OnMeditatePressed;
		_endTurnButton.Pressed += OnEndTurnPressed;
		_drawPileButton.Pressed += OnDrawPilePressed;
		_discardPileButton.Pressed += OnDiscardPilePressed;
		_exhaustPileButton.Pressed += OnExhaustPilePressed;
		_handArea.Resized += OnHandAreaResized;
		_signalsHooked = true;
	}

	private void ApplyCompactButtonStyle(Button button)
	{
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
		_pilePopup.Visible = false;
		EmitSignal(SignalName.AttackRequested);
	}

	private void OnMeditatePressed()
	{
		_pilePopup.Visible = false;
		EmitSignal(SignalName.MeditateRequested);
	}

	private void OnEndTurnPressed()
	{
		_pilePopup.Visible = false;
		EmitSignal(SignalName.EndTurnRequested);
	}

	private void OnDrawPilePressed()
	{
		ShowPilePopup("Draw Pile", _drawPileCards);
	}

	private void OnDiscardPilePressed()
	{
		ShowPilePopup("Discard Pile", _discardPileCards);
	}

	private void OnExhaustPilePressed()
	{
		ShowPilePopup("Exhaust Pile", _exhaustPileCards);
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
