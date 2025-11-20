using System.Collections.Generic;
using System.Linq;
using Elythia;
using Godot;

namespace SpinalShatter;

public partial class Enemy : Combatant
{
	public LevelRoom AssociatedRoom { get; set; }
	private bool _isActive = true;

	private CollisionShape3D collisionShape = new();
	private AIState _currentState = AIState.Idle;

	[ExportGroup("Components")]
	[Export] public EnemyData Data { get; private set; }

	private AnimationPlayer animPlayer;

	private AnimatedSprite3D animatedSprite;
	private AnimatedSprite3D animatedSprite_Eye;
	[Export] private PackedScene _deathParticlesScene;
	private OverheadHealthBar OverheadHealthBar { get; set; }
	private StateSprite3d stateVisual;


	private AudioData AudioData;
	public AudioStreamPlayer3D AudioPlayer_Voice { get; private set; }
	public AudioStreamPlayer3D AudioPlayer_SFX { get; private set; }
	public AudioStreamPlayer3D AudioPlayer_Attack { get; private set; }

	private Timer timerWalk;
	private Timer timerPool;
	private Timer timerAction;
	private Timer timerAttackCooldown;
	private Timer timerBlink;

	// --- STATS (initialized from EnemyData) ---
	// Patrol
	public float RecoveryTime { get; private set; } = 1.0f;
	public float ChaseRotationSpeed { get; private set; } = 5.0f;
	public float WalkSpeed { get; private set; } = 8.0f;
	public float MinWalkTime { get; private set; } = 1.0f;
	public float MaxWalkTime { get; private set; } = 5.0f;
	public float MinWaitTime { get; private set; } = 1.0f;
	public float MaxWaitTime { get; private set; } = 5.0f;

	// Combat
	public IntValueRange MoneyAmountToDrop { get; private set; } = new(10);
	public IntValueRange ManaAmountToDrop { get; private set; } = new(10);

	// Attack
	private float AttackRange { get; set; } = 2.0f;
	private float AttackCooldown { get; set; } = 1.5f;
	public float AttackDamage { get; private set; } = 10f;

	// Projectiles
	public bool ProjectileIsRanged { get; private set; }
	private float ProjectileSpeed { get; set; } = 20.0f;
	private PackedScene ProjectileScene;

	private Area3D DetectionArea;
	private RayCast3D Detection_lineOfSight;
	private Area3D Combat_meleeHitbox;
	private Area3D Combat_hurtbox;
	private Node3D ProjectileSpawnPoint;


	private PlayerBody _player;

	private bool _isWalking = false;
	private bool isDying => _currentState == AIState.Dying;

	[Signal]
	public delegate void EnemyDiedEventHandler(Enemy who);

	public ObjectPoolManager<Node3D> OwningPool { get; set; }

	private float _gravity => Constants.GRAVITY;

	public override void _Ready()
	{
		base._Ready(); // GetComponents, ConnectEvents

		if (Data == null)
		{
			DebugManager.Error($"Enemy '{Name}' is missing EnemyData!");
		}
		else
		{
			ApplyData(Data);
		}

		if (!ProjectileIsRanged && Combat_meleeHitbox != null)
		{
			Combat_meleeHitbox.AreaEntered += area =>
			{
				if (area.Owner is PlayerBody player)
				{
					DebugManager.Debug($"MELEE HIT: Enemy '{Name}' attacking Player for {AttackDamage} damage. IsActive: {_isActive}");
					player.TakeDamage(AttackDamage, GlobalPosition);
				}
			};
		}

		EnableCollisions();

		// Start patrolling
		ChangeState(AIState.Patrolling);
	}


