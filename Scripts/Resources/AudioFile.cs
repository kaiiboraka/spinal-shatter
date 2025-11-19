using Godot;


[GlobalClass]
public partial class AudioFile : AudioStream
{
	[Export] public AudioStream Stream;
	[Export] public float PitchScale = 1f;
	[Export] public float VolumeDb = 0;

	public override AudioStreamPlayback _InstantiatePlayback()
	{
		return Stream?.InstantiatePlayback();
	}

	public override double _GetLength()
	{
		if (Stream == null)
		{
			return 0;
		}
		return Stream.GetLength();
	}

	public override string _GetStreamName()
	{
		return Stream == null ? base._GetStreamName() : Stream._GetStreamName();
	}

	public override bool _IsMonophonic()
	{
		return Stream != null && Stream.IsMonophonic();
	}
}