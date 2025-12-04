using Godot;
using System;
using Elythia;

namespace SpinalShatter;

public partial class Projectile : RigidBody3D
{
	public enum ChargeEffectType
	{
		ScaleSizeAndDamage,
		// Future types
		// IncreaseBounces,
		// IncreaseLockOnTargets
	}

	private enum ProjectileState
	{
		Charging,
		Fired
	}

	[Export] public ChargeEffectType ChargeEffect { get; private set; }
	[Export] private PackedScene _sparkParticlesScene;
	private SpriteBase3D sprite;
	private CollisionShape3D collisionShape;

	[Export] public AudioData AudioData { get; private set; }
	private AudioStreamPlayer3D audioStreamPlayer;

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

	private GpuParticles3D trail;

	public override void _Ready()
	{
		sprite ??= GetNode<SpriteBase3D>("Sprite3D");
		collisionShape ??= GetNode<CollisionShape3D>("CollisionShape3D");
		audioStreamPlayer ??= GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");
		trail ??= GetNode<GpuParticles3D>("GPUTrail3D");

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
		trail.Visible = false;
		this.SizingScale = sizeScale;
		this.Position = Vector3.Zero;
		Charge = 0;
		UpdateChargeState(); // Start at 10% size
	}

	public void UpdateChargeState()
	{
		if (IsFixed) return;

		switch (ChargeEffect)
		{
			case ChargeEffectType.ScaleSizeAndDamage:
				float size = Mathf.Lerp(SizingScale.Min, SizingScale.Max, Charge);
				if (sprite != null)
				{
					sprite.Scale = Vector3.One * size;
				}
				if (collisionShape is { Shape: SphereShape3D sphere })
				{
					sphere.Radius = Mathf.Max(0.05f, size * 0.5f);
				}
				Mass = size;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
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
		trail.Visible = true;
		UpdateChargeState();
		ProjectileOwner = data.Caster;
		
		// Ensure the projectile is removed from its current parent before adding to RoomManager
		if (GetParent() != null)
		{
			GetParent().RemoveChild(this);
		}
		var parent = RoomManager.Instance;
		parent.AddChild(this);
		Marker3D SpellMarker = data.StartPosition;

		Vector3 markerPosition = SpellMarker.Position;
		SpellMarker.Position = SpellMarker.Position with {X = 0};

		GlobalPosition = SpellMarker.GlobalPosition;

		SpellMarker.Position = markerPosition;

		this.Freeze = false;
		collisionShape.Disabled = false;
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

		double w = Math.Log(x);
		if (x > 10)
		{
			w = Math.Log(x / Math.Log(x));
		}

		for (int i = 0; i < 10; i++)
		{
			double expW = Math.Exp(w);
			double wExpW = w * expW;
			double wPlusOne = w + 1;

			double nextW = w - (wExpW - x) / (expW * wPlusOne - (w + 2) * (wExpW - x) / (2 * wPlusOne));

			if (Math.Abs(nextW - w) < 1e-15) 
			{
				return nextW;
			}

			w = nextW;
		}

		return w;
	}

	public void OnEnemyHit()
	{
		ApplyManaLoss(ManaLostAmount, GlobalPosition);
		AudioManager.PlayAtPosition((AudioFile)AudioData["Hit"], GlobalPosition);
		AudioManager.Play(audioStreamPlayer, (AudioFile)AudioData["Hit"]);
	}

	private void HandleWallBounce(Vector3 impactPoint)
	{
		if (DamageGrowthConstant.IsZero())
		{
			Expire();
			return;
		}

		float velocityFactor = LinearVelocity.Length() / AbsoluteMaxProjectileSpeed;

		AudioManager.Play(audioStreamPlayer, (AudioFile)AudioData["Bounce"]);
		ApplyManaLoss(ManaLostAmount, impactPoint);

		bounceCooldown = 0.1f; 
	}

	public void Expire()
	{
		Damage = 0;

		double seconds = trail.Lifetime / trail.FixedFps;
		SceneTreeTimer clearTimer = GetTree().CreateTimer(seconds);
		clearTimer.Timeout += () =>
		{
			trail.QueueFree();
		};
		trail.Reparent(GetParent());

		EjectMana(ManaCost, GlobalPosition);
		Reset(); 
		QueueFree();
	}

	public void ApplyManaLoss(float manaLostAmount, Vector3 impactPosition)
	{
		if (float.IsNaN(manaLostAmount) || float.IsNaN(ManaCost))
		{
			Expire();
			return;
		}
		
		manaLostAmount = Mathf.Min(ManaCost, manaLostAmount);
		EjectMana(manaLostAmount, impactPosition);
		ManaCost -= manaLostAmount;
		ManaCost = Mathf.Max(0, ManaCost);

		if (ManaCost < minManaThreshold)
		{
			Expire();
			return;
		}

		Charge = ManaCost / MaxInitialManaCost;
		Charge = Mathf.Max(0, Charge);

		Damage = ManaCost * Mathf.Pow(4, Charge * DamageGrowthConstant);

		UpdateChargeState();


		if (sprite.Scale.X < 0.1f)
		{
			QueueFree();
		}
	}

	public void EjectMana(float amount, Vector3 spawnPoint)
	{
		float manaToFloor = amount;
		int manaToSpawn = manaToFloor.FloorToInt();

		if (manaToSpawn <= 0 && amount > 0)
		{
			manaToSpawn = 1;
		}

		if (manaToSpawn > 0)
		{
			PickupManager.Instance.SpawnPickupAmount(PickupType.Mana, manaToSpawn, spawnPoint);
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
		Charge = 0;
		lifetimeTimer.Stop();
		GlobalPosition = Vector3.Zero;
	}
}