using System;
using System.Collections.Generic;
using Godot;
using CardChessDemo.Battle.Data;

namespace CardChessDemo.Battle.Board;

public sealed class BoardObject
{
    private readonly HashSet<string> _tags;

    public BoardObject(
        string objectId,
        string definitionId,
        string aiId,
        BoardObjectType objectType,
        Vector2I cell,
        BoardObjectFaction faction,
        IEnumerable<string>? tags,
        int maxHp,
        int currentHp,
        int maxShield,
        int currentShield,
        bool blocksMovement,
        bool blocksLineOfSight,
        bool stackableWithUnit,
        int moveCostModifier,
        Godot.Collections.Dictionary? statePayload = null)
    {
        ObjectId = objectId;
        DefinitionId = definitionId;
        AiId = aiId ?? string.Empty;
        ObjectType = objectType;
        Cell = cell;
        Faction = faction;
        _tags = new HashSet<string>(tags ?? Array.Empty<string>(), StringComparer.Ordinal);
        MaxHp = Math.Max(0, maxHp);
        CurrentHp = MaxHp <= 0 ? 0 : Mathf.Clamp(currentHp, 0, MaxHp);
        MaxShield = Math.Max(0, maxShield);
        CurrentShield = Math.Max(0, currentShield);
        BlocksMovement = blocksMovement;
        BlocksLineOfSight = blocksLineOfSight;
        StackableWithUnit = stackableWithUnit;
        MoveCostModifier = moveCostModifier;
        StatePayload = CloneDictionary(statePayload);
    }

    public string ObjectId { get; }

    public string DefinitionId { get; }

    public string AiId { get; }

    public BoardObjectType ObjectType { get; }

    public Vector2I Cell { get; private set; }

    public BoardObjectFaction Faction { get; }

    public IReadOnlyCollection<string> Tags => _tags;

    public int MaxHp { get; private set; }

    public int CurrentHp { get; private set; }

    public int MaxShield { get; private set; }

    public int CurrentShield { get; private set; }

    public bool BlocksMovement { get; }

    public bool BlocksLineOfSight { get; }

    public bool StackableWithUnit { get; }

    public int MoveCostModifier { get; }

    public Godot.Collections.Dictionary StatePayload { get; }

    public bool IsDestroyed => MaxHp > 0 && CurrentHp <= 0;

    public bool HasTag(string tag)
    {
        return _tags.Contains(tag);
    }

    public void SetCell(Vector2I cell)
    {
        Cell = cell;
    }

    public void ApplyDamage(int amount)
    {
        if (MaxHp <= 0 || amount <= 0)
        {
            return;
        }

        int remainingDamage = amount;
        if (CurrentShield > 0)
        {
            int absorbed = Math.Min(CurrentShield, remainingDamage);
            CurrentShield -= absorbed;
            remainingDamage -= absorbed;
        }

        if (remainingDamage > 0)
        {
            CurrentHp = Math.Max(0, CurrentHp - remainingDamage);
        }
    }

    public void GainShield(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentShield += amount;
    }

    public void ApplyCombatDefaults(int maxHp, int currentHp, int maxShield = 0, int currentShield = 0)
    {
        // Shield-only spawns still need HP defaults, otherwise MaxHp stays 0 and damage is skipped.
        if (MaxHp > 0 || CurrentHp > 0)
        {
            return;
        }

        MaxHp = Math.Max(0, maxHp);
        CurrentHp = MaxHp <= 0 ? 0 : Mathf.Clamp(currentHp, 0, MaxHp);
        MaxShield = Math.Max(0, maxShield);
        CurrentShield = Math.Max(0, currentShield);
    }

    public void SyncCombatStats(int maxHp, int currentHp, int maxShield = 0, int currentShield = 0)
    {
        MaxHp = Math.Max(0, maxHp);
        CurrentHp = MaxHp <= 0 ? 0 : Mathf.Clamp(currentHp, 0, MaxHp);
        MaxShield = Math.Max(0, maxShield);
        CurrentShield = Math.Max(0, currentShield);
    }

    public static BoardObject FromSpawn(BoardObjectSpawnDefinition spawn)
    {
        string definitionId = string.IsNullOrWhiteSpace(spawn.DefinitionId) ? "undefined" : spawn.DefinitionId;
        string objectId;
        if (string.IsNullOrWhiteSpace(spawn.ObjectId))
        {
            string generatedId = $"{definitionId}_{Guid.NewGuid():N}";
            objectId = generatedId[..Math.Min(generatedId.Length, definitionId.Length + 9)];
        }
        else
        {
            objectId = spawn.ObjectId;
        }

        int resolvedCurrentHp = spawn.CurrentHp > 0 ? spawn.CurrentHp : spawn.MaxHp;
        int resolvedCurrentShield = spawn.CurrentShield > 0 ? spawn.CurrentShield : spawn.MaxShield;

        return new BoardObject(
            objectId,
            definitionId,
            spawn.AiId,
            spawn.ObjectType,
            spawn.Cell,
            spawn.Faction,
            spawn.Tags,
            spawn.MaxHp,
            resolvedCurrentHp,
            spawn.MaxShield,
            resolvedCurrentShield,
            spawn.BlocksMovement,
            spawn.BlocksLineOfSight,
            spawn.StackableWithUnit,
            spawn.MoveCostModifier,
            spawn.InitialStatePayload);
    }

    private static Godot.Collections.Dictionary CloneDictionary(Godot.Collections.Dictionary? source)
    {
        Godot.Collections.Dictionary clone = new();
        if (source == null)
        {
            return clone;
        }

        foreach (Variant key in source.Keys)
        {
            clone[key] = source[key];
        }

        return clone;
    }
}
