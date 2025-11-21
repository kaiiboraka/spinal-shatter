using Godot;

namespace SpinalShatter;

[GlobalClass]
public partial class Money : Pickup
{
    public MoneyTier MoneyTier { get; private set; }
    public override MoneyData Data => data as MoneyData;

    [Export] private float _bounciness = 0.5f;
    [Export] private float _friction = 0.95f; // Value closer to 1 means less friction. This is multiplied each frame.
    [Export] private float _minSettleVelocity = 0.5f;
    [Export] private float _explosionSpeed = 4.0f;

    public override void Initialize(PickupData data)
    {
        base.Initialize(data);
        this.data = data as MoneyData;
        if (Data == null) return;
        MoneyTier = Data.MoneyTier;
        Sprite.Play();
        
        _bounciness = Data.Bounciness;
        _explosionSpeed = Data.ExplosionSpeed;
    }

    protected override void ResetMotion()
    {
        // "Explosion" effect
        var horizontalDir = new Vector2((float)GD.Randf() * 2 - 1, (float)GD.Randf() * 2 - 1).Normalized();
        var initialVelocity = new Vector3(horizontalDir.X, 1.0f, horizontalDir.Y).Normalized();
        Velocity = initialVelocity * _explosionSpeed;
    }

    protected override void HandleIdlePhysics(double delta)
    {
        // Apply gravity
        base.HandleIdlePhysics(delta);
        
        // Apply friction to horizontal velocity
        Velocity = new Vector3(Velocity.X * _friction, Velocity.Y, Velocity.Z * _friction);
    }
    
    protected override void HandleCollision(KinematicCollision3D collision)
    {
        // Bounce off surfaces
        Velocity = Velocity.Bounce(collision.GetNormal()) * _bounciness;

        // If velocity is very low after a bounce, settle it to prevent endless tiny bounces.
        if (Velocity.Length() < _minSettleVelocity)
        {
            Velocity = Vector3.Zero;
        }
    }
}