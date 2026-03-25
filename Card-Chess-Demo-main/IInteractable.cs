using Godot;

public interface IInteractable
{
    // 用于 UI 提示，例如“开箱”“对话”“治疗”。
    string GetInteractText(Player player);

    // 可交互条件判断，例如是否已开箱、是否冷却中。
    bool CanInteract(Player player);

    // 交互主逻辑，例如开箱、对话、治疗。
    void Interact(Player player);
}
