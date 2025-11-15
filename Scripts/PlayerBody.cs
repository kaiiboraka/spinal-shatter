using Godot;
using System;
using Elythia;
using FPS_Mods.Scripts;
using Godot.Collections;

[GlobalClass]
public partial class PlayerBody : Combatant
{
	public static PlayerBody Instance;

	const float GRAVITY_MULTIPLIER = 2.00f;
	[Export] private Control controlRoot;

	// private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	[ExportGroup("PlayerMovementSettings")]
	[Export] float GRAVITY = 9.8f * GRAVITY_MULTIPLIER;

	[Export] float CROUCH_SPEED = 5;
	[Export] float WALK_SPEED = 20;
	[Export] float MAX_SPRINT_SPEED = 30;
	[Export] float ACCEL = 4.5f;
	[Export] float SPRINT_ACCEL = 18;
	[Export] float AIR_SPEED = 20;
	[Export] float JUMP_VELOCITY = 10;
	[Export] float DECEL = 16;
	const float MAX_SLOPE_ANGLE = 40;


	[ExportGroup("CameraSettings")]
	[Export] private float cameraLookSensitivity = 0.006f;

	[Export] private float bob_Speed = 1.0f;
	[Export] private float bob_Height = .15f;
	[Export] private float bob_Sway_Percent = 0f;
	[Export] private float t_bob = .0f;

	[Export] private float lookUpDegrees = 80f;
	[Export] private float lookDownDegrees = 65f;
	[Export] private float BaseFOV = 75f;
	[Export] private float FOV_change = 1.5f;
	private double fovJuiceWeight = 8.0f;

	[Signal] public delegate void ViewChangeEventHandler();

	private bool grounded = false;
	private bool isCrouching = false;
	private bool isSprinting = false;

	private int curJumps = 0;
	int maxJumps = 2;
	private int _currentMoney = 0;

	private Vector2 inputDir = Vector2.Zero;
	private Vector3 direction = Vector3.Zero;
	private Vector3 newVelocity = Vector3.Zero;


	private bool MouseIsCaptured => Input.MouseMode == Input.MouseModeEnum.Captured;

	public Vector2 InputDir => inputDir;

	private Node3D headNode;

	private Camera3D camera;

	// private Camera3D camera3P;
	private SpringArm3D arm;

	private Label currAmmoLabel;
	private Label maxAmmoLabel;

	private PlayerHealthBar _playerHealthBar;
	private Label _playerMoneyAmountLabel;

	[ExportGroup("Components")]
	[Export] private ManaComponent _manaComponent;
	[Export] private Area3D pickupArea;

	[ExportGroup("Menus")]
	[Export] private PackedScene _pauseMenuScene;
	[Export] private PackedScene _levelLostMenuScene;

	[ExportGroup("Combat")]
	[ExportSubgroup("Audio", "Audio")]
	[Export] private AudioStream Audio_Hurt;
	[Export] private AudioStream Audio_DieSFX;
	[Export] private AudioStream Audio_DieVoice;

	[Export] private AudioStream _footstepSoundsStream;
	private AudioStreamRandomizer Audio_FootstepSounds => _footstepSoundsStream as AudioStreamRandomizer;

	[Export] private AudioStream _footstepSprintSoundsStream;
	private AudioStreamRandomizer Audio_FootstepSprintSounds => _footstepSprintSoundsStream as AudioStreamRandomizer;

	private CollisionShape3D collider;
	private RayCast3D canStandUpRay;
	private RayCast3D footSoundRay;

	private bool standUpBlocked;
	private Timer _footstepCooldownTimer;
	private double _footstepMaxCooldown;
	private double _sprintFootstepMaxCooldown;

	// public Loadout loadout;
	private Vector3 spawnPosition = new(2.351f, 2, 28.564f);

	private Node3D parentLevel;
	public Node3D ParentLevel => parentLevel;


	// TODO : put the main scene back to this one : res://Scenes/UI/Menu Templates/scenes/opening/opening.tscn


