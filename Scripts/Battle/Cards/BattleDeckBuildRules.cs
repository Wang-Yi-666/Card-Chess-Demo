using Godot;

namespace CardChessDemo.Battle.Cards;

[GlobalClass]
public partial class BattleDeckBuildRules : Resource
{
	[Export] public int MinDeckSize { get; set; } = 10;
	[Export] public int MaxDeckSize { get; set; } = 18;
	[Export] public int BasePointBudget { get; set; } = 20;
	[Export] public int BaseMaxCopiesPerCard { get; set; } = 2;
	[Export] public int BaseCycleCardLimit { get; set; } = 4;
	[Export] public int BaseQuickCycleCardLimit { get; set; } = 2;
	[Export] public int BaseEnergyPositiveCardLimit { get; set; } = 2;
	[Export] public int BaseOverlimitCarrySlots { get; set; } = 1;
}
