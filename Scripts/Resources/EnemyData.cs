using Elythia;
using Godot;
using SpinalShatter;

[GlobalClass]
public partial class EnemyData : Resource
{
    [Export] public string Name { get; private set; }
    [Export] public int BaseCost { get; private set; }
    [Export] public EnemyRank Rank { get; private set; }
    [Export] public EnemyMovementType MovementType { get; private set; }
    [Export] public EnemyRangeType RangeType { get; private set; }

    public bool IsFlying => MovementType == EnemyMovementType.Flying;
    public bool IsGrounded => MovementType == EnemyMovementType.Grounded;
    // Audio
    [ExportGroup("Aesthetics")]
    [Export] public EnemyAudioData AudioData { get; private set; }
    [Export] public int DeathParticleCount { get; private set; } = 10;

    // Patrol
    [ExportGroup("Patrol")]
    [Export] public float RecoveryTime { get; private set; } = 1.0f;
    [Export] public float ChaseRotationSpeed { get; private set; } = 5.0f;
    [Export] public float WalkSpeed { get; private set; } = 8.0f;
    [Export] public float MinWalkTime { get; private set; } = 1.0f;
    [Export] public float MaxWalkTime { get; private set; } = 5.0f;
    [Export] public float MinWaitTime { get; private set; } = 1.0f;
    [Export] public float MaxWaitTime { get; private set; } = 5.0f;

    // Combat
    [ExportCategory("Combat")]
    [ExportSubgroup("Knockback", "Knockback")]
    [Export] public float KnockbackWeight { get; private set; } = 5f;
    [Export] public float KnockbackDamageScalar { get; set; } = 2.0f;
    [Export] public float KnockbackDecayRate { get; set; } = 10.0f;

    [ExportSubgroup("Money", "Money")]
    [Export] public IntValueRange MoneyAmountToDrop { get; private set; } = new(10);
    [ExportSubgroup("Mana", "Mana")]
    [Export] public IntValueRange ManaAmountToDrop { get; private set; } = new(10);

    // Attack
    [ExportSubgroup("Attack", "Attack")]
    [Export] public float AttackRange { get; private set; } = 2.0f;
    [Export] public float AttackCooldown { get; private set; } = 1.5f;
    [Export] public float AttackDamage { get; private set; } = 10f;

    // Projectiles
    [ExportGroup("Projectiles")] private bool _isRanged = false;
    [Export(PropertyHint.GroupEnable, "")] public bool IsRanged
    {
        get => RangeType == EnemyRangeType.Ranged;
        private set
        {
            RangeType = value ? EnemyRangeType.Ranged : EnemyRangeType.Melee;
            _isRanged = value;
        }
    }
    [Export] public float ProjectileSpeed { get; private set; } = 20.0f;
    [Export] public PackedScene ProjectileScene { get; private set; }

}
