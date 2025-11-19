using Elythia;
using Godot;

namespace SpinalShatter;

public partial class Combatant : CharacterBody3D
{
    public HealthComponent HealthComponent { get; private set; }
    private Area3D hurtbox; // Common hurtbox

    protected float KnockbackDamageScalar { get; set; } = 2.0f;
    protected float KnockbackWeight { get; set; } = 5.0f;
    protected Vector3 knockbackVelocity = Vector3.Zero;

    public override void _Ready()
    {
        GetComponents();
        ConnectEvents();
    }

    protected virtual void GetComponents()
    {
        HealthComponent ??= GetNode<HealthComponent>("HealthComponent");
        hurtbox = GetNode<Area3D>("Hurtbox");
    }

    protected virtual void ConnectEvents()
    {
        HealthComponent.Hurt += OnHurt;
        HealthComponent.OutOfHealth += OnRanOutOfHealth;
        hurtbox.BodyEntered += OnHurtboxBodyEntered;
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

        knockbackVelocity = (direction + Lift) * (knockbackDamage / KnockbackWeight);
        // DebugManager.Info($"Combatant Knockback: Damage={damage}, Direction={direction}, Lift={Lift}, KnockbackDamage={knockbackDamage}, KnockbackWeight={KnockbackWeight}, ResultingVelocity={_knockbackVelocity}");
    }

    protected virtual void OnHurt(Vector3 sourcePosition, float damage)
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

    public virtual void OnRanOutOfHealth()
    {
        // Base implementation, can be overridden by children
    }

    public virtual void Reset()
    {
        HealthComponent.Refill();
    }
}