using Godot;
using Godot.Collections;

public partial class LevelRoom : Node3D
{
    [Signal] public delegate void PlayerEnteredEventHandler(LevelRoom room);
    [Signal] public delegate void PlayerExitedEventHandler(LevelRoom room);

    [Export] private Area3D _triggerVolume;
    [Export] private Array<Node3D> _otherRooms;

    public override void _Ready()
    {
        if (_triggerVolume != null)
        {
            _triggerVolume.BodyEntered += OnBodyEntered;
            _triggerVolume.BodyExited += OnBodyExited;
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerBody)
        {
            EmitSignal(SignalName.PlayerEntered, this);
            // ShowOtherRooms();
            ShowRoom();
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerBody)
        {
            EmitSignal(SignalName.PlayerExited, this);
            HideRoom();
            // HideOtherRooms();
        }
    }

    private void ShowOtherRooms()
    {
        if (!_otherRooms.IsNullOrEmpty())
        {
            foreach (Node3D room in _otherRooms)
            {
                room.Visible = true;
            }
        }
    }

    private void HideOtherRooms()
    {
        if (!_otherRooms.IsNullOrEmpty())
        {
            foreach (Node3D room in _otherRooms)
            {
                room.Visible = false;
            }
        }
    }

    public void ShowRoom()
    {
        this.Visible = true;
        // ShowOtherRooms();
    }

    public void HideRoom()
    {
        this.Visible = false;
        // HideOtherRooms();
    }
}
