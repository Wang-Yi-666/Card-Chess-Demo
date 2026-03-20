namespace CardChessDemo.Battle.Turn;

public sealed class TurnActionState
{
    public int TurnIndex { get; private set; }

    public bool HasMoved { get; private set; }

    public bool HasEndedTurn { get; private set; }

    public int QuickChainCount { get; private set; }

    public bool CanMove => !HasMoved && !HasEndedTurn;

    public bool CanAct => !HasEndedTurn;

    public void StartNewTurn(int turnIndex)
    {
        TurnIndex = turnIndex;
        HasMoved = false;
        HasEndedTurn = false;
        QuickChainCount = 0;
    }

    public void MarkMoved()
    {
        HasMoved = true;
    }

    public void MarkEndedTurn()
    {
        HasEndedTurn = true;
    }

    public void IncrementQuickChain()
    {
        QuickChainCount++;
    }

    public void AdvanceToNextTurn()
    {
        StartNewTurn(TurnIndex + 1);
    }
}
