using Godot;

namespace SpinalShatter;

[GlobalClass, Tool]
public partial class MoneyData : PickupData
{
    [Export] public MoneyTier MoneyTier { get; private set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float Bounciness { get; private set; } = 0.5f;
    [Export] public float ExplosionSpeed { get; private set; } = 5.0f;
}

