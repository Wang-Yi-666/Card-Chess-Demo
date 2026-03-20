using Godot;

namespace CardChessDemo.Battle.Presentation;

public partial class BattleObstacleView : BattleAnimatedViewBase
{
    protected override SpriteFrames BuildFallbackFrames()
    {
        return CreateFrames(new Color(0.75f, 0.74f, 0.67f), new Color(0.92f, 0.9f, 0.84f));
    }
}
