using Godot;

public partial class PartnerManager : Node
{
    [Export] public PackedScene PartnerScene;
    [Export] public StringName PartnerJoinedFlagKey = new StringName("partner_joined");
    [Export] public string DefaultPartnerScenePath = "res://Scene/Partner.tscn";
    [Export] public string RuntimePartnerRootName = "RuntimePartnerScene";

    private Node _runtimePartnerRoot;

    public override void _Ready()
    {
        if (PartnerScene == null && !string.IsNullOrWhiteSpace(DefaultPartnerScenePath))
        {
            string normalizedPath = NormalizeLegacyScenePath(DefaultPartnerScenePath.Trim());
            if (ResourceLoader.Exists(normalizedPath))
            {
                PartnerScene = ResourceLoader.Load<PackedScene>(normalizedPath);
            }

            if (PartnerScene == null)
            {
                GD.PushWarning($"PartnerManager: 伙伴场景加载失败，路径='{normalizedPath}'。");
            }
        }
    }

    private static string NormalizeLegacyScenePath(string path)
    {
        if (path.StartsWith("res://Scene(garbage)/"))
        {
            return path.Replace("res://Scene(garbage)/", "res://Scene/");
        }

        return path;
    }

    public override void _Process(double delta)
    {
        Node currentScene = GetTree().CurrentScene;
        if (currentScene == null)
        {
            return;
        }

        if (!IsPartnerJoined())
        {
            CleanupRuntimePartner();
            return;
        }

        Player player = FindFirstPlayer(currentScene);
        if (player == null)
        {
            CleanupRuntimePartner();
            return;
        }

        Partner existingPartner = FindFirstPartner(currentScene);
        if (existingPartner != null)
        {
            if (_runtimePartnerRoot != null && IsInstanceValid(_runtimePartnerRoot))
            {
                _runtimePartnerRoot = existingPartner.GetParent();
            }

            return;
        }

        EnsureRuntimePartner(currentScene, player);
    }

    private void EnsureRuntimePartner(Node currentScene, Player player)
    {
        if (_runtimePartnerRoot != null && IsInstanceValid(_runtimePartnerRoot) && _runtimePartnerRoot.GetParent() == currentScene)
        {
            return;
        }

        CleanupRuntimePartner();

        if (PartnerScene == null)
        {
            GD.PushWarning("PartnerManager: PartnerScene 未配置，无法自动生成伙伴。");
            return;
        }

        Node spawned = PartnerScene.Instantiate();
        spawned.Name = RuntimePartnerRootName;
        currentScene.AddChild(spawned);

        if (spawned is Node2D partnerRoot2D)
        {
            partnerRoot2D.GlobalPosition = player.GlobalPosition + new Vector2(-80.0f, 48.0f);
        }

        _runtimePartnerRoot = spawned;
    }

    private void CleanupRuntimePartner()
    {
        if (_runtimePartnerRoot == null || !IsInstanceValid(_runtimePartnerRoot))
        {
            _runtimePartnerRoot = null;
            return;
        }

        _runtimePartnerRoot.QueueFree();
        _runtimePartnerRoot = null;
    }

    private bool IsPartnerJoined()
    {
        GameSession session = GetNodeOrNull<GameSession>("/root/GameSession");
        if (session == null)
        {
            return false;
        }

        if (!session.world_flags.TryGetValue(PartnerJoinedFlagKey, out Variant value))
        {
            return false;
        }

        return value.VariantType == Variant.Type.Bool && value.AsBool();
    }

    private Partner FindFirstPartner(Node root)
    {
        if (root == null)
        {
            return null;
        }

        if (root is Partner partner)
        {
            return partner;
        }

        foreach (Node child in root.GetChildren())
        {
            Partner found = FindFirstPartner(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private Player FindFirstPlayer(Node root)
    {
        if (root == null)
        {
            return null;
        }

        if (root is Player player)
        {
            return player;
        }

        foreach (Node child in root.GetChildren())
        {
            Player found = FindFirstPlayer(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
