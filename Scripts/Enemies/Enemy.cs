using System.Collections.Generic;
using Elythia;
using Godot;

public partial class Enemy : CharacterBody3D, ITakeDamage
{
	private AIState _currentState = AIState.Idle;

	[ExportGroup("Components")]
	[Export] private AnimationPlayer _animPlayer;

	[Export] private AnimatedSprite3D _animatedSprite;
	[Export] private AnimatedSprite3D _animatedSprite_Eye;
	[Export] private Area3D _hurtbox;
	[Export] private HealthComponent HealthComponent { get; set; }
	[Export] private OverheadHealthBar OverheadHealthBar { get; set; }
	[Export] private StateSprite3d _stateVisual;

	[ExportSubgroup("Audio", "AudioStream")]
	[Export] public AudioStreamPlayer3D AudioPlayer_Die { get; private set; }

	[Export] public AudioStreamPlayer3D AudioPlayer_Hurt { get; private set; }
	[Export] public AudioStreamPlayer3D AudioPlayer_Cast { get; private set; }

	[ExportSubgroup("Timers", "_timer")]
	[Export] private Timer _timerWalk;

	[Export] private Timer _timerWait;
	[Export] private Timer _timerAttackCooldown;

	[ExportGroup("Patrol")]
	[Export] public float WalkSpeed { get; private set; } = 3.0f;

	[Export] public float MinWalkTime { get; private set; } = 1.0f;
	[Export] public float MaxWalkTime { get; private set; } = 5.0f;
	[Export] public float MinWaitTime { get; private set; } = 1.0f;
	[Export] public float MaxWaitTime { get; private set; } = 5.0f;

	[ExportGroup("Combat")]
	[ExportSubgroup("Mana", "Mana")]
	[Export] public int ManaToDrop { get; private set; } = 10;

	[Export(PropertyHint.Range, "0.0, 1.0")]
	private float Mana_minRefundPercent = 0.05f;

	[Export(PropertyHint.Range, "0.0, 1.0")]
	private float Mana_maxRefundPercent = 0.30f;

	[ExportSubgroup("Detection", "Detection")]
	[Export] private Area3D DetectionArea;

	[Export] private RayCast3D Detection_lineOfSight;

	[ExportSubgroup("Knockback", "Knockback")]
	[Export] private float KnockbackWeight { get; set; } = 5.0f;

	private float KnockbackDecay = 0.99f;
	private Vector3 KnockbackVelocity = Vector3.Zero;

	[ExportSubgroup("Attack", "Attack")]
	[Export] private float AttackRange { get; set; } = 2.0f;

	[Export] private float AttackCooldown { get; set; } = 1.5f;
	[Export] public float AttackDamage { get; private set; } = 10f;

	[Export] private Area3D Attack_meleeHitbox;

	[ExportSubgroup("Projectiles", "Projectile")]
	[Export(PropertyHint.GroupEnable, "")] public bool ProjectileIsRanged { get; private set; }

	[Export] private float ProjectileSpeed { get; set; } = 20.0f;
	[Export] private PackedScene ProjectileScene;
	[Export] private Node3D ProjectileSpawnPoint;


	private PlayerBody _player;

	private bool _isWalking = false;


	[Signal]
	public delegate void EnemyDiedEventHandler(Enemy who);

