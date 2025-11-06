using Godot;

[GlobalClass]
public partial class Money : Pickup
{
    public MoneyType Type { get; private set; }

    public void Initialize(MoneyData data)
    {
        base.Initialize(data);
        Type = data.Type;
    }

    public override void Collect()
    {
        // Add money specific collection logic here
        base.Collect();
    }
}