	public override void _Ready()
	{
		base._Ready(); // Sets up HealthComponent, hurtbox, etc.
		Instance = this;

		headNode = GetNode<Node3D>("%Head");
		arm = GetNode<SpringArm3D>("%CameraArm");
		camera = GetNode<Camera3D>("%Camera1P");

		// camera3P = GetNode<Camera3D>("%Camera3P");
		collider = GetNode<CollisionShape3D>("%PlayerCollider");
		canStandUpRay = GetNode<RayCast3D>("%StandUpRay");

		// loadout = GetNode<Loadout>("%Loadout");
		currAmmoLabel = GetNode<Label>("%CurrAmmoText");
		maxAmmoLabel = GetNode<Label>("%MaxAmmoText");

		_manaComponent ??= GetNode<ManaComponent>("%ManaComponent");
		_manaComponent.ManaChanged += UpdateManaHUD;
		UpdateManaHUD(_manaComponent.CurrentMana, _manaComponent.MaxMana);

		_playerHealthBar = GetNode<PlayerHealthBar>("%PlayerHealthBar");
		_playerMoneyAmountLabel = GetNode<Label>("%MoneyAmountLabel");

		HealthComponent.HealthChanged += UpdateHealthHUD;
		UpdateHealthHUD(HealthComponent.CurrentHealth, HealthComponent.MaxHealth);
		
		pickupArea ??= GetNode<Area3D>("PickupArea");
		pickupArea.AreaEntered += OnAreaEnteredPickupArea;

		parentLevel = GetParent() as Node3D;

		Input.MouseMode = Input.MouseModeEnum.Captured;
		
		_footstepCooldownTimer = new Timer { OneShot = true };
		AddChild(_footstepCooldownTimer);

		if (_footstepSoundsStream != null && Audio_FootstepSounds == null)
		{
			DebugManager.Error("PlayerBody: _footstepSoundsStream is not a valid AudioStreamRandomizer.");
		}
		if (_footstepSprintSoundsStream != null && Audio_FootstepSprintSounds == null)
		{
			DebugManager.Error("PlayerBody: _footstepSprintSoundsStream is not a valid AudioStreamRandomizer.");
		}

		_footstepMaxCooldown = Audio_FootstepSounds.GetMaxLength();
		_sprintFootstepMaxCooldown = Audio_FootstepSprintSounds.GetMaxLength() / 1.2f;
		DebugManager.Info($"PlayerBody: Calculated max footstep cooldown: {_footstepMaxCooldown}");
		DebugManager.Info($"PlayerBody: Calculated max sprint footstep cooldown: {_sprintFootstepMaxCooldown}");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Camera Rotation
		if (@event is InputEventMouseMotion motion && MouseIsCaptured)
		{
			this.RotateY(-motion.Relative.X * cameraLookSensitivity);
			camera.RotateX(-motion.Relative.Y * cameraLookSensitivity);

			Vector3 cameraRot = camera.Rotation;
			cameraRot.X = Mathf.Clamp(cameraRot.X, Mathf.DegToRad(-lookUpDegrees), Mathf.DegToRad(lookUpDegrees));
			camera.Rotation = cameraRot;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta); // Decays knockback
		ProcessInput(delta);
		ProcessMovement(delta);
	}

	private void ProcessInput(double delta)
	{
		direction = Vector3.Zero;

		inputDir = Input
				  .GetVector("Player_Move_Left", "Player_Move_Right", "Player_Move_Forward", "Player_Move_Backward")
				  .Normalized();
		direction = (headNode.GlobalTransform.Basis * new Vector3(InputDir.X, 0, InputDir.Y)).Normalized();

		// Jump
		if (Input.IsActionJustPressed("Player_Jump"))
		{
			TryJump();
		}

		if (Input.IsActionPressed("Player_Shoot"))
		{
			// TryShoot();
		}

		if (Input.IsActionJustPressed("Player_Reload"))
		{
			// loadout.CurrentMag.Reload();
		}

		if (Input.IsActionJustPressed("Player_Teleport"))
		{
			Position = spawnPosition;
			Rotation = Vector3.Zero;
		}

		if (Input.IsActionJustPressed("Debug_Refresh_Scene"))
		{
			GetTree().ReloadCurrentScene();
		}

		if (Input.IsActionJustPressed("Debug_ViewChange"))
		{
			EmitSignal(SignalName.ViewChange);
		}

		SprintAndCrouch();

		if (Input.IsActionJustPressed("ui_cancel"))
			ToggleMouseMode();

		if (Input.IsActionJustPressed("Player_Pause"))
		{
			var pauseMenu = _pauseMenuScene.Instantiate();
			controlRoot.AddChild(pauseMenu);

			// GetTree().Paused = true;
		}
	}

