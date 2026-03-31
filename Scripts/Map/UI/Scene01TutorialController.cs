using Godot;

namespace CardChessDemo.Map;

public partial class Scene01TutorialController : Node
{
    [Export] public NodePath PlayerCharacterPath = new("MainPlayer/Player");
    [Export] public NodePath IntroDialogPanelPath = new("TutorialUI/TutorialTipPanel");
    [Export] public NodePath IntroDialogLabelPath = new("TutorialUI/TutorialTipPanel/TutorialTipLabel");
    [Export] public NodePath GuideLabelPath = new("TutorialUI/GuideLabel");
    [Export] public float DialogSlideOffset = 86.0f;

    private CharacterBody2D _player;
    private Panel _introPanel;
    private Label _introLabel;
    private Label _guideLabel;

    private string[] _introLines =
    {
        "你终于醒了，虽然不知道这是哪里，但是准备四处走走。按e继续......"
    };

    private int _lineIndex;
    private bool _isDialogActive;
    private float _panelVisibleTop;
    private float _panelVisibleBottom;

    public override void _Ready()
    {
        _player = PlayerCharacterPath.IsEmpty ? null : GetNodeOrNull<CharacterBody2D>(PlayerCharacterPath);
        _introPanel = IntroDialogPanelPath.IsEmpty ? null : GetNodeOrNull<Panel>(IntroDialogPanelPath);
        _introLabel = IntroDialogLabelPath.IsEmpty ? null : GetNodeOrNull<Label>(IntroDialogLabelPath);
        _guideLabel = GuideLabelPath.IsEmpty ? null : GetNodeOrNull<Label>(GuideLabelPath);

        if (_guideLabel != null)
        {
            _guideLabel.Visible = false;
            _guideLabel.Text = string.Empty;
        }

        if (_introPanel == null || _introLabel == null)
        {
            GD.PushWarning("Scene01TutorialController: 缺少教程对话UI节点，跳过开场对话。 ");
            ShowMovementGuide();
            return;
        }

        _panelVisibleTop = _introPanel.OffsetTop;
        _panelVisibleBottom = _introPanel.OffsetBottom;

        // 先隐藏到屏幕外，再滑入。
        _introPanel.OffsetTop = _panelVisibleTop + DialogSlideOffset;
        _introPanel.OffsetBottom = _panelVisibleBottom + DialogSlideOffset;
        _introPanel.Visible = true;

        _lineIndex = 0;
        _isDialogActive = true;
        _introLabel.Text = _introLines[_lineIndex];

        SetPlayerInputEnabled(false);
        SlideDialogIn();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isDialogActive)
        {
            return;
        }

        if (!@event.IsActionPressed("interact"))
        {
            return;
        }

        _lineIndex++;
        if (_lineIndex < _introLines.Length)
        {
            _introLabel.Text = _introLines[_lineIndex];
            GetViewport().SetInputAsHandled();
            return;
        }

        _isDialogActive = false;
        SlideDialogOutAndHide();
        SetPlayerInputEnabled(true);
        ShowMovementGuide();
        GetViewport().SetInputAsHandled();
    }

    private void ShowMovementGuide()
    {
        if (_guideLabel == null)
        {
            return;
        }

        _guideLabel.Text = "使用wasd进行移动";
        _guideLabel.Visible = true;
    }

    private void SlideDialogIn()
    {
        if (_introPanel == null)
        {
            return;
        }

        Tween tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_introPanel, "offset_top", _panelVisibleTop, 0.20f);
        tween.TweenProperty(_introPanel, "offset_bottom", _panelVisibleBottom, 0.20f);
    }

    private void SlideDialogOutAndHide()
    {
        if (_introPanel == null)
        {
            return;
        }

        float hiddenTop = _panelVisibleTop + DialogSlideOffset;
        float hiddenBottom = _panelVisibleBottom + DialogSlideOffset;

        Tween tween = CreateTween();
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_introPanel, "offset_top", hiddenTop, 0.18f);
        tween.TweenProperty(_introPanel, "offset_bottom", hiddenBottom, 0.18f);
        tween.Finished += () =>
        {
            if (_introPanel != null)
            {
                _introPanel.Visible = false;
            }
        };
    }

    private void SetPlayerInputEnabled(bool enabled)
    {
        if (_player == null)
        {
            return;
        }

        _player.SetPhysicsProcess(enabled);
        _player.SetProcessUnhandledInput(enabled);
    }
}
