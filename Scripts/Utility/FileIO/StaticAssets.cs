using Godot;

namespace Elythia;

public partial class StaticAssets : Node 
{
    public static Font font_MonoMedium;

    public override void _Ready()
    {
        base._Ready();
        font_MonoMedium = new FontFile();
        font_MonoMedium.ResourcePath = "res://Assets/Fonts/JetBrainsMono-Medium.ttf";
    }
}