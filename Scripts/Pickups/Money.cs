using Godot;

namespace SpinalShatter;

[GlobalClass]
public partial class Money : Pickup
{
    public MoneyTier MoneyTier { get; private set; }
    public override MoneyData Data => data as MoneyData;

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        // When not attracting, let the default physics engine handle bouncing and gravity.
        base._IntegrateForces(state);
        if (CanAttract)
        {
            // When attracting, take over the physics state to move towards the player.
            state.LinearVelocity = Velocity;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // If we can attract, update the custom Velocity property.
        // _IntegrateForces will then use this value.
        if (CanAttract)
        {
            CustomIntegrator = true;
            Attract();
        }
        else CustomIntegrator = false;
    }

    public override void Initialize(PickupData data)
    {
        base.Initialize(data);
        this.data = data as MoneyData;
        if (Data == null) return;
        MoneyTier = Data.MoneyTier;
        Sprite.Play();
    }
}
