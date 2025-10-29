using Godot;

public partial class SiphonComponent : Node
{
    [Export] private Area3D _siphonField;
    [Export] private Node3D _target;

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
            ResetParticles();
            _siphonField.Monitoring = false;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_siphonPressed)
        {
            AttractParticles();
        }
    }

    private void AttractParticles()
    {
        foreach (var area in _siphonField.GetOverlappingAreas())
        {
            if (area.GetParent() is ManaParticle particle)
            {
                particle.Attract(_target);
            }
        }
    }

    private void ResetParticles()
    {
        foreach (var area in _siphonField.GetOverlappingAreas())
        {
            if (area.GetParent()  is ManaParticle particle)
            {
                particle.Reset();
            }
        }
    }

}
