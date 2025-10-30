using System.Collections.Generic;
using Elythia;
using Godot;

public partial class Enemy : CharacterBody3D
{
	private enum AIState
	{
		Idle,
		Patrolling,
		Chasing,
		Attacking
	}

	private Dictionary<AIState, string> stateEmoji = new Dictionary<AIState, string>()
	{
		{ AIState.Idle, "⌚" },
		{ AIState.Patrolling, "👁️" },
		{ AIState.Chasing, "🏃‍♂️" },
		{ AIState.Attacking, "⚔️" }
	};

	private AIState _currentState = AIState.Idle;

	[ExportGroup("Components")]
	[Export] private AnimatedSprite3D _animatedSprite;

	[Export] private AnimatedSprite3D _animatedSprite_Eye;
	[Export] private Area3D _hurtbox;
	[Export] private HealthComponent HealthComponent { get; set; }
	[Export] private OverheadHealthBar OverheadHealthBar { get; set; }
	[Export] private RichTextLabel _stateLabel;

	[ExportSubgroup("Audio","AudioStream")]
	[Export] public AudioStreamPlayer3D AudioStream_Vocal { get; private set; }
	[Export] public AudioStream AudioStream_Hurt { get; private set; }
	[Export] public AudioStream AudioStream_Movement { get; private set; }

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
	[Export(PropertyHint.Range, "0.0, 1.0")] private float Mana_minRefundPercent = 0.05f;
	[Export(PropertyHint.Range, "0.0, 1.0")] private float Mana_maxRefundPercent = 0.30f;

	[ExportSubgroup("Detection", "Detection")]
	[Export] private Area3D DetectionArea;
	[Export] private RayCast3D Detection_lineOfSight;

	[ExportSubgroup("Attack", "Attack")]
	[Export] private float AttackRange { get; set; } = 2.0f;
	[Export] private float AttackCooldown { get; set; } = 1.5f;
	[Export] private Area3D Attack_meleeHitbox;
	[Export] private float AttackDamage { get; set; } = 10f;

	[ExportSubgroup("Knockback", "Knockback")]
	[Export] private float KnockbackStrength { get; set; } = 5.0f;
	[Export] private float KnockbackUpwardForce { get; set; } = 2.0f;

	private PlayerBody _player;
	private Vector3 _knockbackVelocity = Vector3.Zero;
	[Export] private float _knockbackDecay = 0.9f;

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
		HealthComponent.HealthChanged += OnHealthChanged;
		HealthComponent.Hurt += OnHurt;

		_hurtbox.BodyEntered += OnHurtboxBodyEntered;

		DetectionArea.BodyEntered += OnDetectionAreaBodyEntered;
		DetectionArea.BodyExited += OnDetectionAreaBodyExited;

		// Initialize Health Bar
		OverheadHealthBar.Initialize(HealthComponent.MaxHealth);

		TryAddTimer(_timerWalk);
		_timerWalk.Timeout += OnWalkTimerTimeout;

		TryAddTimer(_timerWait);
		_timerWait.Timeout += OnWaitTimerTimeout;

		TryAddTimer(_timerAttackCooldown);

		_animatedSprite.AnimationFinished += OnAnimationFinished;

		if (Attack_meleeHitbox != null) Attack_meleeHitbox.BodyEntered += OnAttackMeleeHitboxBodyEntered;

		_stateLabel.Text = "";
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
		Vector3 velocity = Velocity;

		// Add gravity.
		if (!IsOnFloor())
			velocity.Y -= _gravity * (float)delta;

		Velocity = velocity;

		switch (_currentState)
		{
			case AIState.Idle:
				ProcessIdle(delta);
				break;
			case AIState.Patrolling:
				ProcessPatrolling(delta);
				break;
			case AIState.Chasing:
				ProcessChasing(delta);
				break;
			case AIState.Attacking:
				ProcessAttacking(delta);
				break;
		}

		// Apply knockback
		Velocity += _knockbackVelocity;
		_knockbackVelocity = _knockbackVelocity.Lerp(Vector3.Zero, _knockbackDecay * (float)delta);

		if (_player != null)
		{
			Detection_lineOfSight.TargetPosition = _player.GlobalPosition - GlobalPosition;
			if (Detection_lineOfSight.GetCollider() == _player)
			{
				ChangeState(AIState.Chasing);
			}
		}

		// Update animation based on angle to player
		if (_player != null && _currentState != AIState.Attacking)
		{
			Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
			Vector3 enemyForward = -GlobalTransform.Basis.Z;
			float angleToPlayer = Mathf.RadToDeg(enemyForward.SignedAngleTo(toPlayer, Vector3.Up));
			UpdateAnimation(angleToPlayer);
		}

