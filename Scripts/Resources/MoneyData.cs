using Godot;

[GlobalClass, Tool]
public partial class MoneyData : PickupData
{
    [Export] public MoneyTier MoneyTier { get; private set; }
}
