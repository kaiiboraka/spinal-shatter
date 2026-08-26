using Godot;

namespace SpinalShatter;

public partial class ObjectExplosion : Node3D
{
	[Export] private PackedScene _explosionEffectScene;
	[Export] private Marker3D _marker3D;
	[Export] private float maxLaunchAngle = 120;

	[Export(PropertyHint.Range, "1,10")] private float launchDamping = 3f;

	public void Explode(int count, ProjectileLaunchData data)
	{
		data.StartPosition = _marker3D;
		float speed = data.SpellData.SpeedRange.GetLerpedValue(data.ChargeRatio) / launchDamping;
		float maxAngleRad = Mathf.DegToRad(maxLaunchAngle);

		// count = Mathf.Max(data.ChargeRatio.ToSteppedInt(count), 1);

		for (int i = 0; i < count; i++)
		{
			Projectile instance = (Projectile)_explosionEffectScene.Instantiate();
			RoomManager.Instance.CurrentRoom.AddChild(instance);

			// add randomness to this launch vector
			// Generate a random direction within a cone facing up
			float z = (float)GD.RandRange(Mathf.Cos(maxAngleRad), 1.0f);
			float theta = GD.Randf() * 2.0f * Mathf.Pi;
			float r = Mathf.Sqrt(1.0f - z * z);
			float x = r * Mathf.Cos(theta);
			float y = r * Mathf.Sin(theta);

			// This creates a vector on a spherical cap around +Z, then we rotate it to be around +Y (Up).
			Vector3 randomDirection = new Vector3(x, z, -y);
			data.StartPosition.GlobalPosition = GlobalPosition + (randomDirection / 5) + (Vector3.Up / 3f);
			data.InitialVelocity = randomDirection * speed;

			instance.Launch(data);
		}

		QueueFree();
	}
}