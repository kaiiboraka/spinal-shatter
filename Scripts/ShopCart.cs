using Godot;
using SpinalShatter.Scripts.Resources;

namespace SpinalShatter;

public partial class ShopCart : StaticBody3D
{
	private AnimationPlayer animationPlayer;
	private Area3D interactionArea;
	private Node3D shopRoot;
	private Camera3D shopCamera;
	private Marker3D playerSpawnPoint;

	private AnimatedSprite3D _selectionCursor;
	private Godot.Collections.Array<ShopItem> _shopItems = new();
	private int _selectedItemIndex = 0;
	private Tween _cursorTween;

	[Export] private ShopStockData _shopStock;

	private ShopState currentState;

	private enum ShopState
	{
		ClosedWindow,
		OpenWindow,
		PlayerShopping,
	}

	public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		interactionArea = GetNode<Area3D>("InteractionArea3D");
		playerSpawnPoint =  GetNode<Marker3D>("PlayerSpawnPoint");

		shopCamera = GetNode<Camera3D>("%Shop_Camera3D");
		shopRoot = GetNode<Node3D>("%ShopRoot");
		
		_selectionCursor = GetNode<AnimatedSprite3D>("%SelectionCursor_AnimatedSprite3D");
		// Populate _shopItems from ShopRoot/ShopSlots's children that are ShopItem's
		foreach (Node child in shopRoot.GetNode("ShopSlots").GetChildren())
		{
			if (child is Marker3D marker && marker.GetChildOrNull<ShopItem>(0) is ShopItem shopItem)
			{
				_shopItems.Add(shopItem);
			}
		}

		interactionArea.BodyEntered += OnPlayerEntered;
		interactionArea.BodyExited += OnPlayerExited;
		
		if (WaveDirector.Instance != null)
		{
			WaveDirector.Instance.RoundWon += RandomizeStock;
		}
		RandomizeStock(); // Populate the shop for the first time
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

		var availableForPurchase = new Godot.Collections.Array<ShopItemData>();
		foreach (var itemData in _shopStock.AvailableItems)
		{
			// TODO: Add logic to filter out items player has at max rank
			// For now, just add all available items
			availableForPurchase.Add(itemData);
		}

		// Shuffle the list of available items
		availableForPurchase.Shuffle();

		// Populate the shop slots
		for (int i = 0; i < _shopItems.Count; i++)
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

	public override void _Input(InputEvent @event)
	{
		if (currentState == ShopState.PlayerShopping)
		{
			if (@event.IsActionPressed("ui_left"))
			{
				_selectedItemIndex = (_selectedItemIndex - 1 + _shopItems.Count) % _shopItems.Count;
				UpdateSelectionVisuals();
			}
			else if (@event.IsActionPressed("ui_right"))
			{
				_selectedItemIndex = (_selectedItemIndex + 1) % _shopItems.Count;
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

	private void UpdateSelectionVisuals()
	{
		if (_shopItems.Count == 0) return;

		ShopItem selectedShopItem = _shopItems[_selectedItemIndex];
		if (selectedShopItem == null || !IsInstanceValid(selectedShopItem)) return;

		_cursorTween?.Kill();
		_cursorTween = CreateTween();
		
		// The cursor is a child of ShopSlots, so its position is relative to ShopSlots.
		// ShopItem is also a child of ShopSlots (via ShopSlotN Marker3D).
		// So we can directly tween the local position.
		_cursorTween.TweenProperty(_selectionCursor, "position",
			selectedShopItem.Position with { Y = selectedShopItem.Position.Y + 0.4f }, 0.2f)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
	}

	private void TryPurchaseSelectedItem()
	{
		if (_shopItems.Count == 0 || _selectedItemIndex < 0 || _selectedItemIndex >= _shopItems.Count) return;

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
			if (itemData is SpellData spellData)
			{
				// This is a temporary hardcoded assignment.
				// A proper UI would ask the player which slot (Primary, Alt, Automatic) to assign it to.
				// For now, let's assume it's always the Primary slot's variant if available.
				// Need to check if the itemData itself indicates its slot.
				PlayerBody.Instance.Inventory.EquipOrRankUpItem(spellData); // This will equip/rank up based on itemData.Slot
			}
			else
			{
				// For StatItemData or other types, directly equip/rank up
				PlayerBody.Instance.Inventory.EquipOrRankUpItem(itemData);
			}

			selectedShopItem.Visible = false; // Hide purchased item
			PlayerBody.Instance.HideInteractionPrompt();
			// Update visuals to remove purchased item, e.g., move cursor or re-evaluate selection
		}
		else
		{
			// Not enough money
			// TODO: Add "not enough money" feedback (sound, UI message)
			PlayerBody.Instance.ShowPromptToPress("Player_Interact", "Not Enough Money!", "Can't Buy");
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
	}

	private void CloseWindow()
	{
		animationPlayer.Play("Close");
		currentState = ShopState.ClosedWindow;
	}

	public void OpenShop()
	{
		if (currentState == ShopState.PlayerShopping) return;

		currentState = ShopState.PlayerShopping;
		var player = PlayerBody.Instance;

		player.EnterUIMode();
		player.HideInteractionPrompt();

		CameraTransition.Instance.TransitionCamera3D(player.PlayerCamera, shopCamera, 1f);

		CameraTransition.Instance.TransitionFinished += MovePlayer;
		
		_selectedItemIndex = 0; // Initialize selection to the first item
		_selectionCursor.Visible = true;
		UpdateSelectionVisuals();
	}

	private void CloseShop()
	{
		if (currentState != ShopState.PlayerShopping) return;

		var player = PlayerBody.Instance;
		CameraTransition.Instance.TransitionFinished -= MovePlayer;
		MovePlayer();

		CameraTransition.Instance.TransitionFinished += ReEnablePlayer;
		CameraTransition.Instance.TransitionCamera3D(shopCamera, player.PlayerCamera, 1f);
		
		_selectionCursor.Visible = false;
		_cursorTween?.Kill(); // Stop any ongoing tween
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