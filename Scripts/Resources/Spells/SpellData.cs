namespace SpinalShatter;

using Godot;
using Elythia;

[GlobalClass, Tool]
public partial class SpellData : ShopItemData
{
	[ExportGroup("Weapon Properties")]
	[Export] public WeaponType Weapon { get; private set; }
	[Export] public SlotType Slot { get; private set; }
	[Export] public FloatValueRange DamageRange { get; private set; }
	[Export] public FloatValueRange SpeedRange { get; private set; }
	[Export] public FloatValueRange SizeRange { get; private set; }
	[Export] public PackedScene ProjectileScene { get; private set; }
	[Export] public bool UsePlayerMomentum { get; private set; } = false;
	
}
