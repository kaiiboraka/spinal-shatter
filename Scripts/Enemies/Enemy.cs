using System.Collections.Generic;
using System.Linq;
using Elythia;
using Godot;

namespace SpinalShatter;

public partial class Enemy : Combatant
{
	public LevelRoom AssociatedRoom { get; set; }
	private bool _isActive = true;

	private List<CollisionShape3D> _collisionShapes = new();
	private AIState _currentState = AIState.Idle;

	[ExportGroup("Components")]
	[Export] public EnemyData Data { get; private set; }

	private AnimationPlayer _animPlayer;

	private AnimatedSprite3D _animatedSprite;
	private AnimatedSprite3D _animatedSprite_Eye;
	[Export] private PackedScene _deathParticlesScene;
	private OverheadHealthBar OverheadHealthBar { get; set; }
	private StateSprite3d _stateVisual;

	private EnemyAudioData AudioData;

	private Timer _timerWalk;
	private Timer _timerPool;
	private Timer _timerAction;
	private Timer _timerAttackCooldown;
	private Timer _timerBlink;

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
	public int MoneyAmountToDrop { get; private set; } = 10;
	public int ManaAmountToDrop { get; private set; } = 10;
	private float Mana_minRefundPercent = 0.05f;
	private float Mana_maxRefundPercent = 0.30f;

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

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _Ready()
	{
		if (Data == null)
		{
			DebugManager.Error($"Enemy '{Name}' is missing EnemyData!");
		}
		else
		{
			ApplyData(Data);
		}

		base._Ready(); // Sets up HealthComponent, hurtbox, etc.

		GetComponents();

		// Collect all collision shapes for activation/deactivation
		_collisionShapes = GetChildren().OfType<CollisionShape3D>().ToList();

		HealthComponent.HealthChanged += OverheadHealthBar.OnHealthChanged;

		DetectionArea.BodyEntered += OnDetectionAreaBodyEntered;
		DetectionArea.BodyExited += OnDetectionAreaBodyExited;

		_timerPool.WaitTime = Mathf.Max(
			_timerBlink.WaitTime,
			_animPlayer.GetAnimation("Die").Length);

		_timerWalk.Timeout += OnWalkTimerTimeout;
		_timerAction.Timeout += OnActionTimerTimeout;
		_timerPool.Timeout += Despawn;

		// _animPlayer.AnimationFinished += OnAnimationFinished;

		if (!ProjectileIsRanged && Combat_meleeHitbox != null)
		{
			Combat_meleeHitbox.AreaEntered += area =>
			{
				if (area.Owner is PlayerBody player)
				{
					player.TakeDamage(AttackDamage, GlobalPosition);
				}
			};
		}

		EnableCollisions();

		// Start patrolling
		ChangeState(AIState.Patrolling);
	}

	private void GetComponents()
	{
		_animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_animatedSprite = GetNode<AnimatedSprite3D>("Body_AnimatedSprite3D");
		_animatedSprite_Eye = GetNode<AnimatedSprite3D>("Body_AnimatedSprite3D/Eye_AnimatedSprite");
		OverheadHealthBar = GetNode<OverheadHealthBar>("HealthBar");
		_stateVisual = GetNode<StateSprite3d>("StateSprite3D");

		_timerWalk = GetNode<Timer>("Timers/WalkTimer");
		_timerAction = GetNode<Timer>("Timers/ActionWaitTimer");
		_timerAttackCooldown = GetNode<Timer>("Timers/AttackCooldownTimer");
		_timerPool = GetNode<Timer>("Timers/PoolTimer");
		_timerBlink = GetNode<Timer>("Timers/BlinkTimer");

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
		MoneyAmountToDrop = data.MoneyAmountToDrop;
		ManaAmountToDrop = data.ManaAmountToDrop;
		Mana_minRefundPercent = data.ManaMinRefundPercent;
		Mana_maxRefundPercent = data.ManaMaxRefundPercent;

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

	public override void _PhysicsProcess(double delta)
	{
		if (_currentState == AIState.Dying) return;
		if (!_isActive) return;

		base._PhysicsProcess(delta); // Decays knockback

		if (_currentState != AIState.Attacking && _currentState != AIState.Recovery && _currentState != AIState.Dying)
		{
			if (Detection_lineOfSight.IsColliding() && Detection_lineOfSight.GetCollider() is PlayerBody player)
			{
				_player = player;
				ChangeState(AIState.Chasing);
			}
		}


		Vector3 newVelocity = Velocity;

		// Add gravity.
		if (!IsOnFloor() && !isDying)
		{
			newVelocity.Y -= _gravity * (float)delta;
		}
		else
		{
			switch (_currentState)
			{
				case AIState.Idle:
					ProcessIdle(delta);
					newVelocity = Vector3.Zero;
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
			}
		}

		// Apply knockback
		if (!isDying) newVelocity += _knockbackVelocity;
		Velocity = newVelocity;
		if (_player != null && _currentState != AIState.Attacking && _currentState != AIState.Dying)
		{
			Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
			Vector3 enemyForward = -GlobalTransform.Basis.Z;
			float angleToPlayer = Mathf.RadToDeg(enemyForward.SignedAngleTo(toPlayer, Vector3.Up));
			UpdateAnimation(angleToPlayer);
		}
		else Velocity = Vector3.Zero;

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
				_animPlayer.Play("Front_Idle");

				// PlayAnimationOnSprites("Front_Idle");
				break;
			case AIState.Patrolling:
				_animPlayer.Play("Front_Idle");

				// PlayAnimationOnSprites("Front_Idle");
				StartWaiting();
				break;
			case AIState.Chasing:
				_animPlayer.Play("Front_Idle");

				// PlayAnimationOnSprites("Front_Idle");
				break;
			case AIState.Attacking:
				Velocity = Vector3.Zero with { Y = Velocity.Y };
				_animPlayer.Play("Front_Attack");

				// PlayAnimationOnSprites("Front_Attack");
				// PerformAttack();
				_timerAttackCooldown.WaitTime = AttackCooldown;
				_timerAttackCooldown.Start();
				break;
			case AIState.Recovery:
				_animPlayer.Play("Front_Idle");
				_timerAction.WaitTime = RecoveryTime;
				_timerAction.Start();
				break;
			case AIState.Dying:
				TryToDie();
				break;
		}
	}

