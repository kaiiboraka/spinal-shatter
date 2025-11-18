using System.Collections.Generic;
using Godot;

namespace SpinalShatter;

public partial class RoomManager : Node
{
    public static RoomManager Instance;

    public Godot.Collections.Dictionary<string,LevelRoom> Rooms { get; set; } = new();

    [Signal] public delegate void CurrentRoomChangedEventHandler(LevelRoom newRoom);
    public LevelRoom CurrentRoom { get; private set; }
    private LevelRoom _previousRoom;

    public override void _Ready()
    {
        if (Instance == null) Instance = this;
        else QueueFree();

        Callable.From(_LateReady).CallDeferred();
    }

    private async void _LateReady()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        // Initially, hide all rooms except the first one (presumably the starting room)
        UpdateRoomStates();
        if (CurrentRoom != null)
        {
            OnPlayerEnteredRoomBoundary(CurrentRoom);
        }
    }

    public void RegisterRoom(LevelRoom room)
    {
        if (Rooms.ContainsKey(room.Name) && Rooms[room.Name] is null)
        {
            Rooms.Remove(room.Name);
            Rooms.Add(room.Name, room);
        }
        room.SafeSubscribe<LevelRoom>(LevelRoom.SignalName.PlayerEntered, OnPlayerEnteredRoomBoundary);
        room.SafeSubscribe<LevelRoom>(LevelRoom.SignalName.PlayerExited, OnPlayerExitedRoomBoundary);
    }

    public void OnPlayerEnteredRoomBoundary(LevelRoom enteredRoom)
    {
        if (CurrentRoom == enteredRoom) return;
        
        //DebugManager.Trace($"OnPlayerEnteredRoomBoundary: {enteredRoom.Name}");
        // Only update the previous room if we are coming from another room (not a hallway)
        if (CurrentRoom != null)
        {
            _previousRoom = CurrentRoom;
        }
        CurrentRoom = enteredRoom;
        EmitSignalCurrentRoomChanged(CurrentRoom);
        //DebugManager.Debug($"Current Room: {CurrentRoom?.Name ?? "no current"}; Previous Room: {_previousRoom?.Name ?? "no previous"}");
        UpdateRoomStates();
    }

    public void OnPlayerExitedRoomBoundary(LevelRoom exitedRoom)
    {
        //DebugManager.Trace($"OnPlayerExitedRoomBoundary: {exitedRoom.Name}");
        _previousRoom = exitedRoom;
        // CurrentRoom = null; // Player is in a hallway
        //EmitSignal(SignalName.CurrentRoomChanged, null);
        //DebugManager.Debug($"Current Room: {CurrentRoom.Name}; Previous Room: {_previousRoom.Name}");
        UpdateRoomStates();
    }

    private void UpdateRoomStates()
    {
        foreach (var roomEntry in Rooms)
        {
            var room = roomEntry.Value;
            if (room == CurrentRoom && !room.IsActive)
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
