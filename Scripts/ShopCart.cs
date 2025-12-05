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

		interactionArea.BodyEntered += OnPlayerEntered;
		interactionArea.BodyExited += OnPlayerExited;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			if (currentState == ShopState.PlayerShopping) CloseShop();
		}
		if (@event.IsActionPressed("Player_Interact"))
		{
			if (currentState == ShopState.OpenWindow)
				OpenShop();
		}
	}

	private void OnPlayerEntered(Node3D body)
	{
		if (body is PlayerBody player)
		{
			player.ShowPromptToPress("Player_Interact","to\nBUY SOMFIN", "Press");
			animationPlayer.Play("Open");
		}
	}

	private void OnPlayerExited(Node3D body)
	{
		if (body is PlayerBody player)
		{
			player.HideInteractionPrompt();
			animationPlayer.Play("Close");
		}
	}

	public void OpenShop()
	{
		currentState = ShopState.PlayerShopping;
		var player = PlayerBody.Instance;

		player.EnterUIMode();
		player.HideInteractionPrompt();
		
		CameraTransition.Instance.TransitionCamera3D(player.PlayerCamera, shopCamera, 1f);
	}

	private void CloseShop()
	{
		var player = PlayerBody.Instance;

		if (playerSpawnPoint != null)
		{
			player.GlobalTransform = playerSpawnPoint.GlobalTransform;
		}

		CameraTransition.Instance.TransitionFinished += ReEnablePlayer;
		CameraTransition.Instance.TransitionCamera3D(shopCamera, player.PlayerCamera, 1f);
	}

	private void ReEnablePlayer()
	{
		currentState = ShopState.OpenWindow;
		PlayerBody.Instance.ExitUIMode();
		CameraTransition.Instance.TransitionFinished -= CloseShop;
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