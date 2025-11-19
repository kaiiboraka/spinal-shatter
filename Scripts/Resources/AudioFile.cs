using Godot;

[GlobalClass]
public partial class AudioFile : AudioStream
{
	[Export] public AudioStream Stream;
	[Export] public float PitchScale = 1f;
	[Export] public float VolumeDb = 0;
}