namespace SpinalShatter;

using Godot;

[GlobalClass]
public partial class StatItemData : ShopItemData
{
    [ExportGroup("Stat Bonus")]
    [Export] public StatType TargetStat { get; private set; }
    [Export] public float Value { get; private set; }
    [Export] public bool IsMultiplier { get; private set; }
}
