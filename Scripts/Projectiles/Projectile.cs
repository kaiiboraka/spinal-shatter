using System.Linq;
using Godot.Collections;

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
	private Array<SpriteBase3D> sprites;
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

	private float initialSize;
	private float damagePerMana;
	private ProjectileState state = ProjectileState.Charging;
	private Timer lifetimeTimer;
	private float bounceCooldown = 0;
	private GpuParticles3D trail;
	public HashSet<Enemy> HitEnemies { get; private set; } = new();

	public override void _Ready()
	{
		sprites = new Array<SpriteBase3D>();
		foreach (Node child in GetChildren())
		{
			if (child is SpriteBase3D sprite)
			{
				sprites.Add(sprite);
			}
		}

		collisionShape ??= GetNode<CollisionShape3D>("CollisionShape3D");
		audioStreamPlayer ??= GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");
		trail ??= GetNode<GpuParticles3D>("%GPUTrail3D");
		detectionArea3D ??= GetNode<Area3D>("%Detection_Area3D"); // Get reference

		detectionShape = (SphereShape3D)detectionArea3D.GetChild<CollisionShape3D>(0).Shape;

		lifetimeTimer = new Timer();
		lifetimeTimer.WaitTime = _lifetime;
		lifetimeTimer.OneShot = true;
		lifetimeTimer.Timeout += () => Expire();
		AddChild(lifetimeTimer);

		this.Freeze = true;
		collisionShape.Disabled = true;
		if (collisionShape is { Shape: SphereShape3D sphere })
		{
			initialSize = sphere.Radius;
		}
		else if (collisionShape is { Shape: CylinderShape3D cylinder })
		{
			initialSize = cylinder.Height;
		}
		else if (collisionShape is { Shape: CapsuleShape3D capsule })
		{
			initialSize = capsule.Height;
		}

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

				if (collider is Enemy enemy)
				{
					HitEnemies.Add(enemy);
				}
				else if (!collider.IsInGroup("Enemies"))
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
		trail.Visible = false;
		this.SpellData = spellData;
		this.Position = Vector3.Zero;
		this.CurrentCharge = 0;
		UpdateChargeState();
	}

	public void UpdateChargeState()
	{
		if (IsFixed) return;

		float scaledSize;
		if (SpellData is CastedSpellData { VisualSizeOverride: not null } castedSpellData)
		{
			scaledSize = castedSpellData.VisualSizeOverride.GetLerpedValue(CurrentCharge);
		}
		else
		{
			scaledSize = SpellData.SizeRange.GetLerpedValue(CurrentCharge);
		}

		if (!sprites.IsNullOrEmpty())
		{
			foreach (SpriteBase3D sprite in sprites)
			{
				sprite.Scale = Vector3.One * scaledSize;
			}
		}

		if (collisionShape is { Shape: SphereShape3D sphere })
		{
			sphere.Radius = Mathf.Max(0.05f, scaledSize * 0.5f);
		}
		else if (collisionShape is { Shape: CylinderShape3D cylinder })
		{
			cylinder.Height = Mathf.Max(0.05f, initialSize * scaledSize);
		}
		else if (collisionShape is { Shape: CapsuleShape3D capsule })
		{
			capsule.Height = Mathf.Max(0.05f, initialSize * scaledSize);
		}
		Mass = scaledSize;
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
					var explosionRadius = SpellData.ExplosionRadius.GetLerpedValue(CurrentCharge);

					// Set the radius dynamically
					detectionShape.Radius = explosionRadius;

					// Alt-fire rocket launcher should have more base power for more knockback
				}
				else if (Slot == SlotType.Automatic)
				{
					// data.InitialVelocity =
				}

				damagePerMana = CurrentDamage / CurrentMana;
				break;
			case WeaponType.Slash:
				CurrentDamage = SpellData.DamageRange.GetLerpedValue(CurrentCharge);
				damagePerMana = CurrentMana > 0 ? CurrentDamage / CurrentMana : 0;
				break;
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

		if (Slot != SlotType.Automatic)
		{
			Vector3 markerPosition = SpellMarker.Position;
			SpellMarker.Position = SpellMarker.Position with { X = 0 };
			GlobalPosition = SpellMarker.GlobalPosition;
			SpellMarker.Position = markerPosition;
		}
		else
		{
			GlobalPosition = SpellMarker.GlobalPosition;
		}

		LookAt(GlobalPosition + data.InitialVelocity);

		this.Freeze = false;
		collisionShape.Disabled = false;
		this.LinearVelocity = data.InitialVelocity;

		lifetimeTimer.Start();
		if (Slot != SlotType.Automatic) trail.Visible = true;
	}

	public void UpdateChargeAmount(float charge)
	{
		CurrentCharge = charge;
		UpdateChargeState();
	}

	public void OnEnemyHit(float damageDealt)
	{
		// AudioManager.PlayAtPosition((AudioFile)AudioData["Hit"], GlobalPosition);
		// AudioManager.Play(audioStreamPlayer, (AudioFile)AudioData["Hit"]);

		AudioManager.PlayAtPosition((AudioFile)AudioData["Hit"], GlobalPosition);

		switch (SpellData.Weapon)
		{
			case WeaponType.Orb:
				ApplyManaLoss(GlobalPosition);
				if (Slot == SlotType.Secondary) Explode();
				break;
			case WeaponType.Slash:
			case WeaponType.ForceWall:
			case WeaponType.Dice:
			case WeaponType.Lance:
			case WeaponType.Garlic:
			case WeaponType.Chakram:
			case WeaponType.Missiles:
				Expire(false);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void HandleWallBounce(Vector3 impactPoint)
	{
		ApplyManaLoss(impactPoint);

		switch (SpellData.Weapon)
		{
			case WeaponType.Orb:
				if (Slot == SlotType.Secondary)
				{
					Explode();
				}

				break;
			case WeaponType.Slash:
				// Expire();
				return;
			case WeaponType.ForceWall:
				break;
			case WeaponType.Dice:
				break;
			case WeaponType.Lance:
				break;
			case WeaponType.Garlic:
				break;
			case WeaponType.Chakram:
				break;
			case WeaponType.Missiles:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		if (IsFixed)
		{
			Expire();
			return;
		}

		AudioManager.Play(audioStreamPlayer, (AudioFile)AudioData["Bounce"]);

		bounceCooldown = 0.1f;
	}

	private void Explode()
	{
		if (detectionArea3D == null)
		{
			GD.PrintErr("Projectile: _detectionArea3D not found for explosion.");
			Expire(false);
			return;
		}

		// Ensure the detection area is at the projectile's position for the explosion
		detectionArea3D.GlobalPosition = GlobalPosition;

		var explosionRadius = SpellData.ExplosionRadius.GetLerpedValue(CurrentCharge);

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
			if (_explosionEffectScene.Instantiate() is OneshotParticles explosion)
			{
				GetTree().Root.AddChild(explosion);
				explosion.GlobalPosition = GlobalPosition;
				explosion.PlayParticles(explosionRadius,
					150 * Mathf.Min(CurrentCharge, .5f)); // Using a default particle count
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
		// Calculate mana lost based on actual damage dealt, checking for division by zero.

		// if (CurrentDamage > 0)
		// {
		// 	manaLostAmount = damageAmount * (CurrentMana / CurrentDamage);
		// }

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

		if (!sprites.IsNullOrEmpty())
		{
			foreach (SpriteBase3D sprite in sprites)
			{
				if (sprite.Scale.X < 0.1f)
				{
					QueueFree();
				}
			}
		}
	}

	private void EjectMana(float amount, Vector3 spawnPoint)
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

	private void Modulate(Color newColor)
	{
		if (sprites.IsNullOrEmpty()) return;
		foreach (SpriteBase3D sprite in sprites)
		{
			sprite.Modulate = newColor;
		}
	}

	private void Reset()
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
		if (collisionShape is { Shape: SphereShape3D sphere })
		{
			sphere.Radius = initialSize;
		}
		else if (collisionShape is { Shape: CylinderShape3D cylinder })
		{
			cylinder.Height = initialSize;
		}
		else if (collisionShape is { Shape: CapsuleShape3D capsule })
		{
			capsule.Height = initialSize;
		}

		HitEnemies.Clear();
	}
}