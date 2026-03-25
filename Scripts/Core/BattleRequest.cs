using Godot;
using System;

public partial class BattleRequest : RefCounted
{
	public string snapshot_id { get; set; } = Guid.NewGuid().ToString("N");
	public int player_hp { get; set; } = 100;
	public Godot.Collections.Dictionary<StringName, Variant> player_stat_modifiers { get; set; }
		= new Godot.Collections.Dictionary<StringName, Variant>();
	public int deck_runtime_seed { get; set; } = 0;
	public int arakawa_energy { get; set; } = 0;
	public int suitcase_fuel { get; set; } = 0;
	public Godot.Collections.Dictionary<StringName, Variant> active_flags { get; set; }
		= new Godot.Collections.Dictionary<StringName, Variant>();
}
