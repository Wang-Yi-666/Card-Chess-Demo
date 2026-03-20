using System.Linq;
using System.Text;
using Godot;
using CardChessDemo.Battle.State;
using CardChessDemo.Battle.Shared;
using CardChessDemo.Battle.Turn;

namespace CardChessDemo.Battle.UI;

public partial class BattleHudController : CanvasLayer
{
    [Signal] public delegate void EndTurnRequestedEventHandler();
    [Signal] public delegate void MovePointDeltaRequestedEventHandler(int delta);

    [Export] public int ReservedBottomScreenPixels { get; set; } = 44;

    private Label _playerSummary = null!;
    private Label _turnSummary = null!;
    private Label _objectStates = null!;
    private Label _debugHint = null!;
    private Button _endTurnButton = null!;
    private Button _moveMinusButton = null!;
    private Button _movePlusButton = null!;

    private BattleObjectStateManager? _stateManager;
    private GlobalGameSession? _session;
    private TurnActionState? _turnState;

    public override void _Ready()
    {
        _playerSummary = GetNode<Label>("Panel/Margin/VBox/TopRow/PlayerSummary");
        _turnSummary = GetNode<Label>("Panel/Margin/VBox/TopRow/TurnSummary");
        _objectStates = GetNode<Label>("Panel/Margin/VBox/BottomRow/ObjectStates");
        _debugHint = GetNode<Label>("Panel/Margin/VBox/BottomRow/DebugHint");
        _endTurnButton = GetNode<Button>("Panel/Margin/VBox/TopRow/EndTurnButton");
        _moveMinusButton = GetNode<Button>("Panel/Margin/VBox/TopRow/MoveMinusButton");
        _movePlusButton = GetNode<Button>("Panel/Margin/VBox/TopRow/MovePlusButton");

        _endTurnButton.Pressed += OnEndTurnPressed;
        _moveMinusButton.Pressed += OnMoveMinusPressed;
        _movePlusButton.Pressed += OnMovePlusPressed;
    }

    public override void _ExitTree()
    {
        if (IsNodeReady())
        {
            _endTurnButton.Pressed -= OnEndTurnPressed;
            _moveMinusButton.Pressed -= OnMoveMinusPressed;
            _movePlusButton.Pressed -= OnMovePlusPressed;
        }
    }

    public void Bind(BattleObjectStateManager stateManager, GlobalGameSession session, TurnActionState turnState)
    {
        _stateManager = stateManager;
        _session = session;
        _turnState = turnState;
        Refresh();
    }

    public override void _Process(double delta)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_stateManager == null || _session == null || _turnState == null || !IsNodeReady())
        {
            return;
        }

        BattleObjectState? playerState = _stateManager.GetPrimaryPlayerState();
        if (playerState != null)
        {
            _playerSummary.Text = $"HP {playerState.CurrentHp}/{playerState.MaxHp}  MV {playerState.MovePointsPerTurn}  @{FormatCell(playerState.Cell)}";
        }
        else
        {
            _playerSummary.Text = "Player missing";
        }

        _turnSummary.Text = $"Turn {_turnState.TurnIndex}  M:{FormatFlag(_turnState.HasMoved)}  E:{FormatFlag(_turnState.HasEndedTurn)}";
        _endTurnButton.Text = _turnState.HasEndedTurn ? "Next" : "End";
        _objectStates.Text = BuildObjectSummary();
        _debugHint.Text = "LMB move | T/Enter end-turn";
    }

    private void OnEndTurnPressed()
    {
        EmitSignal(SignalName.EndTurnRequested);
    }

    private void OnMoveMinusPressed()
    {
        EmitSignal(SignalName.MovePointDeltaRequested, -1);
    }

    private void OnMovePlusPressed()
    {
        EmitSignal(SignalName.MovePointDeltaRequested, 1);
    }

    private string BuildObjectSummary()
    {
        if (_stateManager == null)
        {
            return "Objs P0 E0 O0";
        }

        int playerCount = 0;
        int enemyCount = 0;
        int obstacleCount = 0;

        foreach (BattleObjectState state in _stateManager.AllStates)
        {
            switch (state.ObjectType)
            {
                case CardChessDemo.Battle.Board.BoardObjectType.Unit when state.IsPlayer:
                    playerCount++;
                    break;
                case CardChessDemo.Battle.Board.BoardObjectType.Unit:
                    enemyCount++;
                    break;
                case CardChessDemo.Battle.Board.BoardObjectType.Obstacle:
                    obstacleCount++;
                    break;
            }
        }

        BattleObjectState? playerState = _stateManager.GetPrimaryPlayerState();
        string animation = playerState?.CurrentAnimation ?? "n/a";
        return $"Objs P{playerCount} E{enemyCount} O{obstacleCount}  Anim {animation}";
    }

    private static string FormatFlag(bool value)
    {
        return value ? "Y" : "N";
    }

    private static string FormatCell(Vector2I cell)
    {
        return $"{cell.X},{cell.Y}";
    }
}
