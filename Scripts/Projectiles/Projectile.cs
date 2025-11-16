using Godot;
using System;
using Elythia;

public partial class Projectile : RigidBody3D
{
	private enum ProjectileState
	{
		Charging,
		Fired
	}

	[Export] private SpriteBase3D _sprite;
	[Export] private CollisionShape3D _collisionShape;
	[Export] public AudioStream AudioStream_Fireball { get; private set; }
	[Export] public AudioStream AudioStream_FireHit { get; private set; }


	[Export(PropertyHint.Range, "0.0, 1.0")]
	private float _minRefundPercent = 0.5f;

	[Export(PropertyHint.Range, "0.0, 1.0")]
	private float _maxRefundPercent = 0.75f;

	[Export(PropertyHint.Range, "0.1, 100.0")]
	private float _lifetime = 10f;

	[Export] private bool IsFixed { get; set; }

	public Node3D LevelParent { get; set; }
	public float Damage { get; private set; }
	public float ManaCost { get; private set; }
	public float Charge { get; set; }
	public float DamageGrowthConstant { get; private set; }
	public float AbsoluteMaxProjectileSpeed { get; private set; }
	public float MaxInitialManaCost { get; private set; }

	private ProjectileState _state = ProjectileState.Charging;
	private Node ProjectileOwner;
	private Timer _lifetimeTimer;
	private float _bounceCooldown = 0;
	private float _minManaThreshold = 1.0f;



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

