using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace SpinalShatter;

[GlobalClass]
public partial class PlayerBody : Combatant
{
	public static PlayerBody Instance;
	[Export] public PlayerData Data { get; private set; }
	[Export] public PlayerInventory Inventory { get; private set; }
	const float GRAVITY_MULTIPLIER = 2.00f;
	public Control ControlRoot { get; private set; }

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
	[Export] int MAX_JUMPS = 2;

	const float MAX_SLOPE_ANGLE = 40;
	private float _baseWalkSpeed;
	private float _baseSprintSpeed;
	private float _baseJumpVelocity;
	private int _baseMaxJumps;

	[ExportGroup("CameraSettings")]
	[Export] private float cameraLookSensitivity = 0.006f;
	[Export] private float bob_Speed = 1.0f;
	[Export] private float bob_Height = .15f;
	[Export] private float bob_Sway_Percent = 0f;
	[Export] private float t_bob = .0f;
	private Vector3 _cameraInitialLocalPosition;
	[Export] private float lookUpDegrees = 80f;
	[Export] private float lookDownDegrees = 65f;
	[Export] private float BaseFOV = 75f;
	[Export] private float FOV_change = 1.5f;
	private double fovJuiceWeight = 8.0f;

	[Signal] public delegate void PlayerDiedEventHandler();

	private bool grounded = false;
	private bool isCrouching = false;
	private bool isSprinting = false;
	private int curJumps = 0;

	public int CurrentMoney
	{
		get => Inventory.CurrentMoney;
		private set => Inventory.CurrentMoney = value;
	}

	private Vector2 inputDir = Vector2.Zero;
	private Vector3 direction = Vector3.Zero;
	private Vector3 newVelocity = Vector3.Zero;
	private bool MouseIsCaptured => Input.MouseMode == Input.MouseModeEnum.Captured;
	public Vector2 InputDir => inputDir;
	private Node3D headNode;

	private Camera3D camera;
	public Camera3D PlayerCamera => camera;

	private MinMaxValuesLabel healthMinMaxLabel;
	private PlayerParamterBar playerHealthBar;
	private MinMaxValuesLabel manaMinMaxLabel;
	private PlayerParamterBar playerManaBar;
	private Label playerMoneyAmountLabel;
	private CenterContainer reticle;
	private ManaComponent manaComponent;
	private Area3D pickupArea;
	[ExportGroup("Menus")]
	[Export] private PackedScene _pauseMenuScene;
	[Export] private InventoryHUD _weaponInventoryHUD;
	[Export] private InventoryHUD _statItemInventoryHUD;
	private Control _detailsControl;
	private RichTextLabel _detailsNameLabel;
	private RichTextLabel _detailsDescriptionLabel;
	private AudioData AudioData;

	[ExportCategory("Combat")]
	[ExportSubgroup("Knockback", "Knockback")]
	[Export] public new float KnockbackWeight { get; private set; } = 5f;
	[Export] public float MeleeAttackDamage { get; private set; } = 25f;

	private CollisionShape3D collider;
	private RayCast3D canStandUpRay;
	private RayCast3D footSoundRay;
	private MagicCaster magicCaster;
	private AutomaticCaster automaticCaster;
	private SiphonComponent siphon;
	private bool standUpBlocked;
	private Timer _footstepCooldownTimer;
	private double _footstepMaxCooldown = 2f;
	private double _sprintFootstepMaxCooldown = 2f / 1.2f;
	private AnimationPlayer animationPlayer;
	private AnimatedSprite3D armLeft;
	private AnimatedSprite3D armRight;
	private Timer meleeResetTimer;
	private bool _inventorySlotsDirty = true;
	private HorizontalDirection lastSwingDirection = HorizontalDirection.None;
	private bool meleeAttackPlaying = false;

	private bool MeleeAttackAllowed
	{
		get => !meleeAttackPlaying;
		set => meleeAttackPlaying = !value;
	}

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
	private Action onDeathVoiceFinished;
	private Action onDeathSfxFinished;

	public enum PlayerControlState
	{
		Piloting,
		UI
	}

	public PlayerControlState CurrentControlState { get; private set; } = PlayerControlState.Piloting;

