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

	private ShopState currentState;

	private enum ShopState
	{
		ClosedWindow,
		OpenWindow,
		PlayerShopping,
		AwaitingSlotSelection,
	}

	private SpellData _purchasedWeaponPendingSlotAssignment;

	public override void _EnterTree()
	{
		base._EnterTree();
		GetComponents();
	}

	public override void _Ready()
	{
		GetComponents();

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
		playerSpawnPoint ??=  GetNode<Marker3D>("PlayerSpawnPoint");

		shopCamera ??= GetNode<Camera3D>("%Shop_Camera3D");
		shopRoot ??= GetNode<Node3D>("%ShopRoot");

		selectionCursor ??= GetNode<AnimatedSprite3D>("%SelectionCursor_AnimatedSprite3D");

		_shopSlots = new();
		_shopItems = new();
		for (int i = 0; i < SHOP_STOCK_COUNT; i++)
		{
			_shopSlots.Add(GetNode<Marker3D>($"%ShopSlot{i+1}"));
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
		else if (currentState == ShopState.AwaitingSlotSelection)
		{
			if (@event.IsActionPressed("Player_Shoot"))
			{
				PlayerBody.Instance.Inventory.EquipOrRankUpItem(_purchasedWeaponPendingSlotAssignment, SlotType.Primary);
				_purchasedWeaponPendingSlotAssignment = null;
				currentState = ShopState.PlayerShopping;
				PlayerBody.Instance.HideInteractionPrompt();
			}
			else if (@event.IsActionPressed("Player_AltFire"))
			{
				PlayerBody.Instance.Inventory.EquipOrRankUpItem(_purchasedWeaponPendingSlotAssignment, SlotType.Secondary);
				_purchasedWeaponPendingSlotAssignment = null;
				currentState = ShopState.PlayerShopping;
				PlayerBody.Instance.HideInteractionPrompt();
			}
			// Add more cases for other weapon slots if needed (e.g., Automatic)
		}
		else if (@event.IsActionPressed("Player_Interact"))
		{
			if (currentState == ShopState.OpenWindow)
			{
				OpenShop();
			}
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

		var selectedOffers = new Array<ShopItemData>();
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		int attempts = 0;
		const int MAX_ITEM_GENERATION_ATTEMPTS = 500; // Max attempts to fill a slot, to prevent infinite loops

		// Try to fill each shop slot
		for (int i = 0; i < SHOP_STOCK_COUNT; i++)
		{
			ShopItemData finalOfferItem = null;
			attempts = 0;

			while (finalOfferItem == null && attempts < MAX_ITEM_GENERATION_ATTEMPTS)
			{
				attempts++;
				ShopItemData chosenBaseItem = (ShopItemData)_shopStock.AvailableItems.PickRandom().Duplicate(); // Clone to avoid modifying original asset

				// Check if this base item (by ItemName) is already in the offers selected for this shop cycle
				bool alreadySelectedInShop = false;
				foreach (var existingOffer in selectedOffers)
				{
					if (existingOffer.ItemName == chosenBaseItem.ItemName)
					{
						alreadySelectedInShop = true;
						break;
					}
				}
				if (alreadySelectedInShop) continue; // Pick another one if already selected for this shop cycle

				bool isMaxRank = false;
				int currentRank = 0;

				// Determine ownership and rank
				if (chosenBaseItem is SpellData spellData)
				{
					var (equippedWeapon, rank, max) = player.Inventory.GetOwnedWeaponInfo(spellData);
					currentRank = rank;
					isMaxRank = max;
				}
				else if (chosenBaseItem is StatItemData statItemData)
				{
					var (equippedStatItem, rank, max) = player.Inventory.GetOwnedStatItemInfo(statItemData);
					currentRank = rank;
					isMaxRank = max;
				}

				if (isMaxRank)
				{
					// Item is max ranked, should not be offered
					continue;
				}

				if (currentRank > 0) // Item is owned and not max rank, offer a rank-up
				{
					// Ensure there's a RankUpData for the next rank
					// currentRank is the actual rank (1-indexed). RankUps is 0-indexed.
					// So for currentRank 1 -> next is Rank 2, uses RankUps[0]
					// So for currentRank N -> next is Rank N+1, uses RankUps[N-1]
					if (chosenBaseItem.RankUps != null && currentRank < chosenBaseItem.RankUps.Count)
					{
						finalOfferItem = chosenBaseItem; // Use the cloned item
						finalOfferItem.ShopRank = currentRank + 1; // Set the target rank
						// The price is already handled by InventoryHUDItem from RankUpPrice
					}
					else
					{
						// No more rank-ups available for this item, treat as maxed or unofferable
						continue;
					}
				}
				else // Item is not owned, offer base item
				{
					finalOfferItem = chosenBaseItem; // Use the cloned item
					finalOfferItem.ShopRank = 1; // Set as base rank offer
				}
			}

			// Add the found offer to our list, or null if no eligible item was found after attempts
			selectedOffers.Add(finalOfferItem);
		}

		// Shuffle the final offers to randomize their position in the shop display
		selectedOffers.Shuffle();

		// Populate the actual shop item nodes
		for (int i = 0; i < SHOP_STOCK_COUNT; i++)
		{
			ShopItemData offer = selectedOffers[i];
			_shopItems[i].Data = offer;
			_shopItems[i].Visible = (offer != null); // Only visible if there's an item to display
		}
		
		// Ensure selection visuals are updated after randomization
		UpdateSelectionVisuals();
		if (PlayerBody.Instance != null)
		{
			PlayerBody.Instance.UpdateShopDetails(null); // Clear previous details
			if (_shopItems.Count > 0 && _selectedItemIndex < _shopItems.Count)
			{
				// Only update details if the initially selected item is valid
				if (_shopItems[_selectedItemIndex].Data != null)
				{
					PlayerBody.Instance.UpdateShopDetails(_shopItems[_selectedItemIndex].Data);
				}
				else
				{
					PlayerBody.Instance.UpdateShopDetails(null); // Clear if first item is null
				}
			}
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

		// Calculate the CORRECT price
		int price = itemData.Price;
		if (itemData.ShopRank > 1)
		{
			// itemData.ShopRank is the target rank (e.g., 2 for first rank-up).
			// RankUpData for first rank-up is at index 0. So index is currentRank - 1, which is ShopRank - 2.
			int rankUpIndex = itemData.ShopRank - 2;
			if (itemData.RankUps != null && rankUpIndex >= 0 && rankUpIndex < itemData.RankUps.Count)
			{
				price = (int)itemData.RankUps[rankUpIndex].RankUpPrice;
			}
			else
			{
				// No valid rank-up price found, cannot purchase.
				GD.PrintErr($"Could not find valid RankUpPrice for {itemData.ItemName} at ShopRank {itemData.ShopRank}");
				return;
			}
		}
		
		if (PlayerBody.Instance.SpendMoney(price))
		{
			// Purchase successful
			// TODO: Add purchase sound effect
			
			if (itemData is SpellData spellData)
			{
				// Check if there are empty weapon slots
				bool hasEmptySlot = false;
				foreach (SlotType slotType in Enum.GetValues(typeof(SlotType)))
				{
					if (slotType != SlotType.Stat && !PlayerBody.Instance.Inventory.EquippedWeapons.ContainsKey(slotType))
					{
						hasEmptySlot = true;
						break;
					}
				}

				if (hasEmptySlot)
				{
					PlayerBody.Instance.Inventory.EquipOrRankUpItem(spellData);
				}
				else // All weapon slots are full, await player input for slot assignment
				{
					_purchasedWeaponPendingSlotAssignment = spellData;
					currentState = ShopState.AwaitingSlotSelection;
					PlayerBody.Instance.ShowPromptToPress("Player_Shoot", "to Primary", "Assign Weapon");
					PlayerBody.Instance.ShowPromptToPress("Player_AltFire", "to Alt", "Assign Weapon");
					selectedShopItem.Visible = false; // Hide the item immediately, it's "bought"
					return; // Exit here, don't hide prompt or item yet.
				}
			}
			else
			{
				// For StatItemData or other types, directly equip/rank up
				PlayerBody.Instance.Inventory.EquipOrRankUpItem(itemData);
			}

			selectedShopItem.Visible = false; // Hide purchased item
			
			// After purchase, find next available item to select
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
				// No other items left, maybe close shop or just show empty selection
				PlayerBody.Instance.HideInteractionPrompt();
				PlayerBody.Instance.UpdateShopDetails(null);
			}
		}
		else
		{
			// Not enough money
			// TODO: Add "not enough money" feedback (sound, UI message)
			PlayerBody.Instance.ShowPromptToPress("", "Not Enough Money!", "Can't Buy");
			GetTree().CreateTimer(1.0f).Timeout += () => PlayerBody.Instance.HideInteractionPrompt();
		}
	}

	private void OnPlayerEntered(Node3D body)
	{
		if (body is PlayerBody player)
		{
			player.ShowPromptToPress("Player_Interact","to\nBUY SOMFIN", "Press");
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
		currentState = toggle ? ShopState.OpenWindow :  ShopState.ClosedWindow;
	}
}