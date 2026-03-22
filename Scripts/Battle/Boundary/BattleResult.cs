using Godot;
using CardChessDemo.Battle.Shared;

namespace CardChessDemo.Battle.Boundary;

public sealed class BattleResult
{
    public BattleResult(Godot.Collections.Dictionary? playerSnapshot = null, bool didPlayerFail = false)
    {
        PlayerSnapshot = CloneDictionary(playerSnapshot);
        DidPlayerFail = didPlayerFail;
    }

    public Godot.Collections.Dictionary PlayerSnapshot { get; }
    public bool DidPlayerFail { get; }

    public static BattleResult FromSession(GlobalGameSession session, bool didPlayerFail)
    {
        return new BattleResult(session.BuildPlayerSnapshot(), didPlayerFail);
    }

    public void ApplyToSession(GlobalGameSession session)
    {
        session.ApplyPlayerSnapshot(PlayerSnapshot);
    }

    private static Godot.Collections.Dictionary CloneDictionary(Godot.Collections.Dictionary? source)
    {
        Godot.Collections.Dictionary clone = new();
        if (source == null)
        {
            return clone;
        }

        foreach (Variant key in source.Keys)
        {
            clone[key] = source[key];
        }

        return clone;
    }
}
