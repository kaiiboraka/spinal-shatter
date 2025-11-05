using Godot;
using Godot.Collections;

public partial class LevelManager : Node
{
    [Export] private Array<LevelRoom> _rooms = new();

    private LevelRoom _currentRoom;
    private LevelRoom _previousRoom;

    public override void _Ready()
    {
        foreach (var room in _rooms)
        {
            room.PlayerEntered += OnPlayerEnteredRoomBoundary;
            room.PlayerExited += OnPlayerExitedRoomBoundary;
        }

        // Initially, hide all rooms except the first one (presumably the starting room)
        if (_rooms.Count > 0)
        {
            _currentRoom = _rooms[0];
            UpdateRoomStates();
        }
    }
    public void OnPlayerEnteredRoomBoundary(LevelRoom enteredRoom)
    {
        // Only update the previous room if we are coming from another room (not a hallway)
        if (_currentRoom != null)
        {
            _previousRoom = _currentRoom;
        }
        _currentRoom = enteredRoom;
        UpdateRoomStates();
    }

    public void OnPlayerExitedRoomBoundary(LevelRoom exitedRoom)
    {
        _previousRoom = exitedRoom;
        _currentRoom = null; // Player is in a hallway
        UpdateRoomStates();
    }

    private void UpdateRoomStates()
    {
        foreach (var room in _rooms)
        {
            // A room is active if it's the one the player is currently in,
            // or the one they just came from.
            bool shouldBeActive = (room == _currentRoom || room == _previousRoom);

            if (shouldBeActive)
            {
                room.Activate();
            }
            else
            {
                room.Deactivate();
            }
        }
    }
}
