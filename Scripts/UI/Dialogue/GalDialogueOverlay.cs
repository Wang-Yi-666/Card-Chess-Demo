using Godot;

namespace CardChessDemo.Map;

public static class GalDialogueOverlay
{
    private const string PanelName = "GalDialoguePanel";
    private const string SpeakerPath = "Margin/VBox/SpeakerLabel";
    private const string ContentPath = "Margin/VBox/ContentLabel";

    public static void Show(Node context, string speaker, string content)
    {
        if (context == null)
        {
            return;
        }

        Node currentScene = context.GetTree()?.CurrentScene;
        if (currentScene == null)
        {
            return;
        }

        CanvasLayer ui = currentScene.FindChild("UI", true, false) as CanvasLayer;
        if (ui == null)
        {
            ui = new CanvasLayer { Name = "UI" };
            currentScene.AddChild(ui);
        }

        Panel panel = ui.GetNodeOrNull<Panel>(PanelName) ?? CreatePanel(ui);
        Label speakerLabel = panel.GetNodeOrNull<Label>(SpeakerPath);
        Label contentLabel = panel.GetNodeOrNull<Label>(ContentPath);
        if (speakerLabel == null || contentLabel == null)
        {
            return;
        }

        speakerLabel.Text = string.IsNullOrWhiteSpace(speaker) ? "旁白" : speaker.Trim();
        contentLabel.Text = string.IsNullOrWhiteSpace(content) ? "..." : content.Trim();
        panel.Visible = true;
    }

    private static Panel CreatePanel(CanvasLayer parent)
    {
        Panel panel = new Panel
        {
            Name = PanelName,
            AnchorLeft = 0.03f,
            AnchorTop = 1.0f,
            AnchorRight = 0.97f,
            AnchorBottom = 1.0f,
            OffsetTop = -164.0f,
            OffsetBottom = -10.0f,
            SelfModulate = new Color(0.06f, 0.08f, 0.12f, 0.92f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        MarginContainer margin = new MarginContainer
        {
            Name = "Margin",
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f,
            OffsetLeft = 18.0f,
            OffsetTop = 14.0f,
            OffsetRight = -18.0f,
            OffsetBottom = -14.0f,
        };

        VBoxContainer vbox = new VBoxContainer { Name = "VBox" };
        vbox.AddThemeConstantOverride("separation", 8);

        Label speaker = new Label { Name = "SpeakerLabel", Text = "旁白" };
        speaker.AddThemeFontSizeOverride("font_size", 18);
        speaker.AddThemeColorOverride("font_color", new Color(1.0f, 0.9f, 0.65f, 1.0f));

        Label body = new Label
        {
            Name = "ContentLabel",
            Text = "...",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            VerticalAlignment = VerticalAlignment.Top,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        body.AddThemeFontSizeOverride("font_size", 16);
        body.AddThemeColorOverride("font_color", new Color(0.98f, 0.98f, 0.98f, 1.0f));

        vbox.AddChild(speaker);
        vbox.AddChild(body);
        margin.AddChild(vbox);
        panel.AddChild(margin);
        parent.AddChild(panel);
        return panel;
    }
}
