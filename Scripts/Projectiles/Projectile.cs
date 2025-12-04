namespace SpinalShatter;

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

	[Export] private PackedScene _sparkParticlesScene;
	private SpriteBase3D sprite;
	private CollisionShape3D collisionShape;

	[Export] public AudioData AudioData { get; private set; }
	private AudioStreamPlayer3D audioStreamPlayer;

	[Export(PropertyHint.Range, "0.1, 100.0")]
	private float _lifetime = 10f;

	[Export] private bool IsFixed { get; set; }

	public SpellData SpellData { get; private set; }
	public Node Caster { get; private set; }
	public SlotType Slot { get; private set; }
	public float CurrentMana { get; private set; }
	public float CurrentCharge { get; private set; }
	public float CurrentDamage { get; private set; } // Final calculated damage


	private ProjectileState state = ProjectileState.Charging;
	private Timer lifetimeTimer;
	private float bounceCooldown = 0;
	private GpuParticles3D trail;

	public override void _Ready()
	{
		sprite ??= GetNode<SpriteBase3D>("Sprite3D");
		collisionShape ??= GetNode<CollisionShape3D>("CollisionShape3D");
		audioStreamPlayer ??= GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");
		trail ??= GetNode<GpuParticles3D>("%GPUTrail3D");

		lifetimeTimer = new Timer();
		lifetimeTimer.WaitTime = _lifetime;
		lifetimeTimer.OneShot = true;
		lifetimeTimer.Timeout += () => QueueFree();
		AddChild(lifetimeTimer);

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
					int particleCount = hitProjectile ? 20 : (int)(CurrentMana * 3);
					sparkParticles.PlayParticles(particleCount);
				}

				if (!collider.IsInGroup("Enemies"))
				{
					Vector3 impactPoint =
						contactLocalPosition.MoveToward(contactLocalNormal, contactLocalNormal.Length());
					HandleWallBounce(impactPoint);
					return;
				}

				if (hitProjectile)
				{
					int more = 0;
					if (collider is Projectile other)
						more = Mathf.Max(other.CurrentMana.CeilingToInt(), other.CurrentDamage.CeilingToInt());
					EjectMana(more + CurrentMana.CeilingToInt(), contactLocalPosition);
				}
			}
		}
	}

	public void BeginChargingProjectile(Node3D parent, SpellData spellData)
	{
		parent.AddChild(this);
		if (trail != null) trail.Visible = false;
		this.SpellData = spellData;
		this.Position = Vector3.Zero;
		this.CurrentCharge = 0;
		UpdateChargeState();
	}

	public void UpdateChargeState()
	{
		if (IsFixed) return;

		var sizingScale = SpellData.SizeRange;
		if (sizingScale != null)
		{
			float scaledSize = Mathf.Lerp(sizingScale.Min, sizingScale.Max, CurrentCharge);
			if (sprite != null)
			{
				sprite.Scale = Vector3.One * scaledSize;
			}

			if (collisionShape is { Shape: SphereShape3D sphere })
			{
				sphere.Radius = Mathf.Max(0.05f, scaledSize * 0.5f);
			}

			Mass = scaledSize;
		}
	}

	public void Launch(ProjectileLaunchData data)
	{
		if (this.state != ProjectileState.Charging)
			return;

		state = ProjectileState.Fired;
		Caster = data.Caster;
		SpellData = data.SpellData;

		CurrentMana = data.ManaCost;
		CurrentCharge = data.ChargeRatio;

		Slot = data.Slot;
		CurrentDamage = Mathf.Lerp(SpellData.DamageRange.Min, SpellData.DamageRange.Max, CurrentCharge);

		UpdateChargeState();

		switch (SpellData.Weapon)
		{
			case WeaponType.Orb:
				// No further modifications needed, base Lerp is enough for Orb.
				break;
			default:
				break;
		}

		if (GetParent() != null)
		{
			GetParent().RemoveChild(this);
		}

		var parent = RoomManager.Instance;
		parent.AddChild(this);
		Marker3D SpellMarker = data.StartPosition;

		Vector3 markerPosition = SpellMarker.Position;
		SpellMarker.Position = SpellMarker.Position with { X = 0 };

		GlobalPosition = SpellMarker.GlobalPosition;

		SpellMarker.Position = markerPosition;

		this.Freeze = false;
		collisionShape.Disabled = false;
		this.LinearVelocity = data.InitialVelocity;

		lifetimeTimer.Start();
		if (trail != null) trail.Visible = true;
	}


	public void UpdateChargeAmount(float charge)
	{
		CurrentCharge = charge;
		UpdateChargeState();
	}

	public void OnEnemyHit()
	{
		ApplyManaLoss(GlobalPosition);
		AudioManager.PlayAtPosition((AudioFile)AudioData["Hit"], GlobalPosition);
		AudioManager.Play(audioStreamPlayer, (AudioFile)AudioData["Hit"]);
	}

	private void HandleWallBounce(Vector3 impactPoint)
	{
		if (IsFixed)
		{
			Expire();
			return;
		}

		AudioManager.Play(audioStreamPlayer, (AudioFile)AudioData["Bounce"]);
		ApplyManaLoss(impactPoint);

		bounceCooldown = 0.1f;
	}

	public void Expire()
	{
		if (trail != null)
		{
			double seconds = trail.Lifetime / trail.FixedFps;
			SceneTreeTimer clearTimer = GetTree().CreateTimer(seconds);
			clearTimer.Timeout += () =>
			{
				if (IsInstanceValid(trail)) trail.QueueFree();
			};
			trail.Reparent(GetParent());
		}

		EjectMana(CurrentMana, GlobalPosition);
		Reset();
		QueueFree();
	}

	public void ApplyManaLoss(Vector3 impactPosition)
	{
		float manaLostAmount = (int)Mathf.Min(SpellData.ManaDroppedAmount.GetRandomValue(), CurrentMana);
		if (float.IsNaN(manaLostAmount) || float.IsNaN(CurrentMana))
		{
			Expire();
			return;
		}

		manaLostAmount = Mathf.Min(CurrentMana, manaLostAmount);
		EjectMana(manaLostAmount, impactPosition);
		CurrentMana -= manaLostAmount;
		CurrentMana = Mathf.Max(0, CurrentMana);

		if (CurrentMana < 1)
		{
			Expire();
			return;
		}

		CurrentCharge = CurrentMana / SpellData.ManaCostRange.Max;
		CurrentCharge = Mathf.Max(0, CurrentCharge);
		// CurrentDamage = CurrentMana * Mathf.Pow(4, CurrentCharge * SpellData.MaxChargeTime);
		CurrentDamage = Mathf.Lerp(CurrentMana, 0, CurrentCharge);

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
		lifetimeTimer.Stop();
		GlobalPosition = Vector3.Zero;

		Caster = null;
		CurrentCharge = 0;
		CurrentMana = 0;
		CurrentDamage = 0;

		if (trail != null)
		{
			trail.Reparent(this);
			trail.Visible = false;
		}
	}
}