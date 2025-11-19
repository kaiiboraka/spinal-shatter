using System;
using Godot;
using Elythia;
using Godot.Collections;

namespace SpinalShatter;

[GlobalClass]
public partial class PlayerBody : Combatant
{
	public static PlayerBody Instance;

	const float GRAVITY_MULTIPLIER = 2.00f;

	public Control ControlRoot { get; private set; }

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

	[Signal] public delegate void PlayerDiedEventHandler();

	private bool grounded = false;
	private bool isCrouching = false;
	private bool isSprinting = false;
	private bool deadNow = false;

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
	private MinMaxValuesLabel _manaMinMaxLabel;
	private PlayerHealthBar _playerHealthBar;
	private Label _playerMoneyAmountLabel;
	private ManaComponent _manaComponent;
	private Area3D pickupArea;

	[ExportGroup("Menus")]
	[Export] private PackedScene _pauseMenuScene;

	private AudioData AudioData;

	[ExportCategory("Combat")]
	[ExportSubgroup("Knockback", "Knockback")]
	[Export] public new float KnockbackWeight { get; private set; } = 5f;

	private CollisionShape3D collider;
	private RayCast3D canStandUpRay;
	private RayCast3D footSoundRay;

	private bool standUpBlocked;
	private Timer _footstepCooldownTimer;
	private double _footstepMaxCooldown = 2f;
	private double _sprintFootstepMaxCooldown = 2f / 1.2f;

	// public Loadout loadout;
	private Vector3 spawnPosition = new(2.351f, 2, 28.564f);

	private Node3D parentLevel;
	public Node3D ParentLevel => parentLevel;


	private AudioFile AudioFile_Walk => (AudioFile)AudioData["Move_Walk"];
	private AudioFile AudioFile_Sprint => (AudioFile)AudioData["Move_Sprint"];

	private AudioStreamPlayer AudioPlayer_Global;
	private AudioStreamPlayer3D AudioPlayer_Voice;
	private AudioStreamPlayer3D AudioPlayer_Oof;
	private AudioStreamPlayer3D AudioPlayer_Money;
	private AudioStreamPlayer3D AudioPlayer_Mana;
	private AudioStreamPlayer3D AudioPlayer_Footsteps;

	// TODO : put the main scene back to this one : res://Scenes/UI/Menu Templates/scenes/opening/opening.tscn

	public override void _Ready()
	{
		base._Ready(); // GetComponents, ConnectEvents
		Instance = this;

		parentLevel = GetParent() as Node3D;

		Input.MouseMode = Input.MouseModeEnum.Captured;

		_footstepCooldownTimer = new Timer { OneShot = true };
		AddChild(_footstepCooldownTimer);


		_footstepMaxCooldown = (AudioFile_Walk.Stream as AudioStreamRandomizer).GetMaxLength();
		_sprintFootstepMaxCooldown = (AudioFile_Sprint.Stream as AudioStreamRandomizer).GetMaxLength() / 1.2f;

		// DebugManager.Info($"PlayerBody: Calculated max footstep cooldown: {_footstepMaxCooldown}");
		// DebugManager.Info($"PlayerBody: Calculated max sprint footstep cooldown: {_sprintFootstepMaxCooldown}");
		AddMoney(0);
		RefillMana();
		RefillLife();

		// Register player with WaveDirector
		if (GetTree().GetRoot().GetNode<WaveDirector>("WaveDirector") is WaveDirector waveDirector)
		{
			waveDirector.SetPlayer(this);
		}
		else
		{
			DebugManager.Error("PlayerBody: WaveDirector not found in scene tree!");
		}
	}

	protected override void GetComponents()
	{
		base.GetComponents();

		ControlRoot = GetNode<Control>("Control");
		headNode = GetNode<Node3D>("%Head");
		camera = GetNode<Camera3D>("%Camera1P");
		collider = GetNode<CollisionShape3D>("%PlayerCollider");
		canStandUpRay = GetNode<RayCast3D>("%StandUpRay");
		_manaMinMaxLabel = GetNode<MinMaxValuesLabel>("%Mana_MinMaxValuesLabel");
		_manaComponent = GetNode<ManaComponent>("%ManaComponent");
		_playerHealthBar = GetNode<PlayerHealthBar>("%PlayerHealthBar");
		_playerMoneyAmountLabel = GetNode<Label>("%MoneyAmountLabel");
		pickupArea = GetNode<Area3D>("PickupArea");

		// Audio Players
		AudioPlayer_Oof = GetNode<AudioStreamPlayer3D>("Audio/Oof_AudioStreamPlayer3D");
		AudioPlayer_Global = GetNode<AudioStreamPlayer>("Audio/Global_AudioStreamPlayer");
		AudioPlayer_Voice = GetNode<AudioStreamPlayer3D>("Audio/Voice_AudioStreamPlayer3D");
		AudioPlayer_Mana = GetNode<AudioStreamPlayer3D>("Audio/Mana_AudioStreamPlayer3D");
		AudioPlayer_Money = GetNode<AudioStreamPlayer3D>("Audio/Money_AudioStreamPlayer3D");
		AudioPlayer_Footsteps = GetNode<AudioStreamPlayer3D>("Audio/Footsteps_AudioStreamPlayer3D");

		var audioData = GD.Load<Resource>("res://assets/Audio/AudioData/AudioData_Player.tres");
		AudioData =  audioData as AudioData;
	}

