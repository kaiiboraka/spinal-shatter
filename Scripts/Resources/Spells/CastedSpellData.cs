namespace SpinalShatter;

using Godot;
using Elythia;

[GlobalClass, Tool]
public partial class CastedSpellData : SpellData
{
    [ExportGroup("Casted Properties")]
    [Export(PropertyHint.Range, "1,16,1")] public int ChargeIntervals { get; private set; } = 8;
    [Export] public IntValueRange ManaDroppedAmount { get; private set; }
    [Export] public FloatValueRange ManaCostRange { get; private set; }
    [Export] public FloatValueRange MaxChargeTime { get; private set; } = new(0, 1.75f);
    
    [ExportGroup("Audio")]
    [Export] public AudioData AudioData { get; private set; }
}
