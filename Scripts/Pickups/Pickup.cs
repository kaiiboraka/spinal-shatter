using Elythia;
using Godot;

namespace SpinalShatter;

[GlobalClass]
public partial class Pickup : RigidBody3D
{
	[Signal] public delegate void CollectedEventHandler(Pickup particle);
	[Signal] public delegate void ReleasedEventHandler(Pickup particle);

	public enum PickupState
	{
		Idle,
		Attracted,
		Collected,
		Expired
	}

	[Export] public PickupType Type { get; private set; }

	[ExportGroup("Components")]
	[Export] protected Timer LifetimeTimer;
	[Export] public AnimatedSprite3D Sprite { get; set; }
	[Export] protected CollisionShape3D CollisionShape;
	[Export] protected CollisionShape3D AreaShape;

	protected double Lifetime;
	protected float DriftSpeed;
	protected float AttractSpeed;
	public int Value { get; protected set; }

	[Export] protected PickupState CurrentState = PickupState.Idle;
	public PickupState State => CurrentState;

	[Export] protected PickupData data;
	public virtual  PickupData Data => data;

	protected Vector3 Velocity = Vector3.Zero;
	protected Node3D Target = null;

	private Tween _decayTween;
	protected Tween BlinkTween;

	public override void _Ready()
	{
		LifetimeTimer.Timeout += OnLifetimeTimeout;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		BlinkRoutine();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CurrentState == PickupState.Attracted && Target != null)
		{
			Vector3 direction = (Target.GlobalPosition - GlobalPosition).Normalized();
			Velocity = direction * AttractSpeed;
		}
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		if (CurrentState == PickupState.Attracted || (Type == PickupType.Mana && CurrentState == PickupState.Idle))
		{
			state.LinearVelocity = Velocity;
		}
		base._IntegrateForces(state);
	}

	public virtual void Initialize(PickupData data)
	{
		// DebugManager.Debug($"Pickup: Initializing {Name} at GlobalPosition: {GlobalPosition}, with data: {data?.ResourcePath ?? "null"}");
		GlobalPosition = Vector3.Down;
		this.data  = data;
		CurrentState = PickupState.Idle;
		Visible = true;
		Value = data.Value;
		DriftSpeed = data.DriftSpeed;
		AttractSpeed = data.AttractSpeed;
		Lifetime = data.Lifetime;
		LifetimeTimer.WaitTime = Lifetime;

		ResetVisuals(data);

		if (CollisionShape is { Shape: SphereShape3D sphere })
		{
			sphere.Radius = data.CollisionShapeRadius;
			CollisionShape.Disabled = false;
		}

		if (AreaShape is { Shape: SphereShape3D areaSphere })
		{
			areaSphere.Radius = data.AreaShapeRadius;
			AreaShape.Disabled = false;
		}

		LifetimeTimer.Start();
		StopMoving();
		if (Type == PickupType.Mana)
		{
			LinearVelocity = DriftIdle();
			ApplyCentralImpulse(Velocity);
		}
		// DebugManager.Debug($"Pickup: {Name} Initialized. Final GlobalPosition: {GlobalPosition}, LinearVelocity: {LinearVelocity}");
	}

	public void Attract(Node3D target)
	{
		if (CurrentState == PickupState.Collected || CurrentState == PickupState.Expired) return;

		CurrentState = PickupState.Attracted;
		Target = target;
		_decayTween?.Kill();
		Sleeping = false;
	}

	public virtual void Collect()
	{
		CurrentState = PickupState.Collected;
		StopMoving();
		BlinkTween?.Kill();


		EmitSignalCollected(this);
		// Removed: EmitSignal(SignalName.Released, this);
	}

	protected virtual void OnLifetimeTimeout()
	{
		CurrentState = PickupState.Expired;
		var tween = CreateTween();
		tween.TweenProperty(this, "scale", Vector3.One * 0.001f, 0.2f).SetTrans(Tween.TransitionType.Quad)
			 .SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(Reset));

		EmitSignalReleased(this);
	}

	protected void ResetVisuals(PickupData data)
	{
		Scale = Vector3.One;
		Sprite.Modulate = Colors.White;

		if (data == null) 
		{
			Sprite.SpriteFrames = null;
			GravityScale = 0;
			PhysicsMaterialOverride = null;
			return;
		};

		Sprite.Modulate = data.Modulate;
		Sprite.Scale = data.Scale;
		Sprite.SpriteFrames = data.SpriteFrames;
	}

	public void Reset()
	{
		Visible = false;
		ResetVisuals(null);
		StopMoving();
		LifetimeTimer.Stop();

		if (CollisionShape != null) CollisionShape.Disabled = true;
		if (AreaShape != null) AreaShape.Disabled = true;
	}

	public void StopMoving()
	{
		LinearVelocity = Vector3.Zero;
		if (CurrentState != PickupState.Collected && CurrentState != PickupState.Expired)
		{
			CurrentState = PickupState.Idle;
		}

		Velocity = Vector3.Zero;
		Target = null;
	}

	public Vector3 DriftIdle()
	{
		Velocity = new Vector3(
			(float)GD.RandRange(-1.0, 1.0),
			(float)GD.RandRange(-1.0, 1.0),
			(float)GD.RandRange(-1.0, 1.0)
		).Normalized() * DriftSpeed;
		return Velocity;
	}

	private void BlinkRoutine()
	{
		if (LifetimeTimer.IsStopped()) return;

		double timeLeft = LifetimeTimer.TimeLeft;

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

	private void Blink(float alpha, float duration)
	{
		if (BlinkTween != null && BlinkTween.IsRunning() || Sprite == null) return;

		BlinkTween = CreateTween().SetLoops(2).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.InOut);
		BlinkTween.TweenProperty(Sprite, "modulate:a", alpha, duration / 2);
		BlinkTween.TweenProperty(Sprite, "modulate:a", 1.0f, duration / 2);
	}
}
