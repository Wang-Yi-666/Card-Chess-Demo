using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CardChessDemo.Battle.Boundary;
using CardChessDemo.Battle.Cards;
using CardChessDemo.Battle.Shared;

namespace CardChessDemo.Map;

public partial class SystemFeatureLabController : CanvasLayer
{
	[Export] public NodePath PlayerPath { get; set; } = new("../Player");

	private Control _panelRoot = null!;
	private Label _hintLabel = null!;
	private Label _statusLabel = null!;
	private TabContainer _tabs = null!;

	private RichTextLabel _statusOverviewText = null!;
	private ItemList _statusEquipmentSlotList = null!;
	private ItemList _statusEquipmentCandidateList = null!;
	private RichTextLabel _statusEquipmentDetailText = null!;
	private Button _statusEquipButton = null!;
	private Button _statusUnequipButton = null!;

	private RichTextLabel _inventoryText = null!;

	private Label _masteryLabel = null!;
	private Label _combatSummaryLabel = null!;
	private ScrollContainer _talentTreeScroll = null!;
	private Control _talentTreeCanvas = null!;
	private Label _cardTreeLabel = null!;
	private Label _roleTreeLabel = null!;
	private PanelContainer _talentDetailPanel = null!;
	private Label _talentDetailTitleLabel = null!;
	private RichTextLabel _talentDetail = null!;
	private Button _unlockTalentButton = null!;
	private Button _refundTalentButton = null!;
	private Button _grantPointButton = null!;
	private Button _revokePointButton = null!;
	private Button _resetTalentButton = null!;
	private readonly Dictionary<string, Button> _talentButtons = new(StringComparer.Ordinal);
	private readonly HashSet<string> _purchasedTalentIds = new(StringComparer.Ordinal);
	private string _selectedTalentId = string.Empty;
	private bool _isDraggingTalentTree;
	private Vector2 _lastTalentDragPosition = Vector2.Zero;
	private float _talentTreeZoom = 1.0f;

	private ItemList _cardCodexList = null!;
	private RichTextLabel _cardCodexDetail = null!;
	private ItemList _enemyCodexList = null!;
	private RichTextLabel _enemyCodexDetail = null!;

	private Label _deckPoolSummaryLabel = null!;
	private Label _deckSummaryLabel = null!;
	private ItemList _availableList = null!;
	private ItemList _deckList = null!;
	private RichTextLabel _deckDetailText = null!;
	private RichTextLabel _deckValidationText = null!;
	private Button _deckAddButton = null!;
	private Button _deckRemoveButton = null!;
	private Button _deckSaveButton = null!;
	private Button _deckResetButton = null!;
	private Button _deckStarterButton = null!;

	private Button _seedInventoryButton = null!;
	private Button _clearInventoryButton = null!;

	private GlobalGameSession? _session;
	private BattleCardLibrary? _cardLibrary;
	private BattleDeckBuildRules? _deckRules;
	private BattleDeckConstructionService? _constructionService;
	private BattleCardTemplate[] _availableTemplates = Array.Empty<BattleCardTemplate>();
	private BattleCardTemplate[] _codexTemplates = Array.Empty<BattleCardTemplate>();
	private EquipmentDefinition[] _visibleEquipmentCandidates = Array.Empty<EquipmentDefinition>();
	private List<string> _workingDeck = new();
	private int _baseMasteryPoints = 6;
	private string _selectedEquipmentSlotId = "weapon";

	private static readonly string[] EquipmentSlotOrder = { "weapon", "armor", "accessory" };

	private readonly EquipmentDefinition[] _equipmentDefinitions =
	{
		new("rusted_blade", "旧钢刀", "weapon", "拆船废料打磨成的近战武器，稳定提升攻击。", "攻击 +1"),
		new("ion_pistol", "脉冲短铳", "weapon", "便携式脉冲副武器，输出更高但更依赖供能。", "攻击 +2"),
		new("patched_coat", "补丁风衣", "armor", "缝补过的旧大衣，提供更扎实的生存空间。", "生命 +4 / 减伤 +5%"),
		new("reactive_plate", "反应护甲片", "armor", "临时拼装的护甲片，强化防御姿态。", "减伤 +10% / 防御附盾 +1"),
		new("signal_charm", "信号挂饰", "accessory", "轻量化挂饰，改善战场移动调度。", "移动 +1"),
		new("tactical_chip", "战术芯片", "accessory", "旧时代战术分析模组，补强防御判断。", "减伤 +5%"),
	};

	private readonly TalentNode[] _talents =
	{
		new("lab.branch.ranged", "远程基础", 1, "获得远程分支资格，解锁重铳与钩射。", new Vector2(72, 58), Array.Empty<string>(), new[] { "lab.branch.ranged" }, new[] { "ranged" }, new[] { "heavy_shot", "hook_shot" }),
		new("lab.branch.melee", "近战基础", 1, "获得近战分支资格，解锁燃刃。", new Vector2(42, 166), Array.Empty<string>(), new[] { "lab.branch.melee" }, new[] { "melee" }, new[] { "burning_edge" }),
		new("lab.branch.flex", "灵活基础", 1, "获得灵活分支资格，解锁沉念。", new Vector2(72, 276), Array.Empty<string>(), new[] { "lab.branch.flex" }, new[] { "flex" }, new[] { "deep_focus" }),
		new("lab.deck.budget", "卡牌扩容", 1, "构筑预算 +2，并解锁蓄能。", new Vector2(332, 92), new[] { "lab.branch.flex" }, new[] { "lab.deck.budget" }, unlockedCardIds: new[] { "surge" }, deckPointBudgetBonus: 2),
		new("lab.deck.copies", "卡牌熟练", 1, "同名上限 +1，并解锁快谋。", new Vector2(332, 242), new[] { "lab.branch.flex" }, new[] { "lab.deck.copies" }, unlockedCardIds: new[] { "quick_plan" }, deckMaxCopiesPerCardBonus: 1),
		new("lab.attack.1", "攻击强化 I", 1, "主角普通攻击 +1。", new Vector2(466, 62), new[] { "lab.branch.melee" }, new[] { "lab.attack.1", "stat.attack_bonus.1" }),
		new("lab.defense.10", "防御校准", 1, "防御减伤 +10%，并解锁架势。", new Vector2(436, 202), new[] { "lab.branch.melee" }, new[] { "lab.defense.10", "stat.defense_reduction_bonus.10" }, unlockedCardIds: new[] { "brace" }),
		new("lab.defense.shield", "防御附盾", 1, "防御动作额外获得 2 护盾。", new Vector2(654, 202), new[] { "lab.defense.10" }, new[] { "lab.defense.shield", "stat.defense_shield_bonus.2" }),
	};

	private readonly EnemyCodexEntry[] _enemyCodexEntries =
	{
		new("grunt_debug", "训练敌人", "基础近战测试敌人。用于验证地图进入战斗与基础回合交互。", true, "初始开放"),
		new("pirate_brute_elite", "搭船客重压者", "擅长贴身压迫与重击，学习其能力可获得高压突进。", false, "在精英战中完成学习后解锁图鉴", "card_pressure_breach"),
		new("alliance_hunter_elite", "联盟猎手标定兵", "擅长标记与远程收割，学习后可获得猎手标定。", false, "在精英战中完成学习后解锁图鉴", "card_hunter_mark"),
		new("boss_port_authority", "空港治安主机", "Boss 级远程压制单位，拥有过载光束。", false, "击败章节 Boss 并完成学习后解锁图鉴", "card_overclock_beam"),
	};

