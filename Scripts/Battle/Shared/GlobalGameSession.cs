using Godot;

namespace CardChessDemo.Battle.Shared;

public partial class GlobalGameSession : Node
{
    [Signal] public delegate void PlayerRuntimeChangedEventHandler();

    [Export] public string PlayerDisplayName { get; set; } = "Traveler";
    [Export] public int PlayerMaxHp { get; set; } = 12;
    [Export] public int PlayerCurrentHp { get; set; } = 12;
    [Export] public int PlayerMovePointsPerTurn { get; set; } = 4;

    public void SetPlayerMovePointsPerTurn(int value)
    {
        PlayerMovePointsPerTurn = Mathf.Max(0, value);
        EmitSignal(SignalName.PlayerRuntimeChanged);
    }

    public void ApplyMovePointDelta(int delta)
    {
        SetPlayerMovePointsPerTurn(PlayerMovePointsPerTurn + delta);
    }

    public Godot.Collections.Dictionary BuildPlayerSnapshot()
    {
        return new Godot.Collections.Dictionary
        {
            ["display_name"] = PlayerDisplayName,
            ["max_hp"] = PlayerMaxHp,
            ["current_hp"] = PlayerCurrentHp,
            ["move_points_per_turn"] = PlayerMovePointsPerTurn,
        };
    }

    public void ApplyPlayerSnapshot(Godot.Collections.Dictionary snapshot)
    {
        if (snapshot.TryGetValue("display_name", out Variant displayName))
        {
            PlayerDisplayName = displayName.AsString();
        }

        if (snapshot.TryGetValue("max_hp", out Variant maxHp))
        {
            PlayerMaxHp = maxHp.AsInt32();
        }

        if (snapshot.TryGetValue("current_hp", out Variant currentHp))
        {
            PlayerCurrentHp = currentHp.AsInt32();
        }

        if (snapshot.TryGetValue("move_points_per_turn", out Variant movePoints))
        {
            PlayerMovePointsPerTurn = movePoints.AsInt32();
        }

        EmitSignal(SignalName.PlayerRuntimeChanged);
    }
}
