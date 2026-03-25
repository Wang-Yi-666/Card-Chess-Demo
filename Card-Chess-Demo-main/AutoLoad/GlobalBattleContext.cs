using Godot;

public partial class GlobalBattleContext : Node
{
    public BattleRequest pending_battle_request { get; private set; }
    public StringName pending_encounter_id { get; private set; } = new StringName("");
    public string return_scene_path { get; private set; } = string.Empty;
    public Vector2 return_player_position { get; private set; } = Vector2.Zero;

    public bool has_pending_battle => pending_battle_request != null;

    public void set_pending_battle(
        BattleRequest request,
        StringName encounterId,
        string returnScenePath,
        Vector2 returnPlayerPosition)
    {
        pending_battle_request = request;
        pending_encounter_id = encounterId;
        return_scene_path = returnScenePath ?? string.Empty;
        return_player_position = returnPlayerPosition;
    }

    public void clear_pending_battle()
    {
        pending_battle_request = null;
        pending_encounter_id = new StringName("");
        return_scene_path = string.Empty;
        return_player_position = Vector2.Zero;
    }
}
