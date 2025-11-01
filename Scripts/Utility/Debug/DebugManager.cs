	namespace Elythia;

using System.Diagnostics;
using Godot;
using Godot.Collections;

public partial class DebugManager : CanvasLayer
{
	public static DebugManager Instance { get; private set; }

	public DebugLogger DEBUG;

	[Export] public bool Enabled { get; private set; } = true;
	[Export] private PackedScene PropertyEntryScene;

	[Signal] public delegate void PropertyChangedEventHandler(string which, string value);
	[Signal] public delegate void VisualsActiveChangedEventHandler(bool visualsActive);

	private Vector2 game_size = new Vector2(
		(float)ProjectSettings.GetSetting("display/window/size/width"),
		(float)ProjectSettings.GetSetting("display/window/size/height")
	);

	private Dictionary<string, string> HUDProperties = new();
	private Array<PropertyEntry> entries = new();
	private VBoxContainer PropertyList;
	// private Control RuntimeStateViewer;

	public Node DebugInputs { get; private set; }

	private PlayerBody Player => PlayerBody.Instance;

	private bool visualsActive = true;
	public bool VisualsActive
	{
		get => visualsActive;
		private set
		{
			visualsActive = value && Enabled;
			Visible = visualsActive;
			EmitSignalVisualsActiveChanged(visualsActive);
		}
	}


	public override void _EnterTree()
	{
		AddToGroup("Debug");
		base._EnterTree();
	}