	protected override void GetComponents()
	{
		base.GetComponents();
		collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		animatedSprite = GetNode<AnimatedSprite3D>("Body_AnimatedSprite3D");
		animatedSprite_Eye = GetNode<AnimatedSprite3D>("Body_AnimatedSprite3D/Eye_AnimatedSprite");
		OverheadHealthBar = GetNode<OverheadHealthBar>("HealthBar");
		stateVisual = GetNode<StateSprite3d>("StateSprite3D");

		timerWalk = GetNode<Timer>("Timers/WalkTimer");
		timerAction = GetNode<Timer>("Timers/ActionWaitTimer");
		timerAttackCooldown = GetNode<Timer>("Timers/AttackCooldownTimer");
		timerPool = GetNode<Timer>("Timers/PoolTimer");
		timerBlink = GetNode<Timer>("Timers/BlinkTimer");

		AudioPlayer_Voice = GetNode<AudioStreamPlayer3D>("Voice_AudioStreamPlayer3D");
		AudioPlayer_SFX = GetNode<AudioStreamPlayer3D>("SFX_AudioStreamPlayer3D");
		AudioPlayer_Attack = GetNode<AudioStreamPlayer3D>("Attack_AudioStreamPlayer3D");

		DetectionArea = GetNode<Area3D>("DetectionArea3D");
		Detection_lineOfSight = GetNode<RayCast3D>("LOS_RayCast3D");

		Combat_hurtbox = GetNode<Area3D>("Hurtbox");

		if (HasNode("MeleeHitbox"))
		{
			Combat_meleeHitbox = GetNode<Area3D>("MeleeHitbox");
		}

		if (HasNode("SpellOrigin"))
		{
			ProjectileSpawnPoint = GetNode<Node3D>("SpellOrigin");
		}
	}

	protected override void ConnectEvents()
	{
		base.ConnectEvents();

		HealthComponent.HealthChanged += OverheadHealthBar.OnHealthChanged;
		DetectionArea.BodyEntered += OnDetectionAreaBodyEntered;
		DetectionArea.BodyExited += OnDetectionAreaBodyExited;

		timerPool.WaitTime = Mathf.Max(
			timerBlink.WaitTime,
			animPlayer.GetAnimation("Die").Length);

		timerWalk.Timeout += OnWalkTimerTimeout;
		timerAction.Timeout += OnActionTimerTimeout;
		timerPool.Timeout += Despawn;
	}

	private void ApplyData(EnemyData data)
	{
		// Patrol
		RecoveryTime = data.RecoveryTime;
		ChaseRotationSpeed = data.ChaseRotationSpeed;
		WalkSpeed = data.WalkSpeed;
		MinWalkTime = data.MinWalkTime;
		MaxWalkTime = data.MaxWalkTime;
		MinWaitTime = data.MinWaitTime;
		MaxWaitTime = data.MaxWaitTime;

		// Combat
		KnockbackWeight = data.KnockbackWeight;

		// Pickups
		MoneyAmountToDrop = data.MoneyAmountToDrop;
		ManaAmountToDrop = data.ManaAmountToDrop;

		// Attack
		AttackRange = data.AttackRange;
		AttackCooldown = data.AttackCooldown;
		AttackDamage = data.AttackDamage;

		// Projectiles
		ProjectileIsRanged = data.IsRanged;
		ProjectileSpeed = data.ProjectileSpeed;
		ProjectileScene = data.ProjectileScene;

		// Audio
		AudioData = data.AudioData;
	}

	private bool InActionableState => _currentState != AIState.Attacking &&
									  _currentState != AIState.Recovery &&
									  _currentState != AIState.Dying;

	public override void _PhysicsProcess(double delta)
	{
		if (_currentState == AIState.Dying) return;
		if (!_isActive) return;

		base._PhysicsProcess(delta); // Decays knockback

		// --- Player Target Acquisition (if _player is null) ---
		if (_player == null)
		{
			var bodies = DetectionArea.GetOverlappingBodies();
			foreach (var body in bodies)
			{
				if (body is PlayerBody player)
				{
					_player = player;
					break; // Found player, no need to check other bodies
				}
			}
		}

		// --- Existing LOS check (now _player should be set if in area) ---
		TryToChase();


		Vector3 newVelocity = Velocity;

		// Add gravity.
		if (Data.IsGrounded && !IsOnFloor())
		{
			newVelocity.Y -= _gravity * (float)delta * Data.KnockbackWeight;
		}

		switch (_currentState)
		{
			case AIState.Idle:
				ProcessIdle(ref newVelocity);
				break;
			case AIState.Patrolling:
				ProcessPatrolling(ref newVelocity);
				break;
			case AIState.Chasing:
				ProcessChasing(ref newVelocity, delta);
				break;
			case AIState.Attacking:
				ProcessAttacking(ref newVelocity);
				break;
			case AIState.Recovery:
				ProcessRecovery(ref newVelocity);
				break;
			case AIState.Dying:
				break;
		}

		// Apply knockback
		if (knockbackVelocity.LengthSquared() > 0)
		{
			newVelocity += knockbackVelocity;
			knockbackVelocity = Vector3.Zero;
		}

		Velocity = newVelocity;

		MoveAndSlide();

		// Update sprite animation based on angle to player, if we have a target.
	}

