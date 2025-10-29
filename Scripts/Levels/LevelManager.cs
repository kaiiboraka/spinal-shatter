using Godot;
using Godot.Collections;

public partial class LevelManager : Node
{
    [Export] private Array<NodePath> _roomPaths = new();

    private Array<LevelRoom> _rooms = new();

    public override void _Ready()
    {
        foreach (var path in _roomPaths)
        {
            var room = GetNode<LevelRoom>(path);
            if (room != null)
            {
                _rooms.Add(room);
                room.PlayerEntered += OnPlayerEnteredRoom;
            }
        }

        // Initially, hide all rooms except the first one (presumably the starting room)
        if (_rooms.Count > 0)
        {
            OnPlayerEnteredRoom(_rooms[0]);
        }
    }

    public void OnPlayerEnteredRoom(LevelRoom enteredRoom)
    {
        foreach (var room in _rooms)
        {
            if (room == enteredRoom)
            {
                room.ShowRoom();
            }
            else
            {
                room.HideRoom();
            }
        }
    }
}
