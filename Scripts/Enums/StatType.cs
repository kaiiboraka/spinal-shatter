namespace SpinalShatter;

public enum StatType
{
	// Player Stats
	Player_MaxHealth,
	Player_MaxMana,
	Player_MoveSpeed,
	Player_Armor,
	Player_PickupRadius,
	Player_JumpHeight,
	Player_AirJumps,
	Player_SiphonRange,
	Player_SiphonSpeed,
	Player_MoneyDropRate,

	// Weapon Stats
	Weapon_Damage,
	Weapon_Size,
	Weapon_Count,
	Weapon_Speed,
	Weapon_Time, // affects Casted charge time, and Automatic Cooldown time (delay between firing)
	Weapon_Duration, // affects

	// Meta Progression Stats
	Meta_RerollHallwayRewards,
	Meta_RerollShop,
	Meta_FreezeCharges,
	Meta_Banish,
	Meta_SellValueRatio,
}
