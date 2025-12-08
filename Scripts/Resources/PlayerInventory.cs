namespace SpinalShatter;

using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PlayerInventory : Resource
{
	[Export] public Dictionary<SlotType, EquippedItem> EquippedWeapons { get; private set; } = new();
	[Export] public Array<EquippedItem> EquippedStatItems { get; private set; } = new(new EquippedItem[3]);

	[Signal] public delegate void WeaponEquippedEventHandler(SlotType slot, EquippedItem weapon);
	[Signal] public delegate void StatItemEquippedEventHandler(int slot, EquippedItem statItem);
	[Signal] public delegate void InventoryChangedEventHandler();


	public void EquipOrRankUpItem(ShopItemData itemData)
	{
		switch (itemData)
		{
			case SpellData spellData:
				EquipOrRankUpWeapon(spellData);
				break;
			case StatItemData statItemData:
				EquipOrRankUpStatItem(statItemData);
				break;
		}
	}

	private void EquipOrRankUpWeapon(SpellData spellData)
	{
		// Check if a weapon with the same data is already equipped, regardless of slot
		var existingWeapon = EquippedWeapons.Values.FirstOrDefault(w => w?.ItemData == spellData);

		if (existingWeapon != null)
		{
			existingWeapon.RankUp();
		}
		else
		{
			var newEquippedItem = new EquippedItem(spellData);
			EquippedWeapons[spellData.Slot] = newEquippedItem;
			EmitSignal(SignalName.WeaponEquipped, (int)spellData.Slot, newEquippedItem);
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
}
