using Godot;
using System;
using FPS_Mods.Scripts;
using Godot.Collections;

public partial class PlayerBody : CharacterBody3D
{
	public static PlayerBody Instance;

	const float GRAVITY_MULTIPLIER = 2.00f;

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

	[Export] private float KnockbackStrength { get; set; } = 10.0f;

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

	private Vector2 inputDir = Vector2.Zero;
	private Vector3 direction = Vector3.Zero;
	private Vector3 velocityTmp = Vector3.Zero;

	private bool MouseIsCaptured => Input.MouseMode == Input.MouseModeEnum.Captured;

	public Vector2 InputDir => inputDir;

	private Node3D headNode;

	private Camera3D camera;

	// private Camera3D camera3P;
	private SpringArm3D arm;

	private Label currAmmoLabel;
	private Label maxAmmoLabel;

	private PlayerHealthBar _playerHealthBar;

	[ExportGroup("Components")]
	[Export] private ManaComponent _manaComponent;

	public HealthComponent HealthComponent { get; private set; }
	[Export] private Area3D pickupArea;

	[ExportSubgroup("Audio", "Audio")]
	[Export] private AudioStreamPlayer3D AudioPlayer_ManaPickups;

	[Export] private AudioStreamPlayer3D AudioPlayer_Footsteps;
	[Export] private Array<AudioStream> Audio_FootstepSounds;
	[Export] private Array<AudioStream> Audio_FootstepSprintSounds;

	private CollisionShape3D collider;
	private RayCast3D canStandUpRay;
	private RayCast3D footSoundRay;

	private bool standUpBlocked;

	// public Loadout loadout;
	private Vector3 spawnPosition = new(2.351f, 2, 28.564f);

	private Node3D parentLevel;
	public Node3D ParentLevel => parentLevel;


	public override void _Ready()
	{
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

		HealthComponent ??= GetNode<HealthComponent>("%HealthComponent");
		_playerHealthBar = GetNode<PlayerHealthBar>("%PlayerHealthBar");
		HealthComponent.HealthChanged += UpdateHealthHUD;
		HealthComponent.Hurt += OnHurt;
		UpdateHealthHUD(HealthComponent.CurrentHealth, HealthComponent.MaxHealth);

		pickupArea ??= GetNode<Area3D>("PickupArea");

		// pickupArea.BodyEntered += OnBodyEnteredPickupArea;
		pickupArea.AreaEntered += OnAreaEnteredPickupArea;

		parentLevel = GetParent() as Node3D;

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Camera Rotation
		if (@event is InputEventMouseMotion motion && MouseIsCaptured)
		{
			this.RotateY(-motion.Relative.X * cameraLookSensitivity);

			// if (firstPerson)
			// {
			// headNode.RotateY(-motion.Relative.X * cameraLookSensitivity);
			camera.RotateX(-motion.Relative.Y * cameraLookSensitivity);

			Vector3 cameraRot = camera.Rotation;
			cameraRot.X = Mathf.Clamp(cameraRot.X, Mathf.DegToRad(-lookUpDegrees), Mathf.DegToRad(lookUpDegrees));
			camera.Rotation = cameraRot;

			// }
			// else
			// {
			//     // this.RotateY(-motion.Relative.X * cameraLookSensitivity);
			//     arm.RotateX(-motion.Relative.Y * cameraLookSensitivity);
			//
			//     Vector3 cameraRot = arm.Rotation;
			//     cameraRot.X = Mathf.Clamp(cameraRot.X, Mathf.DegToRad(-lookUpDegrees), Mathf.DegToRad(lookUpDegrees));
			//     arm.Rotation = cameraRot;
			// }
		}
	}


	public override void _PhysicsProcess(double delta)
	{
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
			TryShoot();
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
			velocityTmp.Y -= GRAVITY * (float)delta;

		var hVel = Velocity.XZ();

		var target = direction;

		// if (grounded)
		// {
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

		// }
		// else
		// {
		// target *= AIR_SPEED;
		// }

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

		velocityTmp.X = hVel.X;
		velocityTmp.Z = hVel.Z;

		Velocity = velocityTmp;

		FOVJuice(delta);

		HeadBob(delta);

		MoveAndSlide();
	}

	private async void PlayFootsteps(Vector3 hVel)
	{
		if (!grounded || Audio_FootstepSounds.IsNullOrEmpty()) return;
		if ((hVel.Length() <= Mathf.Epsilon) || AudioPlayer_Footsteps.IsPlaying()) return;


		if (isSprinting)
		{
			AudioPlayer_Footsteps.Stream = Audio_FootstepSprintSounds.PickRandom();
			AudioPlayer_Footsteps.Play();
		}
		else
		{
			AudioPlayer_Footsteps.Stream = Audio_FootstepSounds.PickRandom();
			await ToSignal(GetTree().CreateTimer(AudioPlayer_Footsteps.Stream.GetLength()), "timeout");
			AudioPlayer_Footsteps.Play();
		}
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
			velocityTmp.Y = JUMP_VELOCITY;
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

	private void OnHurt(Vector3 sourcePosition)
	{
		var direction = (GlobalPosition - sourcePosition).Normalized();
		velocityTmp = direction * KnockbackStrength;
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
	}

	private void PickupManaParticle(ManaParticle particle)
	{
		if (particle.State == ManaParticle.ManaParticleState.Collected) return; // Already collected

		_manaComponent.AddMana(particle.ManaValue);
		particle.Collect();
		AudioPlayer_ManaPickups.Stream = ManaParticleManager.Instance.ParticleData[particle.Size].AudioStream;
		AudioPlayer_ManaPickups.PitchScale = (float)(GD.RandRange(.95, 1.05) *
													 ManaParticleManager.Instance.ParticleData[particle.Size]
																		.AudioPitch);
		AudioPlayer_ManaPickups.Play();
		ManaParticleManager.Instance.Release(particle);
	}
}