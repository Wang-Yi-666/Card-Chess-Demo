using Godot;

public interface IInteractable
{
	string GetInteractText(Player player);
	bool CanInteract(Player player);
	void Interact(Player player);
}
