using Godot;
using Elythia;

namespace SpinalShatter;

[GlobalClass]
public partial class ManaParticle : Pickup
{
    public SizeType SizeType { get; private set; }
    public override ManaParticleData Data => data as ManaParticleData;

    public override void Initialize(PickupData data)
    {
        DebugManager.Debug($"ManaParticle: Initializing {Name} at GlobalPosition: {GlobalPosition}, with data: {data?.ResourcePath ?? "null"}");
        base.Initialize(data);
        this.data = data as ManaParticleData;
        if (Data == null) return;
        SizeType = Data.SizeType;
        Sprite.Play();
        DebugManager.Debug($"ManaParticle: {Name} Initialized. Final GlobalPosition: {GlobalPosition}");
    }

    public override void Collect()
    {
        var pitch = (float)(GD.RandRange(.95, 1.05) * Data.AudioPitch);
        AudioManager.Instance.PlaySoundAtPosition(Data.AudioStream, GlobalPosition, pitch, -12f);

        base.Collect();
    }
}
