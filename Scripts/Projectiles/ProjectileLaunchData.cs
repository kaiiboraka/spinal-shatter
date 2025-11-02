using Godot;

namespace Elythia
{
    public struct ProjectileLaunchData
    {
        public Node Caster { get; set; }
        public float Damage { get; set; }
        public float ManaCost { get; set; }
        public Vector3 InitialVelocity { get; set; }
        public float ChargeRatio { get; set; }
        public float DamageGrowthConstant { get; set; }
        public float AbsoluteMaxProjectileSpeed { get; set; }
        public float MaxInitialManaCost { get; set; }
    }}