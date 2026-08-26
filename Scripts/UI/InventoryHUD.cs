using Godot;
using Godot.Collections;

namespace SpinalShatter;

[Tool]
public partial class InventoryHUD : HBoxContainer
{

    public Array<InventoryHUDItem> GetItemSlots()
    {
        var slots = new Array<InventoryHUDItem>();

        foreach (Node child in GetChildren())
        {
            if (child is InventoryHUDItem item)
            {
                slots.Add(item);
            }
        }
        return slots;
    }
}
