using Godot;

[GlobalClass]
public partial class ManaParticle : RigidBody3D
{
	[Signal] public delegate void CollectedEventHandler(ManaParticle particle);

	[Signal] public delegate void ReleasedEventHandler(ManaParticle particle);

	[Export] private AudioStreamPlayer3D AudioPlayer_ManaPickups;

	public enum ManaParticleState
	{
		Idle,
		Attracted,
		Collected,
		Expired
	}

	[Export] public ManaSize Size { get; private set; }
	[Export] private Timer _lifetimeTimer;

	private double _lifetime;
	private float _driftSpeed;
	private float _attractSpeed;
	[Export] private SpriteBase3D _sprite;
	[Export] private CollisionShape3D _collisionShape;
	[Export] private CollisionShape3D _areaShape;
	public int ManaValue { get; private set; }

	[Export] private ManaParticleState _state = ManaParticleState.Idle;

	public ManaParticleState State => _state;

	private Vector3 _velocity = Vector3.Zero;
	private Node3D _target = null;


	private Tween _decayTween;

	public override void _Ready()
	{
		// This method is only called once when the scene is first instantiated.
		// Signal connections should be made here.
		_lifetimeTimer.Timeout += OnLifetimeTimeout;
	}

	public override void _EnterTree()
	{
		base._EnterTree();
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

	public override void _Process(double delta)
	{
		base._Process(delta);
		BlinkRoutine();
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		if (_state == ManaParticleState.Attracted || _state == ManaParticleState.Idle)
		{
			state.LinearVelocity = _velocity;
		}

		base._IntegrateForces(state);
	}

    public void Attract(Node3D target)
    {
        if (_state == ManaParticleState.Collected || _state == ManaParticleState.Expired) return;

        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is being attracted. Old state: {_state}");
        _state = ManaParticleState.Attracted;
        _target = target;
        _decayTween?.Kill();
        Sleeping = false;
        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is being attracted. New state: {_state}");
    }

    public void Collect()
    {
        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is being collected. Old state: {_state}");
        _state = ManaParticleState.Collected;
        StopMoving();
        _blinkTween?.Kill();

		AudioPlayer_ManaPickups.Stream = ManaParticleManager.Instance.ParticleData[Size].AudioStream;
		AudioPlayer_ManaPickups.PitchScale = (float)(GD.RandRange(.95, 1.05) *
													 ManaParticleManager.Instance.ParticleData[Size]
																		.AudioPitch);
		AudioPlayer_ManaPickups.Play();

        EmitSignal(SignalName.Collected, this);
        EmitSignal(SignalName.Released, this);
        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is being collected. New state: {_state}");
    }

    // Called by the ManaParticleManager when a particle is spawned from the pool.
    public void Initialize(ManaParticleData data)
    {
        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is being initialized. Old state: {_state}");

        _state = ManaParticleState.Idle; // Explicitly set to Idle
        Visible = true;
        this.ManaValue = data.Value;
        this.Size = data.Size;
        _driftSpeed = data.DriftSpeed;
        _attractSpeed = data.AttractSpeed;
        _lifetime = data.Lifetime;
        _lifetimeTimer.WaitTime = _lifetime;

        ResetVisuals(data);

        if (_collisionShape is { Shape: SphereShape3D sphere })
        {
            sphere.Radius = data.CollisionShapeRadius;
        }

        if (_areaShape is { Shape: SphereShape3D areaSphere })
        {
            areaSphere.Radius = data.AreaShapeRadius;
        }

        // Set initial state for a new life
        _lifetimeTimer.Start();
        StopMoving();
        LinearVelocity = DriftIdle();
        ApplyCentralImpulse(_velocity);

        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is being initialized. New state: {_state}");
    }


    private void BlinkRoutine()
    {
        if (_lifetimeTimer.IsStopped()) return;

        double timeLeft = _lifetimeTimer.TimeLeft;

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

    private Tween _blinkTween;

    private void Blink(float alpha, float duration)
    {
        if (_blinkTween != null && _blinkTween.IsRunning()) return;

        // if (AnimatedSprite == null) return;
        // AnimatedSprite.SpeedScale = 1 + (1 - alpha);
        _blinkTween = CreateTween().SetLoops(2).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.InOut);
        _blinkTween.TweenProperty(_sprite, "modulate:a", alpha, duration / 2);
        _blinkTween.TweenProperty(_sprite, "modulate:a", 1.0f, duration / 2);
    }

    private void OnLifetimeTimeout()
    {
        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is timing out. Old state: {_state}");
        _state = ManaParticleState.Expired;
        EmitSignal(SignalName.Released, this);
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector3.One * 0.001f, 0.2f).SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(() => ManaParticleManager.Instance.Release(this)));
        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is timing out. New state: {_state}");
    }

    private void ResetVisuals(ManaParticleData data)
    {
        Scale = Vector3.One;
        _sprite.Modulate = Colors.White;

        // Apply visual data
        if (_sprite != null)
        {
            _sprite.Modulate = data?.Modulate ?? Colors.White;
            _sprite.Scale = data?.Scale ?? Vector3.One;
        }
    }

    // This is called by the ObjectPoolManager when the object is released back into the pool.
    public void Reset()
    {
        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is resetting. Old state: {_state}");
        Visible = false;
        ResetVisuals(null);
        StopMoving();
        _lifetimeTimer.Stop();

        // _blinkTween?.Kill();
        // _blinkTween = null;
        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is resetting. New state: {_state}");
    }

    public void StopMoving()
    {
        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is stopped moving. Old state: {_state}");
        LinearVelocity = Vector3.Zero;
        if (_state != ManaParticleState.Collected && _state != ManaParticleState.Expired)
        {
            _state = ManaParticleState.Idle;
        }

        _velocity = Vector3.Zero;
        _target = null;
        // GD.Print($"{Time.GetTicksMsec()}: ManaParticle {Name} is stopped moving. New state: {_state}");
    }

    public Vector3 DriftIdle()
    {
        // Set a random drift velocity
        _velocity = new Vector3(
            (float)GD.RandRange(-1.0, 1.0),
            (float)GD.RandRange(-1.0, 1.0),
            (float)GD.RandRange(-1.0, 1.0)
        ).Normalized() * _driftSpeed;
        return _velocity;
    }
}