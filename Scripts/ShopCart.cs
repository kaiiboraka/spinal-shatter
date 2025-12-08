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
	}

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
			_shopItems.Add(_shopSlots[0].GetChild<ShopItem>(0));
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

		var availableForPurchase = new Array<ShopItemData>();

		for (int i = 0; i < SHOP_STOCK_COUNT; i++)
		{
			var itemData = _shopStock.AvailableItems.PickRandom();

			// TODO: Add logic to filter out items player has at max rank
			// For now, just add all available items

			if (itemData is StatItemData shopStatItemData)
			{
				bool maxRank = false;
				int currentRank = 0;
				foreach (EquippedItem equippedItem in PlayerBody.Instance.Inventory.EquippedStatItems)
				{
					StatItemData playerStatItemData = (StatItemData)equippedItem.ItemData;
					if (playerStatItemData.TargetStat == shopStatItemData.TargetStat)
					{
						currentRank = equippedItem.Rank;
						maxRank = equippedItem.IsMaxRank;
						break;
					}
				}
				if (maxRank)
				{
					i--;
					continue;
				}
				itemData.ShopRank = currentRank + 1;
			}
			else if (itemData is SpellData shopSpellData)
			{
				bool maxRank = false;
				int currentRank = 0;
				foreach (EquippedItem equippedItem in PlayerBody.Instance.Inventory.EquippedWeapons.Values)
				{
					SpellData playerSpellData = (SpellData)equippedItem.ItemData;
					if (playerSpellData.Weapon == shopSpellData.Weapon)
					{
						currentRank = equippedItem.Rank;
						maxRank = equippedItem.IsMaxRank;
						break;
					}
				}
				if (maxRank)
				{
					i--;
					continue;
				}
				itemData.ShopRank = currentRank + 1;
			}


			availableForPurchase.Add(itemData);
		}

		// Shuffle the list of available items
		availableForPurchase.Shuffle();

		// Populate the shop slots
		for (int i = 0; i < SHOP_STOCK_COUNT; i++)
		{
			if (i < availableForPurchase.Count)
			{
				_shopItems[i].Data = availableForPurchase[i];
				_shopItems[i].Visible = true;
			}
			else
			{
				// If fewer items than slots, hide remaining slots
				_shopItems[i].Data = null;
				_shopItems[i].Visible = false;
			}
		}
		
		// Ensure selection visuals are updated after randomization
		UpdateSelectionVisuals();
	}

	private void UpdateSelectionVisuals()
	{
		Marker3D shopSlot = _shopSlots[_selectedItemIndex];
		if (shopSlot == null || !IsInstanceValid(shopSlot)) return;
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
	}

	private void TryPurchaseSelectedItem()
	{
		if (_selectedItemIndex < 0 || _selectedItemIndex >= SHOP_STOCK_COUNT) return;

		ShopItem selectedShopItem = _shopItems[_selectedItemIndex];
		if (selectedShopItem == null || !IsInstanceValid(selectedShopItem) || !selectedShopItem.Visible) return;

		ShopItemData itemData = selectedShopItem.Data;
		if (itemData == null) return;

		int price = itemData.Price;
		if (PlayerBody.Instance.SpendMoney(price))
		{
			// Purchase successful
			// TODO: Add purchase sound effect
			
			// Handle the SpellData assignment (defaults to Primary for now as discussed)

				// This is a temporary hardcoded assignment.
				// A proper UI would ask the player which slot (Primary, Alt, Automatic) to assign it to.
				// For now, let's assume it's always the Primary slot's variant if available.
				// Need to check if the itemData itself indicates its slot.
				// For StatItemData or other types, directly equip/rank up
			PlayerBody.Instance.Inventory.EquipOrRankUpItem(itemData);


			selectedShopItem.Visible = false; // Hide purchased item
			PlayerBody.Instance.HideInteractionPrompt();
			// Update visuals to remove purchased item, e.g., move cursor or re-evaluate selection
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