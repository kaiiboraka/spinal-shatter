using Elythia;
using Godot;

namespace SpinalShatter;

public partial class Combatant : CharacterBody3D
{
    [ExportGroup("Components")]
    [Export] public HealthComponent HealthComponent { get; set; }
    [Export] private Area3D _hurtbox; // Common hurtbox

    protected float KnockbackDamageScalar { get; set; } = 2.0f;
    protected float KnockbackWeight { get; set; } = 5.0f;
    protected Vector3 _knockbackVelocity = Vector3.Zero;

    public override void _Ready()
    {
        HealthComponent ??= GetNode<HealthComponent>("%HealthComponent");
        HealthComponent.Died += OnDied;
        HealthComponent.Hurt += OnHurt;

        _hurtbox.BodyEntered += OnHurtboxBodyEntered;
    }

    public virtual void OnHurtboxBodyEntered(Node3D body)
    {
        if (body is Projectile projectile)
        {
            // Don't get hurt by our own projectiles
            if (projectile.Owner == this) return;

            float actualDamageDealt = TakeDamage(projectile.Damage, projectile.GlobalPosition);
            
            // Calculate mana lost based on actual damage dealt, checking for division by zero.
            float manaLostAmount = 0;
            if (projectile.Damage > 0)
            {
                manaLostAmount = actualDamageDealt * (projectile.ManaCost / projectile.Damage);
            }
            
            projectile.ApplyManaLoss(manaLostAmount, projectile.GlobalPosition);
        }
    }

    public virtual float TakeDamage(float amount, Vector3 sourcePosition)
    {
        return HealthComponent.TakeDamage(amount, sourcePosition);
    }

    protected static Vector3 Lift => Vector3.Up * .1f;

    protected virtual void ApplyKnockback(float damage, Vector3 direction)
    {
        float knockbackDamage = Mathf.Clamp(damage, 0, 30f);

        _knockbackVelocity = (direction + Lift) * (knockbackDamage / KnockbackWeight);
        // DebugManager.Info($"Combatant Knockback: Damage={damage}, Direction={direction}, Lift={Lift}, KnockbackDamage={knockbackDamage}, KnockbackWeight={KnockbackWeight}, ResultingVelocity={_knockbackVelocity}");
    }

    public virtual void OnHurt(Vector3 sourcePosition, float damage)
    {
        // Common knockback direction for character bodies
        var direction = (GlobalPosition - sourcePosition).XZ().Normalized() + new Vector3(0, 0.1f, 0);
        ApplyKnockback(damage, direction);
        PlayOnHurtFX();
    }


    public virtual void PlayOnHurtFX()
    {
        // Base implementation, can be overridden by children
    }

    public virtual void OnDied()
    {
        // Base implementation, can be overridden by children
    }

    public virtual void Reset()
    {
        HealthComponent.Reset();
    }
}