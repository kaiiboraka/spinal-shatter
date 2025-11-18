using Godot;
using Godot.Collections;

[GlobalClass]
public partial class AudioData : Resource
{
	[Export] public Dictionary<string, AudioStream> Sounds;

	public AudioStream this[string key] => Sounds[key];
}