using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CardChessDemo.Battle.Boundary;
using CardChessDemo.Battle.Cards;

namespace CardChessDemo.Battle.Shared;

public partial class GlobalGameSession : Node
{
	[Signal] public delegate void PlayerRuntimeChangedEventHandler();
	[Signal] public delegate void ArakawaRuntimeChangedEventHandler();

	[Export] public string PlayerDisplayName { get; set; } = "Traveler";
	[Export] public int PlayerMaxHp { get; set; } = 12;
	[Export] public int PlayerCurrentHp { get; set; } = 12;
	[Export] public int PlayerMovePointsPerTurn { get; set; } = 4;
	[Export] public int PlayerAttackRange { get; set; } = 1;
	[Export] public int PlayerAttackDamage { get; set; } = 2;
	[Export] public int PlayerDefenseDamageReductionPercent { get; set; } = 50;
	[Export] public int PlayerDefenseShieldGain { get; set; } = 0;
	[Export] public int ArakawaMaxEnergy { get; set; } = 3;
	[Export] public int ArakawaCurrentEnergy { get; set; } = 3;
	[Export] public int PlayerLevel { get; set; } = 1;
	[Export] public int PlayerExperience { get; set; } = 0;
	[Export] public int PlayerMasteryPoints { get; set; } = 0;
	[Export] public int ArakawaGrowthLevel { get; set; } = 1;
	[Export] public string[] TalentIds { get; set; } = Array.Empty<string>();
	[Export] public string[] ArakawaUnlockIds { get; set; } = Array.Empty<string>();
	[Export] public string[] UnlockedCardIds { get; set; } = Array.Empty<string>();
	[Export] public string[] TalentBranchTags { get; set; } = Array.Empty<string>();
	[Export] public int DeckPointBudgetBonus { get; set; } = 0;
	[Export] public int DeckMinCardCountDelta { get; set; } = 0;
	[Export] public int DeckMaxCardCountDelta { get; set; } = 0;
	[Export] public int DeckMaxCopiesPerCardBonus { get; set; } = 0;
	[Export] public string DeckBuildName { get; set; } = "default";
	[Export] public string[] DeckCardIds { get; set; } = Array.Empty<string>();
	[Export] public string[] DeckRelicIds { get; set; } = Array.Empty<string>();
	[Export] public string EquippedWeaponItemId { get; set; } = string.Empty;
	[Export] public string EquippedArmorItemId { get; set; } = string.Empty;
	[Export] public string EquippedAccessoryItemId { get; set; } = string.Empty;
	[Export] public string LastCheckpointSaveId { get; set; } = string.Empty;
	[Export] public string LastManualSaveId { get; set; } = string.Empty;
	[Export] public string AutoSaveSlotId { get; set; } = "autosave";
	[Export] public string LastCheckpointScenePath { get; set; } = string.Empty;
	[Export] public string LastCheckpointMapId { get; set; } = string.Empty;
	[Export] public string LastCheckpointSpawnId { get; set; } = string.Empty;
	[Export] public string LastAutoSaveTimestampUtc { get; set; } = string.Empty;

	public BattleRequest? PendingBattleRequest { get; private set; }
	public BattleResult? LastBattleResult { get; private set; }
	public MapResumeContext? PendingMapResumeContext { get; private set; }
	public string PendingBattleEncounterId { get; private set; } = string.Empty;
	public PartyRuntimeState PartyState { get; } = new();
	public ProgressionRuntimeState ProgressionState { get; } = new();
	public DeckBuildState DeckBuildState { get; } = new();
	public InventoryRuntimeState InventoryState { get; } = new();
	public SaveRuntimeState SaveState { get; } = new();
	public Godot.Collections.Dictionary InventoryItemCounts => InventoryState.ItemCounts;

	public override void _Ready()
	{
		SyncCompositeStateFromFields();
	}

	public void SetPlayerCurrentHp(int value)
	{
		PartyState.Player.CurrentHp = Mathf.Clamp(value, 0, Math.Max(PartyState.Player.MaxHp, 0));
		SyncFieldsFromCompositeState();
		EmitSignal(SignalName.PlayerRuntimeChanged);
	}