	public override void _Process(double delta)
	{
		if (!_isActive) return;
		base._Process(delta);
		BlinkRoutine();
		FacePlayer();
	}

	private void EnterState(AIState state)
	{
		switch (state)
		{
			case AIState.Idle:
				// animPlayer.Play("Front_Idle");

				// PlayAnimationOnSprites("Front_Idle");
				break;
			case AIState.Patrolling:
				// animPlayer.Play("Front_Idle");

				// PlayAnimationOnSprites("Front_Idle");
				StartWaiting();
				break;
			case AIState.Chasing:
				// animPlayer.Play("Front_Idle");

				// PlayAnimationOnSprites("Front_Idle");
				break;
			case AIState.Attacking:
				Velocity = Vector3.Zero with { Y = Velocity.Y };
				animPlayer.Play("Front_Attack");

				// PlayAnimationOnSprites("Front_Attack");
				// PerformAttack();
				timerAttackCooldown.WaitTime = AttackCooldown;
				timerAttackCooldown.Start();
				break;
			case AIState.Recovery:
				// animPlayer.Play("Front_Idle");
				timerAction.WaitTime = RecoveryTime;
				timerAction.Start();
				break;
			case AIState.Dying:
				TryToDie();
				break;
		}
	}

	private void ChangeState(AIState newState, bool force = false)
	{
		if ((_currentState == newState && !force) || (_currentState == AIState.Dying && !force)) return;

		ExitState(_currentState);
		_currentState = newState;
		EnterState(_currentState);

		if (stateVisual != null)
		{
			stateVisual.CurrentState = newState;
		}
	}

	private void ExitState(AIState state)
	{
		switch (state)
		{
			case AIState.Idle:
				break;
			case AIState.Patrolling:
				timerWalk.Stop();
				timerAction.Stop();
				break;
			case AIState.Chasing:
				break;
			case AIState.Attacking:
				break;
			case AIState.Recovery:
				timerAction.Stop();
				break;
		}
	}

	private void TryToChase()
	{
		if (!InActionableState) return;
		if (_player == null) return; // This check is now more reliable

		Detection_lineOfSight.TargetPosition = ToLocal(_player.GlobalPosition);
		if (Detection_lineOfSight.IsColliding() && Detection_lineOfSight.GetCollider() == _player)
		{
			ChangeState(AIState.Chasing);
		}
	}

	private void PerformAttack()
	{
		AudioManager.Play(AudioPlayer_Attack, (AudioFile)AudioData["Attack"]);
		if (ProjectileIsRanged)
		{
			DebugManager.Debug($"RANGED ATTACK: Enemy '{Name}' is firing a projectile.");
			if (ProjectileScene == null)
			{
				DebugManager.Error($"Enemy: {Name} ProjectileScene is null in PerformAttack!");
				return;
			}

			var projectile = ProjectileScene.Instantiate<Projectile>();
			var direction = (_player.GlobalPosition - GlobalPosition).Normalized();
			var launchData = new ProjectileLaunchData
			{
				Caster = this,
				Damage = AttackDamage,
				InitialVelocity = direction * ProjectileSpeed,
				StartPosition = ProjectileSpawnPoint.GlobalPosition,
				SizingScale = new FloatValueRange(1),
			};
			projectile.Launch(launchData);
		}
		// Melee attack logic (handled by animation keyframes)
	}

	public void StopAnimation()
	{
		animPlayer.Stop();
		animatedSprite.Stop();
		animatedSprite_Eye.Stop();
	}