	public bool ControllingPlayer =>  CurrentControlState == PlayerControlState.Piloting;
	public bool ControllingUI => CurrentControlState == PlayerControlState.UI;

	private PanelContainer _interactionPromptContainer;
	private RichTextLabel _interactionPromptLabel;


	public override void _Ready()
	{
		base._Ready(); // GetComponents, ConnectEvents
		Instance = this;
		parentLevel = GetParent() as Node3D;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		StoreBaseStats();
		_footstepMaxCooldown = (AudioFile_Walk.Stream as AudioStreamRandomizer).GetMaxLength();
		_sprintFootstepMaxCooldown = (AudioFile_Sprint.Stream as AudioStreamRandomizer).GetMaxLength() / 1.2f;
		ReceiveMoney(0);
		RefillMana();
		RefillLife();
		AllowRangedAttack();
		AllowSiphon();
		ShowRightArm();
		ReturnToIdle();
		WaveDirector.Instance.SetPlayer(this);
		CameraTransition.Instance.Initialize(PlayerCamera);
		InitializeInventory();
	}

	protected override void GetComponents()
	{
		base.GetComponents();
		ControlRoot = GetNode<Control>("Control");
		_interactionPromptContainer = GetNode<PanelContainer>("%InteractionPrompt_PanelContainer");
		_interactionPromptLabel = GetNode<RichTextLabel>("%InteractionPrompt_RichTextLabel");
		_interactionPromptContainer.Visible = false;

		headNode = GetNode<Node3D>("%Head");
		camera = GetNode<Camera3D>("%Camera1P");
		_cameraInitialLocalPosition = camera.Position;
		collider = GetNode<CollisionShape3D>("%PlayerCollider");
		canStandUpRay = GetNode<RayCast3D>("%StandUpRay");
		healthMinMaxLabel = GetNode<MinMaxValuesLabel>("%Health_MinMaxValuesLabel");
		playerHealthBar   = GetNode<PlayerParamterBar>("%PlayerHealthBar");
		manaMinMaxLabel   = GetNode<MinMaxValuesLabel>("%Mana_MinMaxValuesLabel");
		playerManaBar     = GetNode<PlayerParamterBar>("%PlayerManaBar");
		reticle  = GetNode<CenterContainer>("%Reticle");

		manaComponent = GetNode<ManaComponent>("%ManaComponent");
		playerMoneyAmountLabel = GetNode<Label>("%MoneyAmountLabel");
		pickupArea = GetNode<Area3D>("PickupArea");
		magicCaster = GetNode<MagicCaster>("%MagicCaster");
		automaticCaster = GetNode<AutomaticCaster>("%AutomaticCaster");
		siphon = GetNode<SiphonComponent>("SiphonComponent");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		armLeft = GetNode<AnimatedSprite3D>("%LeftArm");
		armRight = GetNode<AnimatedSprite3D>("%RightArm");

		_detailsControl = GetNode<Control>("%Details_Control");
		_detailsNameLabel =  GetNode<RichTextLabel>("%Details_Name_RichTextLabel");
		_detailsDescriptionLabel = GetNode<RichTextLabel>("%Details_Description_RichTextLabel");

		// Timers
		_footstepCooldownTimer = GetNode<Timer>("%FootstepCooldownTimer");
		meleeResetTimer = GetNode<Timer>("%MeleeResetTimer");

		// Audio Players
		AudioPlayer_Oof = GetNode<AudioStreamPlayer3D>("Audio/Oof_AudioStreamPlayer3D");
		AudioPlayer_Global = GetNode<AudioStreamPlayer>("Audio/Global_AudioStreamPlayer");
		AudioPlayer_Voice = GetNode<AudioStreamPlayer3D>("Audio/Voice_AudioStreamPlayer3D");
		AudioPlayer_Mana = GetNode<AudioStreamPlayer3D>("Audio/Mana_AudioStreamPlayer3D");
		AudioPlayer_Money = GetNode<AudioStreamPlayer3D>("Audio/Money_AudioStreamPlayer3D");
		AudioPlayer_Footsteps = GetNode<AudioStreamPlayer3D>("Audio/Footsteps_AudioStreamPlayer3D");
		var audioData = GD.Load<Resource>("res://assets/Audio/AudioData/AudioData_Player.tres");
		AudioData = audioData as AudioData;
	}

