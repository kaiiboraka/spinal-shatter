using Godot;

public partial class ManaParticle : RigidBody3D
{
	[Signal] public delegate void CollectedEventHandler(ManaParticle particle);

	private enum ManaParticleState
	{
		Idle,
		Attracted
	}

	[Export] public ManaSize Size { get; private set; }
	[Export] private float _driftSpeed = 0.5f;
	[Export] private float _attractSpeed = 15.0f;
	[Export] private float _lifetime = 5.0f;
	[Export] private SpriteBase3D _sprite;

	public int ManaValue { get; private set; }

	private ManaParticleState _state = ManaParticleState.Idle;
	private Vector3 _velocity = Vector3.Zero;
	private Node3D _target = null;
	private Timer _lifetimeTimer;
	private Tween _decayTween;

	public override void _Ready()
	{
		base._Ready();
		_sprite ??= GetNode<SpriteBase3D>("Sprite3D");
		_lifetimeTimer = new Timer();
		_lifetimeTimer.OneShot = true;
		_lifetimeTimer.Timeout += OnLifetimeTimeout;
		AddChild(_lifetimeTimer);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_state == ManaParticleState.Attracted && _target != null)
		{
			// Move towards the target
			Vector3 direction = (_target.GlobalPosition - this.GlobalPosition).Normalized();
			_velocity = direction * _attractSpeed;
		}
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		base._IntegrateForces(state);

		if (_state == ManaParticleState.Idle && state.GetContactCount() > 0)
		{
			var normal = state.GetContactLocalNormal(0);
			_velocity = _velocity.Bounce(normal);
		}

		state.LinearVelocity = _velocity;
	}

	public void Attract(Node3D target)
	{
		_state = ManaParticleState.Attracted;
		_target = target;
		_decayTween?.Kill();
	}

	public void Collect()
	{
		EmitSignal(SignalName.Collected, this);
	}

	public void Initialize(int manaValue)
	{
		this.ManaValue = manaValue;
		Reset();
		Drift();
		_lifetimeTimer.Start(_lifetime);
		Decay();
	}

	private void Decay()
	{
		_decayTween = GetTree().CreateTween();
		_decayTween.TweenInterval(_lifetime / 2);
		_decayTween.TweenCallback(Callable.From(() =>
		{
			var tween = GetTree().CreateTween();
			tween.TweenProperty(_sprite, "scale", Vector3.One * 0.75f, 1.0f);
			tween.Parallel().TweenProperty(_sprite, "modulate", new Color(1, 1, 1, 0.75f), 1.0f);
		}));
		_decayTween.TweenInterval(_lifetime / 4);
		_decayTween.TweenCallback(Callable.From(() =>
		{
			var tween = GetTree().CreateTween();
			tween.TweenProperty(_sprite, "scale", Vector3.One * 0.5f, 0.5f);
			tween.Parallel().TweenProperty(_sprite, "modulate", new Color(1, 1, 1, 0.5f), 0.5f);
		}));
	}

	private void OnLifetimeTimeout()
	{
		ManaParticleManager.Instance.Release(this);
	}

	public void ReturnToIdle()
	{
		Reset();
		Drift();
	}

	public void Reset()
	{
		_state = ManaParticleState.Idle;
		_target = null;
		_velocity = Vector3.Zero;
		_lifetimeTimer.Stop();
		_decayTween?.Kill();
		_sprite.Scale = Vector3.One;
		_sprite.Modulate = Colors.White;
	}

	private void Drift()
	{
		// Set a random drift velocity
		_velocity = new Vector3(
			(float)GD.RandRange(-1.0, 1.0),
			(float)GD.RandRange(-1.0, 1.0),
			(float)GD.RandRange(-1.0, 1.0)
		).Normalized() * _driftSpeed;
	}
}