using Godot;

public partial class ManaParticle : RigidBody3D
{
	private enum ManaParticleState
	{
		Idle,
		Attracted
	}

	[Export] public ManaSize Size { get; private set; }
	[Export] private float _driftSpeed = 0.5f;
	[Export] private float _attractSpeed = 15.0f;

	public int ManaValue { get; private set; }

	private ManaParticleState _state = ManaParticleState.Idle;
	private Vector3 _velocity = Vector3.Zero;
	private Node3D _target = null;

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
		state.LinearVelocity = _velocity;
	}

	public void Attract(Node3D target)
	{
		_state = ManaParticleState.Attracted;
		_target = target;
	}

	public void Initialize(int manaValue)
	{
		this.ManaValue = manaValue;
		Reset();
		Drift();
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