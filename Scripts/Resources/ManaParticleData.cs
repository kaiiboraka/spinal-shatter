using Godot;

[GlobalClass, Tool]
public partial class ManaParticleData : PickupData
{
    [Export] public SizeType SizeType { get; private set; }
}
