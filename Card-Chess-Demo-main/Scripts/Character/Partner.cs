using Godot;

public partial class Partner : InteractableTemplate
{
	private static readonly StringName PartnerJoinedFlagKey = new StringName("partner_joined");

	[Export] public string PartnerName = "伙伴";
	[Export] public float FollowDistance = 72.0f;
	[Export] public float SideOffset = 36.0f;
	[Export] public float FollowSpeed = 260.0f;
	[Export] public int SideSign = 1;
	[Export] public float SnapDistance = 6.0f;
	[Export] public float StopNearPlayerDistance = 88.0f;
	[Export] public float ResumeFarPlayerDistance = 120.0f;

	private bool _isFollowing = false;
	private bool _holdPositionWhenNear = false;
	private Player _followTarget;
	private Vector2 _lastFacing = Vector2.Down;

	public override void _Ready()
	{
		if (ResumeFarPlayerDistance <= StopNearPlayerDistance)
		{
			ResumeFarPlayerDistance = StopNearPlayerDistance + 8.0f;
		}

		if (IsPartnerJoined())
		{
			Player player = FindFirstPlayer(GetTree().CurrentScene);
			if (player != null)
			{
				BeginFollowing(player, snapToTargetPosition: true);
			}
		}
	}

	public override string GetInteractText(Player player)
	{
		if (_isFollowing)
		{
			return "结伴中";
		}

		return string.IsNullOrWhiteSpace(PromptText) ? "邀请同行" : PromptText;
	}

	public override bool CanInteract(Player player)
	{
		if (_isFollowing)
		{
			return false;
		}

		return base.CanInteract(player);
	}

	protected override void OnInteract(Player player)
	{
		MarkPartnerJoined();
		BeginFollowing(player, snapToTargetPosition: false);
		GD.Print($"{PartnerName}: 我会跟在你身后。\n");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isFollowing || !IsInstanceValid(_followTarget))
		{
			if (_isFollowing)
			{
				Player player = FindFirstPlayer(GetTree().CurrentScene);
				if (player != null)
				{
					BeginFollowing(player, snapToTargetPosition: true);
				}
			}

			return;
		}

		float distanceToPlayer = GlobalPosition.DistanceTo(_followTarget.GlobalPosition);
		if (_holdPositionWhenNear)
		{
			if (distanceToPlayer < ResumeFarPlayerDistance)
			{
				return;
			}

			_holdPositionWhenNear = false;
		}
		else if (distanceToPlayer <= StopNearPlayerDistance)
		{
			_holdPositionWhenNear = true;
			return;
		}

		Vector2 velocityDir = _followTarget.Velocity;
		if (velocityDir.LengthSquared() > 0.0001f)
		{
			_lastFacing = velocityDir.Normalized();
		}

		Vector2 targetPosition = GetFollowTargetPosition();

		float dist = GlobalPosition.DistanceTo(targetPosition);
		if (dist <= SnapDistance)
		{
			GlobalPosition = targetPosition;
			return;
		}

		GlobalPosition = GlobalPosition.MoveToward(targetPosition, FollowSpeed * (float)delta);
	}

	private Vector2 GetFollowTargetPosition()
	{
		Vector2 sideBasis = new Vector2(-_lastFacing.Y, _lastFacing.X);
		return _followTarget.GlobalPosition
			- _lastFacing * FollowDistance
			+ sideBasis * SideOffset * Mathf.Sign(SideSign == 0 ? 1 : SideSign);
	}

	private void BeginFollowing(Player player, bool snapToTargetPosition)
	{
		_followTarget = player;
		_isFollowing = true;
		_holdPositionWhenNear = false;

		if (snapToTargetPosition)
		{
			Vector2 velocityDir = _followTarget.Velocity;
			if (velocityDir.LengthSquared() > 0.0001f)
			{
				_lastFacing = velocityDir.Normalized();
			}

			GlobalPosition = GetFollowTargetPosition();
		}
	}

	private Player FindFirstPlayer(Node root)
	{
		if (root == null)
		{
			return null;
		}

		if (root is Player directPlayer)
		{
			return directPlayer;
		}

		foreach (Node child in root.GetChildren())
		{
			Player found = FindFirstPlayer(child);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private GameSession GetGameSession()
	{
		return GetNodeOrNull<GameSession>("/root/GameSession");
	}

	private bool IsPartnerJoined()
	{
		GameSession session = GetGameSession();
		if (session == null)
		{
			return false;
		}

		if (!session.world_flags.TryGetValue(PartnerJoinedFlagKey, out Variant value))
		{
			return false;
		}

		return value.VariantType == Variant.Type.Bool && value.AsBool();
	}

	private void MarkPartnerJoined()
	{
		GameSession session = GetGameSession();
		session?.set_flag(PartnerJoinedFlagKey, true);
	}
}
