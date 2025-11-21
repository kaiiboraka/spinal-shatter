using Godot;

namespace SpinalShatter;

[GlobalClass]
public partial class ManaParticle : Pickup
{
    public SizeType SizeType { get; private set; }
    public override ManaParticleData Data => data as ManaParticleData;

    public override void Initialize(PickupData data)
    {
        base.Initialize(data);
        this.data = data as ManaParticleData;
        if (Data == null) return;
        SizeType = Data.SizeType;
        Sprite.Play();
    }

    protected override void HandleIdlePhysics(double delta)
    {
        // Mana floats, no gravity
    }

    protected override void HandleCollision(KinematicCollision3D collision)
    {
        // Reflect off surfaces
        Velocity = Velocity.Bounce(collision.GetNormal());
    }
}