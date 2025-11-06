using Godot;

[GlobalClass, Tool]
public partial class MoneyData : PickupData
{
    [Export] public MoneyType MoneyType { get; private set; }
}
