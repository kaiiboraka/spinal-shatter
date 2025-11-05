using Godot;

public partial class AttachedAudioStreamPlayer3D : AudioStreamPlayer3D
{
    public Node3D TargetNode { get; set; }

    public override void _Process(double delta)
    {
        if (TargetNode != null && IsInstanceValid(TargetNode))
        {
            GlobalPosition = TargetNode.GlobalPosition;
        }
    }
}
