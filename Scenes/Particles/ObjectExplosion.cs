using Godot;

namespace SpinalShatter;

public partial class ObjectExplosion : Node3D
{
	[Export] private PackedScene _explosionEffectScene;
	[Export] private Marker3D _marker3D;
	[Export] private float maxLaunchAngle = 120;

	public void Explode(int count, ProjectileLaunchData data)
	{
		data.StartPosition = _marker3D;
		for (var i = 0; i < count; i++)
		{
			Projectile instance = (Projectile)_explosionEffectScene.Instantiate();
			RoomManager.Instance.CurrentRoom.AddChild(instance);
			instance.GlobalPosition = GlobalPosition;

			// add randomness to this launch vector
			data.InitialVelocity = Vector3.Down;

			instance.Launch(data);
		}

		QueueFree();
	}
}