using Godot;
using System;

public partial class Combatant : CharacterBody3D
{
    [ExportGroup("Components")]
    [Export] public HealthComponent HealthComponent { get; set; }
    [Export] private Area3D _hurtbox; // Common hurtbox

    [ExportSubgroup("Knockback", "Knockback")]
    [Export] private float KnockbackStrength { get; set; } = 5.0f;
    [Export] private float KnockbackWeight { get; set; } = 1.0f; // New property
    [Export] private float KnockbackDecay { get; set; } = 0.9f;

    protected Vector3 _knockbackVelocity = Vector3.Zero;

    public override void _Ready()
    {
        HealthComponent ??= GetNode<HealthComponent>("%HealthComponent");
        HealthComponent.Died += OnDied;
        HealthComponent.Hurt += OnHurt;

        _hurtbox.BodyEntered += OnHurtboxBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Apply knockback
        Velocity += _knockbackVelocity;
        _knockbackVelocity = _knockbackVelocity.Lerp(Vector3.Zero, KnockbackDecay * (float)delta);
    }

    public virtual void OnHurtboxBodyEntered(Node3D body)
    {
        // Base implementation, can be overridden
    }

    public virtual void TakeDamage(float amount, Vector3 sourcePosition)
    {
        HealthComponent.TakeDamage(amount, sourcePosition);
        PlayOnHurtFX();
    }

    public virtual void OnHurt(Vector3 sourcePosition, float damage)
    {
        var direction = (GlobalPosition - sourcePosition).Normalized();
        _knockbackVelocity = direction * (damage / KnockbackWeight); // Use damage and weight
    }

    public virtual void PlayOnHurtFX()
    {
        // Base implementation, can be overridden
    }

    public virtual void OnDied()
    {
        // Base implementation, can be overridden
    }

    public virtual void Reset()
    {
        HealthComponent.Reset();
    }
}
