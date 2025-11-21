using Godot;
using System;
using Elythia;

namespace SpinalShatter;

public partial class Projectile : RigidBody3D
{
	private enum ProjectileState
	{
		Charging,
		Fired
	}

	[Export] private PackedScene _sparkParticlesScene;
	private SpriteBase3D sprite;
	private CollisionShape3D collisionShape;

	[Export] public AudioData AudioData { get; private set; }
	private AudioStreamPlayer3D audioStreamPlayer;
	// [Export] public AudioStream AudioStream_FireHit { get; private set; }

	[Export(PropertyHint.Range, "0.1, 100.0")]
	private float _lifetime = 10f;
	[Export] IntValueRange manaDroppedAmount = new IntValueRange(1, 5);
	[Export] private bool IsFixed { get; set; }

	public Node3D LevelParent { get; set; }
	public float Damage { get; private set; }
	public float ManaCost { get; private set; }
	public float Charge { get; set; }
	public float DamageGrowthConstant { get; private set; }
	public float AbsoluteMaxProjectileSpeed { get; private set; }
	public float MaxInitialManaCost { get; private set; }
	public FloatValueRange SizingScale { get; private set; }

	private ProjectileState state = ProjectileState.Charging;
	private Node ProjectileOwner;
	private Timer lifetimeTimer;
	private float bounceCooldown = 0;
	private float minManaThreshold = 1.0f;

	int ManaLostAmount => (int)Mathf.Min(manaDroppedAmount.GetRandomValue(), ManaCost);

	public override void _Ready()
	{
		sprite ??= GetNode<SpriteBase3D>("Sprite3D");
		collisionShape ??= GetNode<CollisionShape3D>("CollisionShape3D");
		audioStreamPlayer ??= GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");

		lifetimeTimer = new Timer();
		lifetimeTimer.WaitTime = _lifetime;
		lifetimeTimer.OneShot = true;
		lifetimeTimer.Timeout += () => QueueFree();
		AddChild(lifetimeTimer);

		// Disable physics until launched
		this.Freeze = true;
		collisionShape.Disabled = true;

		ContactMonitor = true;
		MaxContactsReported = 4;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (bounceCooldown > 0)
		{
			bounceCooldown -= (float)delta;
		}
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		if (bounceCooldown > 0 || this.state != ProjectileState.Fired)
		{
			return;
		}

		for (int i = 0; i < state.GetContactCount(); i++)
		{
			Node collider = state.GetContactColliderObject(i) as Node;
			if (collider != null)
			{
				bool hitProjectile = collider.IsInGroup("Projectile");
				var contactLocalNormal = state.GetContactLocalNormal(i);
				var contactLocalPosition = state.GetContactColliderPosition(i);

				if (_sparkParticlesScene.Instantiate() is OneshotParticles sparkParticles)
				{
					GetTree().Root.AddChild(sparkParticles);
					sparkParticles.GlobalPosition = GlobalPosition;
					sparkParticles.LookAt(contactLocalNormal);
					int particleCount = hitProjectile ? 20 : (int)(ManaCost * 3);
					sparkParticles.PlayParticles(particleCount);
				}

				if (!collider.IsInGroup("Enemies"))
				{
					// Wall bounce
					Vector3 impactPoint = contactLocalPosition.MoveToward(contactLocalNormal, contactLocalNormal.Length());
					HandleWallBounce(impactPoint);
					return; // Handle one bounce per frame
				}

				if (hitProjectile)
				{
					int more = 0;
					if  (collider is Projectile other) more = Mathf.Max(other.ManaCost.CeilingToInt(), other.Damage.CeilingToInt());
					EjectMana(more + ManaCost.CeilingToInt(), contactLocalPosition);
				}

			}
		}
	}

	public void BeginChargingProjectile(Node3D parent, FloatValueRange sizeScale)
	{
		parent.AddChild(this);
		this.SizingScale = sizeScale;
		this.Position = Vector3.Zero;
		Charge = 0;
		UpdateChargeState(); // Start at 10% size
	}

