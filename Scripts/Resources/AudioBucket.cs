using Godot;
using Godot.Collections;

[GlobalClass]
public partial class AudioBucket : AudioStream
{
	[Export] public Array<AudioStream> Bucket;
	public int Count => Bucket.Count;
}