using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace SpinalShatter;

[GlobalClass]
public partial class PlayerData : Resource
{
	[Export] public float MaxHealth { get; private set; } = 100f;

	[Export] public Dictionary<StatType, float> Stats { get; private set; } = new()
	{
		// Player Stats
		{ StatType.Player_MaxHealth, 1 },
		{ StatType.Player_MaxMana, 1 },
		{ StatType.Player_MoveSpeed, 1 },
		{ StatType.Player_Armor, 0 },
		{ StatType.Player_PickupRadius, 1 },
		{ StatType.Player_JumpHeight, 1 },
		{ StatType.Player_AirJumps, 0 },
		{ StatType.Player_SiphonRange, 1 },
		{ StatType.Player_SiphonSpeed, 1 },
		{ StatType.Player_MoneyDropRate, 0 },

		// Weapon Stats 0},
		{ StatType.Weapon_Damage, 0 },
		{ StatType.Weapon_Size, 0 },
		{ StatType.Weapon_Count, 0 },
		{ StatType.Weapon_Speed, 0 },
		{ StatType.Weapon_Time, 0 }, // affects Casted charge time, and Automatic Cooldown time (delay between firing)
		{ StatType.Weapon_Duration, 0 }, // affects lifetime of projectile before it dissipates automatically
		{ StatType.Weapon_Pierce, 0 },
		{ StatType.Weapon_Bounce, 0 }, // for both bounces off of walls and bounces off of enemies, depending on the context of the weapon at hand
		{ StatType.Weapon_Range, 0 }, // mostly for targeting falloff of automatic weapons
		{ StatType.Weapon_Cost, 0 },
		{ StatType.Weapon_Knockback, 0 },
		{ StatType.Weapon_Refund, 0 },
	};

	public float this[StatType type]
	{
		get => Stats[type];
		set => Stats[type] = value;
	}
}