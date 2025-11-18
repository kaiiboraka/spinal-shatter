using Godot;

[GlobalClass]
public partial class EnemyAudioData : Resource
{
	[ExportSubgroup("Audio", "AudioStream")]
	[Export] public AudioStream DieSound { get; private set; }
	[Export] public AudioStream HurtSound { get; private set; }
	[Export] public AudioStream AttackSound { get; private set; }
}