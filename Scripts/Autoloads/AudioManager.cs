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


	public void PlayBucketSimultaneous(AudioStreamPlayer3D player3D, AudioBucket bucket)
	{
		player3D.MaxPolyphony = bucket.Count;
		foreach (var audioStream in bucket.Bucket)
		{
			PlayAudio(player3D, audioStream);
		}
	}

	public void PlayBucketSimultaneous(AudioStreamPlayer player, AudioBucket bucket)
	{
		player.MaxPolyphony = bucket.Count;
		foreach (var audioStream in bucket.Bucket)
		{
			PlayAudio(player, audioStream);
		}
	}

	public void PlayBucketSequential(AudioStreamPlayer player, AudioBucket bucket)
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
				PlayAudio(player, bucket.Bucket[currentIndex]);
				currentIndex++;

				// Subscribe again if there are more sounds to play
				if (currentIndex < bucket.Count)
				{
					player.Finished += onFinished;
				}
			}
		};

		// Start playing the first sound
		PlayAudio(player, bucket.Bucket[0]);
		currentIndex = 1;

		// Subscribe to play the next sound if there are more
		if (currentIndex < bucket.Count)
		{
			player.Finished += onFinished;
		}
	}

	public void PlayBucketSequential(AudioStreamPlayer3D player3D, AudioBucket bucket)
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
				PlayAudio(player3D, bucket.Bucket[currentIndex]);
				currentIndex++;

				// Subscribe again if there are more sounds to play
				if (currentIndex < bucket.Count)
				{
					player3D.Finished += onFinished;
				}
			}
		};

		// Start playing the first sound
		PlayAudio(player3D, bucket.Bucket[0]);
		currentIndex = 1;

		// Subscribe to play the next sound if there are more
		if (currentIndex < bucket.Count)
		{
			player3D.Finished += onFinished;
		}
	}

	public void PlayAudio(AudioStreamPlayer3D player3D, AudioStream audioStream)
	{
		float baseVolume = player3D.VolumeDb;

		player3D.Stream = audioStream;
		if (audioStream is AudioFile custom)
		{
			player3D.PitchScale = custom.PitchScale;
			player3D.VolumeDb = baseVolume * custom.VolumeDbModifier;
			player3D.Stream = custom.Stream;
		}
		player3D.Play();
	}

	public void PlayAudio(AudioStreamPlayer player, AudioStream audioStream)
	{
		float baseVolume = player.VolumeDb;

		player.Stream = audioStream;
		if (audioStream is AudioFile custom)
		{
			player.PitchScale = custom.PitchScale;
			player.VolumeDb = baseVolume * custom.VolumeDbModifier;
			player.Stream = custom.Stream;
		}
		player.Play();
	}

	public AudioStreamPlayer3D PlaySoundAtPosition(AudioStream sound, Vector3 position, float pitch = 1.0f, float volume = 0.0f)
	{
		AudioStreamPlayer3D player = GetAvailableStationaryPlayer();
		player.Stream = sound;
		player.GlobalPosition = position;
		player.PitchScale = pitch;
		player.VolumeDb = volume;
		player.Play();

		player.Finished += () => { player.Stream = null; };

		return player;
	}

	public AttachedAudioStreamPlayer3D PlaySoundAttachedToNode(AudioStream sound, Node3D targetNode, float pitch = 1.0f, float volume = 0.0f)
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

	private AudioStreamPlayer3D GetAvailableStationaryPlayer()
	{
		foreach (var player in _stationaryPool)
		{
			if (!player.Playing)
			{
				return player;
			}
		}

		var newPlayer = new AudioStreamPlayer3D();
		AddChild(newPlayer);
		_stationaryPool.Add(newPlayer);
		return newPlayer;
	}

	private AttachedAudioStreamPlayer3D GetAvailableAttachedPlayer()
	{
		foreach (var player in _attachedPool)
		{
			if (!player.Playing)
			{
				return player;
			}
		}

		var newPlayer = new AttachedAudioStreamPlayer3D();
		AddChild(newPlayer);
		_attachedPool.Add(newPlayer);
		return newPlayer;
	}
}