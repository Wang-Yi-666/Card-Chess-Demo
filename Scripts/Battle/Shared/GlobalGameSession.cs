using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CardChessDemo.Battle.Equipment;
using CardChessDemo.Battle.Boundary;
using CardChessDemo.Battle.Cards;
using CardChessDemo.Battle.Progression;
using CardChessDemo.Battle.Stats;

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
	public EquipmentLoadoutState EquipmentLoadoutState { get; } = new();
	public SaveRuntimeState SaveState { get; } = new();
	public EquipmentCatalog RuntimeEquipmentCatalog { get; } = EquipmentCatalog.CreateFromConfiguredResources();
	public ProgressionRuleSet RuntimeProgressionRuleSet { get; } = ProgressionRuleSet.CreateFromConfiguredRules();
	public Godot.Collections.Dictionary InventoryItemCounts => InventoryState.ItemCounts;

	private EquipmentService _equipmentService = null!;
	private PlayerStatResolver _playerStatResolver = null!;

	public override void _Ready()
	{
		EnsureCompositionServices();
		SyncCompositeStateFromFields();
	}

	public void SetPlayerCurrentHp(int value)
	{
		PartyState.Player.CurrentHp = Mathf.Clamp(value, 0, GetResolvedPlayerMaxHp());
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
		return ResolvePlayerStats().AttackDamage;
	}

	public int GetResolvedPlayerDefenseDamageReductionPercent()
	{
		return ResolvePlayerStats().DefenseDamageReductionPercent;
	}

	public int GetResolvedPlayerDefenseShieldGain()
	{
		return ResolvePlayerStats().DefenseShieldGain;
	}

	public int GetResolvedPlayerMaxHp()
	{
		return ResolvePlayerStats().MaxHp;
	}

	public int GetResolvedPlayerMovePointsPerTurn()
	{
		return ResolvePlayerStats().MovePointsPerTurn;
	}

	public ResolvedPlayerStats ResolvePlayerStats()
	{
		EnsureCompositionServices();
		return _playerStatResolver.Resolve(
			PartyState.Player,
			ProgressionState,
			EquipmentLoadoutState,
			PlayerDefenseDamageReductionPercent,
			PlayerDefenseShieldGain);
	}

	public bool IsEquipmentOwned(string itemId)
	{
		EnsureCompositionServices();
		return _equipmentService.IsOwned(InventoryState, itemId);
	}

	public string GetEquippedItemId(string slotId)
	{
		EnsureCompositionServices();
		return _equipmentService.GetEquippedItemId(EquipmentLoadoutState, slotId);
	}

	public bool TryEquipItem(string slotId, string itemId, out string failureReason)
	{
		EnsureCompositionServices();
		bool equipped = _equipmentService.TryEquipItem(EquipmentLoadoutState, InventoryState, slotId, itemId, out failureReason);
		if (equipped)
		{
			SyncFieldsFromCompositeState();
			EmitSignal(SignalName.PlayerRuntimeChanged);
		}

		return equipped;
	}

	public void UnequipItem(string slotId)
	{
		EnsureCompositionServices();
		_equipmentService.UnequipItem(EquipmentLoadoutState, slotId);
		SyncFieldsFromCompositeState();
		EmitSignal(SignalName.PlayerRuntimeChanged);
	}

	public EquipmentDefinition? FindEquipmentDefinition(string itemId)
	{
		EnsureCompositionServices();
		return RuntimeEquipmentCatalog.FindDefinition(itemId);
	}

	public EquipmentDefinition[] GetEquipmentDefinitionsForSlot(string slotId)
	{
		EnsureCompositionServices();
		return RuntimeEquipmentCatalog.GetDefinitionsForSlot(slotId);
	}

	public int GetExperienceRequiredForNextLevel()
	{
		return RuntimeProgressionRuleSet.GetExperienceRequirementForLevel(PlayerLevel);
	}

	public int GetExperienceProgressWithinLevel()
	{
		int currentLevelFloor = RuntimeProgressionRuleSet.GetAccumulatedExperienceForLevel(PlayerLevel);
		return Math.Max(0, PlayerExperience - currentLevelFloor);
	}

	public int GetExperienceNeededToLevelUp()
	{
		int target = RuntimeProgressionRuleSet.GetAccumulatedExperienceForLevel(PlayerLevel + 1);
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
		string[] starterDeck = cardLibrary?.BuildStarterDeckCardIds() ?? Array.Empty<string>();
		if (starterDeck.Length == 0 && DeckBuildState.CardIds.Length == 0)
		{
			return;
		}

		if (DeckBuildState.CardIds.Length == 0)
		{
			DeckBuildState.CardIds = starterDeck;
		}
		else if (starterDeck.Contains("debug_finisher", StringComparer.Ordinal)
			&& !DeckBuildState.CardIds.Contains("debug_finisher", StringComparer.Ordinal))
		{
			DeckBuildState.CardIds = new[] { "debug_finisher" }.Concat(DeckBuildState.CardIds).ToArray();
		}

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
			["base_max_hp"] = PartyState.Player.MaxHp,
			["max_hp"] = GetResolvedPlayerMaxHp(),
			["current_hp"] = PartyState.Player.CurrentHp,
			["base_move_points_per_turn"] = PartyState.Player.MovePointsPerTurn,
			["move_points_per_turn"] = GetResolvedPlayerMovePointsPerTurn(),
			["attack_range"] = PartyState.Player.AttackRange,
			["base_attack_damage"] = PartyState.Player.AttackDamage,
			["attack_damage"] = GetResolvedPlayerAttackDamage(),
			["base_defense_damage_reduction_percent"] = PlayerDefenseDamageReductionPercent,
			["base_defense_shield_gain"] = PlayerDefenseShieldGain,
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

		if (snapshot.TryGetValue("base_max_hp", out Variant baseMaxHp))
		{
			PartyState.Player.MaxHp = baseMaxHp.AsInt32();
		}
		else if (snapshot.TryGetValue("max_hp", out Variant maxHp))
		{
			// 鍏煎鏃у揩鐓э細濡傛灉娌℃湁鏄惧紡鍩虹鍊硷紝鍙兘閫€鍥炲埌鍘嗗彶瀛楁銆?			PartyState.Player.MaxHp = maxHp.AsInt32();
		}

		if (snapshot.TryGetValue("current_hp", out Variant currentHp))
		{
			PartyState.Player.CurrentHp = currentHp.AsInt32();
		}

		if (snapshot.TryGetValue("base_move_points_per_turn", out Variant baseMovePoints))
		{
			PartyState.Player.MovePointsPerTurn = baseMovePoints.AsInt32();
		}
		else if (snapshot.TryGetValue("move_points_per_turn", out Variant movePoints))
		{
			// 鍏煎鏃у揩鐓э細濡傛灉娌℃湁鏄惧紡鍩虹鍊硷紝鍙兘閫€鍥炲埌鍘嗗彶瀛楁銆?			PartyState.Player.MovePointsPerTurn = movePoints.AsInt32();
		}

		if (snapshot.TryGetValue("attack_range", out Variant attackRange))
		{
			PartyState.Player.AttackRange = attackRange.AsInt32();
		}

		if (snapshot.TryGetValue("base_attack_damage", out Variant baseAttackDamage))
		{
			PartyState.Player.AttackDamage = baseAttackDamage.AsInt32();
		}
		else if (snapshot.TryGetValue("attack_damage", out Variant attackDamage))
		{
			// 鍏煎鏃у揩鐓э細濡傛灉娌℃湁鏄惧紡鍩虹鍊硷紝鍙兘閫€鍥炲埌鍘嗗彶瀛楁銆?			PartyState.Player.AttackDamage = attackDamage.AsInt32();
		}

		if (snapshot.TryGetValue("base_defense_damage_reduction_percent", out Variant baseDefenseReduction))
		{
			PlayerDefenseDamageReductionPercent = baseDefenseReduction.AsInt32();
		}

		if (snapshot.TryGetValue("base_defense_shield_gain", out Variant baseDefenseShieldGain))
		{
			PlayerDefenseShieldGain = baseDefenseShieldGain.AsInt32();
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

	private void EnsureCompositionServices()
	{
		_equipmentService ??= new EquipmentService(RuntimeEquipmentCatalog);
		_playerStatResolver ??= new PlayerStatResolver(RuntimeEquipmentCatalog);
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
		PartyState.Arakawa.DisplayName = "鑽掑窛";
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

		EquipmentLoadoutState.WeaponItemId = EquippedWeaponItemId;
		EquipmentLoadoutState.ArmorItemId = EquippedArmorItemId;
		EquipmentLoadoutState.AccessoryItemId = EquippedAccessoryItemId;

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
		EquippedWeaponItemId = EquipmentLoadoutState.WeaponItemId;
		EquippedArmorItemId = EquipmentLoadoutState.ArmorItemId;
		EquippedAccessoryItemId = EquipmentLoadoutState.AccessoryItemId;

		LastCheckpointSaveId = SaveState.LastCheckpointSaveId;
		LastManualSaveId = SaveState.LastManualSaveId;
		AutoSaveSlotId = SaveState.AutoSaveSlotId;
		LastCheckpointScenePath = SaveState.LastCheckpointScenePath;
		LastCheckpointMapId = SaveState.LastCheckpointMapId;
		LastCheckpointSpawnId = SaveState.LastCheckpointSpawnId;
		LastAutoSaveTimestampUtc = SaveState.LastAutoSaveTimestampUtc;
	}
}
