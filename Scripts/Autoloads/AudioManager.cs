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

    public void PlaySoundAtPosition(AudioStream sound, Vector3 position)
    {
        AudioStreamPlayer3D player = GetAvailableStationaryPlayer();
        player.Stream = sound;
        player.GlobalPosition = position;
        player.PitchScale = 1.0f;
        player.VolumeDb = 0.0f;
                player.Play();
        
                player.Finished += () => {
                    player.Stream = null;
                };
    }

    public void PlaySoundAttachedToNode(AudioStream sound, Node3D targetNode)
    {
        AttachedAudioStreamPlayer3D player = GetAvailableAttachedPlayer();
        player.Stream = sound;
        player.PitchScale = 1.0f;
        player.VolumeDb = 0.0f;
        player.TargetNode = targetNode;

        player.Play();

        player.Finished += () => {
            player.TargetNode = null;
        };
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