		MoveAndSlide();
	}

	private void ChangeState(AIState newState)
	{
		if (_currentState == newState) return;

		ExitState(_currentState);
		_currentState = newState;
		EnterState(_currentState);

		if (_stateLabel != null)
		{
			_stateLabel.Text = stateEmoji[newState];
		}
	}

	private void EnterState(AIState state)
	{
		switch (state)
		{
			case AIState.Idle:
				PlayAnimationOnSprites("Front_Idle");
				break;
			case AIState.Patrolling:
				PlayAnimationOnSprites("Front_Idle");
				;
				StartWaiting();
				break;
			case AIState.Chasing:
				PlayAnimationOnSprites("Front_Idle");
				break;
			case AIState.Attacking:
				Velocity = Vector3.Zero;
				PlayAnimationOnSprites("Front_Attack");
				_timerAttackCooldown.WaitTime = AttackCooldown;
				_timerAttackCooldown.Start();
				break;
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
		Velocity = Vector3.Zero;
	}

	private void ProcessPatrolling(double delta)
	{
		if (_isWalking)
		{
			Wander(delta);
		}
		else
		{
			Velocity = new Vector3(0, Velocity.Y, 0);
		}
	}

	private void ProcessChasing(double delta)
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
			var velocity = Velocity;
			velocity.X = -GlobalTransform.Basis.Z.X * WalkSpeed;
			velocity.Z = -GlobalTransform.Basis.Z.Z * WalkSpeed;
			Velocity = velocity;
		}
		else
		{
			if (_timerAttackCooldown.IsStopped())
			{
				ChangeState(AIState.Attacking);
			}
		}
	}

	private void ProcessAttacking(double delta)
	{
		// Waiting for animation to finish
	}

	private void OnAttackMeleeHitboxBodyEntered(Node3D body)
	{
		if (body is PlayerBody player)
		{
			player.HealthComponent.TakeDamage(AttackDamage, GlobalPosition);
		}
	}

	private void OnAnimationFinished()
	{
		if (_animatedSprite.Animation == "Front_Attack")
		{
			ChangeState(AIState.Chasing);
		}
	}

	private void Wander(double delta)
	{
		// Set horizontal velocity to move forward.
		var velocity = Velocity;
		velocity.X = -GlobalTransform.Basis.Z.X * WalkSpeed;
		velocity.Z = -GlobalTransform.Basis.Z.Z * WalkSpeed;
		Velocity = velocity;

		// Check for wall collision and change direction.
		if (IsOnWall())
		{
			float randomAngle = (float)GD.RandRange(0, Mathf.Pi * 2);
			Rotation = new Vector3(0, randomAngle, 0);
		}
	}

	private void OnWalkTimerTimeout()
	{
		_isWalking = false;
		_timerWait.WaitTime = GD.RandRange(MinWaitTime, MaxWaitTime);
		_timerWait.Start();
	}

	private void OnWaitTimerTimeout()
	{
		_isWalking = true;
		_timerWalk.WaitTime = GD.RandRange(MinWalkTime, MaxWalkTime);
		_timerWalk.Start();
		Rotation = new Vector3(0, (float)GD.RandRange(0, Mathf.Pi * 2), 0);
	}

	private void StartWalking()
	{
		_isWalking = true;
		_timerWalk.WaitTime = GD.RandRange(MinWalkTime, MaxWalkTime);
		_timerWalk.Start();
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

		if (_animatedSprite.Animation != animName)
		{
			_animatedSprite.Play(animName);
			_animatedSprite_Eye.Play(animName);
		}

		_animatedSprite.FlipH = flipH;
		_animatedSprite_Eye.FlipH = flipH;
	}

	private void OnHurt(Vector3 sourcePosition)
	{
		var direction = (GlobalPosition - sourcePosition).Normalized();
		Velocity = direction * KnockbackStrength + Vector3.Up * KnockbackUpwardForce;
		ChangeState(AIState.Chasing);
	}

	private void OnHurtboxBodyEntered(Node3D body)
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

	private void OnHealthChanged(float oldHealth, float newHealth)
	{
		OverheadHealthBar.OnHealthChanged(newHealth, HealthComponent.MaxHealth);
	}

	public void TakeDamage(float amount, Vector3 sourcePosition)
	{
		HealthComponent.TakeDamage(amount, sourcePosition);
		FlashRed();
	}

	private void FlashRed()
	{
		var tween = GetTree().CreateTween();
		tween.TweenProperty(_animatedSprite, "modulate", Colors.Red, 0.1);
		tween.TweenProperty(_animatedSprite, "modulate", Colors.White, 0.1);
	}

	private void OnDied()
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