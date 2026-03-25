using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 300.0f;
	[Export] public float Friction = 0.2f;
	[Export(PropertyHint.Range, "16,240,1")] public float InteractionRange = 96.0f;
	[Export(PropertyHint.Range, "10,89,1")] public float ViewConeHalfAngleDeg = 45.0f;
	[Export] public NodePath InteractionHintLabelPath = "../UI/InteractionHintLabel";
	[Export] public string InteractionHintSuffix = " [E]";
	[Export] public bool RequireLineOfSight = false;
	[Export] public uint InteractionObstacleMask = uint.MaxValue;
	[Export] public float InteractionStickyWindow = 0.35f;
	[Export] public float InteractionStickyBonus = 0.08f;
	[Export] public bool ShowInteractionGizmo = true;
	[Export] public NodePath PlayerHpLabelPath = "../UI/BottomStatusBar/Margin/HBox/PlayerBlock/PlayerHpLabel";
	[Export] public NodePath PlayerHpBarPath = "../UI/BottomStatusBar/Margin/HBox/PlayerBlock/PlayerHpBar";
	[Export] public NodePath PartnerEnergyLabelPath = "../UI/BottomStatusBar/Margin/HBox/PartnerBlock/PartnerEnergyLabel";
	[Export] public NodePath PartnerEnergyBarPath = "../UI/BottomStatusBar/Margin/HBox/PartnerBlock/PartnerEnergyBar";
	[Export] public Color GizmoRangeColor = new Color(0.2f, 0.85f, 1.0f, 0.35f);
	[Export] public Color GizmoConeColor = new Color(0.3f, 1.0f, 0.6f, 0.85f);
	[Export] public Color GizmoForwardColor = new Color(1.0f, 0.85f, 0.25f, 0.95f);

	private Vector2 _lastFacingDirection = Vector2.Down;
	private Area2D _interactionArea;
	private Area2D _lastInteractedArea;
	private Label _interactionHintLabel;
	private Label _playerHpLabel;
	private ProgressBar _playerHpBar;
	private Label _partnerEnergyLabel;
	private ProgressBar _partnerEnergyBar;
	private ulong _lastInteractTimeMs;

	public override void _Ready()
	{
		_interactionArea = GetNode<Area2D>("InteractionArea");
		_interactionHintLabel = GetNodeOrNull<Label>(InteractionHintLabelPath);
		if (_interactionHintLabel != null)
		{
			_interactionHintLabel.Visible = false;
		}
		else
		{
			var fallbackUi = new CanvasLayer();
			fallbackUi.Name = "RuntimeUI";
			AddSibling(fallbackUi);

			_interactionHintLabel = new Label();
			_interactionHintLabel.Name = "InteractionHintLabel";
			_interactionHintLabel.Visible = false;
			_interactionHintLabel.Position = new Vector2(24, 24);
			_interactionHintLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));
			_interactionHintLabel.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 1));
			_interactionHintLabel.AddThemeConstantOverride("outline_size", 3);
			fallbackUi.AddChild(_interactionHintLabel);
			GD.PushWarning("Player: 未找到提示文本节点，已创建运行时提示 UI。请检查 InteractionHintLabelPath 配置。");
		}

		// 防止脚本范围小于实际探测圈，导致看得到但交互不到。
		float areaRadius = GetInteractionAreaRadius();
		if (areaRadius > 0.0f && InteractionRange < areaRadius)
		{
			InteractionRange = areaRadius;
		}

		_playerHpLabel = GetNodeOrNull<Label>(PlayerHpLabelPath);
		_playerHpBar = GetNodeOrNull<ProgressBar>(PlayerHpBarPath);
		_partnerEnergyLabel = GetNodeOrNull<Label>(PartnerEnergyLabelPath);
		_partnerEnergyBar = GetNodeOrNull<ProgressBar>(PartnerEnergyBarPath);
		UpdateStatusUi();

		// 检查是否需要恢复战斗后的位置
		RestorePositionIfNeeded();
	}

	private void RestorePositionIfNeeded()
	{
		GameSession session = GetGameSession();
		if (session == null || !session.should_restore_player_position)
		{
			return;
		}

		// 恢复位置
		GlobalPosition = session.pending_restore_player_position;
		session.should_restore_player_position = false;
		session.pending_restore_player_position = Vector2.Zero;

		GD.Print($"Player: 位置已从战斗中恢复到 {GlobalPosition}");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		if (inputDirection != Vector2.Zero)
		{
			Velocity = inputDirection * Speed;
			_lastFacingDirection = inputDirection.Normalized();
		}
		else
		{
			Velocity = Velocity.Lerp(Vector2.Zero, Friction);
		}

		MoveAndSlide();
	}

	public override void _Process(double delta)
	{
		UpdateInteractionHint();
		UpdateStatusUi();

		if (ShowInteractionGizmo)
		{
			QueueRedraw();
		}
	}

	public void ReceiveHeal(int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		ApplyPlayerHpDelta(amount);
	}

	public void ReceiveDamage(int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		ApplyPlayerHpDelta(-amount);
	}

	public void RestoreHpToFull()
	{
		GameSession session = GetGameSession();
		if (session == null)
		{
			return;
		}

		session.player_runtime.hp_current = Mathf.Max(0, session.player_runtime.hp_max);
		UpdateStatusUi();
	}

	public override void _Draw()
	{
		if (!ShowInteractionGizmo)
		{
			return;
		}

		Vector2 facing = _lastFacingDirection == Vector2.Zero ? Vector2.Down : _lastFacingDirection.Normalized();
		facing = facing.Rotated(-GlobalRotation);

		float halfAngleRad = Mathf.DegToRad(ViewConeHalfAngleDeg);
		float startAngle = facing.Angle() - halfAngleRad;
		float endAngle = facing.Angle() + halfAngleRad;

		DrawArc(Vector2.Zero, InteractionRange, 0.0f, Mathf.Tau, 64, GizmoRangeColor, 1.5f, true);
		DrawArc(Vector2.Zero, InteractionRange, startAngle, endAngle, 32, GizmoConeColor, 2.0f, true);

		Vector2 leftEdge = facing.Rotated(-halfAngleRad) * InteractionRange;
		Vector2 rightEdge = facing.Rotated(halfAngleRad) * InteractionRange;
		Vector2 forward = facing * InteractionRange;

		DrawLine(Vector2.Zero, leftEdge, GizmoConeColor, 2.0f, true);
		DrawLine(Vector2.Zero, rightEdge, GizmoConeColor, 2.0f, true);
		DrawLine(Vector2.Zero, forward, GizmoForwardColor, 2.5f, true);

		if (IsInstanceValid(_lastInteractedArea))
		{
			Vector2 targetLocal = ToLocal(_lastInteractedArea.GlobalPosition);
			DrawLine(Vector2.Zero, targetLocal, new Color(1.0f, 0.4f, 0.2f, 0.8f), 1.5f, true);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("interact"))
		{
			return;
		}

		ulong nowMs = Time.GetTicksMsec();

		// 先按视角优先选择；找不到时回退为全向选择，避免“站门口按 E 无反应”。
		TryGetBestTarget(enforceViewCone: true, out IInteractable bestTarget, out Area2D bestArea);
		if (bestTarget == null)
		{
			TryGetBestTarget(enforceViewCone: false, out bestTarget, out bestArea);
		}

		if (bestTarget == null)
		{
			return;
		}

		_lastInteractedArea = bestArea;
		_lastInteractTimeMs = nowMs;
		bestTarget.Interact(this);
	}

	private void UpdateInteractionHint()
	{
		if (_interactionHintLabel == null)
		{
			return;
		}

		TryGetBestHintTarget(enforceViewCone: true, out IInteractable bestTarget);
		if (bestTarget == null)
		{
			TryGetBestHintTarget(enforceViewCone: false, out bestTarget);
		}

		if (bestTarget == null)
		{
			_interactionHintLabel.Visible = false;
			_interactionHintLabel.Text = "";
			return;
		}

		_interactionHintLabel.Visible = true;
		_interactionHintLabel.Text = bestTarget.GetInteractText(this) + InteractionHintSuffix;
	}

	private void TryGetBestHintTarget(bool enforceViewCone, out IInteractable bestTarget)
	{
		bestTarget = null;

		if (_interactionArea == null)
		{
			return;
		}

		var areas = _interactionArea.GetOverlappingAreas();
		float minDot = Mathf.Cos(Mathf.DegToRad(ViewConeHalfAngleDeg));
		float bestScore = float.NegativeInfinity;

		foreach (Area2D area in areas)
		{
			if (area.GetParent() is not IInteractable item)
			{
				continue;
			}

			Vector2 toTarget = area.GlobalPosition - GlobalPosition;
			float distSq = toTarget.LengthSquared();

			Vector2 directionToItem = distSq < 0.0001f
				? _lastFacingDirection
				: toTarget / Mathf.Sqrt(distSq);
			float dot = _lastFacingDirection.Dot(directionToItem);

			if (enforceViewCone && dot < minDot)
			{
				continue;
			}

			if (!HasLineOfSight(area))
			{
				continue;
			}

			float score = dot;

			if (score > bestScore)
			{
				bestScore = score;
				bestTarget = item;
			}
		}
	}

	private void TryGetBestTarget(bool enforceViewCone, out IInteractable bestTarget, out Area2D bestArea)
	{
		bestTarget = null;
		bestArea = null;

		if (_interactionArea == null)
		{
			return;
		}

		var areas = _interactionArea.GetOverlappingAreas();
		ulong nowMs = Time.GetTicksMsec();
		float minDot = Mathf.Cos(Mathf.DegToRad(ViewConeHalfAngleDeg));
		float bestScore = float.NegativeInfinity;

		foreach (Area2D area in areas)
		{
			if (area.GetParent() is not IInteractable item)
			{
				continue;
			}

			if (!item.CanInteract(this))
			{
				continue;
			}

			Vector2 toTarget = area.GlobalPosition - GlobalPosition;
			float distSq = toTarget.LengthSquared();

			Vector2 directionToItem = distSq < 0.0001f
				? _lastFacingDirection
				: toTarget / Mathf.Sqrt(distSq);
			float dot = _lastFacingDirection.Dot(directionToItem);

			if (enforceViewCone && dot < minDot)
			{
				continue;
			}

			if (!HasLineOfSight(area))
			{
				continue;
			}

			float score = dot;

			if (area == _lastInteractedArea && nowMs - _lastInteractTimeMs <= (ulong)(InteractionStickyWindow * 1000.0f))
			{
				score += InteractionStickyBonus;
			}

			if (score > bestScore)
			{
				bestScore = score;
				bestTarget = item;
				bestArea = area;
			}
		}
	}

	private bool HasLineOfSight(Area2D targetArea)
	{
		if (!RequireLineOfSight)
		{
			return true;
		}

		var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, targetArea.GlobalPosition, InteractionObstacleMask);
		query.CollideWithAreas = true;
		query.CollideWithBodies = true;
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid(), _interactionArea.GetRid() };

		var hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
		if (hit.Count == 0)
		{
			return true;
		}

		if (!hit.ContainsKey("collider"))
		{
			return false;
		}

		GodotObject collider = hit["collider"].AsGodotObject();
		return collider == targetArea || collider == targetArea.GetParent();
	}

	private float GetInteractionAreaRadius()
	{
		if (_interactionArea == null || !_interactionArea.HasNode("CollisionShape2D"))
		{
			return 0.0f;
		}

		CollisionShape2D shapeNode = _interactionArea.GetNode<CollisionShape2D>("CollisionShape2D");
		if (shapeNode.Shape is CircleShape2D circle)
		{
			return circle.Radius;
		}

		if (shapeNode.Shape is RectangleShape2D rectangle)
		{
			return rectangle.Size.Length() * 0.5f;
		}

		return 0.0f;
	}

	private GameSession GetGameSession()
	{
		return GetNodeOrNull<GameSession>("/root/GameSession");
	}

	private void ApplyPlayerHpDelta(int delta)
	{
		GameSession session = GetGameSession();
		if (session == null)
		{
			return;
		}

		int hpMax = Mathf.Max(1, session.player_runtime.hp_max);
		session.apply_resource_delta("player_hp", delta, 0, hpMax);
		UpdateStatusUi();
	}

	private bool IsPartnerJoined()
	{
		GameSession session = GetGameSession();
		if (session == null) return false;

		if (!session.world_flags.TryGetValue(new StringName("partner_joined"), out Variant value))
		{
			return false;
		}

		return value.VariantType == Variant.Type.Bool && value.AsBool();
	}

	private void UpdateStatusUi()
	{
		if (_playerHpLabel == null && _playerHpBar == null && _partnerEnergyLabel == null && _partnerEnergyBar == null)
		{
			return;
		}

		GameSession session = GetGameSession();
		if (session == null)
		{
			return;
		}

		int hpMax = Mathf.Max(1, session.player_runtime.hp_max);
		int hpCurrent = Mathf.Clamp(session.player_runtime.hp_current, 0, hpMax);
		if (session.player_runtime.hp_current != hpCurrent)
		{
			session.player_runtime.hp_current = hpCurrent;
		}

		int energyCap = Mathf.Max(1, session.arakawa_state.energy_cap);
		int energyCurrent = Mathf.Clamp(session.arakawa_state.energy_current, 0, energyCap);
		if (session.arakawa_state.energy_current != energyCurrent)
		{
			session.arakawa_state.energy_current = energyCurrent;
		}

		if (_playerHpLabel != null)
		{
			_playerHpLabel.Text = $"主角 HP {hpCurrent}/{hpMax}";
		}

		if (_playerHpBar != null)
		{
			_playerHpBar.MaxValue = hpMax;
			_playerHpBar.Value = hpCurrent;
		}

		// 只在伙伴加入后显示伙伴能量 UI
		bool partnerJoined = IsPartnerJoined();
		
		if (_partnerEnergyLabel != null)
		{
			_partnerEnergyLabel.Visible = partnerJoined;
			if (partnerJoined)
			{
				_partnerEnergyLabel.Text = $"伙伴 EN {energyCurrent}/{energyCap}";
			}
		}

		if (_partnerEnergyBar != null)
		{
			_partnerEnergyBar.Visible = partnerJoined;
			if (partnerJoined)
			{
				_partnerEnergyBar.MaxValue = energyCap;
				_partnerEnergyBar.Value = energyCurrent;
			}
		}
	}

}
