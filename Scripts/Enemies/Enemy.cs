using Elythia;
using Godot;

public partial class Enemy : CharacterBody3D
{
	[Export] private SpriteBase3D _sprite;
	[Export] private Area3D _hurtbox;
	[Export] private HealthComponent HealthComponent { get; set; }
	[Export] private HealthBar HealthBar { get; set; }

	[Export] public float WalkSpeed { get; private set; } = 3.0f;
	[Export] public int ManaToDrop { get; private set; } = 10;

	[Export(PropertyHint.Range, "0.0, 1.0")]
	private float _minRefundPercent = 0.05f;

	[Export(PropertyHint.Range, "0.0, 1.0")]
	private float _maxRefundPercent = 0.30f;

	[Signal]
	public delegate void EnemyDiedEventHandler(Enemy who);

	public ObjectPoolManager<Node3D> OwningPool { get; set; }

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _Ready()
	{
		HealthComponent ??= GetNode<HealthComponent>("%HealthComponent");
		HealthBar ??= GetNode<HealthBar>("%HealthBar");

		HealthComponent.Died += OnDied;
		HealthComponent.HealthChanged += OnHealthChanged;

		_hurtbox.BodyEntered += OnHurtboxBodyEntered;

		// Initialize Health Bar
		HealthBar.Initialize(HealthComponent.MaxHealth);
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add gravity.
		if (!IsOnFloor())
			velocity.Y -= _gravity * (float)delta;

		// Set horizontal velocity to move forward.
		velocity.X = -GlobalTransform.Basis.Z.X * WalkSpeed;
		velocity.Z = -GlobalTransform.Basis.Z.Z * WalkSpeed;

		Velocity = velocity;
		MoveAndSlide();

		// Check for wall collision and change direction.
		if (IsOnWall())
		{
			float randomAngle = (float)GD.RandRange(0, Mathf.Pi * 2);
			Rotation = new Vector3(0, randomAngle, 0);
		}
	}

	private void OnHurtboxBodyEntered(Node3D body)
	{
		if (body is Projectile projectile)
		{
			// Take damage from the projectile
			TakeDamage(projectile.Damage);

			// Spawn mana particles as a refund
			float refundPercent = (float)GD.RandRange(_minRefundPercent, _maxRefundPercent);
			int manaToSpawn = Mathf.RoundToInt(projectile.InitialManaCost * refundPercent);
			if (manaToSpawn > 0)
			{
				ManaParticleManager.Instance.SpawnMana(manaToSpawn, projectile.GlobalPosition);
			}

			// Destroy the projectile
			projectile.QueueFree();
		}
	}

	private void OnHealthChanged(float oldHealth, float newHealth)
	{
		HealthBar.OnHealthChanged(newHealth, HealthComponent.MaxHealth);
	}

	public void TakeDamage(float amount)
	{
		HealthComponent.TakeDamage(amount);
		FlashRed();
	}

	private void FlashRed()
	{
		var tween = GetTree().CreateTween();
		tween.TweenProperty(_sprite, "modulate", Colors.Red, 0.1);
		tween.TweenProperty(_sprite, "modulate", Colors.White, 0.1);
	}

	private void OnDied()
	{
		EmitSignal(SignalName.EnemyDied, this);

		ManaParticleManager.Instance.SpawnMana(ManaToDrop, this.GlobalPosition);

		if (OwningPool != null)
		{
			OwningPool.Release(this);
		}
		else
		{
			QueueFree(); // Failsafe for enemies not spawned from a pool
		}
	}

	public void Reset()
	{
		HealthComponent.Reset();
	}
}