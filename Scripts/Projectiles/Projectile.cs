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
	private float _minRefundPercent = 0.1f;

	[Export(PropertyHint.Range, "0.0, 1.0")]
	private float _maxRefundPercent = 0.25f;

	[Export(PropertyHint.Range, "0.1, 100.0")]
	private float _lifetime = 10f;

	public Node3D LevelParent { get; set; }
	public float Damage { get; private set; }
	public float ManaCost { get; private set; }
	public float Charge { get; set; }

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

	public void Launch(Node owner, float damage, float manaCost, Vector3 velocity, float chargeRatio)
	{
		if (_state != ProjectileState.Charging)
			return;

		_state = ProjectileState.Fired;
		this.Damage = damage;
		this.ManaCost = manaCost;
		this.Charge = chargeRatio;
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

	public void HandleImpact(float impactDamage)
	{
		if (impactDamage >= Damage)
		{
			Expire();
			return;
		}

		// Damage -= impactDamage;
		// ManaCost;
		// Damage is Expo;
		// Mana is Linear;
		// dmg = (base ^ charge)*mana;
		// dmg` = dmg-impactDamage;
		// dmg` = (base ^ charge`)*mana`
		// log_base(dmg/mana) = charge;
		// charge` = ?
		// x = mana/charge
		// mana`=charge`*x
		// charge = lb(dmg) - lb(charge*X)));
		// c = log_base(d) - lb(c)+lb(X);
		// rCh= ch/ch` = ln(dmg)-ln(mana)/ln(dmg`)-ln(mana`)
		// rCh= (A/x*mana`)ln(Z)== ln(Y)-ln(X)-(A/x*mana`)ln(mana`)
		// 40->20 DMG -> much smaller mana change
		// int manaToSpawn = Mathf.RoundToInt(ManaCost * refundPercent);
		float T = Mathf.Log(Damage / ManaCost) / (Mathf.Log(4) * Charge);
		// D = M * 4^CT;
		// X = (min / max);
		// M = XC + min
		// D` = D - I;
		// D` = M` * 4^C`T;
		// M` = XC` + min
		var MaxManaCost = 50;
		// binary search for D`:
		var f = (float C) => Mathf.Lerp(0, MaxManaCost, C) * Mathf.Pow(4,C * T);
		float new_dmg = Damage - impactDamage;
		float new_charge = .5f;
		while (true)
		{
			float approx = f(new_charge);
			if (approx.FloatEqualsApprox(new_dmg, 10)) break;
			if (approx > new_dmg) new_charge *= .5f;
			else new_charge *= 1.5f;
		}
		DebugManager.Trace($"C: {new_charge}");
		float new_mana = new_charge * (ManaCost / Charge);

		DebugManager.Trace($"IMPACT:{impactDamage} N_DMG:{new_dmg} N_CRG:{new_charge} D_CRG:{Charge-new_charge} N_M:{new_mana} D_M:{ManaCost-new_charge}");

		// 40->20 DMG -> much smaller mana change as dmg is 4^x mana is linear
		EjectMana(ManaCost -  new_mana);
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
		float refundPercent = (float)GD.RandRange(_minRefundPercent, _maxRefundPercent);

		ManaParticleManager.Instance.SpawnMana((amount * (1.0f - refundPercent)).FloorToInt(), GlobalPosition);
	}

	private void HandleWallBounce(Vector3 impactPoint)
	{
		DebugManager.Trace($"HandleWallBounce: LinearVelocity: {LinearVelocity}, ");
		HandleImpact(Damage * (LinearVelocity.Length()/100));
	}
}