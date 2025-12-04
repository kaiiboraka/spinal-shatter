using Godot;
using Elythia;

namespace SpinalShatter;

public enum PrimaryFireType
{
	ChargedProjectile,
	// Future types
	// ContinuousBeam,
	// MeleeArc
}

public enum AltFireType
{
	None,
	// Future types
	// Explode,
	// DefensiveShield,
	// ProjectileNova
}


[GlobalClass]
public partial class SpellData : Resource
{
	[Export] public string Name { get; private set; } = "New Spell";

	[Export(PropertyHint.MultilineText)]
	public string Description { get; private set; }

	[ExportGroup("Primary Fire")]
	[Export] public PrimaryFireType PrimaryFire { get; private set; } = PrimaryFireType.ChargedProjectile;
	[Export] public float MaxChargeTime { get; private set; } = 2.0f;
	[Export(PropertyHint.Range, "1,16,1")] public int ChargeIntervals { get; private set; } = 8;
	[Export] public FloatValueRange ManaCostRange { get; private set; }
	[Export] public FloatValueRange DamageRange { get; private set; }
	[Export] public FloatValueRange SpeedRange { get; private set; }
	[Export] public FloatValueRange SizeRange { get; private set; }
	[Export] public PackedScene ProjectileScene { get; private set; }
	[Export] public bool UsePlayerMomentum { get; private set; } = false;

	[ExportGroup("Alternate Fire")]
	[Export] public AltFireType AltFire { get; private set; } = AltFireType.None;
	[Export] public float AltFireManaCost { get; private set; } = 10f;

	[ExportGroup("Audio")]
	[Export] public AudioData AudioData { get; private set; }
}
