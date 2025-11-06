using Godot;

[GlobalClass, Tool]
public partial class MoneyData : PickupData
{
    [Export] public MoneyType Type { get; private set; }
}
