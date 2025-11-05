using Godot;
using System.Collections.Generic;

public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }

    private List<AudioStreamPlayer3D> _pool = new List<AudioStreamPlayer3D>();
    private int _poolSize = 10;

    public override void _Ready()
    {
        Instance = this;
        for (int i = 0; i < _poolSize; i++)
        {
            var player = new AudioStreamPlayer3D();
            AddChild(player);
            _pool.Add(player);
        }
    }

    public void PlaySoundAtPosition(AudioStream sound, Vector3 position, float pitch = 1.0f, float volumeDb = 0.0f)
    {
        AudioStreamPlayer3D player = GetAvailablePlayer();
        player.Stream = sound;
        player.GlobalPosition = position;
        player.PitchScale = pitch;
        player.VolumeDb = volumeDb;
                player.Play();
        
                player.Finished += () => {
                    player.Stream = null;
                };
    }

    public void PlaySoundAttachedToNode(AudioStream sound, Node3D targetNode, float pitch = 1.0f, float volumeDb = 0.0f)
    {
        AudioStreamPlayer3D player = GetAvailablePlayer();
        player.Stream = sound;
        player.PitchScale = pitch;
        player.VolumeDb = volumeDb;

        Node originalParent = player.GetParent();
        originalParent.RemoveChild(player);
        targetNode.AddChild(player);
        player.Position = Vector3.Zero;

        player.Play();

        player.Finished += () => {
            if (GodotObject.IsInstanceValid(player))
            {
                player.Stream = null;
                if (player.GetParent() != null)
                {
                    player.GetParent().RemoveChild(player);
                }
                originalParent.AddChild(player);
            }
        };
    }

    private AudioStreamPlayer3D GetAvailablePlayer()
    {
        foreach (var player in _pool)
        {
            if (!player.Playing)
            {
                return player;
            }
        }

        // If no player is available, create a new one and add it to the pool.
        var newPlayer = new AudioStreamPlayer3D();
        AddChild(newPlayer);
        _pool.Add(newPlayer);
        return newPlayer;
    }
}