	public ObjectPoolManager<Node3D> OwningPool { get; set; }

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _Ready()
	{
		HealthComponent ??= GetNode<HealthComponent>("%HealthComponent");
		OverheadHealthBar ??= GetNode<OverheadHealthBar>("%HealthBar");

		HealthComponent.Died += OnDied;
		HealthComponent.HealthChanged += OverheadHealthBar.OnHealthChanged;
		HealthComponent.Hurt += OnHurt;

		_hurtbox.BodyEntered += OnHurtboxBodyEntered;

		DetectionArea.BodyEntered += OnDetectionAreaBodyEntered;
		DetectionArea.BodyExited += OnDetectionAreaBodyExited;

		TryAddTimer(_timerWalk);
		_timerWalk.Timeout += OnWalkTimerTimeout;

		TryAddTimer(_timerWait);
		_timerWait.Timeout += OnWaitTimerTimeout;

		TryAddTimer(_timerAttackCooldown);

		_animatedSprite.AnimationFinished += OnAnimationFinished;

		if (!ProjectileIsRanged)
		{
			Attack_meleeHitbox.AreaEntered += area =>
			{
				if (area.Owner is PlayerBody player)
				{
					player.TakeDamage(AttackDamage, GlobalPosition);
				}
			};
		}

		// Start patrolling
		ChangeState(AIState.Patrolling);
	}

	private void TryAddTimer(Timer timer)
	{
		if (timer == null)
		{
			timer = new Timer { OneShot = true };
			AddChild(timer);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 newVelocity = Velocity;

		// Add gravity.
		if (!IsOnFloor())
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
					ProcessChasing(ref newVelocity);
					break;
				case AIState.Attacking:
					ProcessAttacking(ref newVelocity);
					break;
			}
		}

		// Apply knockback
		newVelocity += KnockbackVelocity;
		KnockbackVelocity = KnockbackVelocity.Lerp(Vector3.Zero, KnockbackDecay * (float)delta);

		if (_player != null)
		{
			Detection_lineOfSight.TargetPosition = _player.GlobalPosition - GlobalPosition;
			if (Detection_lineOfSight.GetCollider() == _player)
			{
				ChangeState(AIState.Chasing);
			}

			// Update animation based on angle to player
			if (_currentState != AIState.Attacking)
			{
				Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
				Vector3 enemyForward = -GlobalTransform.Basis.Z;
				float angleToPlayer = Mathf.RadToDeg(enemyForward.SignedAngleTo(toPlayer, Vector3.Up));
				UpdateAnimation(angleToPlayer);
			}
		}