	public override void _Ready()
	{
		_session = GetNodeOrNull<GlobalGameSession>("/root/GlobalGameSession");
		_cardLibrary = GD.Load<BattleCardLibrary>("res://Resources/Battle/Cards/DefaultBattleCardLibrary.tres");
		_deckRules = GD.Load<BattleDeckBuildRules>("res://Resources/Battle/Cards/DefaultBattleDeckBuildRules.tres");
		if (_session != null && _cardLibrary != null && _deckRules != null)
		{
			_session.EnsureDeckBuildInitialized(_cardLibrary);
			_constructionService = new BattleDeckConstructionService(_cardLibrary, _deckRules);
		}

		_panelRoot = GetNode<Control>("PanelRoot");
		_hintLabel = GetNode<Label>("HintLabel");
		_statusLabel = GetNode<Label>("StatusLabel");
		_tabs = GetNode<TabContainer>("PanelRoot/Window/Margin/Root/Tabs");
		_statusOverviewText = GetNode<RichTextLabel>("PanelRoot/Window/Margin/Root/Tabs/StatusTab/Columns/StatusColumn/StatusText");
		_statusEquipmentSlotList = GetNode<ItemList>("PanelRoot/Window/Margin/Root/Tabs/StatusTab/Columns/EquipmentColumn/SlotList");
		_statusEquipmentCandidateList = GetNode<ItemList>("PanelRoot/Window/Margin/Root/Tabs/StatusTab/Columns/EquipmentColumn/CandidateList");
		_statusEquipmentDetailText = GetNode<RichTextLabel>("PanelRoot/Window/Margin/Root/Tabs/StatusTab/Columns/EquipmentColumn/EquipmentDetailPanel/EquipmentDetailText");
		_statusEquipButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/StatusTab/Columns/EquipmentColumn/ActionRow/EquipButton");
		_statusUnequipButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/StatusTab/Columns/EquipmentColumn/ActionRow/UnequipButton");
		_inventoryText = GetNode<RichTextLabel>("PanelRoot/Window/Margin/Root/Tabs/InventoryTab/InventoryText");
		_masteryLabel = GetNode<Label>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Header/MasteryLabel");
		_combatSummaryLabel = GetNode<Label>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Header/CombatSummaryLabel");
		_talentTreeScroll = GetNode<ScrollContainer>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Body/TalentTreeScroll");
		_talentTreeCanvas = GetNode<Control>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Body/TalentTreeScroll/TalentTreeCanvas");
		_cardTreeLabel = GetNode<Label>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Body/TalentTreeScroll/TalentTreeCanvas/CardTreeLabel");
		_roleTreeLabel = GetNode<Label>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Body/TalentTreeScroll/TalentTreeCanvas/RoleTreeLabel");
		_talentDetailPanel = GetNode<PanelContainer>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Body/DetailPanel");
		_talentDetailTitleLabel = GetNode<Label>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Body/DetailPanel/Margin/Content/TitleLabel");
		_talentDetail = GetNode<RichTextLabel>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Body/DetailPanel/Margin/Content/DetailText");
		_unlockTalentButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Body/DetailPanel/Margin/Content/Footer/UnlockTalentButton");
		_refundTalentButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Body/DetailPanel/Margin/Content/Footer/RefundTalentButton");
		_grantPointButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Footer/GrantPointButton");
		_revokePointButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Footer/RevokePointButton");
		_resetTalentButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/TalentTab/Footer/ResetTalentButton");
		_cardCodexList = GetNode<ItemList>("PanelRoot/Window/Margin/Root/Tabs/CodexTab/CodexTabs/CardCodex/Columns/ListColumn/CardList");
		_cardCodexDetail = GetNode<RichTextLabel>("PanelRoot/Window/Margin/Root/Tabs/CodexTab/CodexTabs/CardCodex/Columns/DetailPanel/DetailText");
		_enemyCodexList = GetNode<ItemList>("PanelRoot/Window/Margin/Root/Tabs/CodexTab/CodexTabs/EnemyCodex/Columns/ListColumn/EnemyList");
		_enemyCodexDetail = GetNode<RichTextLabel>("PanelRoot/Window/Margin/Root/Tabs/CodexTab/CodexTabs/EnemyCodex/Columns/DetailPanel/DetailText");
		_deckPoolSummaryLabel = GetNode<Label>("PanelRoot/Window/Margin/Root/Tabs/DeckTab/Header/PoolSummaryLabel");
		_deckSummaryLabel = GetNode<Label>("PanelRoot/Window/Margin/Root/Tabs/DeckTab/Header/DeckSummaryLabel");
		_availableList = GetNode<ItemList>("PanelRoot/Window/Margin/Root/Tabs/DeckTab/Columns/AvailableColumn/AvailableList");
		_deckList = GetNode<ItemList>("PanelRoot/Window/Margin/Root/Tabs/DeckTab/Columns/DeckColumn/DeckList");
		_deckDetailText = GetNode<RichTextLabel>("PanelRoot/Window/Margin/Root/Tabs/DeckTab/DetailPanel/DetailText");
		_deckValidationText = GetNode<RichTextLabel>("PanelRoot/Window/Margin/Root/Tabs/DeckTab/ValidationPanel/ValidationText");
		_deckAddButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/DeckTab/Columns/ControlColumn/AddButton");
		_deckRemoveButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/DeckTab/Columns/ControlColumn/RemoveButton");
		_deckSaveButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/DeckTab/Footer/SaveButton");
		_deckResetButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/DeckTab/Footer/ResetButton");
		_deckStarterButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/DeckTab/Footer/StarterButton");
		_seedInventoryButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/InventoryTab/Footer/SeedInventoryButton");
		_clearInventoryButton = GetNode<Button>("PanelRoot/Window/Margin/Root/Tabs/InventoryTab/Footer/ClearInventoryButton");

		_tabs.SetTabTitle(0, "背包");
		_tabs.SetTabTitle(1, "天赋");
		_tabs.SetTabTitle(2, "图鉴");
		_tabs.SetTabTitle(3, "构筑");
		_tabs.SetTabTitle(0, "角色");
		_tabs.SetTabTitle(1, "背包");
		_tabs.SetTabTitle(2, "天赋");
		_tabs.SetTabTitle(3, "图鉴");
		_tabs.SetTabTitle(4, "构筑");
		_panelRoot.Visible = false;
		_talentDetailPanel.Visible = false;

		_statusEquipmentSlotList.ItemSelected += OnEquipmentSlotSelected;
		_statusEquipmentCandidateList.ItemSelected += OnEquipmentCandidateSelected;
		_statusEquipButton.Pressed += OnEquipButtonPressed;
		_statusUnequipButton.Pressed += OnUnequipButtonPressed;
		_grantPointButton.Pressed += OnGrantPointPressed;
		_revokePointButton.Pressed += OnRevokePointPressed;
		_resetTalentButton.Pressed += OnResetTalentsPressed;
		_unlockTalentButton.Pressed += OnUnlockTalentPressed;
		_refundTalentButton.Pressed += OnRefundTalentPressed;
		_seedInventoryButton.Pressed += OnSeedInventoryPressed;
		_clearInventoryButton.Pressed += OnClearInventoryPressed;
		_availableList.ItemSelected += OnAvailableSelected;
		_deckList.ItemSelected += OnDeckSelected;
		_cardCodexList.ItemSelected += OnCardCodexSelected;
		_enemyCodexList.ItemSelected += OnEnemyCodexSelected;
		_deckAddButton.Pressed += OnDeckAddPressed;
		_deckRemoveButton.Pressed += OnDeckRemovePressed;
		_deckSaveButton.Pressed += OnDeckSavePressed;
		_deckResetButton.Pressed += OnDeckResetPressed;
		_deckStarterButton.Pressed += OnDeckStarterPressed;
		_talentTreeCanvas.GuiInput += OnTalentTreeGuiInput;
		_cardTreeLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		_roleTreeLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		HideTalentTreeScrollBars();

		BuildTalentButtons();
		BuildCodexSource();
		SeedSessionForTesting();
		LoadPurchasedTalentsFromSession();
		RecomputeSessionProgression();
		LoadWorkingDeckFromSession();
		RefreshAll();
	}