	protected override void ConnectEvents()
	{
		base.ConnectEvents();
		manaComponent.ManaChanged += UpdateManaHUD;
		UpdateManaHUD(manaComponent.CurrentMana, manaComponent.MaxMana);
		HealthComponent.HealthChanged += UpdateHealthHUD;
		UpdateHealthHUD(HealthComponent.CurrentHealth, HealthComponent.MaxHealth);
		pickupArea.AreaEntered += OnAreaEnteredPickupArea;
		meleeResetTimer.Timeout += () => { lastSwingDirection = HorizontalDirection.None; };
		armLeft.AnimationFinished += OnMeleeAnimationFinished;
		animationPlayer.AnimationFinished += name =>
		{
			if (name == "Cast_Release")
			{
				OnRangedAnimationFinished();
			}

			if (name.ToString().StartsWith("Melee", StringComparison.Ordinal))
			{
				OnMeleeAnimationFinished();
			}
		};
		if (Inventory != null)
		{
			Inventory.WeaponEquipped += OnWeaponEquipped;
			Inventory.InventoryChanged += RecalculateStats;
		}

		SignalBus.Instance.GameResumed += ExitUIMode;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
        if (CurrentControlState == PlayerControlState.UI)
        {
            // Do not process any player-specific input when in UI mode
            // Allow event to propagate to other nodes, e.g., ShopCart
            return; 
        }

		// Camera Rotation
		if (@event is InputEventMouseMotion motion)
		{
			RotateCamera(motion);
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (_inventorySlotsDirty)
		{
			UpdateInventoryUI();
		}

		ProcessInput(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		ProcessMovement(delta);
	}

	private void ProcessInput(double delta)
	{
		if (DeadNow || !ControllingPlayer) return;

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

		if (Input.IsActionPressed("Player_Melee"))
		{
			TryMelee();
		}

		if (Input.IsActionJustPressed("Player_Reload"))
		{
			RefillMana();
		}

		if (Input.IsActionJustPressed("Player_Teleport"))
		{
			Position = spawnPosition;
			Rotation = Vector3.Zero;
		}

		SprintAndCrouch();

		if (Input.IsActionJustPressed("Player_Pause") && CurrentControlState == PlayerControlState.Piloting)
		{
			EnterUIMode();

			var pauseMenu = _pauseMenuScene.Instantiate();
			ControlRoot.AddChild(pauseMenu);
			// GetTree().Paused = true;
		}
	}

	private void ProcessMovement(double delta)
	{
		if (!ControllingPlayer) return;

		// Can Stand Up Ray
		standUpBlocked = canStandUpRay.IsColliding();
		grounded = IsOnFloor();
		float oldY = newVelocity.Y;
		float grav = GRAVITY * (float)delta;
		if (!grounded)
			newVelocity.Y -= grav;
		if (DeadNow)
		{
			newVelocity = newVelocity.MoveToward(Vector3.Zero, .1f) with { Y = oldY - (grounded ? 0 : grav) };
			return;
		}

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

	private void RotateCamera(InputEventMouseMotion motion)
	{
		if (!ControllingPlayer) return;

		this.RotateY(-motion.Relative.X * cameraLookSensitivity);
		camera.RotateX(-motion.Relative.Y * cameraLookSensitivity);
		Vector3 cameraRot = camera.Rotation;
		cameraRot.X = Mathf.Clamp(cameraRot.X, Mathf.DegToRad(-lookUpDegrees), Mathf.DegToRad(lookUpDegrees));
		camera.Rotation = cameraRot;
	}

	#region State Management & UI

	public void EnterUIMode()
	{
		if (ControllingUI) return;
		CurrentControlState = PlayerControlState.UI;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		reticle.Visible = false;
		this.Visible = false;
		_detailsControl.Visible = true;
		// collider.Disabled = true;
		DisallowMeleeAttack();
		DisallowRangedAttack();
		DisallowSiphon();
		hurtbox.DisableMonitor();
	}

	public void ExitUIMode()
	{
		if (ControllingPlayer) return;

		CurrentControlState = PlayerControlState.Piloting;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		reticle.Visible = true;
		this.Visible = true;
		_detailsControl.Visible = false;
		// collider.Disabled = false;
		AllowMeleeAttack();
		AllowRangedAttack();
		AllowSiphon();
		hurtbox.EnableMonitor();
	}


	public void ShowPromptToPress(string actionName, string message, string prefix = "")
	{
		if (actionName.IsNullOrWhiteSpace())
		{
			_interactionPromptLabel.Text = $"[center]{prefix} {message}[/center]";
			_interactionPromptContainer.Visible = true;
				return;
		}
		string keyName = actionName.GetActionKeyName();
		_interactionPromptLabel.Text = $"[center]{prefix} [{keyName}] {message}[/center]";
		_interactionPromptContainer.Visible = true;
	}

	public void HideInteractionPrompt()
	{
		_interactionPromptContainer.Visible = false;
	}

	public void UpdateShopDetails(ShopItemData itemData)
	{
		// Safety check for UI elements
		if (_detailsNameLabel == null || _detailsDescriptionLabel == null || _detailsControl == null)
		{
			GD.PushError("PlayerBody: Shop details UI labels or control are null.");
			return;
		}

		if (itemData == null)
		{
			_detailsNameLabel.Text = "[center]No Item Selected[/center]"; // Display placeholder
			_detailsDescriptionLabel.Text = "[center]This slot is empty.[/center]"; // Display placeholder
		}
		else
		{
			_detailsNameLabel.Text = itemData.ItemName ?? "";

            // Determine if it's a new item or a rank-up
            bool isOwned = false;
            int currentRank = 0;

            if (itemData is SpellData spellData)
            {
                var (equippedWeapon, rank, max) = Inventory.GetOwnedWeaponInfo(spellData);
                if (equippedWeapon != null)
                {
                    isOwned = true;
                    currentRank = rank;
                }
            }
            else if (itemData is StatItemData statItemData)
            {
                var (equippedStatItem, rank, max) = Inventory.GetOwnedStatItemInfo(statItemData);
                if (equippedStatItem != null)
                {
                    isOwned = true;
                    currentRank = rank;
                }
            }

            // Display logic based on item status
            if (isOwned && itemData.ShopRank > 1) // It's an upgrade in the shop
            {
                _detailsNameLabel.Text = $"{itemData.ItemName ?? ""} {itemData.ShopRank.ToRomanNumerals()}"; // Name + Rank
                
                // Get the RankUpData for this specific rank-up
                if (itemData.RankUps != null && (itemData.ShopRank - 2) >= 0 && (itemData.ShopRank - 2) < itemData.RankUps.Count)
                {
                    RankUpData rankUp = itemData.RankUps[itemData.ShopRank - 2];
                    _detailsDescriptionLabel.Text = "RANK UP : " + FormatStatModifiers(rankUp.StatModifiers);
                }
                else
                {
                    _detailsDescriptionLabel.Text = "RANK UP : No specific data for this rank.";
                }
            }
            else // It's a new item (ShopRank == 1) or an item with no rank-up data (e.g. from Inventory)
            {
                _detailsNameLabel.Text = itemData.ItemName ?? "";
                _detailsDescriptionLabel.Text = itemData.ItemDescription ?? "";
            }
		}
	}

	private string FormatStatModifiers(Dictionary<StatType, float> modifiers)
	{
		if (modifiers == null || modifiers.Count == 0) return "No stat changes.";

		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		bool first = true;
		foreach (var entry in modifiers)
		{
			if (!first)
			{
				sb.Append(", ");
			}
			// Example: "Damage + 10", "Size x 1.5", "Charge Time - 0.1"
			string prefix = "";
			if (entry.Value > 0) prefix = "+ ";
			else if (entry.Value < 0) prefix = "- ";

			// Convert StatType enum to readable string (e.g., Player_MaxHealth -> Max Health)
			string statName = entry.Key.ToString().Replace("Player_", "").Replace("Weapon_", "").Replace("_", " ");

			sb.Append($"{statName} {prefix}{Mathf.Abs(entry.Value)}"); // Add {prefix} and {Mathf.Abs(entry.Value)}

			first = false;
		}
		return sb.ToString();
	}

	#endregion

	public void ReturnToIdle()
	{
		animationPlayer.Play("Cast_Idle");
	}

	private void TryMelee()
	{
		if (!MeleeAttackAllowed) return;
		DisallowRangedAttack();
		DisallowSiphon();
		DisallowMeleeAttack();
		ShowLeftArm();
		string which;
		switch (lastSwingDirection)
		{
			case HorizontalDirection.None:
			case HorizontalDirection.Left:
				which = "Melee_RightSwing";
				lastSwingDirection = HorizontalDirection.Right;
				break;
			case HorizontalDirection.Right:
				which = "Melee_LeftSwing";
				lastSwingDirection = HorizontalDirection.Left;
				break;
			default:
				which = "";
				break;
		}

		animationPlayer.Play(which);
	}

	private void OnMeleeAnimationFinished()
	{
		MeleeAttackFinished();
		meleeResetTimer.Start();
		ShowRightArm();
		ReturnToIdle();
	}

	/// <summary>
	/// Called during the Animations, and then, as a safety net, again when the animations are over.
	/// </summary>
	public void MeleeAttackFinished()
	{
		AllowRangedAttack();
		AllowMeleeAttack();
		AllowSiphon();
	}

	protected override void OnMeleeHitboxAreaEntered(Area3D area)
	{
		if (area.Owner is Enemy enemy)
		{
			// DebugManager.Debug($"MELEE HIT: Player '{Name}' attacking Enemy for {MeleeAttackDamage} damage.");
			enemy.TakeDamage(MeleeAttackDamage, GlobalPosition);
		}
	}

	public void PlayCastCharge()
	{
		ShowRightArm();
		animationPlayer.Play("Cast_Charge");
	}

	public void PlayCastHold()
	{
		ShowRightArm();
		animationPlayer.Play("Cast_Hold");
	}

	public void PlayCastRelease()
	{
		animationPlayer.Play("Cast_Release");
	}

	private void OnRangedAnimationFinished()
	{
		AllowMeleeAttack();
		AllowSiphon();
		ReturnToIdle();
	}

	private void ShowLeftArm()
	{
		armLeft.Visible = true;
		armRight.Visible = false;
	}

	private void ShowRightArm()
	{
		armLeft.Visible = false;
		armRight.Visible = true;
	}

	public void AllowSiphon()
	{
		siphon.CanSiphon = true;
		siphon.SetProcess(true);
	}

	public void DisallowSiphon()
	{
		siphon.CanSiphon = false;
		siphon.SetProcess(false);
	}

	public void AllowRangedAttack()
	{
		magicCaster.CanShoot = true;
	}

	public void DisallowRangedAttack()
	{
		magicCaster.CanShoot = false;
	}

	public void AllowMeleeAttack()
	{
		MeleeAttackAllowed = true;
	}

	public void DisallowMeleeAttack()
	{
		MeleeAttackAllowed = false;
	}

	private void InitializeInventory()
	{
		if (Inventory == null) return;
		foreach (var entry in Inventory.EquippedWeapons)
		{
			OnWeaponEquipped(entry.Key, entry.Value);
		}

		RecalculateStats();
		_inventorySlotsDirty = true;
	}

	#region Inventory Handlers

	private void UpdateInventoryUI()
	{
		if (_weaponInventoryHUD != null)
		{
			var weaponSlots = _weaponInventoryHUD.GetItemSlots();
			for (int i = 0; i < weaponSlots.Count; i++)
			{
				// This assumes a fixed order: Primary, Alt, Auto
				SlotType slotType = (SlotType)i;
				if (Inventory.EquippedWeapons.TryGetValue(slotType, out EquippedItem weapon) && weapon != null)
				{
					weaponSlots[i].ChangeDisplayData(weapon.ItemData, weapon.Rank);
				}
				else
				{
					weaponSlots[i].ChangeDisplayData(null, 0);
				}
			}
		}

		if (_statItemInventoryHUD != null)
		{
			var statSlots = _statItemInventoryHUD.GetItemSlots();
			for (int i = 0; i < statSlots.Count; i++)
			{
				if (i < Inventory.EquippedStatItems.Count && Inventory.EquippedStatItems[i] != null)
				{
					var statItem = Inventory.EquippedStatItems[i];
					statSlots[i].ChangeDisplayData(statItem.ItemData, statItem.Rank);
				}
				else
				{
					statSlots[i].ChangeDisplayData(null, 0);
				}
			}
		}

		_inventorySlotsDirty = false;
	}

	private void OnWeaponEquipped(SlotType slot, EquippedItem weapon)
	{
		_inventorySlotsDirty = true;
		if (weapon?.ItemData is not SpellData spellData) return;
		switch (slot)
		{
			case SlotType.Primary:
				magicCaster.SetPrimaryWeapon(spellData as CastedSpellData);
				break;
			case SlotType.Secondary:
				magicCaster.SetSecondaryWeapon(spellData as CastedSpellData);
				break;
			case SlotType.Automatic:
				automaticCaster.SetAutomaticWeapon(spellData);
				break;
		}
	}

	private void StoreBaseStats()
	{
		if (Data != null)
		{
			HealthComponent.MaxHealth = Data.MaxHealth;
		}

		_baseWalkSpeed = WALK_SPEED;
		_baseSprintSpeed = MAX_SPRINT_SPEED;
		_baseJumpVelocity = JUMP_VELOCITY;
		_baseMaxJumps = MAX_JUMPS;
	}

	private void RecalculateStats()
	{
		if (Inventory == null) return;

		// Reset to base stats
		WALK_SPEED = _baseWalkSpeed;
		MAX_SPRINT_SPEED = _baseSprintSpeed;
		JUMP_VELOCITY = _baseJumpVelocity;
		MAX_JUMPS = _baseMaxJumps;
		if (Data != null) HealthComponent.MaxHealth = Data.MaxHealth;

		// Combine all items that can have stats
		var allItems = Inventory.EquippedStatItems.AsEnumerable()
								.Concat(Inventory.EquippedWeapons.Values);
		foreach (var equippedItem in allItems)
		{
			if (equippedItem?.ItemData == null) continue;

			// Apply base stats from StatItemData
			if (equippedItem.ItemData is StatItemData statItemData)
			{
				ApplyStat(statItemData.TargetStat, statItemData.Value, statItemData.IsMultiplier);
			}

			// Apply rank-up stats
			if (equippedItem.ItemData.RankUps == null) continue;
			for (int i = 0; i < equippedItem.Rank - 1 && i < equippedItem.ItemData.RankUps.Count; i++)
			{
				var rankUp = equippedItem.ItemData.RankUps[i];
				if (rankUp?.StatModifiers == null) continue;
				foreach (var modifier in rankUp.StatModifiers)
				{
					// For RankUps, we assume the bonus is NOT a multiplier unless we add that feature later
					ApplyStat(modifier.Key, modifier.Value, false);
				}
			}
		}

		// Health needs special handling to ensure current health is updated correctly
		HealthComponent.Refill();
		_inventorySlotsDirty = true;
	}

	private void ApplyStat(StatType stat, float value, bool isMultiplier)
	{
		switch (stat)
		{
			case StatType.Player_MaxHealth:
				HealthComponent.MaxHealth =
					isMultiplier ? HealthComponent.MaxHealth * value : HealthComponent.MaxHealth + value;
				break;
			case StatType.Player_MaxMana:
				manaComponent.MaxMana = 
					isMultiplier ? manaComponent.MaxMana * value : manaComponent.MaxMana + value;
				break;
			case StatType.Player_MoveSpeed:
				WALK_SPEED = isMultiplier ? WALK_SPEED * value : WALK_SPEED + value;
				MAX_SPRINT_SPEED = isMultiplier ? MAX_SPRINT_SPEED * value : MAX_SPRINT_SPEED + value;
				break;
			case StatType.Player_JumpHeight:
				JUMP_VELOCITY = isMultiplier ? JUMP_VELOCITY * value : JUMP_VELOCITY + value;
				break;
			case StatType.Player_AirJumps:
				if (!isMultiplier) MAX_JUMPS += (int)value;
				break;

			// Add other stat cases here
		}
	}

	#endregion Inventory Handlers


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
			// DebugManager.Warning($"Using default 0.5s to prevent crash.");
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
		var hVel = Velocity.XZ().Length();
		bool isMovingOnGround = grounded && hVel > 0.1f; // Threshold for movement. Use a small threshold to avoid jitter when almost stopped.

		if (isMovingOnGround)
		{
			t_bob += ((float)delta) * hVel * bob_Speed;
		}
		else
		{
			// Smoothly reset t_bob to 0 when not moving on ground or airborne
			t_bob = Mathf.Lerp(t_bob, 0.0f, (float)delta * 8.0f); // Decay rate for t_bob
		}

		// Calculate bobbing offset
		float bobOffset = Mathf.Sin(t_bob) * bob_Height;

		// The target Y position for the camera
		Vector3 targetCameraLocalPosition = _cameraInitialLocalPosition;
		targetCameraLocalPosition.Y += bobOffset;

		// Smoothly move the camera back to or towards the target position
		camera.Position = camera.Position.Lerp(targetCameraLocalPosition, (float)delta * 10.0f); // Interpolation speed for camera position
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

		bool jumpsRemain = curJumps < MAX_JUMPS;
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

	public void UpdateHealthHUD(float newCurr, float newMax)
	{
		healthMinMaxLabel.TextCurrent = Mathf.RoundToInt(newCurr).ToString();
		healthMinMaxLabel.TextMaximum = Mathf.RoundToInt(newMax).ToString();
		playerHealthBar.OnParameterChanged(newCurr, newMax);
	}

	public void UpdateManaHUD(float newCurr, float newMax)
	{
		manaMinMaxLabel.TextCurrent = Mathf.RoundToInt(newCurr).ToString();
		manaMinMaxLabel.TextMaximum = Mathf.RoundToInt(newMax).ToString();
		playerManaBar.OnParameterChanged(newCurr, newMax);
	}

	public override float TakeDamage(float amount, Vector3 sourcePosition)
	{
		// DebugManager.Debug($"PLAYER_TAKE_DAMAGE: Received {amount} damage from source at {sourcePosition}.");
		return base.TakeDamage(amount, sourcePosition);
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

		// DebugManager.Info($"PlayerBody Knockback: Damage={damage}, Direction={direction}, Lift={Lift}, KnockbackStrength={knockbackStrength}, KnockbackWeight={KnockbackWeight}, ResultingVelocity={Velocity}");
	}

	public override void OnRanOutOfHealth()
	{
		DeadNow = true;
		DisallowSiphon();
		DisallowMeleeAttack();
		DisallowRangedAttack();
		siphon.ProcessMode = ProcessModeEnum.Disabled;
		magicCaster.ProcessMode = ProcessModeEnum.Disabled;
		pickupArea.Monitoring = false;
		pickupArea.Monitorable = false;
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
		manaComponent.AddMana(manaParticle.Value);
		manaParticle.Collect();
		AudioPlayer_Mana.Stream = manaParticle.Data.AudioStream;
		AudioPlayer_Mana.PitchScale = manaParticle.Data.AudioPitch;
		AudioPlayer_Mana.Play();
		PickupManager.Instance.Release(manaParticle);
	}

	private void CollectMoneyPickup(Money moneyParticle)
	{
		if (moneyParticle.State == Pickup.PickupState.Collected) return; // Already collected
		ReceiveMoney(moneyParticle.Value);
		moneyParticle.Collect();
		AudioPlayer_Money.Stream = moneyParticle.Data.AudioStream;
		AudioPlayer_Money.PitchScale = moneyParticle.Data.AudioPitch;
		AudioPlayer_Money.Play();
		PickupManager.Instance.Release(moneyParticle);
	}

	public void ReceiveMoney(int amount)
	{
		CurrentMoney += amount;
		CurrentMoney = CurrentMoney.AtLeastZero();
		playerMoneyAmountLabel.Text = CurrentMoney.ToString();
	}

	public bool SpendMoney(int amount)
	{
		if (CurrentMoney >= amount)
		{
			CurrentMoney -= amount;
			CurrentMoney = CurrentMoney.AtLeastZero();
			playerMoneyAmountLabel.Text = CurrentMoney.ToString();
			return true;
		}
		return false;
	}

	private void RefillLife()
	{
		HealthComponent.Refill();
	}

	public void RefillMana()
	{
		manaComponent.RefillMana();
	}

}