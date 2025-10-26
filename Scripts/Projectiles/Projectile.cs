using Godot;
using System;

public partial class Projectile : RigidBody3D
{
    [Export]
    private Sprite3D _sprite; 

    [Export]
    private CollisionShape3D _collisionShape;

    public float Damage { get; private set; }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        var lifetimeTimer = new Timer();
        lifetimeTimer.WaitTime = 5.0f; // 5 seconds lifetime
        lifetimeTimer.OneShot = true;
        lifetimeTimer.Timeout += () => QueueFree();
        AddChild(lifetimeTimer);
        lifetimeTimer.Start();
    }

    public void Initialize(float damage, float size, Vector3 initialVelocity)
    {
        this.Damage = damage;
        
        this.LinearVelocity = initialVelocity;

        this.Mass = size; 
        if (_sprite != null)
        {
            _sprite.Scale = Vector3.One * size;
        }
        if (_collisionShape != null && _collisionShape.Shape is SphereShape3D sphere)
        {
            sphere.Radius = size * 0.5f; 
        }
    }

    private void OnBodyEntered(Node body)
    {
        if (body.IsInGroup("Enemies"))
        {
            if (body.HasMethod("TakeDamage"))
            {
                body.Call("TakeDamage", this.Damage);
            }
            
            QueueFree();
        }
    }
}
