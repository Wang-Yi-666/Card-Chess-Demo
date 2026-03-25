using Godot;
using System;

public partial class GameSession : Node
{
    // Interface-contract fields (snake_case)
    public string session_id { get; set; } = Guid.NewGuid().ToString("N");
    public StringName current_map_id { get; set; } = new StringName("scene1");
    public StringName current_map_spawn_id { get; set; } = new StringName("");
    public StringName player_profile_id { get; set; } = new StringName("default_player");

    public PlayerRuntimeState player_runtime { get; set; } = new PlayerRuntimeState();
    public DeckState deck_state { get; set; } = new DeckState();
    public InventoryState inventory_state { get; set; } = new InventoryState();
    public ArakawaState arakawa_state { get; set; } = new ArakawaState();
    public SuitcaseState suitcase_state { get; set; } = new SuitcaseState();

    public Godot.Collections.Dictionary<StringName, Variant> world_flags { get; set; }
        = new Godot.Collections.Dictionary<StringName, Variant>();
    public int scan_risk { get; set; } = 0;
    public Godot.Collections.Array<StringName> cleared_encounters { get; set; }
        = new Godot.Collections.Array<StringName>();
    public Godot.Collections.Array<StringName> used_interactables { get; set; }
        = new Godot.Collections.Array<StringName>();

    // 战斗后位置恢复
    public bool should_restore_player_position { get; set; } = false;
    public Vector2 pending_restore_player_position { get; set; } = Vector2.Zero;

    public override void _Ready()
    {
        if (string.IsNullOrWhiteSpace(session_id))
        {
            session_id = Guid.NewGuid().ToString("N");
        }
    }

    public void start_new_session(StringName map_id, StringName spawn_id)
    {
        session_id = Guid.NewGuid().ToString("N");
        current_map_id = map_id;
        current_map_spawn_id = spawn_id;

        player_runtime = new PlayerRuntimeState();
        deck_state = new DeckState();
        inventory_state = new InventoryState();
        arakawa_state = new ArakawaState();
        suitcase_state = new SuitcaseState();

        world_flags.Clear();
        cleared_encounters.Clear();
        used_interactables.Clear();
        scan_risk = 0;
    }

    public void set_flag(StringName key, Variant value)
    {
        world_flags[key] = value;
    }

    public void mark_encounter_cleared(StringName encounter_id)
    {
        if (!cleared_encounters.Contains(encounter_id))
        {
            cleared_encounters.Add(encounter_id);
        }
    }

    public void mark_interactable_used(StringName interactable_id)
    {
        if (!used_interactables.Contains(interactable_id))
        {
            used_interactables.Add(interactable_id);
        }
    }

    public void apply_resource_delta(StringName resource_key, int delta, int? clamp_min = null, int? clamp_max = null)
    {
        int value;
        switch (resource_key)
        {
            case "player_hp":
                value = player_runtime.hp_current + delta;
                player_runtime.hp_current = clamp_optional(value, clamp_min, clamp_max);
                break;
            case "arakawa_energy":
                value = arakawa_state.energy_current + delta;
                arakawa_state.energy_current = clamp_optional(value, clamp_min, clamp_max);
                break;
            case "suitcase_fuel":
                value = suitcase_state.fuel_current + delta;
                suitcase_state.fuel_current = clamp_optional(value, clamp_min, clamp_max);
                break;
            case "scan_risk":
                value = scan_risk + delta;
                scan_risk = clamp_optional(value, clamp_min, clamp_max);
                break;
            default:
                GD.PushWarning($"GameSession: unknown resource key '{resource_key}'.");
                break;
        }
    }

    public void apply_inventory_delta(StringName item_id, int amount)
    {
        int current = 0;
        if (inventory_state.items.TryGetValue(item_id, out int value))
        {
            current = value;
        }

        int next = current + amount;
        if (next <= 0)
        {
            inventory_state.items.Remove(item_id);
            return;
        }

        inventory_state.items[item_id] = next;
    }

    private int clamp_optional(int value, int? min, int? max)
    {
        if (min.HasValue && value < min.Value)
        {
            value = min.Value;
        }

        if (max.HasValue && value > max.Value)
        {
            value = max.Value;
        }

        return value;
    }
}

public class PlayerRuntimeState
{
    public int hp_current { get; set; } = 100;
    public int hp_max { get; set; } = 100;
    public Godot.Collections.Dictionary<StringName, Variant> stat_modifiers { get; set; }
        = new Godot.Collections.Dictionary<StringName, Variant>();
    public Godot.Collections.Dictionary<StringName, Variant> status_runtime { get; set; }
        = new Godot.Collections.Dictionary<StringName, Variant>();
}

public class DeckState
{
    public Godot.Collections.Array<StringName> deck_list { get; set; } = new Godot.Collections.Array<StringName>();
    public int build_version { get; set; } = 1;
    public int deck_runtime_seed { get; set; } = 0;
}

public class InventoryState
{
    public Godot.Collections.Dictionary<StringName, int> items { get; set; }
        = new Godot.Collections.Dictionary<StringName, int>();
    public Godot.Collections.Array<StringName> key_items { get; set; }
        = new Godot.Collections.Array<StringName>();
}

public class ArakawaState
{
    public int energy_current { get; set; } = 100;
    public int energy_cap { get; set; } = 100;
    public int growth_level { get; set; } = 1;
    public Godot.Collections.Array<StringName> unlocks { get; set; } = new Godot.Collections.Array<StringName>();
}

public class SuitcaseState
{
    public int fuel_current { get; set; } = 10;
    public int fuel_cap { get; set; } = 100;
    public Godot.Collections.Dictionary<StringName, Variant> overload_state { get; set; }
        = new Godot.Collections.Dictionary<StringName, Variant>();
    public Godot.Collections.Array<StringName> modules { get; set; } = new Godot.Collections.Array<StringName>();
}