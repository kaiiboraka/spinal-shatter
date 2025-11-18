using Godot;
using Elythia;

namespace SpinalShatter;

[GlobalClass]
public partial class ManaParticle : Pickup
{
    public SizeType SizeType { get; private set; }
    public override ManaParticleData Data => data as ManaParticleData;

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        base._IntegrateForces(state);
        if (CanAttract || (Type == PickupType.Mana && CurrentState == PickupState.Idle))
        {
            state.LinearVelocity = Velocity;
        }

        // if (Type == PickupType.Money)
        // {
        // 	var newVel = state.LinearVelocity;
        // 	newVel.Y -= _gravity;
        // 	state.LinearVelocity = newVel;
        // }
    }

    public override void _PhysicsProcess(double delta)
    {
    	if (CanAttract) Attract();
    }

    public override void Initialize(PickupData data)
    {
        // DebugManager.Debug($"ManaParticle: Initializing {Name} at GlobalPosition: {GlobalPosition}, with data: {data?.ResourcePath ?? "null"}");
        base.Initialize(data);
        this.data = data as ManaParticleData;
        if (Data == null) return;
        SizeType = Data.SizeType;
        Sprite.Play();
        // DebugManager.Debug($"ManaParticle: {Name} Initialized. Final GlobalPosition: {GlobalPosition}");
    }
}
