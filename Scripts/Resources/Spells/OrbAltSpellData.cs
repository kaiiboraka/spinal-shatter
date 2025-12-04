using Elythia;

namespace SpinalShatter;

using Godot;

[GlobalClass]
public partial class OrbAltSpellData : SpellData
{
	[ExportGroup("Explosion Properties")]
	[Export] public FloatValueRange ExplosionRadius { get; private set; }
	[Export(PropertyHint.Range, "0.0, 100.0")] public float KnockbackForce { get; private set; } = 15.0f;
}
