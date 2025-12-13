using System;
using Godot;
using Godot.Collections;
using SpinalShatter;

namespace SpinalShatter;

[GlobalClass]
public partial class ShopCart : StaticBody3D
{
	public const int SHOP_STOCK_COUNT = 3;

	private AnimationPlayer animationPlayer;
	private Area3D interactionArea;
	private Node3D shopRoot;
	private Camera3D shopCamera;
	private Marker3D playerSpawnPoint;

	private AnimatedSprite3D selectionCursor;
	private Array<ShopItem> _shopItems = new();
	private Array<Marker3D> _shopSlots = new();
	private int _selectedItemIndex = 0;

	// private Tween _cursorTween;

	[Export] private ShopStockData _shopStock;
	private WeaponSlotter _weaponSlotter;

	private ShopState currentState;

	private enum ShopState
	{
		ClosedWindow,
		OpenWindow,
		PlayerShopping,
		// AwaitingSlotSelection, // Removed, handled by WeaponSlotter
	}

	// private SpellData _purchasedWeaponPendingSlotAssignment; // Removed

	public override void _EnterTree()
	{
		base._EnterTree();
		GetComponents();
	}

	public override void _Ready()
	{
		GetComponents();

		_weaponSlotter = WeaponSlotter.Instance;
		if (_weaponSlotter != null)
		{
			_weaponSlotter.SlotSelected += OnSlotSelected;
		}
		else
		{
			GD.PrintErr("ShopCart: WeaponSlotter instance not found!");
		}

		// Populate _shopItems from ShopRoot/ShopSlots's children that are ShopItem's

		interactionArea.BodyEntered += OnPlayerEntered;
		interactionArea.BodyExited += OnPlayerExited;

		if (WaveDirector.Instance != null)
		{
			WaveDirector.Instance.RoundWon += RandomizeStock;
		}

		SetProcessInput(false);

		_selectedItemIndex = 0;
		RandomizeStock(); // Populate the shop for the first time
	}

