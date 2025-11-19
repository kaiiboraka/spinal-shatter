using System;
using Godot;
using System.Collections.Generic;

public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; }

	private List<AudioStreamPlayer3D> _stationaryPool = new List<AudioStreamPlayer3D>();
	private List<AttachedAudioStreamPlayer3D> _attachedPool = new List<AttachedAudioStreamPlayer3D>();
	private int _poolSize = 10;

	public override void _Ready()
	{
		Instance = this;
		for (int i = 0; i < _poolSize; i++)
		{
			var stationaryPlayer = new AudioStreamPlayer3D();
			AddChild(stationaryPlayer);
			_stationaryPool.Add(stationaryPlayer);

			var attachedPlayer = new AttachedAudioStreamPlayer3D();
			AddChild(attachedPlayer);
			_attachedPool.Add(attachedPlayer);
		}
	}


	public static void PlayBucketSimultaneous(AudioStreamPlayer3D player3D, AudioBucket bucket)
	{
		player3D.MaxPolyphony = bucket.Count;
		foreach (var audioStream in bucket.Bucket)
		{
			Play(player3D, audioStream);
		}
	}

	public static void PlayBucketSimultaneous(AudioStreamPlayer player, AudioBucket bucket)
	{
		player.MaxPolyphony = bucket.Count;
		foreach (var audioStream in bucket.Bucket)
		{
			Play(player, audioStream);
		}
	}

	public static void PlayBucketSequential(AudioStreamPlayer player, AudioBucket bucket)
	{
		if (bucket == null || bucket.Bucket == null || bucket.Count == 0)
			return;

		int currentIndex = 0;
		Action onFinished = null;

		onFinished = () =>
		{
			// Unsubscribe before potentially subscribing again
			player.Finished -= onFinished;

			// Play the next sound if there are more
			if (currentIndex < bucket.Count)
			{
				Play(player, bucket.Bucket[currentIndex]);
				currentIndex++;

				// Subscribe again if there are more sounds to play
				if (currentIndex < bucket.Count)
				{
					player.Finished += onFinished;
				}
			}
		};

		// Start playing the first sound
		Play(player, bucket.Bucket[0]);
		currentIndex = 1;

		// Subscribe to play the next sound if there are more
		if (currentIndex < bucket.Count)
		{
			player.Finished += onFinished;
		}
	}

	public static void PlayBucketSequential(AudioStreamPlayer3D player3D, AudioBucket bucket)
	{
		if (bucket == null || bucket.Bucket == null || bucket.Count == 0)
			return;

		int currentIndex = 0;
		Action onFinished = null;

		onFinished = () =>
		{
			// Unsubscribe before potentially subscribing again
			player3D.Finished -= onFinished;

			// Play the next sound if there are more
			if (currentIndex < bucket.Count)
			{
				Play(player3D, bucket.Bucket[currentIndex]);
				currentIndex++;

				// Subscribe again if there are more sounds to play
				if (currentIndex < bucket.Count)
				{
					player3D.Finished += onFinished;
				}
			}
		};

		// Start playing the first sound
		Play(player3D, bucket.Bucket[0]);
		currentIndex = 1;

		// Subscribe to play the next sound if there are more
		if (currentIndex < bucket.Count)
		{
			player3D.Finished += onFinished;
		}
	}

	public static void Play(AudioStreamPlayer3D player3D, AudioStream audioStream, float fromPosition = 0)
	{
		player3D.Stream = audioStream;
		if (audioStream is AudioFile custom)
		{
			player3D.PitchScale = custom.PitchScale;
			player3D.VolumeDb = custom.VolumeDb;
			player3D.Stream = custom.Stream;
		}
		player3D.Play(fromPosition);
	}

	public static void Play(AudioStreamPlayer player, AudioStream audioStream, float fromPosition = 0)
	{
		player.Stream = audioStream;
		if (audioStream is AudioFile custom)
		{
			player.PitchScale = custom.PitchScale;
			player.VolumeDb = custom.VolumeDb;
			player.Stream = custom.Stream;
		}
		player.Play(fromPosition);
	}
	public static AudioStreamPlayer3D PlayAtPosition(AudioFile sound, Vector3 location, float fromPosition = 0)
	{
		AudioStreamPlayer3D player = GetAvailableStationaryPlayer();

		player.Stream = sound.Stream;
		player.GlobalPosition = location;
		player.PitchScale = sound.PitchScale;
		player.VolumeDb = sound.VolumeDb;
		player.Play(fromPosition);

		player.Finished += () => { player.Stream = null; };

		return player;
	}

	public static AudioStreamPlayer3D PlayAtPosition(AudioStream sound, Vector3 location, float pitch, float volume, float fromPosition = 0)
	{
		AudioStreamPlayer3D player = GetAvailableStationaryPlayer();

		player.Stream = sound;
		player.GlobalPosition = location;
		player.PitchScale = pitch;
		player.VolumeDb = volume;
		player.Play(fromPosition);

		player.Finished += () => { player.Stream = null; };

		return player;
	}

	public static AttachedAudioStreamPlayer3D PlayAttachedToNode(AudioFile sound, Node3D targetNode)
	{
		AttachedAudioStreamPlayer3D player = GetAvailableAttachedPlayer();
		player.Stream = sound.Stream;
		player.PitchScale = sound.PitchScale;
		player.VolumeDb = sound.VolumeDb;
		player.TargetNode = targetNode;

		player.Play();

		player.Finished += () => { player.TargetNode = null; };

		return player;
	}


	public static AttachedAudioStreamPlayer3D PlayAttachedToNode(AudioStream sound, Node3D targetNode, float pitch, float volume)
	{
		AttachedAudioStreamPlayer3D player = GetAvailableAttachedPlayer();
		player.Stream = sound;
		player.PitchScale = pitch;
		player.VolumeDb = volume;
		player.TargetNode = targetNode;

		player.Play();

		player.Finished += () => { player.TargetNode = null; };

		return player;
	}

	private static AudioStreamPlayer3D GetAvailableStationaryPlayer()
	{
		foreach (var player in Instance._stationaryPool)
		{
			if (!player.Playing)
			{
				return player;
			}
		}

		var newPlayer = new AudioStreamPlayer3D();
		Instance.AddChild(newPlayer);
		Instance._stationaryPool.Add(newPlayer);
		return newPlayer;
	}

	private static AttachedAudioStreamPlayer3D GetAvailableAttachedPlayer()
	{
		foreach (var player in Instance._attachedPool)
		{
			if (!player.Playing)
			{
				return player;
			}
		}

		var newPlayer = new AttachedAudioStreamPlayer3D();
		Instance.AddChild(newPlayer);
		Instance._attachedPool.Add(newPlayer);
		return newPlayer;
	}
}