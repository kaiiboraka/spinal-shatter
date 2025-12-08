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
	private Area3D detectionArea3D; // Added for explosion detection
	private SphereShape3D detectionShape;

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
		detectionArea3D ??= GetNode<Area3D>("%Detection_Area3D"); // Get reference

		detectionShape = (SphereShape3D)detectionArea3D.GetChild<CollisionShape3D>(0).Shape;

		lifetimeTimer = new Timer();
		lifetimeTimer.WaitTime = _lifetime;
		lifetimeTimer.OneShot = true;
		lifetimeTimer.Timeout += () => QueueFree();
		AddChild(lifetimeTimer);

		this.Freeze = true;
		collisionShape.Disabled = true;
		// _detectionArea33D is typically set up in the scene to monitor,
		// but we ensure it's not active prematurely if it's meant for a one-shot explosion.
		// Its collision_mask should be set to detect relevant layers (e.g., ENEMY_HURTBOX_BIT)
		// in the scene file (AltFire_Explode.tscn).
		if (detectionArea3D != null)
		{
			detectionArea3D.Monitoring = true; // Start enabled, as it should always be monitoring for overlaps.
		}

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
					sparkParticles.PlayParticles(particleCount * CurrentCharge);
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
			float scaledSize = sizingScale.GetLerpedValue(CurrentCharge);
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
				CurrentDamage = SpellData.DamageRange.GetLerpedValue(CurrentCharge);
				if (Slot == SlotType.Secondary)
				{
					var explosionRadius = ((OrbAltSpellData)SpellData).ExplosionRadius.GetLerpedValue(CurrentCharge);
					// Set the radius dynamically
					detectionShape.Radius = explosionRadius;
					// Alt-fire rocket launcher should have more base power for more knockback

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
		if (SpellData is OrbAltSpellData orbData && Slot == SlotType.Secondary)
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
		if (SpellData is OrbAltSpellData orbData && Slot == SlotType.Secondary)
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

	private void Explode(OrbAltSpellData orbData)
	{
		if (detectionArea3D == null)
		{
			GD.PrintErr("Projectile: _detectionArea3D not found for explosion.");
			Expire(false);
			return;
		}

		// Ensure the detection area is at the projectile's position for the explosion
		detectionArea3D.GlobalPosition = GlobalPosition;

		var explosionRadius = orbData.ExplosionRadius.GetLerpedValue(CurrentCharge);
		// Set the radius dynamically
		detectionShape.Radius = explosionRadius;
		DebugManager.Trace($"ExplosionRadius:{explosionRadius}");

		var overlappingAreas = detectionArea3D.GetOverlappingAreas();

		foreach (var area in overlappingAreas)
		{
			// The collider is the Area3D hurtbox, its parent is the Combatant
			if (area.GetParent() is Combatant combatant)
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
				explosion.PlayParticles(explosionRadius, 150 * Mathf.Min(CurrentCharge, .5f)); // Using a default particle count
			}
		}
		
		// Placeholder for explosion sound, assuming it's different from "Hit"
		// AudioManager.PlayAtPosition((AudioFile)AudioData["Explosion"], GlobalPosition); 
		var player = AudioManager.PlayAtPosition((AudioFile)AudioData["Hit"], GlobalPosition);
		// player.Finished += () => detectionArea3D.QueueFree();
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
		// detectionArea3D.Reparent(GetParent());

		if (dropMana) EjectMana(CurrentMana, GlobalPosition);
		Reset();
		QueueFree();
	}

	public void ApplyManaLoss(Vector3 impactPosition)
	{
		if (SpellData is not CastedSpellData castedSpell)
		{
			Expire();
			return;
		}
		
		float manaLostAmount = (int)Mathf.Min(castedSpell.ManaDroppedAmount.GetRandomValue(), CurrentMana);
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

		CurrentCharge = CurrentMana / castedSpell.ManaCostRange.Max;
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