	private void GetComponents()
	{
		animationPlayer ??= GetNode<AnimationPlayer>("AnimationPlayer");
		interactionArea ??= GetNode<Area3D>("InteractionArea3D");
		playerSpawnPoint ??= GetNode<Marker3D>("PlayerSpawnPoint");

		shopCamera ??= GetNode<Camera3D>("%Shop_Camera3D");
		shopRoot ??= GetNode<Node3D>("%ShopRoot");

		selectionCursor ??= GetNode<AnimatedSprite3D>("%SelectionCursor_AnimatedSprite3D");

		_shopSlots = new();
		_shopItems = new();
		for (int i = 0; i < SHOP_STOCK_COUNT; i++)
		{
			_shopSlots.Add(GetNode<Marker3D>($"%ShopSlot{i + 1}"));
			_shopItems.Add(_shopSlots[i].GetChild<ShopItem>(0));
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (currentState == ShopState.PlayerShopping)
		{
			if (@event.IsActionPressed("ui_left"))
			{
				_selectedItemIndex = (_selectedItemIndex - 1 + SHOP_STOCK_COUNT) % SHOP_STOCK_COUNT;
				UpdateSelectionVisuals();
			}
			else if (@event.IsActionPressed("ui_right"))
			{
				_selectedItemIndex = (_selectedItemIndex + 1) % SHOP_STOCK_COUNT;
				UpdateSelectionVisuals();
			}
			else if (@event.IsActionPressed("Player_Interact"))
			{
				TryPurchaseSelectedItem();
			}
			else if (@event.IsActionPressed("ui_cancel"))
			{
				CloseShop();
			}
		}
		// else if (currentState == ShopState.AwaitingSlotSelection) { ... logic removed ... }
		else if (@event.IsActionPressed("Player_Interact"))
		{
			if (currentState == ShopState.OpenWindow)
			{
				OpenShop();
			}
		}
	}

	private void OnSlotSelected(SlotType slot, SpellData purchasedSpellData)
	{
		var player = PlayerBody.Instance;
		if (player == null)
		{
			GD.PrintErr("ShopCart: PlayerBody.Instance is null in OnSlotSelected.");
			return;
		}

		player.HideInteractionPrompt(); // Hide any prompt from the player

		if (purchasedSpellData is not PrimarySpellData primarySpell)
		{
			GD.PrintErr("ShopCart: Purchased spell is not PrimarySpellData in OnSlotSelected.");
			currentState = ShopState.PlayerShopping; // Abort
			return;
		}

		SpellData spellToEquip;
		switch (slot)
		{
			case SlotType.Primary:
				spellToEquip = primarySpell;
				break;
			case SlotType.Secondary:
				spellToEquip = primarySpell.Secondary;
				break;
			case SlotType.Automatic:
				spellToEquip = primarySpell.Automatic;
				break;
			default:
				GD.PrintErr($"ShopCart: Invalid SlotType {slot} received in OnSlotSelected.");
				currentState = ShopState.PlayerShopping; // Abort
				return;
		}

		player.Inventory.EquipOrRankUpItem(spellToEquip, slot);
		currentState = ShopState.PlayerShopping;

		// Clear the purchased item from the shop slot
		ShopItem purchasedShopItem = null;
		for (int i = 0; i < SHOP_STOCK_COUNT; i++)
		{
			if (_shopItems[i].Data == purchasedSpellData)
			{
				purchasedShopItem = _shopItems[i];
				break;
			}
		}

		if (purchasedShopItem != null)
		{
			purchasedShopItem.Data = null;
			purchasedShopItem.Visible = false;
		}


		// Find the next available item to select (this logic is adapted from TryPurchaseSelectedItem)
		bool foundNext = false;
		for (int i = 1; i < SHOP_STOCK_COUNT; i++)
		{
			int nextIndex = (_selectedItemIndex + i) % SHOP_STOCK_COUNT;
			if (_shopItems[nextIndex].Visible)
			{
				_selectedItemIndex = nextIndex;
				UpdateSelectionVisuals();
				foundNext = true;
				break;
			}
		}

		if (!foundNext)
		{
			// No other items left, clear details and hide prompt
			player.HideInteractionPrompt();
			player.UpdateShopDetails(null);
		}
	}

	private void RandomizeStock()
	{
		if (_shopStock == null || _shopStock.AvailableItems.Count == 0)
		{
			GD.PrintErr("ShopCart: ShopStockData is not assigned or contains no items.");
			return;
		}

		var player = PlayerBody.Instance;
		if (player == null || player.Inventory == null)
		{
			GD.PrintErr("ShopCart: Player or PlayerInventory is null. Cannot randomize stock.");
			return;
		}

		var potentialOffers = new Array<ShopItemData>();

		// 1. Build a list of ALL possible valid offers
		foreach (var baseItem in _shopStock.AvailableItems)
		{
			bool isMaxRank = false;
			int currentRank = 0;

			// Determine ownership and rank
			if (baseItem is SpellData spellData)
			{
				var (_, rank, max) = player.Inventory.GetOwnedWeaponInfo(spellData);
				currentRank = rank;
				isMaxRank = max;
			}
			else if (baseItem is StatItemData statItemData)
			{
				var (_, rank, max) = player.Inventory.GetOwnedStatItemInfo(statItemData);
				currentRank = rank;
				isMaxRank = max;
			}

			if (isMaxRank)
			{
				continue; // Skip max rank items
			}

			ShopItemData offer = (ShopItemData)baseItem.Duplicate(true);
			if (currentRank > 0) // Item is owned, create a rank-up offer
			{
				offer.ShopRank = currentRank + 1;
			}
			else // Item is not owned, create a base item offer
			{
				offer.ShopRank = 1;
			}

			potentialOffers.Add(offer);
		}

		// 2. Shuffle the list of all potential offers
		potentialOffers.Shuffle();

		// 3. Populate the shop slots with the first N items from the shuffled list
		for (int i = 0; i < SHOP_STOCK_COUNT; i++)
		{
			if (i < potentialOffers.Count)
			{
				_shopItems[i].Data = potentialOffers[i];
				_shopItems[i].Visible = true;
			}
			else
			{
				_shopItems[i].Data = null;
				_shopItems[i].Visible = false;
			}
		}

		// 4. Update visuals and details for the initial selection
		if (_shopItems.Count > 0)
		{
			_selectedItemIndex = 0;
			UpdateSelectionVisuals();
		}
	}

	private void UpdateSelectionVisuals()
	{
		Marker3D shopSlot = _shopSlots[_selectedItemIndex];
		if (shopSlot == null || !IsInstanceValid(shopSlot)) return;

		ShopItem currentSelectedShopItem = _shopItems[_selectedItemIndex]; // Get the currently selected item
		if (currentSelectedShopItem == null || !IsInstanceValid(currentSelectedShopItem)) return;

		// if (_cursorTween != null) _cursorTween.SafeKill();

		var cursorTween = CreateTween();

		// The cursor is a child of ShopSlots, so its position is relative to ShopSlots.
		// ShopItem is also a child of ShopSlots (via ShopSlotN Marker3D).
		// So we can directly tween the local position.
		cursorTween.TweenProperty(selectionCursor, "position",
						shopSlot.Position with { Y = shopSlot.Position.Y + 0.55f }, 0.2f)
				   .SetTrans(Tween.TransitionType.Cubic)
				   .SetEase(Tween.EaseType.Out);

		selectionCursor.Stop();
		selectionCursor.Play();

		// cursorTween.Finished += () => { cursorTween.Kill(); };
		if (PlayerBody.Instance != null)
		{
			PlayerBody.Instance.UpdateShopDetails(currentSelectedShopItem.Data); // Use the locally retrieved item
		}
	}

	private void TryPurchaseSelectedItem()
	{
		if (_selectedItemIndex < 0 || _selectedItemIndex >= SHOP_STOCK_COUNT) return;

		ShopItem selectedShopItem = _shopItems[_selectedItemIndex];
		if (selectedShopItem == null || !IsInstanceValid(selectedShopItem) || !selectedShopItem.Visible) return;

		ShopItemData itemData = selectedShopItem.Data;
		if (itemData == null) return;

		// 1. Calculate the correct price
		int price = itemData.Price;
		if (itemData.ShopRank > 1)
		{
			int rankUpIndex = itemData.ShopRank - 2;
			if (itemData.RankUps != null && rankUpIndex >= 0 && rankUpIndex < itemData.RankUps.Count)
			{
				price = (int)itemData.RankUps[rankUpIndex].RankUpPrice;
			}
			else
			{
				GD.PrintErr($"Could not find valid RankUpPrice for {itemData.ItemName} at ShopRank {itemData.ShopRank}");
				return;
			}
		}

		// 2. Attempt to spend money
		var player = PlayerBody.Instance;
		if (!player.SpendMoney(price))
		{
			player.ShowPromptToPress("", "Not Enough Money!", "Can't Buy");
			GetTree().CreateTimer(1.0f).Timeout += () => player.HideInteractionPrompt();
			return;
		}

		// 3. Handle the purchase logic
		// TODO: Add purchase sound effect

		if (itemData is SpellData spellData)
		{
			bool isOwned = player.Inventory.GetOwnedWeaponInfo(spellData).equippedItem != null;
			if (isOwned)
			{
				player.Inventory.EquipOrRankUpItem(spellData);
			}
			else // It's a new weapon, so we must ask the player where to put it.
			{
				_weaponSlotter.BeginSlotSelection(spellData);

				// Clear data from the slot now that it's pending assignment
				selectedShopItem.Data = null;
				selectedShopItem.Visible = false;
				return; // Wait for player input; don't find the next item yet.
			}
		}
		else // It's a StatItem or other type
		{
			player.Inventory.EquipOrRankUpItem(itemData);
		}

		// 4. Update the shop state for a completed, non-pending purchase (StatItems and Spell Rank-ups)
		selectedShopItem.Data = null; // THIS IS THE FIX: Clear the data from the slot
		selectedShopItem.Visible = false;

		// Find the next available item to select
		bool foundNext = false;
		for (int i = 1; i < _shopItems.Count; i++)
		{
			int nextIndex = (_selectedItemIndex + i) % _shopItems.Count;
			if (_shopItems[nextIndex].Visible)
			{
				_selectedItemIndex = nextIndex;
				UpdateSelectionVisuals();
				foundNext = true;
				break;
			}
		}

		if (!foundNext)
		{
			// No other items left, clear details and hide prompt
			player.HideInteractionPrompt();
			player.UpdateShopDetails(null);
		}
	}

	private void OnPlayerEntered(Node3D body)
	{
		if (body is PlayerBody player)
		{
			player.ShowPromptToPress("Player_Interact", "to\nBUY SOMFIN", "Press");
			OpenWindow();
		}
	}

	private void OnPlayerExited(Node3D body)
	{
		if (body is PlayerBody player)
		{
			player.HideInteractionPrompt();
			CloseWindow();
		}
	}

	private void OpenWindow()
	{
		animationPlayer.Play("Open");
		currentState = ShopState.OpenWindow;
		SetProcessInput(true);
	}

	private void CloseWindow()
	{
		animationPlayer.Play("Close");
		currentState = ShopState.ClosedWindow;
		SetProcessInput(false);
	}

	public void OpenShop()
	{
		if (currentState == ShopState.PlayerShopping) return;

		animationPlayer.Play("OPENED");
		currentState = ShopState.PlayerShopping;
		var player = PlayerBody.Instance;

		player.EnterUIMode();
		player.HideInteractionPrompt();

		CameraTransition.Instance.TransitionCamera3D(player.PlayerCamera, shopCamera, 1f);

		CameraTransition.Instance.TransitionFinished += MovePlayer;

		_selectedItemIndex = 0; // Initialize selection to the first item
		selectionCursor.Visible = true;
		UpdateSelectionVisuals();
		SetProcessInput(true);
	}

	private void CloseShop()
	{
		if (currentState != ShopState.PlayerShopping) return;

		var player = PlayerBody.Instance;
		CameraTransition.Instance.TransitionFinished -= MovePlayer;
		MovePlayer();

		CameraTransition.Instance.TransitionFinished += ReEnablePlayer;
		CameraTransition.Instance.TransitionCamera3D(shopCamera, player.PlayerCamera, 1f);

		selectionCursor.Visible = false;

		// _cursorTween?.SafeKill(); // Stop any ongoing tween
		SetProcessInput(false);
	}

	private void MovePlayer()
	{
		if (playerSpawnPoint != null)
		{
			PlayerBody.Instance.GlobalTransform = playerSpawnPoint.GlobalTransform;
		}
	}

	private void ReEnablePlayer()
	{
		currentState = ShopState.OpenWindow;
		PlayerBody.Instance.ExitUIMode();
		CameraTransition.Instance.TransitionFinished -= ReEnablePlayer;
	}

	/// <summary>
	/// This method is called by the Open and Close animations to hide the innards of the cart when the window is closed, and to show them before it opens.
	/// </summary>
	/// <param name="toggle"></param>
	private void ToggleShopVisibility(bool toggle)
	{
		shopRoot.Visible = toggle;
		currentState = toggle ? ShopState.OpenWindow : ShopState.ClosedWindow;
	}
}