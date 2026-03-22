using Godot;
using CardChessDemo.Battle.Shared;

namespace CardChessDemo.Battle.Boundary;

public sealed class BattleRequest
{
    public BattleRequest(Godot.Collections.Dictionary? playerSnapshot = null)
    {
        PlayerSnapshot = CloneDictionary(playerSnapshot);
    }

    public Godot.Collections.Dictionary PlayerSnapshot { get; }

    public static BattleRequest FromSession(GlobalGameSession session)
    {
        return new BattleRequest(session.BuildPlayerSnapshot());
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
