using Godot;
using CardChessDemo.Battle.Boundary;
using CardChessDemo.Battle.Shared;

public partial class MapSceneController : Node2D
{
    [Export] public NodePath PlayerPath { get; set; } = NodePath.Empty;

    private GlobalGameSession? _globalSession;

    public override void _Ready()
    {
        _globalSession = GetNodeOrNull<GlobalGameSession>("/root/GlobalGameSession");
        if (_globalSession == null)
        {
            return;
        }

        ApplyPendingResumeContext();
        _globalSession.ConsumeLastBattleResult();
    }

    private void ApplyPendingResumeContext()
    {
        if (_globalSession == null)
        {
            return;
        }

        MapResumeContext? resumeContext = _globalSession.PeekPendingMapResumeContext();
        if (resumeContext == null)
        {
            return;
        }

        string currentScenePath = GetTree().CurrentScene?.SceneFilePath ?? SceneFilePath;
        if (!string.Equals(currentScenePath, resumeContext.ScenePath, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (GetNodeOrNull<Node2D>(PlayerPath) is Node2D player)
        {
            player.GlobalPosition = resumeContext.PlayerGlobalPosition;
        }

        _globalSession.ConsumePendingMapResumeContext();
    }
}
