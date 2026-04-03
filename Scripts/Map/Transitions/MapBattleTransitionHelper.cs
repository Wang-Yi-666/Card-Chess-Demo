using Godot;
using CardChessDemo.Battle.Boundary;
using CardChessDemo.Battle.Shared;
using System.Threading.Tasks;

namespace CardChessDemo.Map;

public static class MapBattleTransitionHelper
{
    private const string DefaultTransitionOverlayScenePath = "res://Scene/Transitions/MapBattleTransitionOverlay.tscn";

    public static bool TryEnterBattle(
        Node contextNode,
        Player player,
        PackedScene? battleScene,
        string battleScenePath,
        string battleEncounterId,
        out string failureReason,
        System.Action<string>? deferredFailureCallback = null)
    {
        failureReason = string.Empty;

        if (battleScene == null && string.IsNullOrWhiteSpace(battleScenePath))
        {
            failureReason = "Battle scene is not configured.";
            return false;
        }

        GlobalGameSession? globalSession = contextNode.GetNodeOrNull<GlobalGameSession>("/root/GlobalGameSession");
        if (globalSession == null)
        {
            failureReason = "GlobalGameSession is missing.";
            return false;
        }

        string currentScenePath = contextNode.GetTree().CurrentScene?.SceneFilePath ?? string.Empty;
        globalSession.BeginBattle(BattleRequest.FromSession(globalSession, battleEncounterId));
        globalSession.SetPendingBattleEncounterId(battleEncounterId);
        globalSession.SetPendingMapResumeContext(new MapResumeContext(currentScenePath, player.GlobalPosition));

        _ = ExecuteBattleTransitionAsync(contextNode, battleScene, battleScenePath, globalSession, deferredFailureCallback);
        return true;
    }

    private static async Task ExecuteBattleTransitionAsync(
        Node contextNode,
        PackedScene? battleScene,
        string battleScenePath,
        GlobalGameSession globalSession,
        System.Action<string>? deferredFailureCallback)
    {
        if (!GodotObject.IsInstanceValid(contextNode))
        {
            globalSession.CancelPendingBattleTransition();
            deferredFailureCallback?.Invoke("Battle transition context node is no longer valid.");
            return;
        }

        SceneTree tree = contextNode.GetTree();
        MapBattleTransitionOverlay? overlay = await SpawnTransitionOverlayAsync(tree);
        if (overlay != null)
        {
            await overlay.PlayAsync();
            overlay.QueueFree();
        }

        Error result = battleScene != null
            ? tree.ChangeSceneToPacked(battleScene)
            : tree.ChangeSceneToFile(battleScenePath.Trim());

        if (result != Error.Ok)
        {
            string failureReason = $"Scene change failed, error={result}.";
            globalSession.CancelPendingBattleTransition();
            deferredFailureCallback?.Invoke(failureReason);
            GD.PushError($"MapBattleTransitionHelper: {failureReason}");
        }
    }

    private static async Task<MapBattleTransitionOverlay?> SpawnTransitionOverlayAsync(SceneTree tree)
    {
        PackedScene? overlayScene = GD.Load<PackedScene>(DefaultTransitionOverlayScenePath);
        if (overlayScene == null)
        {
            GD.PushWarning($"MapBattleTransitionHelper: failed to load transition overlay scene at {DefaultTransitionOverlayScenePath}.");
            return null;
        }

        MapBattleTransitionOverlay? overlay = overlayScene.Instantiate<MapBattleTransitionOverlay>();
        if (overlay == null)
        {
            GD.PushWarning("MapBattleTransitionHelper: failed to instantiate transition overlay.");
            return null;
        }

        tree.Root.AddChild(overlay);
        await overlay.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        return overlay;
    }
}
