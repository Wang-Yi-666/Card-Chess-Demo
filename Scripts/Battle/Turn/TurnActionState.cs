using System;

namespace CardChessDemo.Battle.Turn;

public enum TurnPhase
{
    PlayerMove = 0,
    PlayerAction = 1,
    TurnPost = 2,
    EnemyTurn = 3,
}

public enum TurnInputMode
{
    None = 0,
    AttackTargeting = 1,
    CardTargeting = 2,
}

public sealed class TurnActionState
{
    public int TurnIndex { get; private set; }

    public TurnPhase Phase { get; private set; } = TurnPhase.PlayerMove;

    public TurnInputMode InputMode { get; private set; } = TurnInputMode.None;

    public bool HasMoved { get; private set; }

    public bool HasActed { get; private set; }

    public bool HasEndedTurn { get; private set; }

    public int QuickChainCount { get; private set; }

    public string SelectedCardInstanceId { get; private set; } = string.Empty;

    public int EnergyRechargeTurnInterval { get; private set; } = 3;

    public int EnergyRechargeTurnProgress { get; private set; }

    public bool IsPlayerTurn => Phase == TurnPhase.PlayerMove || Phase == TurnPhase.PlayerAction;

    public bool CanMove => Phase == TurnPhase.PlayerMove && !HasMoved;

    public bool CanEnterAttackTargeting => IsPlayerTurn && !HasActed;

    public bool CanSelectCard => IsPlayerTurn && !HasActed;

    public bool IsAttackTargeting => Phase == TurnPhase.PlayerAction && InputMode == TurnInputMode.AttackTargeting;

    public bool IsCardTargeting => Phase == TurnPhase.PlayerAction
        && InputMode == TurnInputMode.CardTargeting
        && !string.IsNullOrWhiteSpace(SelectedCardInstanceId);

    public void StartNewTurn(int turnIndex)
    {
        TurnIndex = turnIndex;
        Phase = TurnPhase.PlayerMove;
        InputMode = TurnInputMode.None;
        HasMoved = false;
        HasActed = false;
        HasEndedTurn = false;
        QuickChainCount = 0;
        SelectedCardInstanceId = string.Empty;
        if (turnIndex <= 1)
        {
            EnergyRechargeTurnProgress = 0;
        }
    }

    public void EnterActionPhase()
    {
        if (!IsPlayerTurn || HasActed)
        {
            return;
        }

        Phase = TurnPhase.PlayerAction;
        InputMode = TurnInputMode.None;
    }

    public void EnterAttackTargeting()
    {
        if (!CanEnterAttackTargeting)
        {
            return;
        }

        if (Phase == TurnPhase.PlayerMove)
        {
            EnterActionPhase();
        }

        SelectedCardInstanceId = string.Empty;
        InputMode = TurnInputMode.AttackTargeting;
    }

    public void EnterCardTargeting(string cardInstanceId)
    {
        if (!CanSelectCard || string.IsNullOrWhiteSpace(cardInstanceId))
        {
            return;
        }

        if (Phase == TurnPhase.PlayerMove)
        {
            EnterActionPhase();
        }

        SelectedCardInstanceId = cardInstanceId;
        InputMode = TurnInputMode.CardTargeting;
    }

    public void ClearSelectedCard()
    {
        SelectedCardInstanceId = string.Empty;
    }

    public void CancelTargeting()
    {
        ClearSelectedCard();
        InputMode = TurnInputMode.None;

        // If targeting was opened directly from the move stage and no real action
        // has been committed yet, return to PlayerMove so the unit can still move.
        if (Phase == TurnPhase.PlayerAction && !HasMoved && !HasActed && !HasEndedTurn)
        {
            Phase = TurnPhase.PlayerMove;
        }
    }

    public void MarkMoved()
    {
        if (!CanMove)
        {
            return;
        }

        HasMoved = true;
        Phase = TurnPhase.PlayerAction;
        SelectedCardInstanceId = string.Empty;
        InputMode = TurnInputMode.None;
    }

    public void MarkActed(bool endsTurn = true)
    {
        if (!CanSelectCard && !IsAttackTargeting && !IsCardTargeting)
        {
            return;
        }

        if (endsTurn)
        {
            HasActed = true;
            HasEndedTurn = true;
            Phase = TurnPhase.TurnPost;
        }
        else
        {
            IncrementQuickChain();
            Phase = TurnPhase.PlayerAction;
        }

        SelectedCardInstanceId = string.Empty;
        InputMode = TurnInputMode.None;
    }

    public void MarkEndedTurn()
    {
        HasEndedTurn = true;
        Phase = TurnPhase.TurnPost;
        SelectedCardInstanceId = string.Empty;
        InputMode = TurnInputMode.None;
    }

    public void BeginEnemyTurn()
    {
        Phase = TurnPhase.EnemyTurn;
        SelectedCardInstanceId = string.Empty;
        InputMode = TurnInputMode.None;
    }

    public void IncrementQuickChain()
    {
        QuickChainCount++;
    }

    public void ConfigureEnergyRechargeInterval(int interval)
    {
        EnergyRechargeTurnInterval = Math.Max(1, interval);
        EnergyRechargeTurnProgress = Math.Min(EnergyRechargeTurnProgress, EnergyRechargeTurnInterval - 1);
    }

    public bool AdvanceEnergyRechargeProgress()
    {
        EnergyRechargeTurnProgress++;
        if (EnergyRechargeTurnProgress < EnergyRechargeTurnInterval)
        {
            return false;
        }

        EnergyRechargeTurnProgress = 0;
        return true;
    }

    public void AdvanceToNextTurn()
    {
        StartNewTurn(TurnIndex + 1);
    }
}
