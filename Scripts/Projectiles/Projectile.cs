namespace SpinalShatter;

using Godot;
using System;
using System.Collections.Generic;
using Elythia;

public partial class Projectile : RigidBody3D
{
	private enum ProjectileState
	{
		Charging,
		Fired
	}

	[Export] private PackedScene _sparkParticlesScene;
	[Export] private PackedScene _explosionEffectScene;
	private SpriteBase3D sprite;
	private CollisionShape3D collisionShape;
	private Area3D _detectionArea3D; // Added for explosion detection

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

	private float damagePerMana;
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
		_detectionArea3D = GetNodeOrNull<Area3D>("%Detection_Area3D"); // Get reference

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

		UpdateChargeState();

		switch (SpellData.Weapon)
		{
			case WeaponType.Orb:
				CurrentDamage = Mathf.Lerp(SpellData.DamageRange.Min, SpellData.DamageRange.Max, CurrentCharge);
				if (Slot == SlotType.Alt)
				{
					// Alt-fire rocket launcher should have more base power for more knockback
					CurrentDamage *= 2.0f;
				}
				damagePerMana = CurrentDamage / CurrentMana;
				break;
			case WeaponType.Slash:
			case WeaponType.ForceWall:
			case WeaponType.Dice:
			case WeaponType.Lance:
			case WeaponType.Garlic:
			case WeaponType.Chakram:
			case WeaponType.Missiles:
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
		if (SpellData is ExplosiveSpellData orbData && Slot == SlotType.Alt)
		{
			Explode(orbData);
			return;
		}
		
		ApplyManaLoss(GlobalPosition);
		AudioManager.PlayAtPosition((AudioFile)AudioData["Hit"], GlobalPosition);
		AudioManager.Play(audioStreamPlayer, (AudioFile)AudioData["Hit"]);
		switch (SpellData.Weapon)
		{
			case WeaponType.Orb:
				Expire(false);
				break;
			case WeaponType.Slash:
			case WeaponType.ForceWall:
			case WeaponType.Dice:
			case WeaponType.Lance:
			case WeaponType.Garlic:
			case WeaponType.Chakram:
			case WeaponType.Missiles:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void HandleWallBounce(Vector3 impactPoint)
	{
		if (SpellData is ExplosiveSpellData orbData && Slot == SlotType.Alt)
		{
			Explode(orbData);
			return;
		}
		
		if (IsFixed)
		{
			Expire();
			return;
		}

		AudioManager.Play(audioStreamPlayer, (AudioFile)AudioData["Bounce"]);
		ApplyManaLoss(impactPoint);

		bounceCooldown = 0.1f;
	}

	private void Explode(ExplosiveSpellData orbData)
	{
		if (_detectionArea3D == null)
		{
			GD.PrintErr("Projectile: _detectionArea3D not found for explosion.");
			Expire(false);
			return;
		}

		// Set the radius dynamically
		if (_detectionArea3D.GetNodeOrNull<CollisionShape3D>("CollisionShape3D") is CollisionShape3D shapeNode && shapeNode.Shape is SphereShape3D sphereShape)
		{
			sphereShape.Radius = orbData.ExplosionRadius;
		}
		else
		{
			GD.PrintErr("Projectile: _detectionArea3D does not have a SphereShape3D child or CollisionShape3D for radius adjustment.");
			Expire(false);
			return;
		}

		// Process overlapping bodies
		// No need to set monitoring true/false if we just get overlapping bodies and then expire.
		// The area's collision mask needs to be set up in the scene to detect hurtboxes
		
		var overlappingBodies = _detectionArea3D.GetOverlappingBodies();
		
		foreach (var body in overlappingBodies)
		{
			if (body.GetParent() is Combatant combatant) // Assuming Combatant is the parent of the Hurtbox Area3D
			{
				// Ensure we don't hit the caster with their own explosion
				if (combatant == Caster) continue;

				combatant.TakeDamage(CurrentDamage, GlobalPosition);
			}
		}

		if (_explosionEffectScene != null)
		{
			if(_explosionEffectScene.Instantiate() is OneshotParticles explosion)
			{
				GetTree().Root.AddChild(explosion);
				explosion.GlobalPosition = GlobalPosition;
				explosion.PlayParticles(120);
			}
		}
		
		// Placeholder for explosion sound, assuming it's different from "Hit"
		// AudioManager.PlayAtPosition((AudioFile)AudioData["Explosion"], GlobalPosition); 
		AudioManager.PlayAtPosition((AudioFile)AudioData["Hit"], GlobalPosition); 
		
		Expire(false); // Expire without dropping mana
	}

	public void Expire(bool dropMana = true)
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

		if (dropMana) EjectMana(CurrentMana, GlobalPosition);
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
		CurrentDamage = damagePerMana * CurrentMana;

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
		damagePerMana = 0f;
		if (trail != null)
		{
			trail.Reparent(this);
			trail.Visible = false;
		}
	}
}