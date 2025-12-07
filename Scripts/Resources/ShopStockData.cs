using Godot;
using Godot.Collections;

namespace SpinalShatter;

[GlobalClass]
public partial class ShopStockData : Resource
{
    [Export] public Array<ShopItemData> AvailableItems { get; private set; } = new();
}
