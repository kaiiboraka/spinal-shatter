namespace SpinalShatter;

using Godot;
using Elythia;

[GlobalClass, Tool]
public partial class SpellData : ShopItemData
{
	[ExportGroup("Weapon Properties")]
	[Export] public WeaponType Weapon { get; private set; }
	[Export] public SlotType Slot { get; private set; }
	[Export] public StatListData StatRanges { get; private set; }

	public FloatValue DamageRange => StatRanges[StatType.Weapon_Damage];
	public FloatValue SpeedRange => StatRanges[StatType.Weapon_Speed];
	public FloatValue SizeRange => StatRanges[StatType.Weapon_Size];
	public FloatValue ManaDroppedAmount => StatRanges[StatType.Weapon_Refund];
	public FloatValue ExplosionRadius => StatRanges[StatType.Weapon_Size];
	public FloatValue ManaCostRange => StatRanges[StatType.Weapon_Cost];
	public float MaxChargeTime => StatRanges[StatType.Weapon_Time].FixedValue;
	public float FireRate => StatRanges[StatType.Weapon_Time].FixedValue;
	public float TargetingRange => StatRanges[StatType.Weapon_Range].FixedValue;
	public float KnockbackForce => StatRanges[StatType.Weapon_Knockback].FixedValue;

	[Export] public PackedScene ProjectileScene { get; private set; }
	[Export] public bool UsePlayerMomentum { get; private set; } = false;

	[ExportGroup("Audio")]
	[Export] public AudioData AudioData { get; private set; }
}