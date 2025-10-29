using Godot;

public partial class LevelRoom : Node3D
{
    [Export] private Area3D _triggerVolume;

    public override void _Ready()
    {
        if (_triggerVolume != null)
        {
            _triggerVolume.BodyEntered += OnBodyEntered;
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerBody)
        {
            GetParent().Call("OnPlayerEnteredRoom", this);
        }
    }

    public void ShowRoom()
    {
        this.Visible = true;
    }

    public void HideRoom()
    {
        this.Visible = false;
    }
}
