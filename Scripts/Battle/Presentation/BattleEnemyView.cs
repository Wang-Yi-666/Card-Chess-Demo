using Godot;

namespace CardChessDemo.Battle.Presentation;

public partial class BattleEnemyView : BattleAnimatedViewBase
{
    protected override SpriteFrames BuildFallbackFrames()
    {
        return CreateFrames(new Color(1.0f, 0.38f, 0.32f), new Color(1.0f, 0.72f, 0.65f));
    }
}
