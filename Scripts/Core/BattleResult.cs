using Godot;
using System;

public partial class BattleResult : RefCounted
{
	public string result_id { get; set; } = Guid.NewGuid().ToString("N");
	public string request_id { get; set; } = string.Empty;
	public bool victory { get; set; } = false;
	public StringName defeat_reason { get; set; } = new StringName("");
	public int remaining_hp { get; set; } = 0;
	public int hp_delta { get; set; } = 0;
	public Godot.Collections.Array<Godot.Collections.Dictionary<StringName, Variant>> status_delta { get; set; }
		= new Godot.Collections.Array<Godot.Collections.Dictionary<StringName, Variant>>();
	public Godot.Collections.Dictionary<StringName, Variant> deck_delta { get; set; }
		= new Godot.Collections.Dictionary<StringName, Variant>();
	public int arakawa_energy_delta { get; set; } = 0;
	public int fuel_delta { get; set; } = 0;
	public Godot.Collections.Array<Godot.Collections.Dictionary<StringName, Variant>> inventory_delta { get; set; }
		= new Godot.Collections.Array<Godot.Collections.Dictionary<StringName, Variant>>();
	public Godot.Collections.Dictionary<StringName, Variant> new_flags { get; set; }
		= new Godot.Collections.Dictionary<StringName, Variant>();
	public StringName cleared_encounter_id { get; set; } = new StringName("");
	public Godot.Collections.Array<StringName> triggered_story_ids { get; set; }
		= new Godot.Collections.Array<StringName>();
	public StringName next_map_spawn_id { get; set; } = new StringName("");
}