	public void PauseAnimation()
	{
		animPlayer.Pause();
		animatedSprite.Pause();
		animatedSprite_Eye.Pause();
	}

	private void OnDetectionAreaBodyEntered(Node3D body)
	{
		if (body is PlayerBody player)
		{
			_player = player;
			if (_currentState != AIState.Attacking && _currentState != AIState.Recovery)
			{
				ChangeState(AIState.Chasing);
			}
		}
	}

	private void OnDetectionAreaBodyExited(Node3D body)
	{
		if (body is PlayerBody player)
		{
			_player = null;
			ChangeState(AIState.Patrolling);
		}
	}

	private void ProcessIdle(ref Vector3 newVelocity)
	{
		newVelocity = Vector3.Zero;
	}

	private void ProcessPatrolling(ref Vector3 newVelocity)
	{
		if (_isWalking)
		{
			Wander(ref newVelocity);
		}
	}

	private void ProcessChasing(ref Vector3 newVelocity, double delta)
	{
		if (_player == null)
		{
			ChangeState(AIState.Patrolling);
			return;
		}

		var targetRotation = new Transform3D(Basis, GlobalPosition).LookingAt(_player.GlobalPosition, Vector3.Up).Basis;
		Basis = Basis.Orthonormalized().Slerp(targetRotation, (float)delta * ChaseRotationSpeed);

		if (GlobalPosition.DistanceTo(_player.GlobalPosition) > AttackRange)
		{
			// Move towards player
			if (Data.IsFlying)
			{
				newVelocity = -GlobalTransform.Basis.Z * WalkSpeed;
			}
			else
			{
				WalkForward(ref newVelocity);
			}
		}
		else
		{
			if (timerAttackCooldown.IsStopped())
			{
				ChangeState(AIState.Attacking);
			}
		}
	}

	private void ProcessAttacking(ref Vector3 newVelocity)
	{
		// Waiting for animation to finish
		newVelocity = Vector3.Zero;
	}

	private void ProcessRecovery(ref Vector3 newVelocity)
	{
		newVelocity = Vector3.Zero;
	}

	// private void OnAnimationFinished(StringName animName)
	// {
	// 	DebugManager.Debug($"Enemy: {Name} AnimationFinished: {animName}");
	// 	if (animName == "Front_Attack")
	// 	{
	// 		ChangeState(AIState.Recovery);
	// 	}
	// }
	private void OnActionTimerTimeout()
	{
		if (_currentState == AIState.Patrolling)
		{
			Rotation = new Vector3(0, (float)GD.RandRange(0, Mathf.Pi * 2), 0);
			StartWalking();
		}
		else if (_currentState == AIState.Recovery)
		{
			ChangeState(AIState.Patrolling);
		}
	}

	private void WalkForward(ref Vector3 newVelocity)
	{
		newVelocity.X = -GlobalTransform.Basis.Z.X * WalkSpeed;
		newVelocity.Z = -GlobalTransform.Basis.Z.Z * WalkSpeed;
	}

	private void StartWalking()
	{
		_isWalking = true;
		timerWalk.WaitTime = GD.RandRange(MinWalkTime, MaxWalkTime);
		timerWalk.Start();
	}

	private void Wander(ref Vector3 newVelocity)
	{
		// Set velocity to move forward.
		if (Data.IsFlying)
		{
			newVelocity = -GlobalTransform.Basis.Z * WalkSpeed;
		}
		else
		{
			WalkForward(ref newVelocity);
		}

		// Check for wall collision and change direction for grounded enemies.
		if (Data.IsGrounded && IsOnWall())
		{
			var collision = GetLastSlideCollision();
			if (collision != null)
			{
				var forward = -GlobalTransform.Basis.Z.Normalized();
				var reflectDir = forward.Bounce(collision.GetNormal());
				LookAt(GlobalPosition + reflectDir, Vector3.Up);
			}
			else
			{
				// Fallback to random rotation if no collision data
				float randomAngle = (float)GD.RandRange(0, Mathf.Pi * 2);
				Rotation = new Vector3(0, randomAngle, 0);
			}
		}
	}

	private void OnWalkTimerTimeout()
	{
		StartWaiting();
	}

