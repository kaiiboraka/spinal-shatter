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
		// Check if a weapon with the same weapon type is already equipped, regardless of slot
		var existingWeapon = EquippedWeapons.Values.FirstOrDefault(w => (w?.ItemData as SpellData)?.Weapon == spellData.Weapon);

		if (existingWeapon != null)
		{
			existingWeapon.RankUp();
			EmitSignal(SignalName.InventoryChanged);
			return;
		}

		SlotType targetSlot = spellData.Slot; // Default to the spell's inherent slot
		if (preferredSlot != SlotType.None && !EquippedWeapons.ContainsKey(preferredSlot))
		{
			targetSlot = preferredSlot; // Use preferred slot if provided and empty
		}
		else // If preferred slot is taken or not provided, try to find an empty slot.
		{
			bool foundEmpty = false;
			foreach(SlotType slot in Enum.GetValues(typeof(SlotType)))
			{
				if (slot == SlotType.Stat || EquippedWeapons.ContainsKey(slot)) continue;
				targetSlot = slot;
				foundEmpty = true;
				break;
			}
			if (!foundEmpty) // If all slots are full, it will overwrite the spellData.Slot
			{
				targetSlot = spellData.Slot;
			}
		}


		var newEquippedItem = new EquippedItem(spellData);
		EquippedWeapons[targetSlot] = newEquippedItem; // Use targetSlot
		EmitSignal(SignalName.WeaponEquipped, (int)targetSlot, newEquippedItem); // Emit signal with targetSlot
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
