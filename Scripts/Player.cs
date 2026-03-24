using Godot;
using CardChessDemo.Battle.Shared;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 300.0f;
	[Export] public float Friction = 0.2f;
	[Export(PropertyHint.Range, "16,240,1")] public float InteractionRange = 96.0f;
	[Export(PropertyHint.Range, "10,89,1")] public float ViewConeHalfAngleDeg = 45.0f;
	[Export] public bool RequireLineOfSight = false;
	[Export] public uint InteractionObstacleMask = uint.MaxValue;
	[Export] public float InteractionStickyWindow = 0.35f;
	[Export] public float InteractionStickyBonus = 0.08f;
	[Export] public bool ShowInteractionGizmo = true;
	[Export] public Color GizmoRangeColor = new Color(0.2f, 0.85f, 1.0f, 0.35f);
	[Export] public Color GizmoConeColor = new Color(0.3f, 1.0f, 0.6f, 0.85f);
	[Export] public Color GizmoForwardColor = new Color(1.0f, 0.85f, 0.25f, 0.95f);

	private Vector2 _lastFacingDirection = Vector2.Down;
	private Area2D _interactionArea;
	private Area2D _lastInteractedArea;
	private ulong _lastInteractTimeMs;
	private GlobalGameSession? _globalSession;

	public override void _Ready()
	{
		_globalSession = GetNodeOrNull<GlobalGameSession>("/root/GlobalGameSession");
		_interactionArea = GetNode<Area2D>("InteractionArea");

		// 防止脚本范围小于实际探测圈，导致看得到但交互不到。
		float areaRadius = GetInteractionAreaRadius();
		if (areaRadius > 0.0f && InteractionRange < areaRadius)
		{
			InteractionRange = areaRadius;
		}
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
		if (ShowInteractionGizmo)
		{
			QueueRedraw();
		}
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

		var areas = _interactionArea.GetOverlappingAreas();
		ulong nowMs = Time.GetTicksMsec();
		float maxDistSq = InteractionRange * InteractionRange;
		float minDot = Mathf.Cos(Mathf.DegToRad(ViewConeHalfAngleDeg));

		IInteractable bestTarget = null;
		Area2D bestArea = null;
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

			if (distSq > maxDistSq)
			{
				continue;
			}

			Vector2 directionToItem = distSq < 0.0001f
				? _lastFacingDirection
				: toTarget / Mathf.Sqrt(distSq);
			float dot = _lastFacingDirection.Dot(directionToItem);

			if (dot < minDot)
			{
				continue;
			}

			if (!HasLineOfSight(area))
			{
				continue;
			}

			float normalizedDistance = distSq / maxDistSq;
			float score = dot - normalizedDistance * 0.15f;

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

		if (bestTarget == null)
		{
			return;
		}

		_lastInteractedArea = bestArea;
		_lastInteractTimeMs = nowMs;
		bestTarget.Interact(this);
	}

	public void ReceiveHeal(int amount)
	{
		if (amount <= 0 || _globalSession == null)
		{
			return;
		}

		_globalSession.SetPlayerCurrentHp(_globalSession.PlayerCurrentHp + amount);
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
}
