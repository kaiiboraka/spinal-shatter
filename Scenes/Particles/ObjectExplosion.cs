using Godot;

namespace SpinalShatter;

public partial class ObjectExplosion : Node3D
{
	[Export] private PackedScene _explosionEffectScene;

	public void Explode(int count)
	{
		for (var i = 0; i < count; i++)
		{
			Node3D instance = (Node3D)_explosionEffectScene.Instantiate();
			RoomManager.Instance.CurrentRoom.AddChild(instance);
			instance.GlobalPosition = GlobalPosition;
		}
		
		QueueFree();
	}
}