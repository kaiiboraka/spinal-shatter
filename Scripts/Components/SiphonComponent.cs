using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class SiphonComponent : Node
{
    [Export] private Area3D _siphonField;
    [Export] private Node3D _target;

    private HashSet<ManaParticle> _attractedParticles = new();

    private bool _siphonPressed;
    public override void _Ready()
    {
        base._Ready();
        _siphonField.Visible = false;
        _siphonField.Monitoring = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("Player_Siphon"))
        {
            _siphonField.Monitoring = true;
            _siphonField.Visible = true;
            _siphonPressed = true;
            AttractParticles();
        }

        if (@event.IsActionReleased("Player_Siphon"))
        {
            _siphonPressed = false;
            _siphonField.Visible = false;
            ReleaseAllParticles();
            _siphonField.Monitoring = false;
        }
    }

    public override void _Process(double delta)
    {
        if (_siphonPressed)
        {
            AttractParticles();
        }
        else
        {
            ReleaseAllParticles();
        }
    }

    private void AttractParticles()
    {
        if (_siphonField == null) return;

        foreach (var area in _siphonField.GetOverlappingAreas())
        {
            if (area.GetOwner() is ManaParticle particle)
            {
                // Attract the particle if it's not already being attracted
                if (_attractedParticles.Add(particle))
                {
                    particle.Attract(_target);
                }
            }
        }
    }

    private void ReleaseAllParticles()
    {
        if (_attractedParticles.Count == 0) return;

        foreach (var particle in _attractedParticles)
        {
            if (IsInstanceValid(particle))
            {
                particle.DriftIdle();
            }
        }
        _attractedParticles.Clear();
    }
}