		Velocity = newVelocity;
		MoveAndSlide();
	}

	private void ChangeState(AIState newState, bool force = false)
	{
		if (_currentState == newState && !force) return;

		ExitState(_currentState);
		_currentState = newState;
		EnterState(_currentState);

		if (_stateVisual != null)
		{
			_stateVisual.CurrentState = newState;
		}
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

				_animPlayer.Play("Front_Attack");

				// PlayAnimationOnSprites("Front_Attack");
				// PerformAttack();
				_timerAttackCooldown.WaitTime = AttackCooldown;
				_timerAttackCooldown.Start();
				break;
		}
	}

	private void PerformAttack()
	{
		if (ProjectileIsRanged)
		{
			var projectile = ProjectileScene.Instantiate<Projectile>();
			GetTree().Root.AddChild(projectile);
			projectile.GlobalPosition = ProjectileSpawnPoint.GlobalPosition;
			var direction = (_player.GlobalPosition - GlobalPosition).Normalized();
			projectile.Launch(Owner, AttackDamage, 0, direction * ProjectileSpeed);
		}
		else //if (Attack_meleeHitbox != null)
		{
			// Melee attack logic (handled by animation keyframes)
		}
	}

	private void PlayAnimationOnSprites(string which)
	{
		_animatedSprite.Play(which);
		_animatedSprite_Eye.Play(which);
	}

	private void ExitState(AIState state)
	{
		switch (state)
		{
			case AIState.Idle:
				break;
			case AIState.Patrolling:
				_timerWalk.Stop();
				_timerWait.Stop();
				break;
			case AIState.Chasing:
				break;
			case AIState.Attacking:
				break;
		}
	}

	private void OnDetectionAreaBodyEntered(Node3D body)
	{
		if (body is PlayerBody player)
		{
			_player = player;
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

	private void ProcessChasing(ref Vector3 newVelocity)
	{
		if (_player == null)
		{
			ChangeState(AIState.Patrolling);
			return;
		}

		LookAt(_player.GlobalPosition, Vector3.Up);

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

	private void WalkForward(ref Vector3 newVelocity)
	{
		newVelocity.X = -GlobalTransform.Basis.Z.X * WalkSpeed;
		newVelocity.Z = -GlobalTransform.Basis.Z.Z * WalkSpeed;
	}

	private void ProcessAttacking(ref Vector3 newVelocity)
	{
		// Waiting for animation to finish
		newVelocity = Vector3.Zero;
		;
	}

	private void OnAnimationFinished()
	{
		if (_animatedSprite.Animation == "Front_Attack")
		{
			ChangeState(AIState.Chasing);
		}
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

	private void OnWaitTimerTimeout()
	{
		Rotation = new Vector3(0, (float)GD.RandRange(0, Mathf.Pi * 2), 0);
		StartWalking();
	}

	private void StartWalking()
	{
		_isWalking = true;
		_timerWalk.WaitTime = GD.RandRange(MinWalkTime, MaxWalkTime);
		_timerWalk.Start();
	}

	private void OnWalkTimerTimeout()
	{
		StartWaiting();
	}

	private void StartWaiting()
	{
		_isWalking = false;
		_timerWait.WaitTime = GD.RandRange(MinWaitTime, MaxWaitTime);
		_timerWait.Start();
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

	public void OnHurtboxBodyEntered(Node3D body)
	{
		if (body is Projectile projectile)
		{
			// Take damage from the projectile
			TakeDamage(projectile.Damage, projectile.GlobalPosition);

			// Spawn mana particles as a refund
			float refundPercent = (float)GD.RandRange(Mana_minRefundPercent, Mana_maxRefundPercent);
			int manaToSpawn = Mathf.RoundToInt(projectile.InitialManaCost * refundPercent);
			if (manaToSpawn > 0)
			{
				ManaParticleManager.Instance.SpawnMana(manaToSpawn, projectile.GlobalPosition);
			}

			// Destroy the projectile
			projectile.QueueFree();
		}
	}

	public void TakeDamage(float amount, Vector3 sourcePosition)
	{
		HealthComponent.TakeDamage(amount, sourcePosition);
	}

	public void OnHurt(Vector3 sourcePosition, float damage)
	{
		PlayOnHurtFX();

		// GD.Print($"{Time.GetTicksMsec()}: Enemy {Name} OnHurt: GlobalPosition={{GlobalPosition}}, SourcePosition={{sourcePosition}}");
		var direction = (GlobalPosition - sourcePosition).XZ().Normalized() + new Vector3(0, 0.1f, 0);

		// GD.Print($"{Time.GetTicksMsec()}: Enemy {Name} OnHurt: Calculated Knockback Direction={{direction}}");
		KnockbackVelocity = direction * (damage / KnockbackWeight); // + Vector3.Up;
		ChangeState(AIState.Chasing);
	}

	public void PlayOnHurtFX()
	{
		var tween = GetTree().CreateTween();
		tween.TweenProperty(_animatedSprite, "modulate", Colors.Red, 0.1);
		tween.TweenProperty(_animatedSprite, "modulate", Colors.White, 0.1);
	}

	public void OnDied()
	{
		EmitSignal(SignalName.EnemyDied, this);

		ManaParticleManager.Instance.SpawnMana(ManaToDrop, this.GlobalPosition);

		if (OwningPool != null)
		{
			OwningPool.Release(this);
		}
		else
		{
			QueueFree(); // Failsafe for enemies not spawned from a pool
		}
	}

	public void Reset()
	{
		HealthComponent.Reset();
	}
}

public interface ITakeDamage
{
	public void OnHurt(Vector3 sourcePosition, float damage);
	public void OnHurtboxBodyEntered(Node3D body);
	public void TakeDamage(float amount, Vector3 sourcePosition);
	public void PlayOnHurtFX();
	public void OnDied();
}