	protected override void ConnectEvents()
	{
		base.ConnectEvents();

		_manaComponent.ManaChanged += UpdateManaHUD;
		UpdateManaHUD(_manaComponent.CurrentMana, _manaComponent.MaxMana);

		HealthComponent.HealthChanged += UpdateHealthHUD;
		UpdateHealthHUD(HealthComponent.CurrentHealth, HealthComponent.MaxHealth);

		pickupArea.AreaEntered += OnAreaEnteredPickupArea;
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
		ProcessInput(delta);
		ProcessMovement(delta);
	}

	private void ProcessInput(double delta)
	{
		if (deadNow) return;

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
			EmitSignalViewChange();
		}

		SprintAndCrouch();

		if (Input.IsActionJustPressed("ui_cancel"))
			ToggleMouseMode();

		if (Input.IsActionJustPressed("Player_Pause"))
		{
			var pauseMenu = _pauseMenuScene.Instantiate();
			ControlRoot.AddChild(pauseMenu);

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
		newVelocity += knockbackVelocity;

		Velocity = newVelocity;

		FOVJuice(delta);

		HeadBob(delta);

		MoveAndSlide();
	}

	private void PlayFootsteps(Vector3 hVel)
	{
		if (!grounded || !_footstepCooldownTimer.IsStopped()) return;
		if (hVel.Length() <= Mathf.Epsilon) return;

		AudioFile sound;
		double cooldown;

		if (isSprinting)
		{
			sound = AudioFile_Walk;
			cooldown = _sprintFootstepMaxCooldown; // faster steps

			// DebugManager.Info("Playing sprinting footstep sound.");
		}
		else
		{
			sound = AudioFile_Sprint;
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

		AudioManager.Play(AudioPlayer_Footsteps, sound);
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
		_manaMinMaxLabel.TextCurrent = Mathf.RoundToInt(newCurr).ToString();
		_manaMinMaxLabel.TextMaximum = Mathf.RoundToInt(newMax).ToString();
	}

	public void UpdateHealthHUD(float newCurr, float newMax)
	{
		_playerHealthBar.OnHealthChanged(newCurr, newMax);
	}

	public override void PlayOnHurtFX()
	{
		AudioManager.PlayBucketSimultaneous(AudioPlayer_Oof, (AudioBucket)AudioData["Hurt"]);
	}

	protected override void ApplyKnockback(float damage, Vector3 direction)
	{
		// Zero out current velocity and apply an impulse, as requested.
		Velocity = Vector3.Zero;
		float knockbackStrength = Mathf.Max(damage, 0) / KnockbackWeight;
		Velocity += (direction + Lift) * knockbackStrength;

		// Prevent base class decay/application from interfering
		knockbackVelocity = Vector3.Zero;
		Elythia.DebugManager.Info(
			$"PlayerBody Knockback: Damage={damage}, Direction={direction}, Lift={Lift}, KnockbackStrength={knockbackStrength}, KnockbackWeight={KnockbackWeight}, ResultingVelocity={Velocity}");
	}

	private Action onDeathVoiceFinished;
	private Action onDeathSfxFinished;

	public override void OnRanOutOfHealth()
	{
		deadNow = true;

		AudioManager.Play(AudioPlayer_Voice, (AudioFile)AudioData["Die_Voice"]);

		onDeathSfxFinished = () =>
		{
			AudioPlayer_Global.Finished -= onDeathSfxFinished;
			EmitSignalPlayerDied();
		};
		onDeathVoiceFinished = () =>
		{
			AudioPlayer_Voice.Finished -= onDeathVoiceFinished;
			AudioManager.PlayBucketSimultaneous(AudioPlayer_Global, (AudioBucket)AudioData["Die_SFX"]);
		};
		AudioPlayer_Voice.Finished += onDeathVoiceFinished;
		AudioPlayer_Global.Finished += onDeathSfxFinished;
	}

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

	private void PickupManaParticle(ManaParticle manaParticle)
	{
		if (manaParticle.State == Pickup.PickupState.Collected) return; // Already collected

		_manaComponent.AddMana(manaParticle.Value);
		manaParticle.Collect();

		AudioPlayer_Mana.Stream = manaParticle.Data.AudioStream;
		AudioPlayer_Mana.PitchScale = manaParticle.Data.AudioPitch;
		AudioPlayer_Mana.Play();

		PickupManager.Instance.Release(manaParticle);
	}

	private void CollectMoneyPickup(Money moneyParticle)
	{
		if (moneyParticle.State == Pickup.PickupState.Collected) return; // Already collected

		AddMoney(moneyParticle.Value);
		moneyParticle.Collect();

		AudioPlayer_Money.Stream = moneyParticle.Data.AudioStream;
		AudioPlayer_Money.PitchScale = moneyParticle.Data.AudioPitch;
		AudioPlayer_Money.Play();

		PickupManager.Instance.Release(moneyParticle);
	}

	public void AddMoney(int amount)
	{
		_currentMoney += amount;
		_currentMoney = _currentMoney.AtLeastZero();
		_playerMoneyAmountLabel.Text = _currentMoney.ToString();
	}

	private void RefillLife()
	{
		HealthComponent.Refill();
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