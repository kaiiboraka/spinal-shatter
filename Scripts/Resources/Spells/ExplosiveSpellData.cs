namespace SpinalShatter;

using Godot;

[GlobalClass]
public partial class ExplosiveSpellData : SpellData
{
	[ExportGroup("Explosion Properties")]
	[Export(PropertyHint.Range, "0.5, 20.0")] public float ExplosionRadius { get; private set; } = 3.0f;
	[Export(PropertyHint.Range, "0.0, 100.0")] public float KnockbackForce { get; private set; } = 15.0f;
}
