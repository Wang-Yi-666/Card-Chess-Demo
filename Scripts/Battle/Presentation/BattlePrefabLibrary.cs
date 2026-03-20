using System;
using System.Linq;
using Godot;

namespace CardChessDemo.Battle.Presentation;

[GlobalClass]
public partial class BattlePrefabLibrary : Resource
{
    [Export] public BattlePrefabEntry[] Entries { get; set; } = Array.Empty<BattlePrefabEntry>();

    public BattlePrefabEntry? FindEntry(string definitionId)
    {
        return Entries.FirstOrDefault(entry => string.Equals(entry.DefinitionId, definitionId, StringComparison.Ordinal));
    }
}
