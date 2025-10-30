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
	[Export] private Timer _lifetimeTimer;

	private double _lifetime = 5f;
	[Export(PropertyHint.Range, "5,120")] private double Lifetime
	{
		get => _lifetimeTimer?.WaitTime ?? _lifetime;
		set
		{
			_lifetime = value;
			if (_lifetimeTimer != null) _lifetimeTimer.WaitTime = value;
		}
	}

	[Export] private SpriteBase3D _sprite;
	[Export] private CollisionShape3D _collisionShape;
	[Export] private CollisionShape3D _areaShape;
	public int ManaValue { get; private set; }

	private ManaParticleState _state = ManaParticleState.Idle;
	private Vector3 _velocity = Vector3.Zero;
	private Node3D _target = null;


	private Tween _decayTween;

	public override void _Ready()
	{
		// This method is only called once when the scene is first instantiated.
		// Signal connections should be made here.
		_lifetimeTimer.Timeout += OnLifetimeTimeout;
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
		// if (_state == ManaParticleState.Idle && state.GetContactCount() > 0)
		// {
		// 	var normal = state.GetContactLocalNormal(0);
		// 	_velocity = _velocity.Bounce(normal);
		// }

		if (_state == ManaParticleState.Attracted) state.LinearVelocity = _velocity;
		base._IntegrateForces(state);
	}

	public void Attract(Node3D target)
	{
		_state = ManaParticleState.Attracted;
		_target = target;
		_decayTween?.Kill();
	}

	public void Collect()
	{
		StopMoving();
		EmitSignal(SignalName.Collected, this);
	}

	// Called by the ManaParticleManager when a particle is spawned from the pool.
	public void Initialize(int manaValue, ManaParticleData data)
	{
		this.ManaValue = manaValue;

		// Apply visual data
		if (_sprite != null)
		{
			_sprite.Modulate = data.Modulate;
			_sprite.Scale = data.Scale;
		}
		if (_collisionShape != null && _collisionShape.Shape is SphereShape3D sphere)
		{
			sphere.Radius = data.CollisionShapeRadius;
		}
		if (_areaShape != null && _areaShape.Shape is SphereShape3D areaSphere)
		{
			areaSphere.Radius = data.AreaShapeRadius;
		}

		// Set initial state for a new life
		_state = ManaParticleState.Idle;
		_target = null;

		DriftIdle();
		_lifetimeTimer.Start();
		Decay();
	}

	private void Decay()
	{
		_decayTween?.Kill(); // Ensure any old tween is killed before starting a new one.
		_decayTween = GetTree().CreateTween().BindNode(this);
		float totalLifetime = (float)_lifetimeTimer.WaitTime;

		// First decay stage: 5 seconds before end
		float firstDecayStart = Mathf.Max(0, totalLifetime - 5.0f);
		if (firstDecayStart > 0)
		{
			_decayTween.TweenInterval(firstDecayStart);
		}

		_decayTween.TweenCallback(Callable.From(() =>
		{
			var tween = GetTree().CreateTween().BindNode(this);
			tween.TweenProperty(_sprite, "scale", Vector3.One * 0.75f, 1.0f);
			tween.Parallel().TweenProperty(_sprite, "modulate", new Color(1, 1, 1, 0.75f), 1.0f);
		}));

		// Second decay stage: 2.5 seconds before end
		float secondDecayStart = Mathf.Max(0, totalLifetime - 2.5f);

		// Calculate the interval from the *previous* tween's end, or from the start.
		// It's cleaner to calculate from the start of the particle's life.
		// The interval for the second tween should be from the end of the first tween.
		// Or, I can just use a sequence of intervals from the start.
		// Let's use a sequence of intervals from the start.
		_decayTween.TweenInterval(secondDecayStart - firstDecayStart);
		_decayTween.TweenCallback(Callable.From(() =>
		{
			var tween = GetTree().CreateTween().BindNode(this);
			tween.TweenProperty(_sprite, "scale", Vector3.One * 0.5f, 0.5f);
			tween.Parallel().TweenProperty(_sprite, "modulate", new Color(1, 1, 1, 0.5f), 0.5f);
		}));
	}

	private void OnLifetimeTimeout()
	{
		ManaParticleManager.Instance.Release(this);
	}

	// This is called by the ObjectPoolManager when the object is released back into the pool.
	public void Reset()
	{
		StopMoving();
		_lifetimeTimer.Stop();
		_decayTween?.Kill();
		_decayTween = null;
	}

	public void DriftIdle()
	{
		StopMoving();

		// Set a random drift velocity
		_velocity = new Vector3(
			(float)GD.RandRange(-1.0, 1.0),
			(float)GD.RandRange(-1.0, 1.0),
			(float)GD.RandRange(-1.0, 1.0)
		).Normalized() * _driftSpeed;
	}

	private void StopMoving()
	{
		LinearVelocity = Vector3.Zero;
		_state = ManaParticleState.Idle;
		_velocity = Vector3.Zero;
		_target = null;
	}
}