	private void TryShoot()
	{
		// loadout.Shoot();
	}

	private void ProcessMovement(double delta)
	{
		// Can Stand Up Ray
		standUpBlocked = canStandUpRay.IsColliding();

		grounded = IsOnFloor();

		if (!grounded)
			newVelocity.Y -= GRAVITY * (float)delta;

		var hVel = Velocity.XZ();

		var target = direction;

		if (isSprinting)
		{
			target *= MAX_SPRINT_SPEED;
		}
		else if (isCrouching)
		{
			target *= CROUCH_SPEED;
		}
		else
		{
			target *= WALK_SPEED;
		}

		float acceleration = ACCEL;
		if (direction.Dot(hVel) > 0)
		{
			if (isSprinting && grounded)
			{
				acceleration = SPRINT_ACCEL;
			}
			else
			{
				acceleration = ACCEL;
			}
		}
		else
		{
			acceleration = DECEL;
		}

		hVel = hVel.Lerp(target, (float)(acceleration * delta));

		PlayFootsteps(hVel);

		newVelocity.X = hVel.X;
		newVelocity.Z = hVel.Z;

		// Apply knockback
		newVelocity += _knockbackVelocity;

		Velocity = newVelocity;

		FOVJuice(delta);

		HeadBob(delta);

		MoveAndSlide();
	}

	private void PlayFootsteps(Vector3 hVel)
	{
		if (!grounded || !_footstepCooldownTimer.IsStopped()) return;
		if (hVel.Length() <= Mathf.Epsilon) return;

		AudioStream sound;
		double cooldown;

		if (isSprinting)
		{
			if (Audio_FootstepSprintSounds == null)
			{
				DebugManager.Error("Audio_FootstepSprintSounds is null in PlayFootsteps (sprinting).");
				return;
			}
			sound = Audio_FootstepSprintSounds;
			cooldown = _sprintFootstepMaxCooldown; // faster steps
			// DebugManager.Info("Playing sprinting footstep sound.");
		}
		else
		{
			if (Audio_FootstepSounds == null)
			{
				DebugManager.Error("Audio_FootstepSounds is null in PlayFootsteps (walking).");
				return;
			}
			sound = Audio_FootstepSounds;
			cooldown = _footstepMaxCooldown;
			// DebugManager.Info("Playing walking footstep sound.");
		}
		// DebugManager.Info($"Footstep cooldown is {cooldown}.");

		// Ensure cooldown is a positive value to prevent timer errors.
		if (cooldown <= 0)
		{
			DebugManager.Warning($"Using default 0.5s to prevent crash.");
			cooldown = 0.5;
		}

		AudioManager.Instance.PlaySoundAttachedToNode(sound, this);
		_footstepCooldownTimer.WaitTime = cooldown;
		_footstepCooldownTimer.Start();
	}

	private void FOVJuice(double delta)
	{
		// if (!firstPerson) return;

		var clampedVel = Mathf.Clamp(Velocity.Length(), 0.5, MAX_SPRINT_SPEED * 2);
		var targetFOV = BaseFOV + (FOV_change * clampedVel);
		camera.Fov = camera.Fov.Lerp(targetFOV, delta * fovJuiceWeight);
	}

	private void HeadBob(double delta)
	{
		// if (!firstPerson) return;

		// bool canBob = grounded &&;
		var hVel = Velocity.XZ().Length();
		t_bob += ((float)delta) * hVel * (grounded ? 1 : 0);
		var camTran = camera.Transform;

		var pos = Vector3.Zero;
		pos.Y = Mathf.Sin(t_bob * bob_Speed) * bob_Height;
		camTran.Origin = pos;
		camera.Transform = camTran;
	}

