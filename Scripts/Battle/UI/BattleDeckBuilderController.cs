using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CardChessDemo.Battle.Boundary;
using CardChessDemo.Battle.Cards;
using CardChessDemo.Battle.Shared;

namespace CardChessDemo.Battle.UI;

public partial class BattleDeckBuilderController : Control
{
	[Export] public BattleCardLibrary? BattleCardLibrary { get; set; }
	[Export] public BattleDeckBuildRules? BattleDeckBuildRules { get; set; }

	private GlobalGameSession? _session;
	private BattleDeckConstructionService? _constructionService;
	private ItemList _availableList = null!;
	private ItemList _deckList = null!;
	private Label _poolSummaryLabel = null!;
	private Label _deckSummaryLabel = null!;
	private RichTextLabel _detailLabel = null!;
	private RichTextLabel _validationLabel = null!;
	private Button _addButton = null!;
	private Button _removeButton = null!;
	private Button _saveButton = null!;
	private Button _resetButton = null!;
	private Button _starterButton = null!;

	private BattleCardTemplate[] _availableTemplates = Array.Empty<BattleCardTemplate>();
	private List<string> _workingDeck = new();

	public override void _Ready()
	{
		_session = GetNodeOrNull<GlobalGameSession>("/root/GlobalGameSession");
		BattleCardLibrary ??= GD.Load<BattleCardLibrary>("res://Resources/Battle/Cards/DefaultBattleCardLibrary.tres");
		BattleDeckBuildRules ??= GD.Load<BattleDeckBuildRules>("res://Resources/Battle/Cards/DefaultBattleDeckBuildRules.tres");

		if (_session == null || BattleCardLibrary == null || BattleDeckBuildRules == null)
		{
			GD.PushError("BattleDeckBuilderController: required session or resources are missing.");
			return;
		}

		_session.EnsureDeckBuildInitialized(BattleCardLibrary);
		_constructionService = new BattleDeckConstructionService(BattleCardLibrary, BattleDeckBuildRules);

		_availableList = GetNode<ItemList>("Margin/Root/Columns/AvailableColumn/AvailableList");
		_deckList = GetNode<ItemList>("Margin/Root/Columns/DeckColumn/DeckList");
		_poolSummaryLabel = GetNode<Label>("Margin/Root/Columns/AvailableColumn/PoolSummary");
		_deckSummaryLabel = GetNode<Label>("Margin/Root/Columns/DeckColumn/DeckSummary");
		_detailLabel = GetNode<RichTextLabel>("Margin/Root/DetailPanel/DetailText");
		_validationLabel = GetNode<RichTextLabel>("Margin/Root/ValidationPanel/ValidationText");
		_addButton = GetNode<Button>("Margin/Root/Columns/ControlColumn/AddButton");
		_removeButton = GetNode<Button>("Margin/Root/Columns/ControlColumn/RemoveButton");
		_saveButton = GetNode<Button>("Margin/Root/Footer/SaveButton");
		_resetButton = GetNode<Button>("Margin/Root/Footer/ResetButton");
		_starterButton = GetNode<Button>("Margin/Root/Footer/StarterButton");

		_availableList.ItemSelected += OnAvailableSelected;
		_deckList.ItemSelected += OnDeckSelected;
		_addButton.Pressed += OnAddPressed;
		_removeButton.Pressed += OnRemovePressed;
		_saveButton.Pressed += OnSavePressed;
		_resetButton.Pressed += OnResetPressed;
		_starterButton.Pressed += OnStarterPressed;

		LoadWorkingDeckFromSession();
		RefreshAll();
	}

	private void LoadWorkingDeckFromSession()
	{
		if (_session == null)
		{
			return;
		}

		_workingDeck = _session.DeckBuildState.CardIds.ToList();
	}

	public void RefreshFromExternalState()
	{
		LoadWorkingDeckFromSession();
		RefreshAll();
	}

	private void RefreshAll()
	{
		if (_session == null || _constructionService == null || BattleCardLibrary == null || BattleDeckBuildRules == null)
		{
			return;
		}

		ProgressionSnapshot progression = _session.BuildProgressionSnapshotModel();
		_availableTemplates = _constructionService.GetAvailableCardPool(progression).ToArray();

		_availableList.Clear();
		foreach (BattleCardTemplate template in _availableTemplates)
		{
			bool isOverlimitCandidate = !template.CanCarryNormally(progression) && template.CanCarryOverlimit(progression);
			string learnedLabel = template.IsLearnedCard ? "  [学习]" : string.Empty;
			string overlimitLabel = isOverlimitCandidate ? "  [超规]" : string.Empty;
			_availableList.AddItem($"{template.DisplayName}{learnedLabel}{overlimitLabel}  C{template.Cost}  I{template.BuildPoints}");
		}

		_deckList.Clear();
		foreach (string cardId in _workingDeck)
		{
			BattleCardTemplate? template = BattleCardLibrary.FindTemplate(cardId);
			if (template == null)
			{
				_deckList.AddItem(cardId);
				continue;
			}

			bool usesOverlimitCarry = !template.CanCarryNormally(progression) && template.CanCarryOverlimit(progression);
			_deckList.AddItem(usesOverlimitCarry ? $"{template.DisplayName} [超规]" : template.DisplayName);
		}

		_poolSummaryLabel.Text = $"可选牌库 {_availableTemplates.Length} 张";

		DeckBuildSnapshot snapshot = new()
		{
			BuildName = _session.DeckBuildState.BuildName,
			CardIds = _workingDeck.ToArray(),
			RelicIds = _session.DeckBuildState.RelicIds,
		};
		BattleDeckValidationResult validation = _constructionService.ValidateDeck(snapshot, progression);
		_deckSummaryLabel.Text = $"当前牌组 {validation.TotalCardCount} 张 / 影响 {validation.TotalBuildPoints} / 超规 {validation.UsedOverlimitCarrySlots}";
		_validationLabel.Text = BuildValidationText(validation);
		_saveButton.Disabled = !validation.IsValid;

		if (_availableTemplates.Length > 0 && _availableList.ItemCount > 0)
		{
			_availableList.Select(Mathf.Clamp(_availableList.GetSelectedItems().FirstOrDefault(), 0, _availableList.ItemCount - 1));
		}
	}

