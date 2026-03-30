using Godot;
using CardChessDemo.Battle.Boundary;
using CardChessDemo.Battle.Shared;

namespace CardChessDemo.Map;

public static class MapBattleTransitionHelper
{
    public static bool TryEnterBattle(
        Node contextNode,
        Player player,
        PackedScene? battleScene,
        string battleScenePath,
        string battleEncounterId,
        out string failureReason)
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

        Error result = battleScene != null
            ? contextNode.GetTree().ChangeSceneToPacked(battleScene)
            : contextNode.GetTree().ChangeSceneToFile(battleScenePath.Trim());

        if (result != Error.Ok)
        {
            failureReason = $"Scene change failed, error={result}.";
            globalSession.CancelPendingBattleTransition();
            return false;
        }

        return true;
    }
}