	public override void _Process(double delta)
	{
		UpdateStatusHint();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo || keyEvent.Keycode != Key.C)
		{
			return;
		}

		_panelRoot.Visible = !_panelRoot.Visible;
		_hintLabel.Visible = !_panelRoot.Visible;
		SetPlayerInputEnabled(!_panelRoot.Visible);
		if (_panelRoot.Visible)
		{
			RefreshAll();
		}

		GetViewport().SetInputAsHandled();
	}

	private void SetPlayerInputEnabled(bool enabled)
	{
		Node? player = PlayerPath.IsEmpty ? null : GetNodeOrNull(PlayerPath);
		if (player == null)
		{
			return;
		}

		player.SetPhysicsProcess(enabled);
		player.SetProcess(enabled);
		player.SetProcessInput(enabled);
		player.SetProcessUnhandledInput(enabled);
	}

	private void UpdateStatusHint()
	{
		Node? playerNode = PlayerPath.IsEmpty ? null : GetNodeOrNull(PlayerPath);
		Area2D? interactionArea = playerNode?.GetNodeOrNull<Area2D>("InteractionArea");
		if (interactionArea == null)
		{
			_statusLabel.Text = "未找到玩家交互范围";
			return;
		}

		foreach (Area2D area in interactionArea.GetOverlappingAreas())
		{
			if (area.GetParent() is IInteractable interactable && area.GetParent() is Node ownerNode)
			{
				Player? player = playerNode as Player ?? GetNodeOrNull<Player>(PlayerPath);
				_statusLabel.Text = $"可交互对象: {ownerNode.Name} · {interactable.GetInteractText(player!)}";
				return;
			}
		}

		_statusLabel.Text = _panelRoot.Visible ? "系统面板已打开，按 C 关闭" : "就近靠近训练敌人后按 E 进入固定 roomB";
	}

	private void OnTalentTreeGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton wheelEvent && wheelEvent.Pressed)
		{
			if (wheelEvent.ButtonIndex == MouseButton.WheelUp)
			{
				ApplyTalentTreeZoom(_talentTreeZoom + 0.1f, wheelEvent.Position);
				GetViewport().SetInputAsHandled();
				return;
			}

			if (wheelEvent.ButtonIndex == MouseButton.WheelDown)
			{
				ApplyTalentTreeZoom(_talentTreeZoom - 0.1f, wheelEvent.Position);
				GetViewport().SetInputAsHandled();
				return;
			}
		}

		if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
		{
			_isDraggingTalentTree = mouseButton.Pressed;
			_lastTalentDragPosition = mouseButton.Position;
			return;
		}

		if (!_isDraggingTalentTree || @event is not InputEventMouseMotion mouseMotion)
		{
			return;
		}

		Vector2 delta = mouseMotion.Position - _lastTalentDragPosition;
		_talentTreeScroll.ScrollHorizontal = Math.Max(0, _talentTreeScroll.ScrollHorizontal - Mathf.RoundToInt(delta.X));
		_talentTreeScroll.ScrollVertical = Math.Max(0, _talentTreeScroll.ScrollVertical - Mathf.RoundToInt(delta.Y));
		_lastTalentDragPosition = mouseMotion.Position;
		GetViewport().SetInputAsHandled();
	}

	private void HideTalentTreeScrollBars()
	{
		ScrollBar? horizontal = _talentTreeScroll.GetHScrollBar();
		if (horizontal != null)
		{
			horizontal.Visible = false;
			horizontal.Modulate = new Color(1, 1, 1, 0);
			horizontal.MouseFilter = Control.MouseFilterEnum.Ignore;
			horizontal.CustomMinimumSize = Vector2.Zero;
		}

		ScrollBar? vertical = _talentTreeScroll.GetVScrollBar();
		if (vertical != null)
		{
			vertical.Visible = false;
			vertical.Modulate = new Color(1, 1, 1, 0);
			vertical.MouseFilter = Control.MouseFilterEnum.Ignore;
			vertical.CustomMinimumSize = Vector2.Zero;
		}
	}

	private void ApplyTalentTreeZoom(float targetZoom, Vector2 mousePosition)
	{
		float clampedZoom = Mathf.Clamp(targetZoom, 0.75f, 1.6f);
		if (Mathf.IsEqualApprox(clampedZoom, _talentTreeZoom))
		{
			return;
		}

		float oldZoom = _talentTreeZoom;
		Vector2 logicalMouse = new(
			(_talentTreeScroll.ScrollHorizontal + mousePosition.X) / oldZoom,
			(_talentTreeScroll.ScrollVertical + mousePosition.Y) / oldZoom);

		_talentTreeZoom = clampedZoom;
		_talentTreeCanvas.Scale = new Vector2(_talentTreeZoom, _talentTreeZoom);

		_talentTreeScroll.ScrollHorizontal = Math.Max(0, Mathf.RoundToInt(logicalMouse.X * _talentTreeZoom - mousePosition.X));
		_talentTreeScroll.ScrollVertical = Math.Max(0, Mathf.RoundToInt(logicalMouse.Y * _talentTreeZoom - mousePosition.Y));
	}

	private void BuildTalentButtons()
	{
		foreach (TalentNode talent in _talents)
		{
			Button button = new()
			{
				CustomMinimumSize = new Vector2(104, 42),
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
			};

			string talentId = talent.Id;
			button.Pressed += () => OnTalentPressed(talentId);
			button.Position = talent.TreePosition;
			_talentTreeCanvas.AddChild(button);
			_talentButtons[talentId] = button;
		}

		RefreshTalentTreeLines();
	}

	private void RefreshTalentTreeLines()
	{
		foreach (Node child in _talentTreeCanvas.GetChildren().Where(node => node.Name.ToString().StartsWith("TreeLine_", StringComparison.Ordinal)).ToArray())
		{
			child.QueueFree();
		}

		Vector2 cardRootCenter = GetControlCenter(_cardTreeLabel);
		Vector2 roleRootCenter = GetControlCenter(_roleTreeLabel);

		foreach (TalentNode talent in _talents)
		{
			if (!_talentButtons.TryGetValue(talent.Id, out Button? targetButton))
			{
				continue;
			}

			Vector2 targetCenter = GetControlCenter(targetButton);
			if (talent.PrerequisiteTalentIds.Length == 0)
			{
				Vector2 rootCenter = talent.TreePosition.X < 400.0f ? cardRootCenter : roleRootCenter;
				AddTreeLine(rootCenter, targetCenter, new Color(0.58f, 0.82f, 1.0f, 0.96f));
				continue;
			}

			foreach (string prerequisiteId in talent.PrerequisiteTalentIds)
			{
				if (!_talentButtons.TryGetValue(prerequisiteId, out Button? prerequisiteButton))
				{
					continue;
				}

				Color lineColor = _purchasedTalentIds.Contains(prerequisiteId)
					? new Color(1.0f, 0.84f, 0.38f, 1.0f)
					: new Color(0.45f, 0.54f, 0.68f, 0.92f);
				AddTreeLine(GetControlCenter(prerequisiteButton), targetCenter, lineColor);
			}
		}
	}

	private void AddTreeLine(Vector2 from, Vector2 to, Color color)
	{
		float cornerX = from.X + (to.X - from.X) * 0.45f;
		Vector2 cornerA = new(cornerX, from.Y);
		Vector2 cornerB = new(cornerX, to.Y);
		AddTreeSegment(from, cornerA, color);
		AddTreeSegment(cornerA, cornerB, color);
		AddTreeSegment(cornerB, to, color);
	}

	private void AddTreeSegment(Vector2 from, Vector2 to, Color color)
	{
		ColorRect segment = new()
		{
			Name = $"TreeLine_{Guid.NewGuid():N}",
			Color = color,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		Vector2 min = new(Mathf.Min(from.X, to.X), Mathf.Min(from.Y, to.Y));
		Vector2 max = new(Mathf.Max(from.X, to.X), Mathf.Max(from.Y, to.Y));
		segment.Position = min;
		segment.Size = new Vector2(Mathf.Max(3.0f, max.X - min.X + 3.0f), Mathf.Max(3.0f, max.Y - min.Y + 3.0f));
		_talentTreeCanvas.AddChild(segment);
		_talentTreeCanvas.MoveChild(segment, 0);
	}

	private static Vector2 GetControlCenter(Control control)
	{
		return control.Position + control.Size * 0.5f;
	}

	private void BuildCodexSource()
	{
		_codexTemplates = _cardLibrary?.Entries
			.Where(entry => entry != null)
			.OrderBy(entry => entry.CardId, StringComparer.Ordinal)
			.ToArray()
			?? Array.Empty<BattleCardTemplate>();
	}

	private void SeedSessionForTesting()
	{
		if (_session == null)
		{
			return;
		}

		_session.ProgressionState.PlayerLevel = Math.Max(3, _session.ProgressionState.PlayerLevel);
		_session.PlayerLevel = _session.ProgressionState.PlayerLevel;
		_session.PlayerAttackDamage = Math.Max(2, _session.PlayerAttackDamage);
		_session.PlayerDefenseDamageReductionPercent = Math.Max(50, _session.PlayerDefenseDamageReductionPercent);
		if (_session.InventoryState.ItemCounts.Count == 0)
		{
			SeedInventoryDefaults();
		}
	}

	private void LoadPurchasedTalentsFromSession()
	{
		if (_session == null)
		{
			return;
		}

		_purchasedTalentIds.Clear();
		foreach (TalentNode talent in _talents)
		{
			if (_session.ProgressionState.TalentIds.Contains(talent.Id, StringComparer.Ordinal))
			{
				_purchasedTalentIds.Add(talent.Id);
			}
		}

		_baseMasteryPoints = Math.Max(6, _session.ProgressionState.PlayerMasteryPoints + GetSpentPoints());
	}

	private void LoadWorkingDeckFromSession()
	{
		if (_session == null)
		{
			return;
		}

		_workingDeck = _session.DeckBuildState.CardIds.ToList();
	}

	private void RefreshAll()
	{
		RefreshStatusView();
		RefreshBagView();
		RefreshTalentSummary();
		RefreshTalentButtons();
		RefreshTalentDetail();
		RefreshCodexView();
		RefreshDeckView();
	}

	private void RefreshStatusView()
	{
		if (_session == null)
		{
			_statusOverviewText.Text = "未找到 GlobalGameSession";
			_statusEquipmentDetailText.Text = "无法读取装备信息。";
			return;
		}

		List<string> lines = new()
		{
			$"[b]{_session.PlayerDisplayName}[/b]",
			$"等级: Lv.{_session.PlayerLevel}",
			$"经验: {_session.GetExperienceProgressWithinLevel()}/{_session.GetExperienceRequiredForNextLevel()}",
			$"距下一级: {_session.GetExperienceNeededToLevelUp()}",
			$"专精点: {_session.ProgressionState.PlayerMasteryPoints}",
			string.Empty,
			$"生命: {_session.PlayerCurrentHp}/{_session.GetResolvedPlayerMaxHp()}",
			$"移动: {_session.GetResolvedPlayerMovePointsPerTurn()}",
			$"攻击: {_session.GetResolvedPlayerAttackDamage()}",
			$"攻击范围: {_session.PlayerAttackRange}",
			$"防御减伤: {_session.GetResolvedPlayerDefenseDamageReductionPercent()}%",
			$"防御附盾: {_session.GetResolvedPlayerDefenseShieldGain()}",
			string.Empty,
			$"荒川成长等级: {_session.ArakawaGrowthLevel}",
			$"荒川能量: {_session.ArakawaCurrentEnergy}/{_session.ArakawaMaxEnergy}",
			string.Empty,
			$"已解锁天赋: {_session.ProgressionState.TalentIds.Length}",
			$"已解锁卡牌: {_session.ProgressionState.UnlockedCardIds.Length}",
			$"构筑预算加成: +{_session.ProgressionState.DeckPointBudgetBonus}",
		};

		_statusOverviewText.Text = string.Join('\n', lines);
		RefreshEquipmentSection();
	}

	private void RefreshBagView()
	{
		if (_session == null)
		{
			_inventoryText.Text = "未找到 GlobalGameSession";
			return;
		}

		List<string> lines = new()
		{
			"[b]背包物品[/b]",
			string.Empty,
		};

		if (_session.InventoryState.ItemCounts.Count == 0)
		{
			lines.Add("- （空）");
		}
		else
		{
			foreach (Variant key in _session.InventoryState.ItemCounts.Keys)
			{
				lines.Add($"- {key.AsString()} x{_session.InventoryState.ItemCounts[key].AsInt32()}");
			}
		}

		lines.Add(string.Empty);
		lines.Add("[b]额外解锁卡牌[/b]");
		lines.AddRange(_session.ProgressionState.UnlockedCardIds.Length == 0
			? new[] { "- （无）" }
			: _session.ProgressionState.UnlockedCardIds.OrderBy(value => value, StringComparer.Ordinal).Select(value => $"- {value}"));

		_inventoryText.Text = string.Join('\n', lines);
	}

	private void RefreshEquipmentSection()
	{
		if (_session == null)
		{
			return;
		}

		if (!EquipmentSlotOrder.Contains(_selectedEquipmentSlotId, StringComparer.Ordinal))
		{
			_selectedEquipmentSlotId = EquipmentSlotOrder[0];
		}

		_statusEquipmentSlotList.Clear();
		foreach (string slotId in EquipmentSlotOrder)
		{
			string equippedItemId = _session.GetEquippedItemId(slotId);
			_statusEquipmentSlotList.AddItem($"{GetEquipmentSlotDisplayName(slotId)}: {GetEquipmentDisplayName(equippedItemId)}");
		}

		int selectedSlotIndex = Math.Max(0, Array.IndexOf(EquipmentSlotOrder, _selectedEquipmentSlotId));
		if (_statusEquipmentSlotList.ItemCount > 0)
		{
			_statusEquipmentSlotList.Select(selectedSlotIndex);
		}

		_visibleEquipmentCandidates = _equipmentDefinitions
			.Where(definition => string.Equals(definition.SlotId, _selectedEquipmentSlotId, StringComparison.Ordinal)
				&& _session.IsEquipmentOwned(definition.ItemId))
			.ToArray();

		_statusEquipmentCandidateList.Clear();
		foreach (EquipmentDefinition definition in _visibleEquipmentCandidates)
		{
			string suffix = string.Equals(_session.GetEquippedItemId(_selectedEquipmentSlotId), definition.ItemId, StringComparison.Ordinal)
				? " [已装备]"
				: string.Empty;
			_statusEquipmentCandidateList.AddItem($"{definition.DisplayName}{suffix}");
		}

		_statusUnequipButton.Disabled = string.IsNullOrWhiteSpace(_session.GetEquippedItemId(_selectedEquipmentSlotId));
		_statusEquipButton.Disabled = _visibleEquipmentCandidates.Length == 0;

		if (_visibleEquipmentCandidates.Length == 0)
		{
			_statusEquipmentDetailText.Text = $"[b]{GetEquipmentSlotDisplayName(_selectedEquipmentSlotId)}[/b]\n当前没有可装备物品。";
			return;
		}

		_statusEquipmentCandidateList.Select(0);
		RefreshEquipmentDetail(_visibleEquipmentCandidates[0]);
	}

	private void RefreshEquipmentDetail(EquipmentDefinition definition)
	{
		if (_session == null)
		{
			return;
		}

		bool equipped = string.Equals(_session.GetEquippedItemId(definition.SlotId), definition.ItemId, StringComparison.Ordinal);
		int ownedCount = _session.InventoryState.ItemCounts.TryGetValue(definition.ItemId, out Variant ownedValue)
			? ownedValue.AsInt32()
			: 0;
		_statusEquipmentDetailText.Text = string.Join('\n', new[]
		{
			$"[b]{definition.DisplayName}[/b]",
			$"部位: {GetEquipmentSlotDisplayName(definition.SlotId)}",
			$"拥有数量: {ownedCount}",
			$"状态: {(equipped ? "已装备" : "未装备")}",
			$"效果: {definition.BonusSummary}",
			string.Empty,
			definition.Description,
		});
	}

	private void RefreshInventoryView()
	{
		if (_session == null)
		{
			_inventoryText.Text = "未找到 GlobalGameSession";
			return;
		}

		List<string> lines = new()
		{
			"[b]测试背包[/b]",
			string.Empty,
			$"主角 HP: {_session.PlayerCurrentHp}/{_session.PlayerMaxHp}",
			$"攻击: {_session.GetResolvedPlayerAttackDamage()}",
			$"防御减伤: {_session.GetResolvedPlayerDefenseDamageReductionPercent()}%",
			$"防御附盾: {_session.GetResolvedPlayerDefenseShieldGain()}",
			string.Empty,
			"[b]物品[/b]",
		};

		if (_session.InventoryState.ItemCounts.Count == 0)
		{
			lines.Add("- （空）");
		}
		else
		{
			foreach (Variant key in _session.InventoryState.ItemCounts.Keys)
			{
				lines.Add($"- {key.AsString()} x{_session.InventoryState.ItemCounts[key].AsInt32()}");
			}
		}

		lines.Add(string.Empty);
		lines.Add("[b]额外解锁卡牌[/b]");
		lines.AddRange(_session.ProgressionState.UnlockedCardIds.Length == 0
			? new[] { "- （无）" }
			: _session.ProgressionState.UnlockedCardIds.OrderBy(value => value, StringComparer.Ordinal).Select(value => $"- {value}"));

		_inventoryText.Text = string.Join('\n', lines);
	}

	private void RefreshTalentSummary()
	{
		if (_session == null)
		{
			return;
		}

		_masteryLabel.Text = $"专精点: {GetAvailablePoints()} / 基础池 {_baseMasteryPoints}";
		_combatSummaryLabel.Text = $"攻击 {_session.GetResolvedPlayerAttackDamage()}  防御减伤 {_session.GetResolvedPlayerDefenseDamageReductionPercent()}%  护盾+{_session.GetResolvedPlayerDefenseShieldGain()}  预算+{_session.ProgressionState.DeckPointBudgetBonus}";
	}

	private void RefreshTalentButtons()
	{
		foreach (TalentNode talent in _talents)
		{
			Button button = _talentButtons[talent.Id];
			bool purchased = _purchasedTalentIds.Contains(talent.Id);
			bool canPurchase = CanPurchase(talent);
			bool canRefund = CanRefund(talent);
			bool selected = string.Equals(_selectedTalentId, talent.Id, StringComparison.Ordinal);
			string status = purchased ? (canRefund ? "已解锁" : "已解锁") : canPurchase ? "可购买" : ArePrerequisitesMet(talent) ? "点数不足" : "前置未满足";
			string prefix = purchased ? "◆" : canPurchase ? "◇" : "■";
			string selectedPrefix = selected ? ">> " : string.Empty;
			button.Text = $"{selectedPrefix}{prefix} {talent.DisplayName}\n{status}";
			button.Modulate = selected
				? (purchased ? new Color(0.78f, 1.0f, 0.84f, 1f) : canPurchase ? new Color(1f, 0.98f, 0.72f, 1f) : new Color(0.72f, 0.76f, 0.84f, 1f))
				: purchased ? new Color(0.56f, 0.92f, 0.66f, 1f) : canPurchase ? new Color(1f, 0.9f, 0.54f, 1f) : new Color(0.48f, 0.48f, 0.48f, 1f);
		}

		RefreshTalentTreeLines();
	}

	private void RefreshTalentDetail()
	{
		TalentNode? talent = _talents.FirstOrDefault(item => string.Equals(item.Id, _selectedTalentId, StringComparison.Ordinal));
		if (talent == null)
		{
			_talentDetailPanel.Visible = false;
			_talentDetailTitleLabel.Text = "天赋详情";
			_unlockTalentButton.Disabled = true;
			_unlockTalentButton.Text = "请先选择天赋";
			_refundTalentButton.Disabled = true;
			_refundTalentButton.Text = "未选择节点";
			_talentDetail.Text = "拖动画布查看完整天赋树，点击节点查看详情。";
			return;
		}

		bool purchased = _purchasedTalentIds.Contains(talent.Id);
		bool canPurchase = CanPurchase(talent);
		bool canRefund = CanRefund(talent);
		_talentDetailPanel.Visible = true;
		_talentDetailTitleLabel.Text = $"天赋详情 | {talent.DisplayName}";
		_talentDetail.Text = string.Join('\n', new[]
		{
			$"[b]{talent.DisplayName}[/b]",
			talent.Description,
			$"花费: {talent.Cost}",
			$"前置: {(talent.PrerequisiteTalentIds.Length == 0 ? "无" : string.Join(", ", talent.PrerequisiteTalentIds))}",
			$"分支: {(talent.GrantedBranchTags.Length == 0 ? "无" : string.Join(", ", talent.GrantedBranchTags))}",
			$"解锁卡牌: {(talent.UnlockedCardIds.Length == 0 ? "无" : string.Join(", ", talent.UnlockedCardIds))}",
			$"预算修正: +{talent.DeckPointBudgetBonus} / 同名上限 +{talent.DeckMaxCopiesPerCardBonus}",
			$"附加 TalentIds: {(talent.GrantedTalentIds.Length == 0 ? "无" : string.Join(", ", talent.GrantedTalentIds))}",
			string.Empty,
			"点击节点只会选中天赋，不会直接解锁。",
			"请在右侧按钮中确认解锁或退点。",
		});
		_unlockTalentButton.Disabled = purchased || !canPurchase;
		_unlockTalentButton.Text = purchased ? "已解锁" : canPurchase ? $"解锁 ({talent.Cost}点)" : "不可解锁";
		_refundTalentButton.Disabled = !purchased || !canRefund;
		_refundTalentButton.Text = canRefund ? "退点" : "不可退点";
	}

	private void RefreshCodexView()
	{
		if (_session == null)
		{
			return;
		}

		ProgressionSnapshot progression = _session.BuildProgressionSnapshotModel();
		_cardCodexList.Clear();
		foreach (BattleCardTemplate template in _codexTemplates)
		{
			bool unlocked = template.IsOwned(progression);
			int index = _cardCodexList.AddItem(unlocked ? template.DisplayName : "■■■■");
			if (!unlocked)
			{
				_cardCodexList.SetItemCustomFgColor(index, new Color(0.16f, 0.16f, 0.16f, 1.0f));
			}
		}

		_enemyCodexList.Clear();
		foreach (EnemyCodexEntry entry in _enemyCodexEntries)
		{
			bool unlocked = IsEnemyCodexUnlocked(entry, progression);
			int index = _enemyCodexList.AddItem(unlocked ? entry.DisplayName : "■■■■");
			if (!unlocked)
			{
				_enemyCodexList.SetItemCustomFgColor(index, new Color(0.16f, 0.16f, 0.16f, 1.0f));
			}
		}

		if (_cardCodexList.ItemCount > 0)
		{
			OnCardCodexSelected(Mathf.Clamp(_cardCodexList.GetSelectedItems().FirstOrDefault(), 0, _cardCodexList.ItemCount - 1));
		}

		if (_enemyCodexList.ItemCount > 0)
		{
			OnEnemyCodexSelected(Mathf.Clamp(_enemyCodexList.GetSelectedItems().FirstOrDefault(), 0, _enemyCodexList.ItemCount - 1));
		}
	}

	private void RefreshDeckView()
	{
		if (_session == null || _constructionService == null || _cardLibrary == null)
		{
			return;
		}

		ProgressionSnapshot progression = _session.BuildProgressionSnapshotModel();
		_availableTemplates = _constructionService.GetAvailableCardPool(progression).ToArray();
		_availableList.Clear();
		foreach (BattleCardTemplate template in _availableTemplates)
		{
			bool overlimit = !template.CanCarryNormally(progression) && template.CanCarryOverlimit(progression);
			string suffix = template.IsLearnedCard ? " [学习]" : overlimit ? " [超限]" : string.Empty;
			_availableList.AddItem($"{template.DisplayName}{suffix}");
		}

		_deckList.Clear();
		foreach (string cardId in _workingDeck)
		{
			BattleCardTemplate? template = _cardLibrary.FindTemplate(cardId);
			_deckList.AddItem(template?.DisplayName ?? cardId);
		}

		DeckBuildSnapshot snapshot = new()
		{
			BuildName = _session.DeckBuildState.BuildName,
			CardIds = _workingDeck.ToArray(),
			RelicIds = _session.DeckBuildState.RelicIds,
		};
		BattleDeckValidationResult validation = _constructionService.ValidateDeck(snapshot, progression);
		_deckPoolSummaryLabel.Text = $"可选 {_availableTemplates.Length}";
		_deckSummaryLabel.Text = $"当前 {validation.TotalCardCount} 张 / 影响 {validation.TotalBuildPoints}";
		_deckValidationText.Text = BuildDeckValidationText(validation);
		_deckSaveButton.Disabled = !validation.IsValid;
	}

	private static string BuildDeckValidationText(BattleDeckValidationResult validation)
	{
		List<string> lines = new()
		{
			$"最低卡数: {validation.TotalCardCount}/{validation.EffectiveMinDeckSize}",
			$"影响因子: {validation.TotalBuildPoints}/{validation.EffectivePointBudget}",
			$"同名上限: {validation.EffectiveMaxCopiesPerCard}",
			$"超限槽位: {validation.UsedOverlimitCarrySlots}/{validation.EffectiveOverlimitCarrySlots}",
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

		return string.Join('\n', lines);
	}

	private void OnTalentPressed(string talentId)
	{
		_selectedTalentId = talentId;
		RefreshTalentButtons();
		RefreshTalentDetail();
	}

	private void OnUnlockTalentPressed()
	{
		TalentNode? talent = _talents.FirstOrDefault(item => string.Equals(item.Id, _selectedTalentId, StringComparison.Ordinal));
		if (talent == null || !CanPurchase(talent))
		{
			return;
		}

		_purchasedTalentIds.Add(talent.Id);
		RecomputeSessionProgression();
		LoadWorkingDeckFromSession();
		RefreshAll();
	}

	private void OnRefundTalentPressed()
	{
		TalentNode? talent = _talents.FirstOrDefault(item => string.Equals(item.Id, _selectedTalentId, StringComparison.Ordinal));
		if (talent == null || !CanRefund(talent))
		{
			return;
		}

		_purchasedTalentIds.Remove(talent.Id);
		RecomputeSessionProgression();
		LoadWorkingDeckFromSession();
		RefreshAll();
	}

	private void OnCardCodexSelected(long index)
	{
		if (_session == null || index < 0 || index >= _codexTemplates.Length)
		{
			return;
		}

		BattleCardTemplate template = _codexTemplates[index];
		ProgressionSnapshot progression = _session.BuildProgressionSnapshotModel();
		bool unlocked = template.IsOwned(progression);
		_cardCodexDetail.Text = unlocked
			? string.Join('\n', new[]
			{
				$"[b]{template.DisplayName}[/b]",
				template.Description,
				$"费用 {template.Cost} / 影响因子 {template.BuildPoints}",
				$"Quick {template.IsQuick} / Exhaust {template.ExhaustsOnPlay}",
				"状态: 已解锁",
			})
			: string.Join('\n', new[]
			{
				"[b]■■■■[/b]",
				"[ 黑色剪影 ]",
				$"解锁方式: {BuildCardUnlockHint(template)}",
			});
	}

	private void OnEnemyCodexSelected(long index)
	{
		if (_session == null || index < 0 || index >= _enemyCodexEntries.Length)
		{
			return;
		}

		EnemyCodexEntry entry = _enemyCodexEntries[index];
		bool unlocked = IsEnemyCodexUnlocked(entry, _session.BuildProgressionSnapshotModel());
		_enemyCodexDetail.Text = unlocked
			? string.Join('\n', new[]
			{
				$"[b]{entry.DisplayName}[/b]",
				entry.Description,
				"状态: 已解锁",
			})
			: string.Join('\n', new[]
			{
				"[b]■■■■[/b]",
				"[ 黑色剪影 ]",
				$"解锁方式: {entry.UnlockHint}",
			});
	}

	private bool CanPurchase(TalentNode talent)
	{
		return !_purchasedTalentIds.Contains(talent.Id) && ArePrerequisitesMet(talent) && GetAvailablePoints() >= talent.Cost;
	}

	private bool CanRefund(TalentNode talent)
	{
		return _purchasedTalentIds.Contains(talent.Id)
			&& !_talents.Any(other => _purchasedTalentIds.Contains(other.Id) && other.PrerequisiteTalentIds.Contains(talent.Id, StringComparer.Ordinal));
	}

	private bool ArePrerequisitesMet(TalentNode talent)
	{
		return talent.PrerequisiteTalentIds.All(requiredId => _purchasedTalentIds.Contains(requiredId));
	}

	private static string BuildCardUnlockHint(BattleCardTemplate template)
	{
		if (template.IsLearnedCard)
		{
			return "通过学习强敌招牌技解锁";
		}

		if (template.RequiredTalentIds.Length > 0 || template.RequiredBranchTags.Length > 0)
		{
			return "满足天赋树要求并获得卡牌后解锁";
		}

		if (template.RequiredPlayerLevel > 1)
		{
			return $"角色达到 Lv.{template.RequiredPlayerLevel} 并获得卡牌后解锁";
		}

		return template.UnlockedByDefault ? "初始已解锁" : "通过探索、商店或奖励获得";
	}

	private bool IsEnemyCodexUnlocked(EnemyCodexEntry entry, ProgressionSnapshot progression)
	{
		return entry.UnlockedByDefault
			|| entry.RequiredUnlockedCardIds.All(cardId => progression.UnlockedCardIds.Contains(cardId, StringComparer.Ordinal));
	}

	private int GetSpentPoints()
	{
		return _talents.Where(talent => _purchasedTalentIds.Contains(talent.Id)).Sum(talent => talent.Cost);
	}

	private int GetAvailablePoints()
	{
		return Math.Max(0, _baseMasteryPoints - GetSpentPoints());
	}

	private void OnGrantPointPressed()
	{
		_baseMasteryPoints += 1;
		RecomputeSessionProgression();
		RefreshAll();
	}

	private void OnRevokePointPressed()
	{
		if (_baseMasteryPoints > GetSpentPoints())
		{
			_baseMasteryPoints -= 1;
			RecomputeSessionProgression();
			RefreshAll();
		}
	}

	private void OnResetTalentsPressed()
	{
		_purchasedTalentIds.Clear();
		_selectedTalentId = string.Empty;
		RecomputeSessionProgression();
		LoadWorkingDeckFromSession();
		RefreshAll();
	}

	private void OnSeedInventoryPressed()
	{
		SeedInventoryDefaults();
		RefreshStatusView();
		RefreshBagView();
	}

	private void OnClearInventoryPressed()
	{
		if (_session == null)
		{
			return;
		}

		_session.InventoryState.ItemCounts.Clear();
		_session.UnequipItem("weapon");
		_session.UnequipItem("armor");
		_session.UnequipItem("accessory");
		RefreshStatusView();
		RefreshBagView();
	}

	private void OnEquipmentSlotSelected(long index)
	{
		if (index < 0 || index >= EquipmentSlotOrder.Length)
		{
			return;
		}

		_selectedEquipmentSlotId = EquipmentSlotOrder[index];
		RefreshEquipmentSection();
	}

	private void OnEquipmentCandidateSelected(long index)
	{
		if (index < 0 || index >= _visibleEquipmentCandidates.Length)
		{
			return;
		}

		RefreshEquipmentDetail(_visibleEquipmentCandidates[index]);
	}

	private void OnEquipButtonPressed()
	{
		if (_session == null || _statusEquipmentCandidateList.GetSelectedItems().Length == 0)
		{
			return;
		}

		int selectedIndex = _statusEquipmentCandidateList.GetSelectedItems()[0];
		if (selectedIndex < 0 || selectedIndex >= _visibleEquipmentCandidates.Length)
		{
			return;
		}

		EquipmentDefinition definition = _visibleEquipmentCandidates[selectedIndex];
		if (!_session.TryEquipItem(_selectedEquipmentSlotId, definition.ItemId, out _))
		{
			return;
		}

		RefreshAll();
		_statusEquipmentCandidateList.Select(selectedIndex);
		RefreshEquipmentDetail(definition);
	}

	private void OnUnequipButtonPressed()
	{
		if (_session == null)
		{
			return;
		}

		_session.UnequipItem(_selectedEquipmentSlotId);
		RefreshAll();
	}

	private void OnAvailableSelected(long index)
	{
		if (index < 0 || index >= _availableTemplates.Length)
		{
			return;
		}

		_deckDetailText.Text = BuildDeckDetailText(_availableTemplates[index]);
	}

	private void OnDeckSelected(long index)
	{
		if (index < 0 || index >= _workingDeck.Count || _cardLibrary == null)
		{
			return;
		}

		BattleCardTemplate? template = _cardLibrary.FindTemplate(_workingDeck[(int)index]);
		_deckDetailText.Text = template != null ? BuildDeckDetailText(template) : _workingDeck[(int)index];
	}

	private static string BuildDeckDetailText(BattleCardTemplate template)
	{
		return string.Join('\n', new[]
		{
			$"[b]{template.DisplayName}[/b]",
			template.Description,
			$"费用 {template.Cost} / 影响因子 {template.BuildPoints}",
			$"伤害 {template.Damage} / 治疗 {template.HealingAmount} / 抽牌 {template.DrawCount} / 回能 {template.EnergyGain} / 护盾 {template.ShieldGain}",
			$"Quick {template.IsQuick} / Exhaust {template.ExhaustsOnPlay}",
		});
	}

	private void OnDeckAddPressed()
	{
		if (_constructionService == null || _session == null || _availableList.GetSelectedItems().Length == 0)
		{
			return;
		}

		BattleCardTemplate template = _availableTemplates[_availableList.GetSelectedItems()[0]];
		List<string> candidateDeck = new(_workingDeck) { template.CardId };
		BattleDeckValidationResult validation = _constructionService.ValidateDeck(
			new DeckBuildSnapshot { CardIds = candidateDeck.ToArray(), RelicIds = _session.DeckBuildState.RelicIds },
			_session.BuildProgressionSnapshotModel());
		if (!validation.IsValid)
		{
			_deckValidationText.Text = BuildDeckValidationText(validation);
			return;
		}

		_workingDeck = candidateDeck;
		RefreshDeckView();
	}

	private void OnDeckRemovePressed()
	{
		if (_deckList.GetSelectedItems().Length == 0)
		{
			return;
		}

		_workingDeck.RemoveAt(_deckList.GetSelectedItems()[0]);
		RefreshDeckView();
	}

	private void OnDeckSavePressed()
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
			_deckValidationText.Text = BuildDeckValidationText(validation);
			return;
		}

		_session.ApplyDeckBuildSnapshot(snapshot.ToDictionary());
		_deckValidationText.Text = BuildDeckValidationText(validation) + "\n已保存到 GlobalGameSession";
	}

	private void OnDeckResetPressed()
	{
		LoadWorkingDeckFromSession();
		RefreshDeckView();
	}

	private void OnDeckStarterPressed()
	{
		if (_cardLibrary == null)
		{
			return;
		}

		_workingDeck = _cardLibrary.BuildStarterDeckCardIds().ToList();
		RefreshDeckView();
	}

	private void SeedInventoryDefaults()
	{
		if (_session == null)
		{
			return;
		}

		Godot.Collections.Dictionary items = _session.InventoryState.ItemCounts;
		items.Clear();
		items["steel_scrap"] = 5;
		items["charged_core"] = 2;
		items["medical_gel"] = 3;
		items["optical_part"] = 1;
		items["rusted_blade"] = 1;
		items["ion_pistol"] = 1;
		items["patched_coat"] = 1;
		items["reactive_plate"] = 1;
		items["signal_charm"] = 1;
		items["tactical_chip"] = 1;
	}

	private void RecomputeSessionProgression()
	{
		if (_session == null)
		{
			return;
		}

		List<string> talentIds = new();
		List<string> branchTags = new();
		List<string> unlockedCardIds = new();
		int budgetBonus = 0;
		int copiesBonus = 0;

		foreach (TalentNode talent in _talents.Where(talent => _purchasedTalentIds.Contains(talent.Id)))
		{
			talentIds.Add(talent.Id);
			talentIds.AddRange(talent.GrantedTalentIds);
			branchTags.AddRange(talent.GrantedBranchTags);
			unlockedCardIds.AddRange(talent.UnlockedCardIds);
			budgetBonus += talent.DeckPointBudgetBonus;
			copiesBonus += talent.DeckMaxCopiesPerCardBonus;
		}

		_session.ProgressionState.PlayerLevel = Math.Max(3, _session.ProgressionState.PlayerLevel);
		_session.ProgressionState.PlayerMasteryPoints = GetAvailablePoints();
		_session.ProgressionState.TalentIds = talentIds.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray();
		_session.ProgressionState.TalentBranchTags = branchTags.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray();
		_session.ProgressionState.UnlockedCardIds = unlockedCardIds.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray();
		_session.ProgressionState.DeckPointBudgetBonus = budgetBonus;
		_session.ProgressionState.DeckMaxCopiesPerCardBonus = copiesBonus;

		_session.PlayerLevel = _session.ProgressionState.PlayerLevel;
		_session.PlayerMasteryPoints = _session.ProgressionState.PlayerMasteryPoints;
		_session.TalentIds = _session.ProgressionState.TalentIds;
		_session.TalentBranchTags = _session.ProgressionState.TalentBranchTags;
		_session.UnlockedCardIds = _session.ProgressionState.UnlockedCardIds;
		_session.DeckPointBudgetBonus = budgetBonus;
		_session.DeckMaxCopiesPerCardBonus = copiesBonus;
	}

	private EquipmentDefinition? FindEquipmentDefinition(string itemId)
	{
		return _equipmentDefinitions.FirstOrDefault(definition => string.Equals(definition.ItemId, itemId, StringComparison.Ordinal));
	}

	private string GetEquipmentDisplayName(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId))
		{
			return "未装备";
		}

		return FindEquipmentDefinition(itemId)?.DisplayName ?? itemId;
	}

	private static string GetEquipmentSlotDisplayName(string slotId)
	{
		return slotId switch
		{
			"weapon" => "武器",
			"armor" => "护甲",
			"accessory" => "饰品",
			_ => slotId,
		};
	}

	private sealed class TalentNode
	{
		public TalentNode(string id, string displayName, int cost, string description, Vector2 treePosition, string[] prerequisiteTalentIds, string[] grantedTalentIds, string[]? grantedBranchTags = null, string[]? unlockedCardIds = null, int deckPointBudgetBonus = 0, int deckMaxCopiesPerCardBonus = 0)
		{
			Id = id;
			DisplayName = displayName;
			Cost = cost;
			Description = description;
			TreePosition = treePosition;
			PrerequisiteTalentIds = prerequisiteTalentIds ?? Array.Empty<string>();
			GrantedTalentIds = grantedTalentIds ?? Array.Empty<string>();
			GrantedBranchTags = grantedBranchTags ?? Array.Empty<string>();
			UnlockedCardIds = unlockedCardIds ?? Array.Empty<string>();
			DeckPointBudgetBonus = deckPointBudgetBonus;
			DeckMaxCopiesPerCardBonus = deckMaxCopiesPerCardBonus;
		}

		public string Id { get; }
		public string DisplayName { get; }
		public int Cost { get; }
		public string Description { get; }
		public Vector2 TreePosition { get; }
		public string[] PrerequisiteTalentIds { get; }
		public string[] GrantedTalentIds { get; }
		public string[] GrantedBranchTags { get; }
		public string[] UnlockedCardIds { get; }
		public int DeckPointBudgetBonus { get; }
		public int DeckMaxCopiesPerCardBonus { get; }
	}

	private sealed class EnemyCodexEntry
	{
		public EnemyCodexEntry(string id, string displayName, string description, bool unlockedByDefault, string unlockHint, params string[] requiredUnlockedCardIds)
		{
			Id = id;
			DisplayName = displayName;
			Description = description;
			UnlockedByDefault = unlockedByDefault;
			UnlockHint = unlockHint;
			RequiredUnlockedCardIds = requiredUnlockedCardIds ?? Array.Empty<string>();
		}

		public string Id { get; }
		public string DisplayName { get; }
		public string Description { get; }
		public bool UnlockedByDefault { get; }
		public string UnlockHint { get; }
		public string[] RequiredUnlockedCardIds { get; }
	}

	private sealed class EquipmentDefinition
	{
		public EquipmentDefinition(string itemId, string displayName, string slotId, string description, string bonusSummary)
		{
			ItemId = itemId;
			DisplayName = displayName;
			SlotId = slotId;
			Description = description;
			BonusSummary = bonusSummary;
		}

		public string ItemId { get; }
		public string DisplayName { get; }
		public string SlotId { get; }
		public string Description { get; }
		public string BonusSummary { get; }
	}
}
