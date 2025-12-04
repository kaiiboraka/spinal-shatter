namespace SpinalShatter;

using Godot;
using Elythia;

[GlobalClass]
public partial class SpellData : Resource
{
	[Export] public string Name { get; private set; } = "New Spell";

	[Export(PropertyHint.MultilineText)]
	public string Description { get; private set; }

	[ExportGroup("Weapon Properties")]
	[Export] public WeaponType Weapon { get; private set; }
	[Export] public float MaxChargeTime { get; private set; } = 2.0f;
	[Export(PropertyHint.Range, "1,16,1")] public int ChargeIntervals { get; private set; } = 8;
	[Export] public FloatValueRange ManaCostRange { get; private set; }
	[Export] public IntValueRange ManaDroppedAmount { get; private set; }
	[Export] public FloatValueRange DamageRange { get; private set; }
	[Export] public FloatValueRange SpeedRange { get; private set; }
	[Export] public FloatValueRange SizeRange { get; private set; }
	[Export] public PackedScene ProjectileScene { get; private set; }
	[Export] public bool UsePlayerMomentum { get; private set; } = false;
	
	[ExportGroup("Alt-Fire Properties")]
	[Export] public float AltFireManaCost { get; private set; } = 10f;
	[Export] public PackedScene AltFireProjectileScene { get; private set; }

	[ExportGroup("Audio")]
	[Export] public AudioData AudioData { get; private set; }
}
