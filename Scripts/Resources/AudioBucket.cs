using Godot;
using Godot.Collections;

[GlobalClass]
public partial class AudioBucket : AudioStream
{
	[Export] public Array<AudioStream> Bucket;
	public int Count => Bucket?.Count ?? 0;

	[Export] public float PitchScale = 1f;
	[Export] public float VolumeDb = 0;

	private AudioStream GetRandomStream()
	{
		return Count == 0 ? null : Bucket[GD.RandRange(0, Count - 1)];
	}

	public override AudioStreamPlayback _InstantiatePlayback()
	{
		var stream = GetRandomStream();
		return stream?.InstantiatePlayback();
	}
    
	public override double _GetLength()
	{
		if (Count == 0)
		{
			return 0;
		}
		// The length of a bucket is not super well-defined.
		// Let's return the length of the first stream for preview purposes.
		return Bucket[0]?.GetLength() ?? 0;
	}

	public override string _GetStreamName()
	{
		// The name of a bucket is not super well-defined.
		return base._GetStreamName();
	}

	public override bool _IsMonophonic()
	{
		if (Count == 0)
		{
			return false;
		}
		// Assume all streams in the bucket have the same monophonic status.
		return Bucket[0]?.IsMonophonic() ?? false;
	}
}