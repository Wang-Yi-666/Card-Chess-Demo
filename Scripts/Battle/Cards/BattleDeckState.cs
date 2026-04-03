using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CardChessDemo.Battle.Cards;

public sealed class BattleDeckState
{
    private readonly List<BattleCardDefinition> _startingDeck;
    private readonly List<BattleCardInstance> _drawPile = new();
    private readonly List<BattleCardInstance> _hand = new();
    private readonly List<BattleCardInstance> _discardPile = new();
    private readonly List<BattleCardInstance> _exhaustPile = new();
    private readonly RandomNumberGenerator _rng = new();
    private int _instanceCounter;

    public BattleDeckState(
        IEnumerable<BattleCardDefinition> startingDeck,
        ulong seed,
        int handSize = 3,
        int maxEnergyPerTurn = 2,
        BattleDeckRuntimeInit? runtimeInit = null)
    {
        _startingDeck = startingDeck.ToList();
        HandSize = runtimeInit != null && runtimeInit.HandSizeOverride > 0
            ? runtimeInit.HandSizeOverride
            : Math.Max(1, handSize);
        MaxEnergyPerTurn = runtimeInit != null && runtimeInit.MaxEnergyOverride >= 0
            ? Math.Max(0, runtimeInit.MaxEnergyOverride)
            : Math.Max(0, maxEnergyPerTurn);
        _rng.Seed = seed == 0 ? 1UL : seed;
        ResetForBattle(runtimeInit);
    }

    public int HandSize { get; }

    public int MaxEnergyPerTurn { get; }

    public int CurrentEnergy { get; private set; }

    public int EnergyRegenIntervalTurns { get; private set; } = 3;

    public int EnergyRegenAmount { get; private set; } = 1;

    public IReadOnlyList<BattleCardInstance> Hand => _hand;

    public IReadOnlyList<BattleCardInstance> DrawPileCards => _drawPile;

    public IReadOnlyList<BattleCardInstance> DiscardPileCards => _discardPile;

    public IReadOnlyList<BattleCardInstance> ExhaustPileCards => _exhaustPile;

    public int DrawPileCount => _drawPile.Count;

    public int DiscardPileCount => _discardPile.Count;

    public int ExhaustPileCount => _exhaustPile.Count;

    public void ResetForBattle(BattleDeckRuntimeInit? runtimeInit = null)
    {
        _drawPile.Clear();
        _hand.Clear();
        _discardPile.Clear();
        _exhaustPile.Clear();
        _instanceCounter = 0;

        if (runtimeInit != null && runtimeInit.HasExplicitPiles)
        {
            AddInstances(_hand, runtimeInit.StartingHandCards);
            AddInstances(_drawPile, runtimeInit.StartingDrawPileCards);
            AddInstances(_discardPile, runtimeInit.StartingDiscardPileCards);
            AddInstances(_exhaustPile, runtimeInit.StartingExhaustPileCards);
        }
        else
        {
            IEnumerable<BattleCardDefinition> sourceDeck = runtimeInit != null && runtimeInit.BuildCards.Length > 0
                ? runtimeInit.BuildCards
                : _startingDeck;

            foreach (BattleCardDefinition definition in sourceDeck)
            {
                _drawPile.Add(CreateInstance(definition));
            }
        }

        CurrentEnergy = runtimeInit != null && runtimeInit.InitialEnergy >= 0
            ? Mathf.Clamp(runtimeInit.InitialEnergy, 0, MaxEnergyPerTurn)
            : MaxEnergyPerTurn;
    }

    public void StartPlayerTurn()
    {
        // Energy no longer refills every turn. It is driven by the turn-based
        // recharge loop and card effects.
    }

    public void EndPlayerTurn()
    {
    }

    public void DiscardHand()
    {
        if (_hand.Count == 0)
        {
            return;
        }

        _discardPile.AddRange(_hand);
        _hand.Clear();
    }

    public void DrawToHandSize()
    {
        DrawUpToHandSize();
    }

    public int DrawCards(int count)
    {
        int drawnCount = 0;
        for (int index = 0; index < count; index++)
        {
            if (!TryDrawOne())
            {
                break;
            }

            drawnCount++;
        }

        return drawnCount;
    }

    public bool TryGetHandCard(string instanceId, out BattleCardInstance? cardInstance)
    {
        cardInstance = _hand.FirstOrDefault(card => string.Equals(card.InstanceId, instanceId, StringComparison.Ordinal));
        return cardInstance != null;
    }

    public bool CanPlay(string instanceId, out string failureReason)
    {
        failureReason = string.Empty;

        if (!TryGetHandCard(instanceId, out BattleCardInstance? cardInstance) || cardInstance == null)
        {
            failureReason = $"Card {instanceId} was not found in hand.";
            return false;
        }

        if (cardInstance.Definition.Cost > CurrentEnergy)
        {
            failureReason = $"Not enough energy. Need {cardInstance.Definition.Cost}, have {CurrentEnergy}.";
            return false;
        }

        return true;
    }

    public bool CommitPlayedCard(string instanceId, out BattleCardInstance? playedCard, out string failureReason)
    {
        playedCard = null;
        if (!CanPlay(instanceId, out failureReason))
        {
            return false;
        }

        int handIndex = _hand.FindIndex(card => string.Equals(card.InstanceId, instanceId, StringComparison.Ordinal));
        if (handIndex < 0)
        {
            failureReason = $"Card {instanceId} was not found in hand.";
            return false;
        }

        playedCard = _hand[handIndex];
        _hand.RemoveAt(handIndex);
        if (playedCard.Definition.ExhaustsOnPlay)
        {
            _exhaustPile.Add(playedCard);
        }
        else
        {
            _discardPile.Add(playedCard);
        }

        CurrentEnergy = Math.Max(0, CurrentEnergy - playedCard.Definition.Cost);
        return true;
    }

    public void GainEnergy(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentEnergy = Math.Min(MaxEnergyPerTurn, CurrentEnergy + amount);
    }

    public void ModifyEnergyRegenInterval(int delta)
    {
        EnergyRegenIntervalTurns = Math.Max(1, EnergyRegenIntervalTurns + delta);
    }

    public void ModifyEnergyRegenAmount(int delta)
    {
        EnergyRegenAmount = Math.Max(1, EnergyRegenAmount + delta);
    }

    private void DrawUpToHandSize()
    {
        while (_hand.Count < HandSize && TryDrawOne())
        {
        }
    }

    private bool TryDrawOne()
    {
        EnsureDrawPile();
        if (_drawPile.Count == 0)
        {
            return false;
        }

        BattleCardInstance nextCard = _drawPile[0];
        _drawPile.RemoveAt(0);
        _hand.Add(nextCard);
        return true;
    }

    private void EnsureDrawPile()
    {
        if (_drawPile.Count > 0 || _discardPile.Count == 0)
        {
            return;
        }

        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);
    }

    private BattleCardInstance CreateInstance(BattleCardDefinition definition)
    {
        _instanceCounter++;
        return new BattleCardInstance($"{definition.CardId}_{_instanceCounter:D3}", definition);
    }

    private void AddInstances(List<BattleCardInstance> target, IEnumerable<BattleCardDefinition> definitions)
    {
        foreach (BattleCardDefinition definition in definitions)
        {
            target.Add(CreateInstance(definition));
        }
    }

    private void Shuffle(List<BattleCardInstance> cards)
    {
        for (int index = cards.Count - 1; index > 0; index--)
        {
            int swapIndex = _rng.RandiRange(0, index);
            (cards[index], cards[swapIndex]) = (cards[swapIndex], cards[index]);
        }
    }
}
