using Elythia;
using Godot;
using System.Linq;

namespace SpinalShatter;

public abstract partial class Combatant : CharacterBody3D
{
    public HealthComponent HealthComponent { get; private set; }
    public EffectsComponent EffectComponent { get; private set; }
    protected Area3D hurtbox; // Common hurtbox
    protected Area3D meleeHitbox;
    protected float slowMultiplier = 1.0f;

    public Vector3 HurtboxPosition => hurtbox.GlobalPosition;

    protected float KnockbackWeight { get; set; } = 5.0f;
    protected Vector3 knockbackVelocity = Vector3.Zero;

	public bool DeadNow { get; protected set; } = false;

    public override void _Ready()
    {
        GetComponents();
        ConnectEvents();
    }

    protected virtual void GetComponents()
    {
        HealthComponent ??= GetNode<HealthComponent>("HealthComponent");
        
        EffectComponent ??= GetNodeOrNull<EffectsComponent>("EffectComponent");

        hurtbox = GetNode<Area3D>("Hurtbox");
        if (HasNode("MeleeHitbox"))
        {
            meleeHitbox = GetNode<Area3D>("MeleeHitbox");
        }
    }

    protected virtual void ConnectEvents()
    {
        HealthComponent.Hurt += OnHurt;
        HealthComponent.OutOfHealth += OnRanOutOfHealth;
        
        EffectComponent.EffectTicked += OnEffectComponentTicked;
        EffectComponent.EffectStacksChanged += OnEffectComponentStacksChanged;
        EffectComponent.EffectExpired += OnEffectComponentExpired;

        hurtbox.BodyEntered += OnHurtboxBodyEntered;

        if (meleeHitbox != null)
        {
            meleeHitbox.AreaEntered += OnMeleeHitboxAreaEntered;
        }
    }

    public void ApplyEffect(StackingEffectType effectType, int stacks = 1)
    {
	    EffectComponent.ApplyEffect(effectType, stacks);
    }
    
    protected virtual void OnEffectComponentTicked(StackingEffectType type, float intensity)
    {
        if (type == StackingEffectType.Poison)
        {
            TakeDamage(intensity, GlobalPosition);
        }
    }
    
    protected virtual void OnEffectComponentStacksChanged(StackingEffectType type, int newStackCount)
    {
        var data = EffectsComponent.EffectData[type];
        var intensity = newStackCount * data.IntensityPerStack;
        switch (type)
        {
            case StackingEffectType.Slow:
            {
                slowMultiplier = Mathf.Clamp(1.0f - (intensity / 100.0f), 0.01f, 1.0f);
                break;
            }
        }
    }
    
    protected virtual void OnEffectComponentExpired(StackingEffectType type)
    {
        if (type == StackingEffectType.Slow)
        {
            slowMultiplier = 1.0f;
        }
    }

    public void OnHurtboxBodyEntered(Node3D body)
    {
		if (DeadNow) return;

        if (body is not Projectile projectile) return;

        if (projectile.SpellData.Weapon == WeaponType.Slash)
        {
	        if (this is Enemy enemy && !projectile.HitEnemies.Add(enemy))
	        {
		        // Already hit by this projectile instance, do nothing.
		        return;
	        }
        }

        // Don't get hurt by our own projectiles
        if (projectile.Caster == this) return;

        float actualDamageDealt = TakeDamage(projectile.CurrentDamage, projectile.GlobalPosition);

        if (projectile.EffectsToApply != null)
        {
	        foreach (var (effectType, stacks) in projectile.EffectsToApply)
	        {
		        // Validate that the spell is allowed to apply this effect
		        if (projectile.SpellData.ApplicableEffects.Any(e => e == effectType))
		        {
			        ApplyEffect(effectType, stacks);
		        }
	        }
        }
            
        if (this is Enemy) projectile.OnEnemyHit(actualDamageDealt);
    }

    protected abstract void OnMeleeHitboxAreaEntered(Area3D area);

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