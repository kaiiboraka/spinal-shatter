using Godot;

namespace SpinalShatter;

[GlobalClass, Tool]
public partial class PrimarySpellData : CastedSpellData
{
	[ExportGroup("Other Variants")]
	[Export] public CastedSpellData Secondary { get; private set; }
	[Export] public AutomaticSpellData Automatic { get; private set; }

}