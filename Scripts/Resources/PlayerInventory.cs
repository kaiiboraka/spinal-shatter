using System;

namespace SpinalShatter;

using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PlayerInventory : Resource
{
	[Export] public int CurrentMoney { get; set; } = 0;

	[Export] public Dictionary<SlotType, EquippedItem> EquippedWeapons { get; private set; } = new();
	[Export] public Array<EquippedItem> EquippedStatItems { get; private set; } = new(new EquippedItem[3]);

	[Signal] public delegate void WeaponEquippedEventHandler(SlotType slot, EquippedItem weapon);
	[Signal] public delegate void StatItemEquippedEventHandler(int slot, EquippedItem statItem);
	[Signal] public delegate void InventoryChangedEventHandler();


	public void EquipOrRankUpItem(ShopItemData itemData, SlotType preferredSlot = SlotType.None)
	{
		switch (itemData)
		{
			case SpellData spellData:
				EquipOrRankUpWeapon(spellData, preferredSlot);
				break;
			case StatItemData statItemData:
				EquipOrRankUpStatItem(statItemData);
				break;
		}
	}

	private void EquipOrRankUpWeapon(SpellData spellData, SlotType preferredSlot)
	{
		// 1. Rank-up check: If we already own this weapon type, just rank it up.
		var (existingItem, _, _) = GetOwnedWeaponInfo(spellData);
		if (existingItem != null)
		{
			existingItem.RankUp();
			EmitSignal(SignalName.InventoryChanged);
			return;
		}

		// 2. New weapon logic with "bump and find empty"
		var newEquippedItem = new EquippedItem(spellData);

		// If the preferred slot is invalid for a weapon, abort.
		if (preferredSlot == SlotType.None || preferredSlot == SlotType.Stat)
		{
			GD.PrintErr($"Invalid weapon slot {preferredSlot} specified for equipping {spellData.ItemName}.");
			return;
		}

		// See if an item is currently in the preferred slot.
		EquippedWeapons.TryGetValue(preferredSlot, out EquippedItem bumpedItem);
		
		// Place the new item in the user's chosen slot. This overwrites the dictionary entry.
		EquippedWeapons[preferredSlot] = newEquippedItem;
		EmitSignal(SignalName.WeaponEquipped, (int)preferredSlot, newEquippedItem);

		// If an item was bumped, find a new home for it.
		if (bumpedItem != null)
		{
			// Define the slot order to check for an empty space.
			var slotOrder = new[] { SlotType.Primary, SlotType.Secondary, SlotType.Automatic };
			
			// Find the first empty slot and place the bumped item there.
			foreach (var slot in slotOrder)
			{
				// Find the first slot that ISN'T the one we just placed the new item in,
				// and is also not currently occupied by any other item.
				if (slot != preferredSlot && !EquippedWeapons.ContainsKey(slot))
				{
					EquippedWeapons[slot] = bumpedItem;
					EmitSignal(SignalName.WeaponEquipped, (int)slot, bumpedItem);
					break; // Stop after placing the bumped item
				}
			}
			// If no empty slot was found, the bumped item is unequipped from active slots.
		}

		EmitSignal(SignalName.InventoryChanged);
	}
	
	private void EquipOrRankUpStatItem(StatItemData statItemData)
	{
		// Check if the same stat item is already equipped
		var existingItem = EquippedStatItems.FirstOrDefault(i => i?.ItemData == statItemData);
		if (existingItem != null)
		{
			existingItem.RankUp();
		}
		else
		{
			// Find the first empty slot
			int emptySlot = -1;
			for (int i = 0; i < EquippedStatItems.Count; i++)
			{
				if (EquippedStatItems[i] == null)
				{
					emptySlot = i;
					break;
				}
			}

			if (emptySlot != -1)
			{
				var newEquippedItem = new EquippedItem(statItemData);
				EquippedStatItems[emptySlot] = newEquippedItem;
				EmitSignal(SignalName.StatItemEquipped, emptySlot, newEquippedItem);
			}
			else
			{
				GD.Print("No empty stat item slots available!");
				// Optionally handle replacing an item
			}
		}
		EmitSignal(SignalName.InventoryChanged);
	}

	public (EquippedItem equippedItem, int currentRank, bool isMaxRank) GetOwnedWeaponInfo(SpellData baseSpellData)
	{
		var existingWeapon = EquippedWeapons.Values.FirstOrDefault(w => (w?.ItemData as SpellData)?.Weapon == baseSpellData.Weapon);
		if (existingWeapon != null)
		{
			return (existingWeapon, existingWeapon.Rank, existingWeapon.IsMaxRank);
		}
		return (null, 0, false);
	}

	public (EquippedItem equippedItem, int currentRank, bool isMaxRank) GetOwnedStatItemInfo(StatItemData baseStatItemData)
	{
		var existingItem = EquippedStatItems.FirstOrDefault(i => (i?.ItemData as StatItemData)?.TargetStat == baseStatItemData.TargetStat);
		if (existingItem != null)
		{
			return (existingItem, existingItem.Rank, existingItem.IsMaxRank);
		}
		return (null, 0, false);
	}
}
