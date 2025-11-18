using Godot;

namespace SpinalShatter;

[GlobalClass]
public partial class Money : Pickup
{
    public MoneyTier MoneyTier { get; private set; }
    public override MoneyData Data => data as MoneyData;

    public override void _PhysicsProcess(double delta)
    {
        // Once the pickup can be attracted to the player, switch from default physics
        // to the custom integrator in the base Pickup class, which handles attraction.
        CustomIntegrator = CanAttract;

        // The base class might have attraction logic in _PhysicsProcess as well.
        if (CanAttract)
        {
            base._PhysicsProcess(delta);
        }
    }

    public override void Initialize(PickupData data)
    {
        base.Initialize(data);
        this.data = data as MoneyData;
        if (Data == null) return;
        MoneyTier = Data.MoneyTier;
        Sprite.Play();
        
        // Start with default physics integration to allow bouncing.
        // _PhysicsProcess will switch to custom integration when it's time to attract the player.
        CustomIntegrator = false;
    }
}
