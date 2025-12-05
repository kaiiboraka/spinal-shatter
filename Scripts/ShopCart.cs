using Godot;
using PhantomCamera;

namespace SpinalShatter;

public partial class ShopCart : StaticBody3D
{
	private AnimationPlayer animationPlayer;
	private Area3D interactionArea;
	private Node3D shopRoot;
	private Camera3D shopCamera;
	private Marker3D playerSpawnPoint;

	private PlayerBody player;
	private bool _isOpen = false;

	public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		interactionArea = GetNode<Area3D>("InteractionArea3D");
		playerSpawnPoint =  GetNode<Marker3D>("PlayerSpawnPoint");

		shopCamera = GetNode<Camera3D>("%Shop_Camera3D");
		shopRoot = GetNode<Node3D>("%ShopRoot");

		interactionArea.BodyEntered += OnPlayerEntered;
		interactionArea.BodyExited += OnPlayerExited;
	}

	public override void _Input(InputEvent @event)
	{
		if (_isOpen && @event.IsActionPressed("ui_cancel"))
		{
			CloseShop();
		}
		else if (!_isOpen && player != null && @event.IsActionPressed("Player_Interact"))
		{
			OpenShop();
		}
	}

	private void OnPlayerEntered(Node3D body)
	{
		if (body is PlayerBody player)
		{
			this.player = player;
			this.player.ShowInteractionPrompt("[E] to Shop");
			animationPlayer.Play("Open");
		}
	}

	private void OnPlayerExited(Node3D body)
	{
		if (body is PlayerBody player)
		{
			player.HideInteractionPrompt();
			this.player = null;
			animationPlayer.Play("Close");
		}
	}

	public void OpenShop()
	{
		if (player == null) return;
		
		_isOpen = true;
		player.EnterUIMode();
		player.HideInteractionPrompt();
		
		if (playerSpawnPoint != null)
		{
			player.GlobalTransform = playerSpawnPoint.GlobalTransform;
		}

		CameraTransition.Instance.TransitionCamera3D(player.PlayerCamera, shopCamera, 1f);
	}

	private void CloseShop()
	{
		if (player == null && PlayerBody.Instance != null)
		{
			player = PlayerBody.Instance;
		}
		if (player == null) return;

		_isOpen = false;
		player.ExitUIMode();
		CameraTransition.Instance.TransitionCamera3D(shopCamera, player.PlayerCamera, 1f);
	}

	/// <summary>
	/// This method is called by the Open and Close animations to hide the innards of the cart when the window is closed, and to show them before it opens.
	/// </summary>
	/// <param name="toggle"></param>
	private void ToggleShopVisibility(bool toggle)
	{
		shopRoot.Visible = toggle;
	}
}