	private void ChangeState(AIState newState, bool force = false)
	{
		if ((_currentState == newState && !force) || _currentState == AIState.Dying) return;

		ExitState(_currentState);
		_currentState = newState;
		EnterState(_currentState);

		if (_stateVisual != null)
		{
			_stateVisual.CurrentState = newState;
		}
	}

	private void PerformAttack()
	{
		if (ProjectileIsRanged)
		{
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
		else //if (Attack_meleeHitbox != null)
		{
			// Melee attack logic (handled by animation keyframes)
		}
	}

	public void StopAnimation()
	{
		_animPlayer.Stop();
		_animatedSprite.Stop();
		_animatedSprite_Eye.Stop();
	}

	public void PauseAnimation()
	{
		_animPlayer.Pause();
		_animatedSprite.Pause();
		_animatedSprite_Eye.Pause();
	}

	private void ExitState(AIState state)
	{
		switch (state)
		{
			case AIState.Idle:
				break;
			case AIState.Patrolling:
				_timerWalk.Stop();
				_timerAction.Stop();
				break;
			case AIState.Chasing:
				break;
			case AIState.Attacking:
				break;
			case AIState.Recovery:
				_timerAction.Stop();
				break;
		}
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

	private void ProcessIdle(double delta)
	{
		// Not moving
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

		var targetRotation = Transform.LookingAt(_player.GlobalPosition, Vector3.Up).Basis;
		Basis = Basis.Orthonormalized().Slerp(targetRotation, (float)delta * ChaseRotationSpeed);

		if (GlobalPosition.DistanceTo(_player.GlobalPosition) > AttackRange)
		{
			// Move towards player
			WalkForward(ref newVelocity);
		}
		else
		{
			if (_timerAttackCooldown.IsStopped())
			{
				ChangeState(AIState.Attacking);
			}
		}
	}

	private void ProcessAttacking(ref Vector3 newVelocity)
	{
		// Waiting for animation to finish
		newVelocity = Vector3.Zero;
		;
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
		_timerWalk.WaitTime = GD.RandRange(MinWalkTime, MaxWalkTime);
		_timerWalk.Start();
	}

	private void Wander(ref Vector3 newVelocity)
	{
		// Set horizontal velocity to move forward.
		WalkForward(ref newVelocity);

		// Check for wall collision and change direction.
		if (IsOnWall())
		{
			float randomAngle = (float)GD.RandRange(0, Mathf.Pi * 2);
			Rotation = new Vector3(0, randomAngle, 0);
		}
	}

	private void OnWalkTimerTimeout()
	{
		StartWaiting();
	}

	private void StartWaiting()
	{
		_isWalking = false;
		_timerAction.WaitTime = GD.RandRange(MinWaitTime, MaxWaitTime);
		_timerAction.Start();
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

		_animPlayer.Play(animName);

		// if (_animatedSprite.Animation != animName)
		// {
		// 	_animatedSprite.Play(animName);
		// 	_animatedSprite_Eye.Play(animName);
		// }

		_animatedSprite.FlipH = flipH;
		_animatedSprite_Eye.FlipH = flipH;
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
		base.OnHurtboxBodyEntered(body); // Handles damage + projectile destruction

		if (body is Projectile projectile && projectile.Owner != this)
		{
			projectile.OnEnemyHit(projectile.GlobalPosition);

			// Enemy-specific: Spawn mana particles as a refund
			float refundPercent = (float)GD.RandRange(Mana_minRefundPercent, Mana_maxRefundPercent);
			int manaToSpawn = Mathf.RoundToInt(projectile.ManaCost * refundPercent);
			if (manaToSpawn > 0)
			{
				PickupManager.Instance.SpawnPickupAmount(PickupType.Mana, manaToSpawn, projectile.GlobalPosition);
			}
		}
	}

	public override void OnHurt(Vector3 sourcePosition, float damage)
	{
		base.OnHurt(sourcePosition, damage);
		ChangeState(AIState.Chasing);
	}

	public override void PlayOnHurtFX()
	{
		var tween = GetTree().CreateTween();
		tween.TweenProperty(_animatedSprite, "modulate", Colors.Red, 0.1);
		tween.TweenProperty(_animatedSprite, "modulate", Colors.White, 0.1);
	}

	public override void OnDied()
	{
		DebugManager.Debug($"Enemy: {Name} OnDied called.");
		ChangeState(AIState.Dying);
	}

	private void TryToDie()
	{
		DebugManager.Debug($"Enemy: {Name} TryToDie called. Current state: {_currentState}\n" +
						   $"Death particles GlobalPosition before: {GlobalPosition}");

		if (_deathParticlesScene.Instantiate() is OneshotParticles deathParticles)
		{
			GetParent().AddChild(deathParticles);
			deathParticles.GlobalPosition = GlobalPosition;
			DebugManager.Debug($"Enemy: {Name} Death particles GlobalPosition after: {deathParticles.GlobalPosition}");
			deathParticles.PlayParticles(Data.DeathParticleCount);
		}

		_animPlayer.Play("Die");
		_timerBlink.Start();

		AudioManager.Instance.PlaySoundAtPosition(AudioData.DieSound, GlobalPosition);

		PickupManager.Instance.SpawnPickupAmount(PickupType.Mana, ManaAmountToDrop, this.GlobalPosition);
		PickupManager.Instance.SpawnPickupAmount(PickupType.Money, MoneyAmountToDrop, this.GlobalPosition);

		StopMoving();
		StopActionTimers();
		DisableCollisions();
		EmitSignalEnemyDied(this);

		_timerPool.Start();
	}

	private void Despawn()
	{
		DebugManager.Debug($"Enemy: {Name} Animation timer finished. Releasing enemy.");
		if (OwningPool != null) OwningPool.Release(this);
		else QueueFree();
	}

	public override void Reset()
	{
		base.Reset();

		// Add any enemy-specific reset logic here
		Activate();
		_animPlayer.Stop(); // Stop any playing animation
		_animPlayer.Play("Front_Idle"); // Reset to a default idle animation
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
		foreach (var shape in _collisionShapes)
		{
			shape.Disabled = true;
		}

		Combat_hurtbox.SetDeferred("Monitorable", false);
		Combat_hurtbox.SetDeferred("Monitoring", false);

		DetectionArea.SetDeferred("Monitorable", false);
		DetectionArea.SetDeferred("Monitoring", false);

		if (Combat_meleeHitbox != null)
		{
			Combat_meleeHitbox.SetDeferred("Monitorable", false);
			Combat_meleeHitbox.SetDeferred("Monitoring", false);
		}
	}


	private void EnableCollisions()
	{
		foreach (var shape in _collisionShapes)
		{
			shape.Disabled = false;
		}

		Combat_hurtbox.SetDeferred("Monitorable", true);
		Combat_hurtbox.SetDeferred("Monitoring", true);

		DetectionArea.SetDeferred("Monitorable", true);
		DetectionArea.SetDeferred("Monitoring", true);

		if (Combat_meleeHitbox != null)
		{
			Combat_meleeHitbox.SetDeferred("Monitorable", true);
			Combat_meleeHitbox.SetDeferred("Monitoring", true);
		}
	}

	private void StopActionTimers()
	{
		_timerWalk?.Stop();
		_timerAction?.Stop();
		_timerAttackCooldown?.Stop();
	}

	public void Activate()
	{
		if (_isActive) return;

		_isActive = true;
		Visible = true;
		SetProcess(true);
		SetPhysicsProcess(true);

		foreach (var shape in _collisionShapes)
		{
			shape.Disabled = false;
		}
	}

	private void BlinkRoutine()
	{
		if (_timerBlink.IsStopped()) return;

		double timeLeft = _timerBlink.TimeLeft;

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
		if (BlinkTween != null && BlinkTween.IsRunning() || _animatedSprite == null) return;

		BlinkTween = CreateTween().SetLoops(2).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.InOut);
		BlinkTween.TweenProperty(_animatedSprite, "modulate:a", alpha, duration / 2);
		BlinkTween.TweenProperty(_animatedSprite, "modulate:a", 1.0f, duration / 2);
	}
}