namespace SpinalShatter;

using Godot;

[GlobalClass, Tool]
public partial class AutomaticSpellData : SpellData
{
    [ExportGroup("Automatic Properties")]
    [Export(PropertyHint.Range, "0.1, 10.0, 0.1")]
    public float FireRate { get; private set; } = 1.0f; // Shots per second

    [Export(PropertyHint.Range, "0.0, 1000.0")]
    public float TargetingRange { get; private set; } = 30.0f; //

}
