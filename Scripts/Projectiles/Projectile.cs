using Godot;
using System;

public partial class Projectile : RigidBody3D
{
    private enum ProjectileState
    {
        Charging,
        Fired
    }

    [Export] private SpriteBase3D _sprite;
    [Export] private CollisionShape3D _collisionShape;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _minRefundPercent = 0.1f;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _maxRefundPercent = 0.25f;

    public Node3D LevelParent { get; set; }
    public float Damage { get; private set; }
    public float InitialManaCost { get; private set; }

    private ProjectileState _state = ProjectileState.Charging;
    private Timer _lifetimeTimer;

    public override void _Ready()
    {
        if (_sprite == null) _sprite = GetNode<SpriteBase3D>("Sprite3D");
        if (_collisionShape == null) _collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");

        _lifetimeTimer = new Timer();
        _lifetimeTimer.WaitTime = 5.0f; // 5 seconds lifetime
        _lifetimeTimer.OneShot = true;
        _lifetimeTimer.Timeout += () => QueueFree();
        AddChild(_lifetimeTimer);

        // Disable physics until launched
        this.Freeze = true;
        _collisionShape.Disabled = true;

        BodyEntered += OnBodyEntered;
    }

    public void BeginCharge(Node3D parent)
    {
        parent.AddChild(this);
        this.Position = Vector3.Zero;
        UpdateChargeVisuals(0.1f); // Start at 10% size
    }

    public void UpdateChargeVisuals(float size)
    {
        if (_state != ProjectileState.Charging) return;

        if (_sprite != null)
        {
            _sprite.Scale = Vector3.One * size;
        }
        if (_collisionShape != null && _collisionShape.Shape is SphereShape3D sphere)
        {
            // Ensure the collision shape doesn't get too small
            sphere.Radius = Mathf.Max(0.05f, size * 0.5f);
        }
    }

    public void Launch(float damage, float initialManaCost, Vector3 initialVelocity)
    {
        if (_state != ProjectileState.Charging) return;

        _state = ProjectileState.Fired;
        this.Damage = damage;
        this.InitialManaCost = initialManaCost;

        // Re-parent to the root and maintain global position
        var globalTransform = this.GlobalTransform;
        if (GetParent() is Node3D parent)
        {
            parent.RemoveChild(this);
        }
        LevelParent.AddChild(this);
        this.GlobalTransform = globalTransform;

        // Enable physics and launch
        this.Freeze = false;
        _collisionShape.Disabled = false;
        this.LinearVelocity = initialVelocity;
        this.Mass = _sprite.Scale.X; // Set mass based on final size

        _lifetimeTimer.Start();
    }

    private void OnBodyEntered(Node body)
    {
        // Assuming walls and ground are on specific physics layers you can check.
        // For now, we'll just check if the body is NOT an enemy.
        // A more robust way is to use physics layers (e.g., if (body.IsInLayer(LayerNames.PHYSICS_3D.SOLID_WALL_BIT)))

        if (!body.IsInGroup("Enemies"))
        {
            HandleWallBounce();
        }
    }

    private void HandleWallBounce()
    {
        float refundPercent = (float)GD.RandRange(_minRefundPercent, _maxRefundPercent);
        int manaToSpawn = Mathf.RoundToInt(InitialManaCost * refundPercent);

        if (manaToSpawn > 0)
        {
            ManaParticleManager.Instance.SpawnMana(manaToSpawn, this.GlobalPosition);
        }

        // Reduce size and damage
        this.Damage *= (1.0f - refundPercent);
        _sprite.Scale *= (1.0f - refundPercent);
        if (_collisionShape.Shape is SphereShape3D sphere)
        {
            sphere.Radius *= (1.0f - refundPercent);
        }
        this.Mass *= (1.0f - refundPercent);

        // Destroy if too small
        if (_sprite.Scale.X < 0.1f)
        {
            QueueFree();
        }
    }
}
