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
        if (bounceCooldown > 0)
        {
            return;
        }

        for (int i = 0; i < state.GetContactCount(); i++)
        {
            Node collider = state.GetContactColliderObject(i) as Node;
            if (collider != null)
            {
                var contactLocalNormal = state.GetContactLocalNormal(i);
                var contactLocalPosition = state.GetContactColliderPosition(i);

                state.LinearVelocity = state.LinearVelocity.Bounce(contactLocalNormal) * this.PhysicsMaterialOverride.Bounce;
                break;
            }
        }

        if (CanAttract)
        {
            state.LinearVelocity = Velocity;
        }
        if (Type == PickupType.Money)
        {
        	var newVel = state.LinearVelocity;
        	newVel.Y -= 1;
        	state.LinearVelocity = newVel;



        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
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
