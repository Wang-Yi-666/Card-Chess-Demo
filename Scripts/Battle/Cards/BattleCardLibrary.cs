using System;
using System.Linq;
using System.Collections.Generic;
using Godot;

namespace CardChessDemo.Battle.Cards;

[GlobalClass]
public partial class BattleCardLibrary : Resource
{
	[Export] public BattleCardTemplate[] Entries { get; set; } = Array.Empty<BattleCardTemplate>();

	public BattleCardTemplate? FindTemplate(string cardId)
	{
		return Entries.FirstOrDefault(entry => string.Equals(entry.CardId, cardId, StringComparison.Ordinal));
	}

	public BattleCardDefinition[] BuildDefinitions()
	{
		return Entries
			.Where(entry => entry != null)
			.Select(entry => entry.BuildRuntimeDefinition())
			.ToArray();
	}

	public string[] BuildStarterDeckCardIds()
	{
		List<string> cardIds = new();
		foreach (BattleCardTemplate template in Entries.Where(entry => entry != null))
		{
			int copies = Math.Max(0, template.DefaultStarterCopies);
			for (int copyIndex = 0; copyIndex < copies; copyIndex++)
			{
				cardIds.Add(template.CardId);
			}
		}

		return cardIds.ToArray();
	}
}