	private void StartWaiting()
	{
		_isWalking = false;
		timerAction.WaitTime = GD.RandRange(MinWaitTime, MaxWaitTime);
		timerAction.Start();
	}

	private void UpdateAnimation(float angleToPlayer)
	{
		string animName;
		bool flipH = false; // Default to not flipped

		// Determine animation based on angle
		if (angleToPlayer >= -45 && angleToPlayer <= 45) // Front cone
		{
			// HACK: we don't have straight sprites yet.
			animName = "Front_Idle";
			if (angleToPlayer < -2) // Player is to enemy's front-left
			{
				flipH = true;
			}
			else // Player is to enemy's front-right or directly front
			{
				flipH = false;
			}
		}
		else if (angleToPlayer > 45 && angleToPlayer <= 135) // Right side cone
		{
			animName = "Side";
			flipH = true; // Player is to enemy's right
		}
		else if (angleToPlayer < -45 && angleToPlayer >= -135) // Left side cone
		{
			animName = "Side";
			flipH = false; // Player is to enemy's left
		}
		else // Back cone
		{
			animName = "Back";
			if (angleToPlayer < 0) // Player is to enemy's back-left
			{
				flipH = false;
			}
			else // Player is to enemy's back-right
			{
				flipH = true;
			}
		}

		if (animPlayer.CurrentAnimation != animName)
		{
			animPlayer.Play(animName);
		}

		animatedSprite.FlipH = flipH;
		animatedSprite_Eye.FlipH = flipH;
	}

	// Update animation based on angle to player
	private void FacePlayer()
	{
		if (_player == null || _currentState == AIState.Dying) return;
		if (_currentState == AIState.Attacking) return;
		Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
		Vector3 enemyForward = -GlobalTransform.Basis.Z;
		float angleToPlayer = Mathf.RadToDeg(enemyForward.SignedAngleTo(toPlayer, Vector3.Up));
		UpdateAnimation(angleToPlayer);
	}

	public override void OnHurtboxBodyEntered(Node3D body)
	{
		if (isDying) return;

		base.OnHurtboxBodyEntered(body); // Handles damage + projectile destruction

		if (body is Projectile projectile && projectile.Owner != this)
		{
			projectile.OnEnemyHit();
		}
	}

	protected override void OnHurt(Vector3 sourcePosition, float damage)
	{
		base.OnHurt(sourcePosition, damage);
		ChangeState(AIState.Chasing);
	}

	public override void PlayOnHurtFX()
	{
		var tween = GetTree().CreateTween();
		tween.TweenProperty(animatedSprite, "modulate", Colors.Red, 0.1);
		tween.TweenProperty(animatedSprite, "modulate", Colors.White, 0.1);

		if (isDying) return;
		AudioManager.Play(AudioPlayer_SFX, (AudioFile)AudioData["Hurt"]);
	}

	public override void OnRanOutOfHealth()
	{
		// DebugManager.Debug($"Enemy: {Name} OnDied called.");
		ChangeState(AIState.Dying);
	}

	private void Despawn()
	{
		// DebugManager.Debug($"Enemy: {Name} Animation timer finished. Releasing enemy.");
		if (OwningPool != null) OwningPool.Release(this);
		else QueueFree();
	}

	private void TryToDie()
	{
		// DebugManager.Debug($"Enemy: {Name} TryToDie called. Current state: {_currentState}\n" +
		// 				   $"Death particles GlobalPosition before: {GlobalPosition}");

		if (_deathParticlesScene.Instantiate() is OneshotParticles deathParticles)
		{
			GetParent().AddChild(deathParticles);
			deathParticles.GlobalPosition = GlobalPosition;

			// DebugManager.Debug($"Enemy: {Name} Death particles GlobalPosition after: {deathParticles.GlobalPosition}");
			deathParticles.PlayParticles(Data.DeathParticleCount);
		}
		AudioManager.Play(AudioPlayer_Voice, (AudioFile)AudioData["Die"]);

		animPlayer.Play("Die");
		timerBlink.Start();


		PickupManager.Instance.SpawnPickupAmount(PickupType.Mana, ManaAmountToDrop.GetRandomValue(),
			collisionShape.GlobalPosition);
		PickupManager.Instance.SpawnPickupAmount(PickupType.Money, MoneyAmountToDrop.GetRandomValue(),
			collisionShape.GlobalPosition);

		StopMoving();
		StopActionTimers();
		DisableCollisions();
		EmitSignalEnemyDied(this);

		timerPool.Start();
	}

