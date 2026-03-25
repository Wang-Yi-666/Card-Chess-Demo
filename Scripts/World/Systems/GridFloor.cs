using Godot;

public partial class GridFloor : Node2D
{
    [Export] public int CellSize = 128;
    [Export] public int GridWidth = 30;
    [Export] public int GridHeight = 20;
    [Export] public Color GridColor = new Color(0.45f, 0.55f, 0.65f, 0.9f);
    [Export] public Color FillColor = new Color(0.09f, 0.1f, 0.12f, 1.0f);
    [Export] public bool DrawFill = true;

    public override void _Ready()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        int widthPx = GridWidth * CellSize;
        int heightPx = GridHeight * CellSize;

        if (DrawFill)
        {
            DrawRect(new Rect2(Vector2.Zero, new Vector2(widthPx, heightPx)), FillColor, true);
        }

        for (int x = 0; x <= GridWidth; x++)
        {
            float px = x * CellSize;
            DrawLine(new Vector2(px, 0), new Vector2(px, heightPx), GridColor, 1.0f, true);
        }

        for (int y = 0; y <= GridHeight; y++)
        {
            float py = y * CellSize;
            DrawLine(new Vector2(0, py), new Vector2(widthPx, py), GridColor, 1.0f, true);
        }
    }
}