	public void UpdateChargeState()
	{
		float size = Mathf.Lerp(SizingScale.Min, SizingScale.Max, Charge);

		if (IsFixed) return;
		if (sprite != null)
		{
			sprite.Scale = Vector3.One * size;
		}

		if (collisionShape is { Shape: SphereShape3D sphere })
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
			MaxInitialManaCost = 1.0f, // Nominal max initial mana cost for fixed-damage projectiles
			SizingScale = new FloatValueRange(1)
		};
		Launch(launchData);
	}

	public void Launch(ProjectileLaunchData data)
	{
		if (state != ProjectileState.Charging)
			return;

		state = ProjectileState.Fired;
		this.Damage = data.Damage;
		this.ManaCost = data.ManaCost;
		this.Charge = data.ChargeRatio;
		this.DamageGrowthConstant = data.DamageGrowthConstant;
		this.AbsoluteMaxProjectileSpeed = data.AbsoluteMaxProjectileSpeed;
		this.MaxInitialManaCost = data.MaxInitialManaCost;
		this.SizingScale = data.SizingScale;
		UpdateChargeState();
		ProjectileOwner = data.Caster;
		
		// Ensure the projectile is removed from its current parent before adding to RoomManager
		if (GetParent() != null)
		{
			// DebugManager.Debug($"Projectile: Launch - Removing from parent: {GetParent().Name}");
			GetParent().RemoveChild(this);
		}
		var parent = RoomManager.Instance;
		parent.AddChild(this);
		Marker3D SpellMarker = data.StartPosition;

		// OPTIONAL: Fire at center?
		Vector3 markerPosition = SpellMarker.Position;
		SpellMarker.Position = SpellMarker.Position with {X = 0};

		GlobalPosition = SpellMarker.GlobalPosition;

		SpellMarker.Position = markerPosition;

		// Enable physics and launch
		this.Freeze = false;
		collisionShape.Disabled = false;
		// DebugManager.Debug($"Projectile: Launch - Freeze: {this.Freeze}, CollisionShape.Disabled: {_collisionShape.Disabled}");
		this.LinearVelocity = data.InitialVelocity;

		lifetimeTimer.Start();
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

	public void OnEnemyHit()
	{
		ApplyManaLoss(ManaLostAmount, GlobalPosition);
		AudioManager.PlayAtPosition((AudioFile)AudioData["Hit"], GlobalPosition);
		AudioManager.Play(audioStreamPlayer, (AudioFile)AudioData["Hit"]);
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

		float velocityFactor = LinearVelocity.Length() / AbsoluteMaxProjectileSpeed;

		AudioManager.Play(audioStreamPlayer, (AudioFile)AudioData["Bounce"]);
		// DebugManager.Trace($"projectile impact point: {impactPoint}");
		ApplyManaLoss(ManaLostAmount, impactPoint);

		bounceCooldown = 0.1f; // Prevent rapid re-bouncing
	}

	public void Expire()
	{
		Damage = 0;
		EjectMana(ManaCost, GlobalPosition);
		Reset(); // Reset state before queuing for free
		QueueFree();
	}

	public void ApplyManaLoss(float manaLostAmount, Vector3 impactPosition)
	{
		if (float.IsNaN(manaLostAmount))
		{
			Expire();
			return;
		}
		
		if (float.IsNaN(ManaCost))
		{
			Expire();
			return;
		}
		
		// DebugManager.Debug(
		// 	$"AML: ManaLostAmount: {manaLostAmount}, CurrentMana: {ManaCost}");

		// Clamp manaLostAmount to current ManaCost to prevent negative mana
		manaLostAmount = Mathf.Min(ManaCost, manaLostAmount);

		// Eject mana particles
		// float manaToEject = isEnemyHit ? manaLostAmount * EnemyManaRefundFraction : manaLostAmount;
		EjectMana(manaLostAmount, impactPosition);

		// Reduce projectile's mana
		ManaCost -= manaLostAmount;
		ManaCost = Mathf.Max(0, ManaCost); // Ensure ManaCost doesn't go below zero

		// If ManaCost drops below threshold, expire the projectile
		if (ManaCost < minManaThreshold)
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
		if (sprite.Scale.X < 0.1f)
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

	public void Modulate(Color newColor)
	{
		sprite.Modulate = newColor;
	}

	public void Reset()
	{
		state = ProjectileState.Charging;
		LinearVelocity = Vector3.Zero;
		AngularVelocity = Vector3.Zero;
		Freeze = true;
		collisionShape.Disabled = true;
		// DebugManager.Debug($"Projectile: Reset - Freeze: {this.Freeze}, CollisionShape.Disabled: {_collisionShape.Disabled}");
		Charge = 0;
		lifetimeTimer.Stop();
		GlobalPosition = Vector3.Zero; // Reset position to a default
		// Reset any other relevant properties
	}
}