using Godot;
using CardChessDemo.Battle.Board;

namespace CardChessDemo.Battle;

public partial class BattleBoardDebugView : Node2D
{
    [Export] public Vector2 CellSize { get; set; } = new(96.0f, 96.0f);
    [Export] public Color BackgroundColor { get; set; } = new(0.08f, 0.1f, 0.14f, 0.95f);
    [Export] public Color GridColor { get; set; } = new(0.28f, 0.32f, 0.4f, 1.0f);
    [Export] public Color UnitColor { get; set; } = new(0.95f, 0.55f, 0.3f, 0.7f);
    [Export] public Color ObstacleColor { get; set; } = new(0.55f, 0.62f, 0.72f, 0.75f);
    [Export] public Color FieldColor { get; set; } = new(0.3f, 0.75f, 0.95f, 0.65f);
    [Export] public Color DeviceColor { get; set; } = new(0.8f, 0.7f, 0.25f, 0.7f);
    [Export] public Color PickupColor { get; set; } = new(0.45f, 0.9f, 0.45f, 0.7f);

    private BattleSceneController? _controller;

    public void Bind(BattleSceneController controller)
    {
        _controller = controller;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_controller?.BoardState == null || _controller.QueryService == null)
        {
            return;
        }

        BoardState boardState = _controller.BoardState;
        BoardQueryService queryService = _controller.QueryService;

        for (int y = 0; y < boardState.Size.Y; y++)
        {
            for (int x = 0; x < boardState.Size.X; x++)
            {
                Vector2 cellPosition = new(x * CellSize.X, y * CellSize.Y);
                Rect2 rect = new(cellPosition, CellSize);
                DrawRect(rect, BackgroundColor, true);

                Color fillColor = GetCellFillColor(queryService.GetObjectsAtCell(new Vector2I(x, y)));
                if (fillColor.A > 0.0f)
                {
                    DrawRect(rect.Grow(-6.0f), fillColor, true);
                }

                DrawRect(rect, GridColor, false, 2.0f);
            }
        }
    }

    private Color GetCellFillColor(System.Collections.Generic.IReadOnlyList<BoardObject> objects)
    {
        foreach (BoardObject boardObject in objects)
        {
            switch (boardObject.ObjectType)
            {
                case BoardObjectType.Unit:
                    return UnitColor;
                case BoardObjectType.Obstacle:
                    return ObstacleColor;
                case BoardObjectType.Field:
                    return FieldColor;
                case BoardObjectType.Device:
                    return DeviceColor;
                case BoardObjectType.Pickup:
                    return PickupColor;
            }
        }

        return Colors.Transparent;
    }
}