	private static string BuildValidationText(BattleDeckValidationResult validation)
	{
		List<string> lines = new()
		{
			$"最低卡数: {validation.TotalCardCount}/{validation.EffectiveMinDeckSize}",
			$"影响因子: {validation.TotalBuildPoints}/{validation.EffectivePointBudget}",
			$"同名上限: {validation.EffectiveMaxCopiesPerCard}",
			$"超规槽位: {validation.UsedOverlimitCarrySlots}/{validation.EffectiveOverlimitCarrySlots}",
			$"循环限制: cycle {validation.EffectiveCycleCardLimit} / quick_cycle {validation.EffectiveQuickCycleCardLimit} / energy_positive {validation.EffectiveEnergyPositiveCardLimit}",
		};

		if (validation.Errors.Count == 0)
		{
			lines.Add("状态: 通过");
		}
		else
		{
			lines.Add("状态: 不通过");
			lines.AddRange(validation.Errors.Select(error => $"- {error}"));
		}

		if (validation.Warnings.Count > 0)
		{
			lines.Add("提示:");
			lines.AddRange(validation.Warnings.Select(warning => $"- {warning}"));
		}

		return string.Join('\n', lines);
	}

	private void OnAvailableSelected(long index)
	{
		if (index < 0 || index >= _availableTemplates.Length)
		{
			return;
		}

		BattleCardTemplate template = _availableTemplates[index];
		_detailLabel.Text = BuildTemplateDetailText(template);
	}

	private void OnDeckSelected(long index)
	{
		if (index < 0 || index >= _workingDeck.Count || BattleCardLibrary == null)
		{
			return;
		}

		BattleCardTemplate? template = BattleCardLibrary.FindTemplate(_workingDeck[(int)index]);
		_detailLabel.Text = template != null ? BuildTemplateDetailText(template) : _workingDeck[(int)index];
	}

	private static string BuildTemplateDetailText(BattleCardTemplate template)
	{
		return string.Join('\n', new[]
		{
			$"[b]{template.DisplayName}[/b] ({template.CardId})",
			template.Description,
			$"费用 {template.Cost} / 范围 {template.Range} / 影响因子 {template.BuildPoints}",
			$"伤害 {template.Damage} / 治疗 {template.HealingAmount} / 抽牌 {template.DrawCount} / 回能 {template.EnergyGain} / 护盾 {template.ShieldGain}",
			$"目标 {template.TargetingMode} / Quick {template.IsQuick} / Exhaust {template.ExhaustsOnPlay}",
			$"学习牌 {template.IsLearnedCard} / 可超规 {(!template.DisallowOverlimitCarry)} / 同名上限 {template.MaxCopiesInDeck}",
			$"循环标签: {(template.CycleTags.Length == 0 ? "无" : string.Join(", ", template.CycleTags))}",
		});
	}

	private void OnAddPressed()
	{
		if (_constructionService == null || _session == null || BattleCardLibrary == null)
		{
			return;
		}

		int[] selected = _availableList.GetSelectedItems();
		if (selected.Length == 0)
		{
			return;
		}

		BattleCardTemplate template = _availableTemplates[selected[0]];
		List<string> candidateDeck = new(_workingDeck) { template.CardId };
		BattleDeckValidationResult validation = _constructionService.ValidateDeck(
			new DeckBuildSnapshot { CardIds = candidateDeck.ToArray(), RelicIds = _session.DeckBuildState.RelicIds },
			_session.BuildProgressionSnapshotModel());
		if (!validation.IsValid)
		{
			_validationLabel.Text = BuildValidationText(validation);
			return;
		}

		_workingDeck = candidateDeck;
		RefreshAll();
	}

	private void OnRemovePressed()
	{
		int[] selected = _deckList.GetSelectedItems();
		if (selected.Length == 0)
		{
			return;
		}

		_workingDeck.RemoveAt(selected[0]);
		RefreshAll();
	}

	private void OnSavePressed()
	{
		if (_session == null || _constructionService == null)
		{
			return;
		}

		DeckBuildSnapshot snapshot = new()
		{
			BuildName = _session.DeckBuildState.BuildName,
			CardIds = _workingDeck.ToArray(),
			RelicIds = _session.DeckBuildState.RelicIds,
		};
		BattleDeckValidationResult validation = _constructionService.ValidateDeck(snapshot, _session.BuildProgressionSnapshotModel());
		if (!validation.IsValid)
		{
			_validationLabel.Text = BuildValidationText(validation);
			return;
		}

		_session.ApplyDeckBuildSnapshot(snapshot.ToDictionary());
		_validationLabel.Text = BuildValidationText(validation) + "\n已保存到 GlobalGameSession";
	}

	private void OnResetPressed()
	{
		LoadWorkingDeckFromSession();
		RefreshAll();
	}

	private void OnStarterPressed()
	{
		if (BattleCardLibrary == null)
		{
			return;
		}

		_workingDeck = BattleCardLibrary.BuildStarterDeckCardIds().ToList();
		RefreshAll();
	}
}
