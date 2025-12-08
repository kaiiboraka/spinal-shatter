namespace SpinalShatter;

using Godot;

[GlobalClass]
public partial class EquippedItem : Resource
{
    [Export] public ShopItemData ItemData { get; private set; }
    [Export] public int Rank { get; private set; } = 1;

    public bool IsMaxRank => Rank > ItemData.RankUps.Count;

    public void RankUp()
    {
        if (ItemData == null || Rank > ItemData.RankUps.Count) return;
        Rank++;
    }

    public EquippedItem() {}

    public EquippedItem(ShopItemData itemData)
    {
        ItemData = itemData;
        Rank = 1;
    }
}
