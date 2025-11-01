using Godot;
using System;

public partial class Projectile : RigidBody3D
{
	private enum ProjectileState
	{
		Charging,
		Fired
	}

	[Export] private SpriteBase3D _sprite;
	[Export] private CollisionShape3D _collisionShape;
	[Export] public AudioStreamPlayer3D AudioStreamPlayer3D { get; private set; }
	[Export] public AudioStream AudioStream_Fireball { get; private set; }
	[Export] public AudioStream AudioStream_FireHit { get; private set; }

	[Export(PropertyHint.Range, "0.0, 1.0")]
	private float _minRefundPercent = 0.1f;

	[Export(PropertyHint.Range, "0.0, 1.0")]
	private float _maxRefundPercent = 0.25f;

	[Export(PropertyHint.Range, "0.1, 100.0")]
	private float _lifetime = 10f;

	public Node3D LevelParent { get; set; }
	public float Damage { get; private set; }
	public float InitialManaCost { get; private set; }

	private ProjectileState _state = ProjectileState.Charging;
	private Timer _lifetimeTimer;
	private float _bounceCooldown = 0;

	[Export] private float initialDb = 46;

	public override void _Ready()
	{
		if (_sprite == null) _sprite = GetNode<SpriteBase3D>("Sprite3D");
		if (_collisionShape == null) _collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");

		_lifetimeTimer = new Timer();
		_lifetimeTimer.WaitTime = _lifetime;
		_lifetimeTimer.OneShot = true;
		_lifetimeTimer.Timeout += () => QueueFree();
		AddChild(_lifetimeTimer);

		// Disable physics until launched
		this.Freeze = true;
		_collisionShape.Disabled = true;

		ContactMonitor = true;
        MaxContactsReported = 4;
	}

	public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (_bounceCooldown > 0)
        {
            _bounceCooldown -= (float)delta;
        }
    }

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		if (_bounceCooldown > 0 || _state != ProjectileState.Fired)
		{
			return;
		}

		if (state.GetContactCount() > 0)
		{
			AudioStreamPlayer3D.PitchScale = 1f; // Reset pitch on any collision
		}

		for (int i = 0; i < state.GetContactCount(); i++)
		{
			Node collider = state.GetContactColliderObject(i) as Node;
			if (collider != null)
			{
				if (!collider.IsInGroup("Enemies"))
				{
					// Wall bounce
					Vector3 impactPoint = state.GetContactColliderPosition(i);
					HandleWallBounce(impactPoint);
					_bounceCooldown = 0.1f; // Prevent rapid re-bouncing
					return; // Handle one bounce per frame
				}
				else
				{
					// Enemy hit
					AudioStreamPlayer3D.VolumeDb = initialDb;
					AudioStreamPlayer3D.Stream = AudioStream_FireHit;
					AudioStreamPlayer3D.Play();
					// The projectile will be destroyed by the enemy's hurtbox logic.
					return;
				}
			}
		}
	}

	public void BeginCharge(Node3D parent)
	{
		parent.AddChild(this);
		initialDb = AudioStreamPlayer3D.VolumeDb;
		this.Position = Vector3.Zero;
		UpdateChargeVisuals(0.1f); // Start at 10% size
	}

	public void UpdateChargeVisuals(float size)
	{
		if (_state != ProjectileState.Charging) return;

		if (_sprite != null)
		{
			_sprite.Scale = Vector3.One * size;
		}

		if (_collisionShape != null && _collisionShape.Shape is SphereShape3D sphere)
		{
			// Ensure the collision shape doesn't get too small
			sphere.Radius = Mathf.Max(0.05f, size * 0.5f);
		}
	}

	public void Launch(Node owner, float damage, float initialManaCost, Vector3 initialVelocity)
	{
		if (_state != ProjectileState.Charging) return;

		_state = ProjectileState.Fired;
		this.Damage = damage;
		this.InitialManaCost = initialManaCost;
		Reparent(owner);
		this.Owner = owner;

		// Enable physics and launch
		this.Freeze = false;
		_collisionShape.Disabled = false;
		this.LinearVelocity = initialVelocity;
		this.Mass = _sprite.Scale.X; // Set mass based on final size

		_lifetimeTimer.Start();
	}

	private void HandleWallBounce(Vector3 impactPoint)
	{
		float refundPercent = (float)GD.RandRange(_minRefundPercent, _maxRefundPercent);
		int manaToSpawn = Mathf.RoundToInt(InitialManaCost * refundPercent);

		if (manaToSpawn > 0)
		{
			ManaParticleManager.Instance.SpawnMana(manaToSpawn, impactPoint);
		}

		// Reduce size and damage
		this.Damage *= (1.0f - refundPercent);
		_sprite.Scale *= (1.0f - refundPercent);
		if (_collisionShape.Shape is SphereShape3D sphere)
		{
			sphere.Radius *= (1.0f - refundPercent);
		}

		this.Mass *= (1.0f - refundPercent);

		AudioStreamPlayer3D.VolumeDb *= (1.0f - refundPercent);
		AudioStreamPlayer3D.Stream = AudioStream_Fireball;
		AudioStreamPlayer3D.Play();

		// Destroy if too small
		if (_sprite.Scale.X < 0.1f)
		{
			QueueFree();
		}
	}
}