	public void SprintAndCrouch()
	{
		// Sprint
		isSprinting = (Input.IsActionPressed("Player_Sprint") && ((CapsuleShape3D)collider.Shape).Height == 2);
		isCrouching = Input.IsActionPressed("Player_Crouch");
		if (isSprinting)
		{
		}

		// Crouch
		else if (isCrouching)
		{
			((CapsuleShape3D)collider.Shape).Height -= 0.1f;
			((CapsuleShape3D)collider.Shape).Height = Mathf.Clamp(((CapsuleShape3D)collider.Shape).Height, 1f, 2f);
		}
		else
		{
			if (standUpBlocked == false)
			{
				((CapsuleShape3D)collider.Shape).Height += 0.1f;
				((CapsuleShape3D)collider.Shape).Height = Mathf.Clamp(((CapsuleShape3D)collider.Shape).Height, 1f, 2f);
			}
		}
	}

	void TryJump()
	{
		if (CanJump())
		{
			newVelocity.Y = JUMP_VELOCITY;
			curJumps += 1;
		}
	}

	bool CanJump()
	{
		var grounded = IsOnFloor();
		if (grounded)
		{
			curJumps = 0;
		}

		bool jumpsRemain = curJumps < maxJumps;
		return jumpsRemain && !standUpBlocked;
	}

	void ToggleMouseMode()
	{
		if (MouseIsCaptured)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public void UpdateManaHUD(float newCurr, float newMax)
	{
		currAmmoLabel.Text = Mathf.RoundToInt(newCurr).ToString();
		maxAmmoLabel.Text = Mathf.RoundToInt(newMax).ToString();
	}

	public void UpdateHealthHUD(float newCurr, float newMax)
	{
		_playerHealthBar.OnHealthChanged(newCurr, newMax);
	}

	public override void OnHurtboxBodyEntered(Node3D body)
	{
		base.OnHurtboxBodyEntered(body);
	}

	public override void PlayOnHurtFX()
	{
		// var tween = GetTree().CreateTween();
		// tween.TweenProperty(_animatedSprite, "modulate", Colors.Red, 0.1);
		// tween.TweenProperty(_animatedSprite, "modulate", Colors.White, 0.1);
		AudioManager.Instance.PlaySoundAttachedToNode(Audio_Hurt, this);
		//TODO: add UI overlay
	}


	public override void OnHurt(Vector3 sourcePosition, float damage)
	{
		base.OnHurt(sourcePosition, damage);
	}

	public override void OnDied()
	{
		AudioManager.Instance.PlaySoundAttachedToNode(Audio_DieVoice, this);
		AudioManager.Instance.PlaySoundAtPosition(Audio_DieSFX, GlobalPosition);

		// AudioPlayer_MiscFX.Finished += () =>
		// 	{
		// 		// GetTree().CreateTimer(2f, true).Timeout += () =>
		// 		{
		// 			var levelLostMenu = _levelLostMenuScene.Instantiate();
		// 			controlRoot.AddChild(levelLostMenu);
		// 		};
		// 	};

	}

	// private void OnBodyEnteredPickupArea(Node3D body)
	// {
	//     if (body is ManaParticle particle)
	//     {
	//         PickupManaParticle(particle);
	//     }
	// }

	private void OnAreaEnteredPickupArea(Area3D area)
	{
		if (area.GetOwner() is ManaParticle particle)
		{
			// GD.Print($"{Time.GetTicksMsec()}: PlayerBody: PickupArea entered by ManaParticle {particle.Name}");
			PickupManaParticle(particle);
		}
		else if (area.GetOwner() is Money moneyPickup)
		{
			CollectMoneyPickup(moneyPickup);
		}
	}

	private void PickupManaParticle(ManaParticle particle)
	{
		if (particle.State == Pickup.PickupState.Collected) return; // Already collected

		_manaComponent.AddMana(particle.Value);
		particle.Collect();

		PickupManager.Instance.Release(particle);
	}

	private void CollectMoneyPickup(Money moneyParticle)
	{
		if (moneyParticle.State == Pickup.PickupState.Collected) return; // Already collected

		AddMoney(moneyParticle.Value);
		moneyParticle.Collect();
		PickupManager.Instance.Release(moneyParticle);
	}

	public void AddMoney(int amount)
	{
		_currentMoney += amount;
		_currentMoney = _currentMoney.AtLeastZero();
		_playerMoneyAmountLabel.Text = _currentMoney.ToString();
	}


	public void RefillMana()
	{
		_manaComponent.RefillMana();
	}

	public static void FillPlayerMana()
	{
		Instance.RefillMana();
	}
}