	public void SetPlayerMovePointsPerTurn(int value)
	{
		PartyState.Player.MovePointsPerTurn = Mathf.Max(0, value);
		SyncFieldsFromCompositeState();
		EmitSignal(SignalName.PlayerRuntimeChanged);
	}

	public void SetArakawaCurrentEnergy(int value)
	{
		PartyState.Arakawa.CurrentEnergy = Mathf.Clamp(value, 0, Math.Max(PartyState.Arakawa.MaxEnergy, 0));
		SyncFieldsFromCompositeState();
		EmitSignal(SignalName.ArakawaRuntimeChanged);
	}

	public bool TrySpendArakawaEnergy(int amount)
	{
		if (amount <= 0)
		{
			return true;
		}

		if (PartyState.Arakawa.CurrentEnergy < amount)
		{
			return false;
		}

		SetArakawaCurrentEnergy(PartyState.Arakawa.CurrentEnergy - amount);
		return true;
	}

	public void RestoreArakawaEnergy(int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		SetArakawaCurrentEnergy(ArakawaCurrentEnergy + amount);
	}

	public void ApplyMovePointDelta(int delta)
	{
		SetPlayerMovePointsPerTurn(PlayerMovePointsPerTurn + delta);
	}

	public int GetResolvedPlayerAttackDamage()
	{
		return Math.Max(0, PlayerAttackDamage + SumTalentScalarBonuses("stat.attack_bonus.") + GetEquipmentAttackBonus());
	}

	public int GetResolvedPlayerDefenseDamageReductionPercent()
	{
		return Mathf.Clamp(
			PlayerDefenseDamageReductionPercent + SumTalentScalarBonuses("stat.defense_reduction_bonus.") + GetEquipmentDefenseReductionBonus(),
			0,
			100);
	}

	public int GetResolvedPlayerDefenseShieldGain()
	{
		return Math.Max(0, PlayerDefenseShieldGain + SumTalentScalarBonuses("stat.defense_shield_bonus.") + GetEquipmentDefenseShieldBonus());
	}

	public int GetResolvedPlayerMaxHp()
	{
		return Math.Max(1, PlayerMaxHp + GetEquipmentMaxHpBonus());
	}

	public int GetResolvedPlayerMovePointsPerTurn()
	{
		return Math.Max(0, PlayerMovePointsPerTurn + GetEquipmentMoveBonus());
	}

