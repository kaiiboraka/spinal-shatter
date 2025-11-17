using Godot;
using SpinalShatter;

[GlobalClass]
public partial class EnemyData : Resource
{
    [Export] public string Name { get; private set; }
    [Export] public int BaseCost { get; private set; } = 1;
    [Export] public EnemyRank Rank { get; private set; } = EnemyRank.Rank1_Bone;

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
    [Export] public float KnockbackWeight { get; private set; } = 10f;
    [ExportSubgroup("Money", "Money")]
    [Export] public int MoneyAmountToDrop { get; private set; } = 10;
    [ExportSubgroup("Mana", "Mana")]
    [Export] public int ManaAmountToDrop { get; private set; } = 10;
    [Export(PropertyHint.Range, "0.0, 1.0")] public float ManaMinRefundPercent { get; private set; } = 0.05f;
    [Export(PropertyHint.Range, "0.0, 1.0")] public float ManaMaxRefundPercent { get; private set; } = 0.30f;

    // Attack
    [ExportSubgroup("Attack", "Attack")]
    [Export] public float AttackRange { get; private set; } = 2.0f;
    [Export] public float AttackCooldown { get; private set; } = 1.5f;
    [Export] public float AttackDamage { get; private set; } = 10f;

    // Projectiles
    [ExportGroup("Projectiles")]
    [Export(PropertyHint.GroupEnable, "")]
    public bool IsRanged { get; private set; }
    [Export] public float ProjectileSpeed { get; private set; } = 20.0f;
    [Export] public PackedScene ProjectileScene { get; private set; }

}
