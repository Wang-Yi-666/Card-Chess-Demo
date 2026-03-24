using Godot;

namespace CardChessDemo.Battle.Boundary;

public sealed class MapResumeContext
{
    public MapResumeContext(string scenePath, Vector2 playerGlobalPosition)
    {
        ScenePath = scenePath ?? string.Empty;
        PlayerGlobalPosition = playerGlobalPosition;
    }

    public string ScenePath { get; }

    public Vector2 PlayerGlobalPosition { get; }
}
