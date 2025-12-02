using Godot;
namespace SpinalShatter;

public partial class ShopCart : StaticBody3D
{
	private AnimationPlayer _animationPlayer;
	private Area3D _interactionArea;

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_interactionArea = GetNode<Area3D>("InteractionArea3D");

		_interactionArea.BodyEntered += OnPlayerEntered;
		_interactionArea.BodyExited += OnPlayerExited;
	}

	private void OnPlayerEntered(Node3D body)
	{
		if (body is PlayerBody)
		{
			_animationPlayer.Play("Open");
		}
	}

	private void OnPlayerExited(Node3D body)
	{
		if (body is PlayerBody)
		{
			_animationPlayer.Play("Close");
		}
	}
}
