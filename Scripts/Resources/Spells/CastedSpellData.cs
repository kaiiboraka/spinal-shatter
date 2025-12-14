namespace SpinalShatter;

using Godot;
using Elythia;

[GlobalClass, Tool]
public partial class CastedSpellData : SpellData
{
    [ExportGroup("Casted Properties")]
    [Export(PropertyHint.Range, "1,16,1")] public int ChargeIntervals { get; private set; } = 8;
    [Export] public FloatValue VisualSizeOverride { get; private set; }
}