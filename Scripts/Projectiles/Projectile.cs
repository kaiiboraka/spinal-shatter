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
	[Export] public AudioStreamPlayer3D AudioStreamPlayer3D { get; private set; }
	[Export] public AudioStream AudioStream_Fireball { get; private set; }
	[Export] public AudioStream AudioStream_FireHit { get; private set; }

	[Export(PropertyHint.Range, "0.0, 1.0")]
	private float _minRefundPercent = 0.5f;

	[Export(PropertyHint.Range, "0.0, 1.0")]
	private float _maxRefundPercent = 0.75f;

	[Export(PropertyHint.Range, "0.1, 100.0")]
	private float _lifetime = 10f;

	public Node3D LevelParent { get; set; }
	public float Damage { get; private set; }
	public float ManaCost { get; private set; }
	public float Charge { get; set; }
	public float DamageGrowthConstant { get; private set; }
	public float AbsoluteMaxProjectileSpeed { get; private set; }

	private ProjectileState _state = ProjectileState.Charging;
	private Node ProjectileOwner;
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
			if (state.GetContactColliderObject(i) is Node collider)
			{
				if (collider == ProjectileOwner || ProjectileOwner.IsAncestorOf(collider)) return;
				if (collider.IsInGroup("Enemies"))
				{
					// Enemy hit
					AudioStreamPlayer3D.VolumeDb = initialDb;
					AudioStreamPlayer3D.Stream = AudioStream_FireHit;
					AudioStreamPlayer3D.Play();

					// The projectile will be destroyed by the enemy's hurtbox logic.
				}
				else
				{
					// Wall bounce
					Vector3 impactPoint = state.GetContactColliderPosition(i);
					HandleWallBounce(impactPoint);
					_bounceCooldown = 0.1f; // Prevent rapid re-bouncing
					return; // Handle one bounce per frame
				}
			}
		}
	}

	public void BeginCharge(Node3D parent)
	{
		parent.AddChild(this);
		initialDb = AudioStreamPlayer3D.VolumeDb;
		this.Position = Vector3.Zero;
		Charge = 0;
		UpdateChargeState(); // Start at 10% size
	}

	public void UpdateChargeState()
	{
		float size = Mathf.Lerp(0.1f, 1.2f, Charge);
		if (_state != ProjectileState.Charging) return;

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

	public void Launch(Node owner, Vector3 velocity, ProjectileLaunchData data)
	{
		if (_state != ProjectileState.Charging)
			return;

		_state = ProjectileState.Fired;
		this.Damage = data.Damage;
		this.ManaCost = data.ManaCost;
		this.Charge = data.ChargeRatio;
		this.DamageGrowthConstant = data.DamageGrowthConstant;
		this.AbsoluteMaxProjectileSpeed = data.AbsoluteMaxProjectileSpeed;
		UpdateChargeState();
		ProjectileOwner = owner;
		Reparent(owner.Owner);
		Owner = owner.Owner;

		// Enable physics and launch
		this.Freeze = false;
		_collisionShape.Disabled = false;
		this.LinearVelocity = velocity;

		_lifetimeTimer.Start();
	}

	public void Launch(Node owner, float damage, Vector3 velocity)
	{
		// Construct ProjectileLaunchData for fixed-damage projectiles
		ProjectileLaunchData fixedDamageData = new ProjectileLaunchData
		{
			Damage = damage,
			ManaCost = 0, // No mana cost for fixed-damage projectiles
			ChargeRatio = 1, // Considered fully charged for visual consistency
			DamageGrowthConstant = 0, // Indicates no scaling
			AbsoluteMaxProjectileSpeed = velocity.Length() // Use actual launch speed
		};
		Launch(owner, velocity, fixedDamageData); // Call the full Launch method
	}

	public void HandleImpact(float impactDamage)
	{
		DebugManager.Debug($"HI: ImpactDmg: {impactDamage}, CurrentDmg: {Damage}, CurrentMana: {ManaCost}, CurrentCharge: {Charge}");

		// If DamageGrowthConstant is 0, this is a fixed-damage projectile (e.g., enemy projectile).
		// It should not scale or eject mana, just expire.
		if (DamageGrowthConstant.IsZero())
		{
			Expire();
			return;
		}

		if (impactDamage >= Damage)
		{
			Expire();
			return;
		}

		float new_dmg = Damage - impactDamage;
		DebugManager.Debug($"HI: NewDmg: {new_dmg}");

		// Calculate MaxInitialManaCost from current state
		// This assumes ManaCost = Charge * MaxInitialManaCost
		if (Charge.IsZero())
		{
			Expire(); // If charge is zero, it has no damage/mana left
			return;
		}
		float maxInitialManaCost = ManaCost / Charge;
		DebugManager.Debug($"HI: MaxInitManaCost: {maxInitialManaCost}");

		float A = DamageGrowthConstant * Mathf.Log(4);
		DebugManager.Debug($"HI: DmgGrowthConst: {DamageGrowthConstant}, A: {A}");

		// Calculate the argument for LambertW0
		float lambertArg = new_dmg * A / maxInitialManaCost;
		DebugManager.Debug($"HI: LambertArg: {lambertArg}");

		// LambertW0 is defined for x >= -1/e. For our use case, new_dmg should be positive.
		// If new_dmg is very small or zero, lambertArg could be zero or negative.
		if (lambertArg < -0.36787944117f) // -1/e approx
		{
			Expire(); // Effectively no damage left
			return;
		}

		float new_charge = (float)LambertW0(lambertArg) / A;
		DebugManager.Debug($"HI: NewCharge (raw): {new_charge}");

		// Ensure new_charge is not negative or extremely small due to floating point inaccuracies
		new_charge = Mathf.Max(0, new_charge);

		// Ensure new_charge does not exceed current charge (should be handled by new_dmg < Damage check, but for safety)
		new_charge = Mathf.Min(Charge, new_charge);
		DebugManager.Debug($"HI: NewCharge (clamped): {new_charge}");

		// Calculate new_mana based on the linear relationship
		float new_mana = new_charge * maxInitialManaCost;
		DebugManager.Debug($"HI: NewMana: {new_mana}");

		DebugManager.Trace($"IMPACT:{impactDamage} N_DMG:{new_dmg} N_CRG:{new_charge} D_CRG:{Charge-new_charge} N_M:{new_mana} D_M:{ManaCost-new_mana}");

		// Eject mana, update properties, and visual state
		float manaLost = ManaCost - new_mana;
		DebugManager.Debug($"HI: ManaLost (before eject): {manaLost}");
		EjectMana(manaLost);
		Damage = new_dmg;
		ManaCost = new_mana;
		Charge = new_charge;
		UpdateChargeState();

		AudioStreamPlayer3D.VolumeDb *= 1.0f - Charge;
		AudioStreamPlayer3D.Stream = AudioStream_Fireball;
		AudioStreamPlayer3D.Play();

		// Destroy if too small
		if (_sprite.Scale.X < 0.1f)
		{
			QueueFree();
		}
	}
	private static double LambertW0(double x)
	{
		if (x < 0)
		{
			throw new ArgumentException("LambertW0 is not defined for negative x in this simple real-valued implementation.");
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

	public void Expire()
	{
		Damage = 0;
		EjectMana(ManaCost);
		QueueFree();
	}

	public void EjectMana(float amount)
	{
		DebugManager.Debug($"EM: Amount received: {amount}");
		float refundPercent = (float)GD.RandRange(_minRefundPercent, _maxRefundPercent);
		DebugManager.Debug($"EM: RefundPercent: {refundPercent}");

		float manaToFloor = amount * (1.0f - refundPercent);
		int manaToSpawn = manaToFloor.FloorToInt();
		DebugManager.Debug($"EM: Mana to floor: {manaToFloor}, Mana to spawn (raw): {manaToSpawn}");

		if (manaToSpawn <= 0 && amount > 0)
		{
			manaToSpawn = 1; // Ensure at least 1 mana is spawned if there was a loss
			DebugManager.Debug($"EM: Mana to spawn adjusted to 1 (was 0, amount > 0)");
		}

		if (manaToSpawn > 0)
		{
			ManaParticleManager.Instance.SpawnMana(manaToSpawn, GlobalPosition);
			DebugManager.Debug($"EM: Spawning {manaToSpawn} mana particles.");
		}
		else
		{
			DebugManager.Debug($"EM: No mana spawned (manaToSpawn <= 0).");
		}
	}

	private void HandleWallBounce(Vector3 impactPoint)
	{
		DebugManager.Trace($"HandleWallBounce: LinearVelocity: {LinearVelocity}, ");
		float reductionFactor = (float)GD.RandRange(_minRefundPercent, _maxRefundPercent);
		float velocityFactor = LinearVelocity.Length() / AbsoluteMaxProjectileSpeed;
		float actualImpactDamage = Damage * reductionFactor * velocityFactor;
		DebugManager.Debug($"HW: Reduct: {reductionFactor}, VelFact: {velocityFactor}, ActImpDmg: {actualImpactDamage}");
		HandleImpact(actualImpactDamage);
	}
}