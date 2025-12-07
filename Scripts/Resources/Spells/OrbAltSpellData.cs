using Elythia;

namespace SpinalShatter;

using Godot;

[GlobalClass, Tool]
public partial class OrbAltSpellData : CastedSpellData
{
	[ExportGroup("Explosion Properties")]
	[Export] public FloatValueRange ExplosionRadius { get; private set; }
	[Export(PropertyHint.Range, "0.0, 100.0")] public float KnockbackForce { get; private set; } = 15.0f;
}