	public override void Reset()
	{
		base.Reset();
		HealthComponent.Reset();
		Activate();

		// Add any enemy-specific reset logic here
		animPlayer.Stop(); // Stop any playing animation
		ChangeState(AIState.Idle, true);
	}

	public void Activate()
	{
		if (_isActive) return;

		_isActive = true;
		Visible = true;
		SetProcess(true);
		SetPhysicsProcess(true);

		EnableCollisions();
	}

	public void Deactivate()
	{
		if (!_isActive) return;
		_isActive = false;

		HideVisuals();

		StopProcessing();

		StopMoving();

		DisableCollisions();

		StopActionTimers();
	}

	private void HideVisuals()
	{
		Visible = false;
	}

	private void StopProcessing()
	{
		SetProcess(false);
		SetPhysicsProcess(false);
	}

	private void StopMoving()
	{
		Velocity = Vector3.Zero;
	}

	private void DisableCollisions()
	{
		// Remove enemy from its physics layers, making it undetectable.
		this.RemoveCollisionLayer3D(LayerNames.PHYSICS_3D.ENEMY_COLLISION_NUM);
		Combat_hurtbox.RemoveCollisionLayer3D(LayerNames.PHYSICS_3D.ENEMY_HURTBOX_NUM);
		Combat_hurtbox.RemoveCollisionMask3D(LayerNames.PHYSICS_3D.PLAYER_HITBOX_NUM);
		Combat_hurtbox.RemoveCollisionMask3D(LayerNames.PHYSICS_3D.PLAYER_PROJECTILE_NUM);

		if (Combat_meleeHitbox != null)
		{
			Combat_meleeHitbox.SetDeferred("monitoring", false);
			Combat_meleeHitbox.SetDeferred("monitorable", false);
		}
	}


	private void EnableCollisions()
	{
		// Restore enemy to its physics layers.
		this.AddCollisionLayer3D(LayerNames.PHYSICS_3D.ENEMY_COLLISION_NUM);
		Combat_hurtbox.AddCollisionLayer3D(LayerNames.PHYSICS_3D.ENEMY_HURTBOX_NUM);
		Combat_hurtbox.AddCollisionMask3D(LayerNames.PHYSICS_3D.PLAYER_HITBOX_NUM);
		Combat_hurtbox.AddCollisionMask3D(LayerNames.PHYSICS_3D.PLAYER_PROJECTILE_NUM);

		if (Combat_meleeHitbox != null)
		{
			Combat_meleeHitbox.SetDeferred("monitoring", true);
			Combat_meleeHitbox.SetDeferred("monitorable", true);
		}
	}

	private void StopActionTimers()
	{
		timerWalk?.Stop();
		timerAction?.Stop();
		timerAttackCooldown?.Stop();
	}

	private void BlinkRoutine()
	{
		if (timerBlink.IsStopped()) return;

		double timeLeft = timerBlink.TimeLeft;

		float duration = timeLeft switch
		{
			<= 5.0 and > 3.0 => .5f,
			<= 3.0 and > 1.0 => 0.25f,
			<= 1.0 and > 0.0 => 0.125f,
			_ => 0
		};
		float alpha = timeLeft switch
		{
			<= 5.0 and > 3.0 => 0.5f,
			<= 3.0 and > 1.0 => 0.375f,
			<= 1.0 and > 0.0 => 0.25f,
			_ => 0
		};

		Blink(alpha, duration);
	}

	protected Tween BlinkTween;

	private void Blink(float alpha, float duration)
	{
		if (BlinkTween != null && BlinkTween.IsRunning() || animatedSprite == null) return;

		BlinkTween = CreateTween().SetLoops(2).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.InOut);
		BlinkTween.TweenProperty(animatedSprite, "modulate:a", alpha, duration / 2);
		BlinkTween.TweenProperty(animatedSprite, "modulate:a", 1.0f, duration / 2);
	}
}