	[Export] private float ManaLossPercentageOnEnemyHit = 0.1f;
	[Export] private float EnemyManaRefundFraction = 0.25f;


	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		if (_bounceCooldown > 0 || _state != ProjectileState.Fired)
		{
			return;
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
					return; // Handle one bounce per frame
				}
			}
		}
	}

	public void BeginCharge(Node3D parent)
	{
		parent.AddChild(this);
		this.Position = Vector3.Zero;
		Charge = 0;
		UpdateChargeState(); // Start at 10% size
	}

	public void UpdateChargeState()
	{
		float size = Mathf.Lerp(0.1f, 1.2f, Charge);

		if (IsFixed) return;

		// if (_state != ProjectileState.Charging) return;


		if (_sprite != null)
		{
			_sprite.Scale = Vector3.One * size;
		}

		if (_collisionShape is { Shape: SphereShape3D sphere })
		{
			// Ensure the collision shape doesn't get too small
			sphere.Radius = Mathf.Max(0.05f, size * 0.5f);
		}

		Mass = size;
	}

	public void Launch(Node3D caster, float damage, Vector3 initialVelocity)
	{
		ProjectileLaunchData launchData = new ProjectileLaunchData
		{
			Caster = caster,
			Damage = damage,
			ManaCost = 1.0f, // Fixed-damage projectiles have a nominal mana cost of 1
			InitialVelocity = initialVelocity,
			DamageGrowthConstant = 0.0f, // Indicates fixed damage, no scaling
			AbsoluteMaxProjectileSpeed = initialVelocity.Length(), // Use current speed as max for fixed projectiles
			MaxInitialManaCost = 1.0f // Nominal max initial mana cost for fixed-damage projectiles
		};
		Launch(launchData);
	}

	public void Launch(ProjectileLaunchData data)
	{
		if (_state != ProjectileState.Charging)
			return;

		_state = ProjectileState.Fired;
		this.Damage = data.Damage;
		this.ManaCost = data.ManaCost;
		this.Charge = data.ChargeRatio;
		this.DamageGrowthConstant = data.DamageGrowthConstant;
		this.AbsoluteMaxProjectileSpeed = data.AbsoluteMaxProjectileSpeed;
		this.MaxInitialManaCost = data.MaxInitialManaCost;
		UpdateChargeState();
		ProjectileOwner = data.Caster;
		
		// Ensure the projectile is removed from its current parent before adding to RoomManager
		if (GetParent() != null)
		{
			DebugManager.Debug($"Projectile: Launch - Removing from parent: {GetParent().Name}");
			GetParent().RemoveChild(this);
		}
		var parent = RoomManager.Instance;
		parent.AddChild(this);
		DebugManager.Debug($"Projectile: Launch - Added to parent: {parent.Name}");
		GlobalPosition = data.StartPosition;
		DebugManager.Debug($"Projectile: Launch - data.StartPosition: {data.StartPosition}, GlobalPosition after assignment: {GlobalPosition}");

		// Enable physics and launch
		this.Freeze = false;
		_collisionShape.Disabled = false;
		this.LinearVelocity = data.InitialVelocity;

		_lifetimeTimer.Start();
	}


	private static double LambertW0(double x)
	{
		if (x < 0)
		{
			throw new ArgumentException(
				"LambertW0 is not defined for negative x in this simple real-valued implementation.");
		}

		if (x == 0)
		{
			return 0;
		}

		// Initial guess (good approximation for large x)
		double w = Math.Log(x);
		if (x > 10)
		{
			w = Math.Log(x / Math.Log(x));
		}

		// Halley's method for refinement (usually 3-5 iterations suffice for machine precision)
		for (int i = 0; i < 10; i++)
		{
			double expW = Math.Exp(w);
			double wExpW = w * expW;
			double wPlusOne = w + 1;

			// Halley's method iteration formula
			double nextW = w - (wExpW - x) / (expW * wPlusOne - (w + 2) * (wExpW - x) / (2 * wPlusOne));

			if (Math.Abs(nextW - w) < 1e-15) // Check for convergence
			{
				return nextW;
			}

			w = nextW;
		}

		return w; // Return the result after max iterations
	}

	public void OnEnemyHit(Vector3 impactPoint)
	{
		float manaLostAmount = ManaCost * ManaLossPercentageOnEnemyHit;
		AudioManager.Instance.PlaySoundAtPosition(AudioStream_FireHit, impactPoint);
		ApplyManaLoss(manaLostAmount, impactPoint, true);
	}

	private void HandleWallBounce(Vector3 impactPoint)
	{
		// DebugManager.Instance.DEBUG.Info($"HWB: Wall Bounce! Current Mana: {ManaCost}");

		// If DamageGrowthConstant is 0, this is a fixed-damage projectile (e.g., enemy projectile).
		// It should not scale or eject mana, just expire.
		if (DamageGrowthConstant.IsZero())
		{
			Expire();
			return;
		}

		float reductionPercent = (float)GD.RandRange(_minRefundPercent, _maxRefundPercent);
		float velocityFactor = LinearVelocity.Length() / AbsoluteMaxProjectileSpeed;
		float manaLostAmount = ManaCost * reductionPercent; // * velocityFactor;

		AudioManager.Instance.PlaySoundAttachedToNode(AudioStream_Fireball, this);
		ApplyManaLoss(manaLostAmount, impactPoint, false);

		_bounceCooldown = 0.1f; // Prevent rapid re-bouncing
	}

	public void Expire()
	{
		Damage = 0;
		EjectMana(ManaCost, GlobalPosition);
		Reset(); // Reset state before queuing for free
		QueueFree();
	}

	public void ApplyManaLoss(float manaLostAmount, Vector3 impactPosition, bool isEnemyHit)
	{
		DebugManager.Debug(
			$"AML: ManaLostAmount: {manaLostAmount}, CurrentMana: {ManaCost}, IsEnemyHit: {isEnemyHit}");

		// Clamp manaLostAmount to current ManaCost to prevent negative mana
		manaLostAmount = Mathf.Min(ManaCost, manaLostAmount);

		// Eject mana particles
		float manaToEject = isEnemyHit ? manaLostAmount * EnemyManaRefundFraction : manaLostAmount;
		EjectMana(manaToEject, impactPosition);

		// Reduce projectile's mana
		ManaCost -= manaLostAmount;
		ManaCost = Mathf.Max(0, ManaCost); // Ensure ManaCost doesn't go below zero

		// If ManaCost drops below threshold, expire the projectile
		if (ManaCost < _minManaThreshold)
		{
			Expire();
			return;
		}

		// Recalculate Charge based on new ManaCost
		// MaxInitialManaCost is the ManaCost when Charge is 1
		Charge = ManaCost / MaxInitialManaCost;
		Charge = Mathf.Max(0, Charge); // Ensure Charge doesn't go below zero

		// Recalculate Damage based on new Charge
		Damage = ManaCost * Mathf.Pow(4, Charge * DamageGrowthConstant);

		// Update visual state
		UpdateChargeState();


		// Destroy if too small
		if (_sprite.Scale.X < 0.1f)
		{
			QueueFree();
		}
	}

	public void EjectMana(float amount, Vector3 spawnPoint)
	{
		// DebugManager.Debug($"EM: Amount received: {amount}");

		// float refundPercent = (float)GD.RandRange(_minRefundPercent, _maxRefundPercent);
		// DebugManager.Debug($"EM: RefundPercent: {refundPercent}");

		// float manaToFloor = amount * (1.0f - refundPercent);

		float manaToFloor = amount;
		int manaToSpawn = manaToFloor.FloorToInt();
		// DebugManager.Debug($"EM: Mana to floor: {manaToFloor}, Mana to spawn (raw): {manaToSpawn}");

		if (manaToSpawn <= 0 && amount > 0)
		{
			manaToSpawn = 1; // Ensure at least 1 mana is spawned if there was a loss
			// DebugManager.Debug($"EM: Mana to spawn adjusted to 1 (was 0, amount > 0)");
		}

		if (manaToSpawn > 0)
		{
			PickupManager.Instance.SpawnPickupAmount(PickupType.Mana, manaToSpawn, spawnPoint);
			// DebugManager.Debug($"EM: Spawning {manaToSpawn} mana particles.");
		}
		else
		{
			// DebugManager.Debug($"EM: No mana spawned (manaToSpawn <= 0).");
		}
	}

	public void Reset()
	{
		_state = ProjectileState.Charging;
		LinearVelocity = Vector3.Zero;
		AngularVelocity = Vector3.Zero;
		Freeze = true;
		_collisionShape.Disabled = true;
		Charge = 0;
		_lifetimeTimer.Stop();
		GlobalPosition = Vector3.Zero; // Reset position to a default
		// Reset any other relevant properties
	}
}