	public override void _ExitTree()
	{
		RemoveFromGroup("Debug");
		base._ExitTree();
	}

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
		}

		DebugInputs = GetNode<Node>("DebugInputs");
		// RuntimeStateViewer = GetNode<Control>("%RuntimeStateViewer");
		// RuntimeStateViewer.Visible = true;

		DEBUG = new DebugLogger(this);

		PropertyEntryScene ??= GD.Load<PackedScene>("res://Scenes/UI/Debug/PropertyEntry.tscn");

		// this.DeferredCall(_LateReady);
		Callable.From(_LateReady).CallDeferred();
	}

	private async void _LateReady()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		// player ??= GetTree().GetFirstNodeInGroup("Player") as PlayerBody;
		// if (player?.PlayerCamera == null)
		// {
		// 	player?.InitCamera();
		// }

		UpdateHUDValues();
		FillDebugHUD();
		VisualsActive = visualsActive;
	}

	public static void Trace(string message)
	{
		Instance?.DEBUG.Trace(message);
	}

	public static void Debug(string message)
	{
		Instance?.DEBUG.Debug(message);
	}

	public static void Warning(string message)
	{
		Instance?.DEBUG.Warning(message);
	}

	public static void Error(string message)
	{
		Instance?.DEBUG.Error(message);
	}

	public static void Info(string message)
	{
		Instance?.DEBUG.Info(message);
	}


	private void FillDebugHUD()
	{
		HUDProperties ??= new();

		PropertyList = GetNode<VBoxContainer>("%PropertyList");
		foreach (var child in PropertyList.GetChildren())
		{
			child.QueueFree();
		}

		entries = new();
		foreach (var (label, value) in HUDProperties)
		{
			// Debug.Assert(PropertyEntryScene != null, "PropertyEntryScene cannot be null");
			var propertyEntry = PropertyEntryScene.Instantiate<PropertyEntry>();

			PropertyList.AddChild(propertyEntry);
			propertyEntry.Owner = this;

			propertyEntry.PropertyText = label;
			propertyEntry.ValueText = value;

			PropertyChanged += propertyEntry.UpdateValueText;
			entries.Add(propertyEntry);
		}
	}

	public void ToggleVisibility()
	{
		VisualsActive = !VisualsActive;
	}

	public override void _Process(double delta)
	{
		UpdateHUDValues();
		base._Process(delta);
	}

	public void UpdateProperty<T>(string which, T value)
	{
		if (!VisualsActive) return;

		string propertyValue = value.ToString();
		HUDProperties[which] = propertyValue;
		EmitSignal(SignalName.PropertyChanged, which, propertyValue);
	}

	private void UpdateHUDValues()
	{
		if (!VisualsActive) return;

		if (Player == null)
		{
			// DEBUG.Error("Player Not Found!");
			return;
		}
		// Debug.Assert(DebugHUD != null, "HUD cannot be null - inspector");

		UpdateProperty_GameInfo();

		UpdateProperty_Movement();

		// UpdateProperty_Camera();

		// UpdateProperty_Attack();

		// UpdateProperty_Slide();

		// UpdateProperty_Jump();

		// UpdateProperty_BufferFrames();

		// UpdateProperty_Crouch();

		// UpdateProperty_AnimationStates();
	}

	private void UpdateProperty_GameInfo()
	{
		UpdateProperty("~~_ Game _~~", "~~~~~~~~~~~~");
		UpdateProperty("Game Time", ((double)Time.GetTicksMsec()).MsToSec(0));
		UpdateProperty("Time Scale Steps", DebugInputs.Get("time_scale_steps"));
		UpdateProperty("Time Scale", Engine.TimeScale);
	}

	private void UpdateProperty_Movement()
	{
		UpdateProperty("~~_ Movement _~~", "~~~~~~~~~~~~");
		UpdateProperty("xInput", Player.InputDir);
		// UpdateProperty("yInput", Player.YInput);
		// UpdateProperty("CurrentSpeed", Player.CurrentMaxSpeed / Constants.PX_PER_TILE);
		// UpdateProperty("CurrentGravity", Player.CurrentGravity / Constants.PX_PER_TILE);
		// UpdateProperty("SpeedToCarry", Player.SpeedToCarry / Constants.PX_PER_TILE);
		UpdateProperty("Velocity Tiles", (Player.Velocity / Constants.PX_PER_TILE).WithPrecision(3));
		UpdateProperty("Velocity Pix", Player.Velocity.WithPrecision(3));
		// UpdateProperty("Floor Angle", Player.FloorAngleDegrees);
	}


	// private void UpdateProperty_Camera()
	// {
	// 	var camera = Player.PlayerCamera;
	// 	UpdateProperty("~~_ Camera _~~", "~~~~~~~~~~~~");
	// 	UpdateProperty("influences", camera.Get("influences"));
	// 	UpdateProperty("priority_influences", camera.Get("priority_influences"));
	// 	UpdateProperty("enabled_influences", camera.Get("enabled_influences"));
	// 	var prev_velocity = (Vector2)camera.Get("prev_velocity");
	// 	UpdateProperty("prev_velocity", prev_velocity.WithPrecision(3));
	// 	var velocity_target = (Vector2)camera.Get("velocity_target_pos");
	// 	UpdateProperty("velocity_target", velocity_target.WithPrecision(3));
	// }

	/*private void UpdateProperty_Attack()
	{
		var attackControl = Player.Behavior.AttackControl;
		UpdateProperty("~~_ Attack _~~", "~~~~~~~~~~");
		UpdateProperty("AllowedToAttack", Player.AllowedToAttack);
		UpdateProperty("IsAttacking", Player.IsAttacking);
		UpdateProperty("Ground Combo Step", attackControl.GroundComboSelector.Counter);
		// UpdateProperty("Air Combo Step", attackControl.AirComboState.ComboCount);
		UpdateProperty("AttackPressed", Player.AttackPressed);
		UpdateProperty("AttackBuffered", attackControl.AttackBuffered);
		UpdateProperty("AttackBufferActive", attackControl.AttackBufferActive);
		UpdateProperty("AttackBufferFrames", attackControl.AttackBufferFrames);
	}* /
	/*
	private void UpdateProperty_Slide()
	{
		var slideState = Player.SlideState;
		UpdateProperty("~~_ Slide _~~", "~~~~~~~~~~");
		UpdateProperty("Slide Pressed", Player.SlidePressed);
		UpdateProperty("Slide Speed", slideState.MaxSpeed);
		UpdateProperty("can slide", Player.CanSlide);
		UpdateProperty("IsSliding", Player.IsSliding);
		UpdateProperty("Slide Duration", slideState.SlideMaxDuration);
		UpdateProperty("Slide Timer", slideState.SlideTime.RoundToPrecision(2));
	}*/

	/*private void UpdateProperty_BufferFrames()
	{
		UpdateProperty("CurrentCoyoteFrames", Player.CoyoteFrames);
		UpdateProperty("MaxCoyoteFrames", Player.MaxCoyoteFrames);
		UpdateProperty("CurrentJumpBufferFrames", Player.CurrentJumpBufferFrames);
		UpdateProperty("MaxJumpBufferFrames", Player.MaxJumpBufferFrames);
	}*/

	/*private void UpdateProperty_Crouch()
	{
		UpdateProperty("~Crouch~", "~~~~~~~~~~");
		UpdateProperty("CrouchToggled", Player.CrouchToggled);
		UpdateProperty("Crouching", Player.Crouching);
	}*/

	/*
	private void UpdateProperty_Jump()
	{
		UpdateProperty("~~_ Jump _~~", "~~~~~~~~~~");
		UpdateProperty("JumpsRemaining", Player.JumpsRemaining);
		UpdateProperty("NumJumps", Player.NumJumps);
		UpdateProperty("JumpBuffered", Player.JumpBuffered);
		UpdateProperty("Can Jump", Player.HasJumpsRemaining);
		UpdateProperty("JumpStarted", Player.JumpStarted);
		// UpdateProperty("Free to RISE", Player.FreeToRise);
	}
	*/

	/*private void UpdateProperty_AnimationStates()
	{
		var animator = Player.AnimatedSprite.Animator;
		UpdateProperty("~~_ Animation _~~", "~~~~~~~~~~");
		UpdateProperty("Anim Queue", animator.GetQueue().Length);
		UpdateProperty("Active States", Player.LeafStateText);
		UpdateProperty("Animation", animator.CurrentAnimation);
		UpdateProperty("AnimationSpeed", animator.SpeedScale);
	}*/
}