using Elythia;
using Godot;
using Godot.Collections;

public partial class RoomManager : Node
{
    public static RoomManager Instance;

    public Array<LevelRoom> Rooms { get; set; } = new();

    private LevelRoom _currentRoom;
    private LevelRoom _previousRoom;

    public override void _Ready()
    {
        Instance = this;

        Callable.From(_LateReady).CallDeferred();
    }

    private async void _LateReady()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        // Initially, hide all rooms except the first one (presumably the starting room)
        UpdateRoomStates();
    }

    public void RegisterRoom(LevelRoom room)
    {
        Rooms.Add(room);
        room.PlayerEntered += OnPlayerEnteredRoomBoundary;
        room.PlayerExited += OnPlayerExitedRoomBoundary;
    }

    public void OnPlayerEnteredRoomBoundary(LevelRoom enteredRoom)
    {
        //DebugManager.Trace($"OnPlayerEnteredRoomBoundary: {enteredRoom.Name}");
        // Only update the previous room if we are coming from another room (not a hallway)
        if (_currentRoom != null)
        {
            _previousRoom = _currentRoom;
        }
        _currentRoom = enteredRoom;
        //DebugManager.Debug($"Current Room: {_currentRoom?.Name ?? "no current"}; Previous Room: {_previousRoom?.Name ?? "no previous"}");
        UpdateRoomStates();
    }

    public void OnPlayerExitedRoomBoundary(LevelRoom exitedRoom)
    {
        //DebugManager.Trace($"OnPlayerExitedRoomBoundary: {exitedRoom.Name}");
        _previousRoom = exitedRoom;
        // _currentRoom = null; // Player is in a hallway
        //DebugManager.Debug($"Current Room: {_currentRoom.Name}; Previous Room: {_previousRoom.Name}");
        UpdateRoomStates();
    }

    private void UpdateRoomStates()
    {
        foreach (var room in Rooms)
        {
            if (room == _currentRoom && !room.IsActive)
            {
                //DebugManager.Debug($"UpdateRoomStates.Activate: {room.Name}");
                room.Activate();
            }
            else //if (room.IsActive)
            {
                //DebugManager.Debug($"UpdateRoomStates.Deactivate: {room.Name}");
                room.Deactivate();
            }
        }
    }
}
