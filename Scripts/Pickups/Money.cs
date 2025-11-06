using Godot;

[GlobalClass]
public partial class Money : Pickup
{
    public MoneyType Type { get; private set; }
    public override MoneyData Data => data as MoneyData;

    public override void Initialize(PickupData data)
    {
        base.Initialize(data);
        this.data = data as MoneyData;
        if (Data == null) return;
        Type = Data.MoneyType;
        Sprite.Play();
    }

    public override void Collect()
    {
        // Add money specific collection logic here
        base.Collect();
    }
}