	public bool IsEquipmentOwned(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId))
		{
			return false;
		}

		return InventoryItemCounts.TryGetValue(itemId, out Variant amount) && amount.AsInt32() > 0;
	}

	public string GetEquippedItemId(string slotId)
	{
		return NormalizeEquipmentSlotId(slotId) switch
		{
			"weapon" => EquippedWeaponItemId,
			"armor" => EquippedArmorItemId,
			"accessory" => EquippedAccessoryItemId,
			_ => string.Empty,
		};
	}

	public bool TryEquipItem(string slotId, string itemId, out string failureReason)
	{
		string normalizedSlotId = NormalizeEquipmentSlotId(slotId);
		if (string.IsNullOrWhiteSpace(normalizedSlotId))
		{
			failureReason = "unknown_slot";
			return false;
		}

		string normalizedItemId = itemId?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalizedItemId))
		{
			failureReason = "missing_item";
			return false;
		}

		if (!IsEquipmentOwned(normalizedItemId))
		{
			failureReason = "item_not_owned";
			return false;
		}

		if (!CanEquipItemInSlot(normalizedItemId, normalizedSlotId))
		{
			failureReason = "slot_mismatch";
			return false;
		}

		SetEquippedItemId(normalizedSlotId, normalizedItemId);
		failureReason = string.Empty;
		return true;
	}

	public void UnequipItem(string slotId)
	{
		string normalizedSlotId = NormalizeEquipmentSlotId(slotId);
		if (string.IsNullOrWhiteSpace(normalizedSlotId))
		{
			return;
		}

		SetEquippedItemId(normalizedSlotId, string.Empty);
	}

	public int GetExperienceRequiredForNextLevel()
	{
		return GetExperienceRequirementForLevel(PlayerLevel);
	}

	public int GetExperienceProgressWithinLevel()
	{
		int currentLevelFloor = GetAccumulatedExperienceForLevel(PlayerLevel);
		return Math.Max(0, PlayerExperience - currentLevelFloor);
	}

	public int GetExperienceNeededToLevelUp()
	{
		int target = GetAccumulatedExperienceForLevel(PlayerLevel + 1);
		return Math.Max(0, target - PlayerExperience);
	}

	public void BeginBattle(BattleRequest? request = null)
	{
		BattleRequest resolvedRequest = request ?? BattleRequest.FromSession(this);
		if (!resolvedRequest.TryValidate(out string failureReason))
		{
			GD.PushError($"GlobalGameSession.BeginBattle: invalid request. {failureReason}");
			return;
		}

		PendingBattleRequest = resolvedRequest;
		LastBattleResult = null;
	}

	public void EnsureDeckBuildInitialized(BattleCardLibrary? cardLibrary)
	{
		if (DeckBuildState.CardIds.Length > 0)
		{
			return;
		}

		string[] starterDeck = cardLibrary?.BuildStarterDeckCardIds() ?? Array.Empty<string>();
		if (starterDeck.Length == 0)
		{
			return;
		}

		DeckBuildState.CardIds = starterDeck;
		SyncFieldsFromCompositeState();
	}

	public void CancelPendingBattleTransition()
	{
		PendingBattleRequest = null;
		PendingBattleEncounterId = string.Empty;
		PendingMapResumeContext = null;
	}

	public void SetPendingMapResumeContext(MapResumeContext? resumeContext)
	{
		PendingMapResumeContext = resumeContext;
	}

	public MapResumeContext? PeekPendingMapResumeContext()
	{
		return PendingMapResumeContext;
	}

	public MapResumeContext? ConsumePendingMapResumeContext()
	{
		MapResumeContext? resumeContext = PendingMapResumeContext;
		PendingMapResumeContext = null;
		return resumeContext;
	}

	public void SetPendingBattleEncounterId(string encounterId)
	{
		PendingBattleEncounterId = encounterId?.Trim() ?? string.Empty;
	}

	public string ConsumePendingBattleEncounterId()
	{
		string encounterId = PendingBattleEncounterId;
		PendingBattleEncounterId = string.Empty;
		return encounterId;
	}

	public BattleRequest? ConsumePendingBattleRequest()
	{
		BattleRequest? request = PendingBattleRequest;
		PendingBattleRequest = null;
		return request;
	}

	public void CompleteBattle(BattleResult result)
	{
		if (!result.TryValidate(out string failureReason))
		{
			GD.PushError($"GlobalGameSession.CompleteBattle: invalid result. {failureReason}");
			return;
		}

		LastBattleResult = result;
		result.ApplyToSession(this);
	}

	public BattleResult? ConsumeLastBattleResult()
	{
		BattleResult? result = LastBattleResult;
		LastBattleResult = null;
		return result;
	}

	public Godot.Collections.Dictionary BuildPlayerSnapshot()
	{
		return new Godot.Collections.Dictionary
		{
			["display_name"] = PartyState.Player.DisplayName,
			["max_hp"] = GetResolvedPlayerMaxHp(),
			["current_hp"] = PartyState.Player.CurrentHp,
			["move_points_per_turn"] = GetResolvedPlayerMovePointsPerTurn(),
			["attack_range"] = PartyState.Player.AttackRange,
			["attack_damage"] = GetResolvedPlayerAttackDamage(),
			["arakawa_max_energy"] = PartyState.Arakawa.MaxEnergy,
			["arakawa_current_energy"] = PartyState.Arakawa.CurrentEnergy,
		};
	}

	public Godot.Collections.Dictionary BuildCompanionSnapshot()
	{
		return new Godot.Collections.Dictionary
		{
			["companion_id"] = PartyState.Arakawa.CompanionId,
			["display_name"] = PartyState.Arakawa.DisplayName,
			["growth_level"] = PartyState.Arakawa.GrowthLevel,
			["arakawa_max_energy"] = PartyState.Arakawa.MaxEnergy,
			["arakawa_current_energy"] = PartyState.Arakawa.CurrentEnergy,
		};
	}

	public Godot.Collections.Dictionary BuildProgressionSnapshot()
	{
		return BuildProgressionSnapshotModel().ToDictionary();
	}

	public ProgressionSnapshot BuildProgressionSnapshotModel()
	{
		return new ProgressionSnapshot
		{
			PlayerLevel = ProgressionState.PlayerLevel,
			PlayerExperience = ProgressionState.PlayerExperience,
			PlayerMasteryPoints = ProgressionState.PlayerMasteryPoints,
			ArakawaGrowthLevel = ProgressionState.ArakawaGrowthLevel,
			TalentIds = ProgressionState.TalentIds,
			ArakawaUnlockIds = ProgressionState.ArakawaUnlockIds,
			UnlockedCardIds = ProgressionState.UnlockedCardIds,
			TalentBranchTags = ProgressionState.TalentBranchTags,
			DeckPointBudgetBonus = ProgressionState.DeckPointBudgetBonus,
			DeckMinCardCountDelta = ProgressionState.DeckMinCardCountDelta,
			DeckMaxCardCountDelta = ProgressionState.DeckMaxCardCountDelta,
			DeckMaxCopiesPerCardBonus = ProgressionState.DeckMaxCopiesPerCardBonus,
		};
	}

	public Godot.Collections.Dictionary BuildDeckBuildSnapshot()
	{
		return BuildDeckBuildSnapshotModel().ToDictionary();
	}

	public DeckBuildSnapshot BuildDeckBuildSnapshotModel()
	{
		return new DeckBuildSnapshot
		{
			BuildName = DeckBuildState.BuildName,
			CardIds = DeckBuildState.CardIds,
			RelicIds = DeckBuildState.RelicIds,
		};
	}

	public Godot.Collections.Dictionary BuildInventorySnapshot()
	{
		return CloneDictionary(InventoryItemCounts);
	}

	public Godot.Collections.Dictionary BuildSaveRuntimeSnapshot()
	{
		return new Godot.Collections.Dictionary
		{
			["last_checkpoint_save_id"] = SaveState.LastCheckpointSaveId,
			["last_manual_save_id"] = SaveState.LastManualSaveId,
			["auto_save_slot_id"] = SaveState.AutoSaveSlotId,
			["last_checkpoint_scene_path"] = SaveState.LastCheckpointScenePath,
			["last_checkpoint_map_id"] = SaveState.LastCheckpointMapId,
			["last_checkpoint_spawn_id"] = SaveState.LastCheckpointSpawnId,
			["last_auto_save_timestamp_utc"] = SaveState.LastAutoSaveTimestampUtc,
			["preferred_rollback_slot_kind"] = (int)SaveState.PreferredRollbackSlotKind,
		};
	}

	public void ApplyPlayerSnapshot(Godot.Collections.Dictionary snapshot)
	{
		if (snapshot.TryGetValue("display_name", out Variant displayName))
		{
			PartyState.Player.DisplayName = displayName.AsString();
		}

		if (snapshot.TryGetValue("max_hp", out Variant maxHp))
		{
			PartyState.Player.MaxHp = maxHp.AsInt32();
		}

		if (snapshot.TryGetValue("current_hp", out Variant currentHp))
		{
			PartyState.Player.CurrentHp = currentHp.AsInt32();
		}

		if (snapshot.TryGetValue("move_points_per_turn", out Variant movePoints))
		{
			PartyState.Player.MovePointsPerTurn = movePoints.AsInt32();
		}

		if (snapshot.TryGetValue("attack_range", out Variant attackRange))
		{
			PartyState.Player.AttackRange = attackRange.AsInt32();
		}

		if (snapshot.TryGetValue("attack_damage", out Variant attackDamage))
		{
			PartyState.Player.AttackDamage = attackDamage.AsInt32();
		}

		if (snapshot.TryGetValue("arakawa_max_energy", out Variant arakawaMaxEnergy))
		{
			PartyState.Arakawa.MaxEnergy = arakawaMaxEnergy.AsInt32();
		}

		if (snapshot.TryGetValue("arakawa_current_energy", out Variant arakawaCurrentEnergy))
		{
			PartyState.Arakawa.CurrentEnergy = arakawaCurrentEnergy.AsInt32();
		}

		SyncFieldsFromCompositeState();
		EmitSignal(SignalName.PlayerRuntimeChanged);
		EmitSignal(SignalName.ArakawaRuntimeChanged);
	}

	public void ApplyCompanionSnapshot(Godot.Collections.Dictionary snapshot)
	{
		if (snapshot.TryGetValue("growth_level", out Variant growthLevel))
		{
			PartyState.Arakawa.GrowthLevel = Mathf.Max(1, growthLevel.AsInt32());
		}

		if (snapshot.TryGetValue("arakawa_max_energy", out Variant arakawaMaxEnergy))
		{
			PartyState.Arakawa.MaxEnergy = Mathf.Max(0, arakawaMaxEnergy.AsInt32());
		}

		if (snapshot.TryGetValue("arakawa_current_energy", out Variant arakawaCurrentEnergy))
		{
			PartyState.Arakawa.CurrentEnergy = Mathf.Clamp(arakawaCurrentEnergy.AsInt32(), 0, Math.Max(PartyState.Arakawa.MaxEnergy, 0));
		}

		SyncFieldsFromCompositeState();
		EmitSignal(SignalName.ArakawaRuntimeChanged);
	}

	public void ApplyProgressionSnapshot(Godot.Collections.Dictionary snapshot)
	{
		ProgressionSnapshot progression = ProgressionSnapshot.FromDictionary(snapshot);
		ProgressionState.PlayerLevel = progression.PlayerLevel;
		ProgressionState.PlayerExperience = progression.PlayerExperience;
		ProgressionState.PlayerMasteryPoints = progression.PlayerMasteryPoints;
		ProgressionState.ArakawaGrowthLevel = progression.ArakawaGrowthLevel;
		ProgressionState.TalentIds = progression.TalentIds;
		ProgressionState.ArakawaUnlockIds = progression.ArakawaUnlockIds;
		ProgressionState.UnlockedCardIds = progression.UnlockedCardIds;
		ProgressionState.TalentBranchTags = progression.TalentBranchTags;
		ProgressionState.DeckPointBudgetBonus = progression.DeckPointBudgetBonus;
		ProgressionState.DeckMinCardCountDelta = progression.DeckMinCardCountDelta;
		ProgressionState.DeckMaxCardCountDelta = progression.DeckMaxCardCountDelta;
		ProgressionState.DeckMaxCopiesPerCardBonus = progression.DeckMaxCopiesPerCardBonus;

		SyncFieldsFromCompositeState();
	}

	public void ApplyDeckBuildSnapshot(Godot.Collections.Dictionary snapshot)
	{
		DeckBuildSnapshot deckBuild = DeckBuildSnapshot.FromDictionary(snapshot);
		DeckBuildState.BuildName = string.IsNullOrWhiteSpace(deckBuild.BuildName) ? "default" : deckBuild.BuildName;
		DeckBuildState.CardIds = deckBuild.CardIds;
		DeckBuildState.RelicIds = deckBuild.RelicIds;

		SyncFieldsFromCompositeState();
	}

	public void ApplyProgressionDelta(Godot.Collections.Dictionary delta)
	{
		ApplyProgressionDelta(Boundary.ProgressionDelta.FromDictionary(delta));
	}

	public void ApplyProgressionDelta(Boundary.ProgressionDelta delta)
	{
		if (delta.ExperienceDelta != 0)
		{
			ProgressionState.PlayerExperience = Math.Max(0, ProgressionState.PlayerExperience + delta.ExperienceDelta);
		}

		if (delta.MasteryPointDelta != 0)
		{
			ProgressionState.PlayerMasteryPoints = Math.Max(0, ProgressionState.PlayerMasteryPoints + delta.MasteryPointDelta);
		}

		if (delta.PlayerLevelDelta != 0)
		{
			ProgressionState.PlayerLevel = Math.Max(1, ProgressionState.PlayerLevel + delta.PlayerLevelDelta);
		}

		if (delta.ArakawaGrowthLevelDelta != 0)
		{
			ProgressionState.ArakawaGrowthLevel = Math.Max(1, ProgressionState.ArakawaGrowthLevel + delta.ArakawaGrowthLevelDelta);
		}

		if (delta.TalentUnlockIds.Length > 0)
		{
			ProgressionState.TalentIds = MergeUniqueStrings(ProgressionState.TalentIds, delta.TalentUnlockIds);
		}

		if (delta.ArakawaUnlockIds.Length > 0)
		{
			ProgressionState.ArakawaUnlockIds = MergeUniqueStrings(ProgressionState.ArakawaUnlockIds, delta.ArakawaUnlockIds);
		}

		SyncFieldsFromCompositeState();
	}

	public void ApplyInventoryDelta(Godot.Collections.Dictionary delta)
	{
		ApplyInventoryDelta(Boundary.InventoryDelta.FromDictionary(delta));
	}

	public void ApplyInventoryDelta(Boundary.InventoryDelta delta)
	{
		foreach ((string itemId, int amount) in delta.ItemDeltas)
		{
			int currentAmount = InventoryItemCounts.TryGetValue(itemId, out Variant currentValue)
				? currentValue.AsInt32()
				: 0;
			int nextAmount = currentAmount + amount;
			if (nextAmount <= 0)
			{
				InventoryItemCounts.Remove(itemId);
				continue;
			}

			InventoryItemCounts[itemId] = nextAmount;
		}
	}

	public void ApplyInventorySnapshot(Godot.Collections.Dictionary snapshot)
	{
		InventoryItemCounts.Clear();
		foreach (Variant key in snapshot.Keys)
		{
			InventoryItemCounts[key.AsString()] = snapshot[key].AsInt32();
		}
	}

	public void ApplySaveRuntimeSnapshot(Godot.Collections.Dictionary snapshot)
	{
		if (snapshot.TryGetValue("last_checkpoint_save_id", out Variant checkpointSaveId))
		{
			SaveState.LastCheckpointSaveId = checkpointSaveId.AsString();
		}

		if (snapshot.TryGetValue("last_manual_save_id", out Variant manualSaveId))
		{
			SaveState.LastManualSaveId = manualSaveId.AsString();
		}

		if (snapshot.TryGetValue("auto_save_slot_id", out Variant autoSaveSlotId))
		{
			SaveState.AutoSaveSlotId = autoSaveSlotId.AsString();
		}

		if (snapshot.TryGetValue("last_checkpoint_scene_path", out Variant checkpointScenePath))
		{
			SaveState.LastCheckpointScenePath = checkpointScenePath.AsString();
		}

		if (snapshot.TryGetValue("last_checkpoint_map_id", out Variant checkpointMapId))
		{
			SaveState.LastCheckpointMapId = checkpointMapId.AsString();
		}

		if (snapshot.TryGetValue("last_checkpoint_spawn_id", out Variant checkpointSpawnId))
		{
			SaveState.LastCheckpointSpawnId = checkpointSpawnId.AsString();
		}

		if (snapshot.TryGetValue("last_auto_save_timestamp_utc", out Variant autoSaveTimestamp))
		{
			SaveState.LastAutoSaveTimestampUtc = autoSaveTimestamp.AsString();
		}

		if (snapshot.TryGetValue("preferred_rollback_slot_kind", out Variant rollbackSlotKind))
		{
			SaveState.PreferredRollbackSlotKind = (SaveSlotKind)rollbackSlotKind.AsInt32();
		}

		SyncFieldsFromCompositeState();
	}

	private static string[] MergeUniqueStrings(string[] current, string[] incoming)
	{
		return current
			.Concat(incoming)
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Distinct(StringComparer.Ordinal)
			.ToArray();
	}

	private static Godot.Collections.Array<string> ToVariantArray(IEnumerable<string> values)
	{
		Godot.Collections.Array<string> array = new();
		foreach (string value in values)
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				array.Add(value);
			}
		}

		return array;
	}

	private static string[] ToStringArray(Variant value)
	{
		if (value.VariantType == Variant.Type.Nil)
		{
			return Array.Empty<string>();
		}

		if (value.Obj is Godot.Collections.Array rawArray)
		{
			List<string> values = new();
			foreach (Variant item in rawArray)
			{
				string text = item.AsString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					values.Add(text);
				}
			}

			return values.ToArray();
		}

		return Array.Empty<string>();
	}

	private static Godot.Collections.Dictionary CloneDictionary(Godot.Collections.Dictionary source)
	{
		Godot.Collections.Dictionary clone = new();
		foreach (Variant key in source.Keys)
		{
			clone[key] = source[key];
		}

		return clone;
	}

	private int SumTalentScalarBonuses(string prefix)
	{
		if (string.IsNullOrWhiteSpace(prefix))
		{
			return 0;
		}

		int total = 0;
		foreach (string talentId in ProgressionState.TalentIds)
		{
			if (string.IsNullOrWhiteSpace(talentId) || !talentId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			string valueText = talentId[prefix.Length..];
			if (int.TryParse(valueText, out int value))
			{
				total += value;
			}
		}

		return total;
	}

	private static string NormalizeEquipmentSlotId(string? slotId)
	{
		return slotId?.Trim().ToLowerInvariant() switch
		{
			"weapon" => "weapon",
			"armor" => "armor",
			"accessory" => "accessory",
			_ => string.Empty,
		};
	}

	private static bool CanEquipItemInSlot(string itemId, string slotId)
	{
		return NormalizeEquipmentSlotId(slotId) switch
		{
			"weapon" => itemId is "rusted_blade" or "ion_pistol",
			"armor" => itemId is "patched_coat" or "reactive_plate",
			"accessory" => itemId is "signal_charm" or "tactical_chip",
			_ => false,
		};
	}

	private void SetEquippedItemId(string slotId, string itemId)
	{
		switch (NormalizeEquipmentSlotId(slotId))
		{
			case "weapon":
				EquippedWeaponItemId = itemId;
				break;
			case "armor":
				EquippedArmorItemId = itemId;
				break;
			case "accessory":
				EquippedAccessoryItemId = itemId;
				break;
		}
	}

	private int GetEquipmentAttackBonus()
	{
		return GetEquippedItemId("weapon") switch
		{
			"rusted_blade" => 1,
			"ion_pistol" => 2,
			_ => 0,
		};
	}

	private int GetEquipmentDefenseReductionBonus()
	{
		int bonus = 0;
		bonus += GetEquippedItemId("armor") switch
		{
			"patched_coat" => 5,
			"reactive_plate" => 10,
			_ => 0,
		};
		bonus += GetEquippedItemId("accessory") switch
		{
			"tactical_chip" => 5,
			_ => 0,
		};
		return bonus;
	}

	private int GetEquipmentDefenseShieldBonus()
	{
		return GetEquippedItemId("armor") switch
		{
			"reactive_plate" => 1,
			_ => 0,
		};
	}

	private int GetEquipmentMaxHpBonus()
	{
		return GetEquippedItemId("armor") switch
		{
			"patched_coat" => 4,
			"reactive_plate" => 2,
			_ => 0,
		};
	}

	private int GetEquipmentMoveBonus()
	{
		return GetEquippedItemId("accessory") switch
		{
			"signal_charm" => 1,
			_ => 0,
		};
	}

	private static int GetExperienceRequirementForLevel(int level)
	{
		return Math.Max(10, 10 + (Math.Max(1, level) - 1) * 5);
	}

	private static int GetAccumulatedExperienceForLevel(int level)
	{
		int total = 0;
		for (int current = 1; current < Math.Max(1, level); current++)
		{
			total += GetExperienceRequirementForLevel(current);
		}

		return total;
	}

	private void SyncCompositeStateFromFields()
	{
		PartyState.Player.DisplayName = PlayerDisplayName;
		PartyState.Player.MaxHp = PlayerMaxHp;
		PartyState.Player.CurrentHp = PlayerCurrentHp;
		PartyState.Player.MovePointsPerTurn = PlayerMovePointsPerTurn;
		PartyState.Player.AttackRange = PlayerAttackRange;
		PartyState.Player.AttackDamage = PlayerAttackDamage;

		PartyState.Arakawa.CompanionId = "arakawa";
		PartyState.Arakawa.DisplayName = "荒川";
		PartyState.Arakawa.GrowthLevel = ArakawaGrowthLevel;
		PartyState.Arakawa.MaxEnergy = ArakawaMaxEnergy;
		PartyState.Arakawa.CurrentEnergy = ArakawaCurrentEnergy;

		ProgressionState.PlayerLevel = PlayerLevel;
		ProgressionState.PlayerExperience = PlayerExperience;
		ProgressionState.PlayerMasteryPoints = PlayerMasteryPoints;
		ProgressionState.ArakawaGrowthLevel = ArakawaGrowthLevel;
		ProgressionState.TalentIds = TalentIds;
		ProgressionState.ArakawaUnlockIds = ArakawaUnlockIds;
		ProgressionState.UnlockedCardIds = UnlockedCardIds;
		ProgressionState.TalentBranchTags = TalentBranchTags;
		ProgressionState.DeckPointBudgetBonus = DeckPointBudgetBonus;
		ProgressionState.DeckMinCardCountDelta = DeckMinCardCountDelta;
		ProgressionState.DeckMaxCardCountDelta = DeckMaxCardCountDelta;
		ProgressionState.DeckMaxCopiesPerCardBonus = DeckMaxCopiesPerCardBonus;

		DeckBuildState.BuildName = DeckBuildName;
		DeckBuildState.CardIds = DeckCardIds;
		DeckBuildState.RelicIds = DeckRelicIds;

		SaveState.LastCheckpointSaveId = LastCheckpointSaveId;
		SaveState.LastManualSaveId = LastManualSaveId;
		SaveState.AutoSaveSlotId = AutoSaveSlotId;
		SaveState.LastCheckpointScenePath = LastCheckpointScenePath;
		SaveState.LastCheckpointMapId = LastCheckpointMapId;
		SaveState.LastCheckpointSpawnId = LastCheckpointSpawnId;
		SaveState.LastAutoSaveTimestampUtc = LastAutoSaveTimestampUtc;
	}

	private void SyncFieldsFromCompositeState()
	{
		PlayerDisplayName = PartyState.Player.DisplayName;
		PlayerMaxHp = PartyState.Player.MaxHp;
		PlayerCurrentHp = PartyState.Player.CurrentHp;
		PlayerMovePointsPerTurn = PartyState.Player.MovePointsPerTurn;
		PlayerAttackRange = PartyState.Player.AttackRange;
		PlayerAttackDamage = PartyState.Player.AttackDamage;

		ArakawaMaxEnergy = PartyState.Arakawa.MaxEnergy;
		ArakawaCurrentEnergy = PartyState.Arakawa.CurrentEnergy;
		ArakawaGrowthLevel = PartyState.Arakawa.GrowthLevel;

		PlayerLevel = ProgressionState.PlayerLevel;
		PlayerExperience = ProgressionState.PlayerExperience;
		PlayerMasteryPoints = ProgressionState.PlayerMasteryPoints;
		TalentIds = ProgressionState.TalentIds;
		ArakawaUnlockIds = ProgressionState.ArakawaUnlockIds;
		UnlockedCardIds = ProgressionState.UnlockedCardIds;
		TalentBranchTags = ProgressionState.TalentBranchTags;
		DeckPointBudgetBonus = ProgressionState.DeckPointBudgetBonus;
		DeckMinCardCountDelta = ProgressionState.DeckMinCardCountDelta;
		DeckMaxCardCountDelta = ProgressionState.DeckMaxCardCountDelta;
		DeckMaxCopiesPerCardBonus = ProgressionState.DeckMaxCopiesPerCardBonus;

		DeckBuildName = DeckBuildState.BuildName;
		DeckCardIds = DeckBuildState.CardIds;
		DeckRelicIds = DeckBuildState.RelicIds;

		LastCheckpointSaveId = SaveState.LastCheckpointSaveId;
		LastManualSaveId = SaveState.LastManualSaveId;
		AutoSaveSlotId = SaveState.AutoSaveSlotId;
		LastCheckpointScenePath = SaveState.LastCheckpointScenePath;
		LastCheckpointMapId = SaveState.LastCheckpointMapId;
		LastCheckpointSpawnId = SaveState.LastCheckpointSpawnId;
		LastAutoSaveTimestampUtc = SaveState.LastAutoSaveTimestampUtc;
	}
}
