using Godot;

namespace Elythia
{
    public struct ProjectileLaunchData
    {
        public float Damage { get; set; }
        public float ManaCost { get; set; }
        public float ChargeRatio { get; set; }
        public float DamageGrowthConstant { get; set; }
        public float AbsoluteMaxProjectileSpeed { get; set; }
    }
}