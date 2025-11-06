using Godot;

[GlobalClass]
public partial class ManaParticle : Pickup
{
    public SizeType SizeType { get; private set; }
    private ManaParticleData _manaParticleData;

    public void Initialize(ManaParticleData data)
    {
        base.Initialize(data);
        SizeType = data.SizeType;
        _manaParticleData = data;
    }

    public override void Collect()
    {
        var audioStream = _manaParticleData.AudioStream;
        var pitch = (float)(GD.RandRange(.95, 1.05) * _manaParticleData.AudioPitch);
        AudioManager.Instance.PlaySoundAtPosition(audioStream, GlobalPosition, pitch);

        base.Collect();
    }
}
