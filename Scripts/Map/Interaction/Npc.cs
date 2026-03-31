using Godot;

namespace CardChessDemo.Map;

public partial class Npc : InteractableTemplate
{
	[Export] public string NpcName = "村民";
	[Export] public string DialogueText = "你好，旅行者。";

	protected override void OnInteract(Player player)
	{
			GalDialogueOverlay.Show(this, NpcName, DialogueText);
		PlayInteractionPulse();
	}
}
