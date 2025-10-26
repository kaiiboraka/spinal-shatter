#if TOOLS
using Godot;

public partial class CustomNode_GroundSensor : EditorPlugin
{
    public override void _EnterTree()
    {
        // Initialization of the plugin goes here.
        // Add the new type with a name, a parent type, a script and an icon.
        var script = GD.Load<Script>("res://Scripts/StateMachine/GroundSensor.cs");
        // var texture = GD.Load<Texture>("");
        AddCustomType("MyButton", "Button", script, null);
    }

    public override void _ExitTree()
    {
        // Clean-up of the plugin goes here.
        // Always remember to remove it from the engine when deactivated.
        RemoveCustomType("MyButton");
    }
}
#endif