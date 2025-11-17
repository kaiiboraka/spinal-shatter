using Godot;

namespace SpinalShatter;

[GlobalClass, Tool]
public partial class MoneyData : PickupData
{
    [Export] public MoneyTier MoneyTier { get; private set; }
}
