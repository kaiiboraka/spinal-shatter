using Godot;

namespace SpinalShatter;

[GlobalClass]
public partial class Money : Pickup
{
    public MoneyTier MoneyTier { get; private set; }
    public override MoneyData Data => data as MoneyData;

    private float _bounciness = 0.5f;
    private float _friction = 0.1f;
    private float _minSettleVelocity = 0.1f;
    private float _explosionSpeed = 5.0f;

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
        // Apply gravity if not on the floor
        if (!IsOnFloor())
        {
            Velocity += Vector3.Down * Constants.GRAVITY_MAG * (float)delta;
        }
        // Apply friction if on the floor
        else
        {
            Velocity = Velocity.Lerp(Vector3.Zero, _friction);
            if (Velocity.Length() < _minSettleVelocity)
            {
                Velocity = Vector3.Zero;
            }
        }
    }
    
    protected override void HandleCollision(KinematicCollision3D collision, Vector3 originalVelocity)
    {
        // Use the built-in bounce calculation and apply our bounciness
        Velocity = originalVelocity.Bounce(collision.GetNormal()) * _bounciness;
    }
}
