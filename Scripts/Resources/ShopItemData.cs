using Godot;
using Godot.Collections;

namespace SpinalShatter;

[GlobalClass]
public partial class ShopItemData : Resource
{
    [ExportGroup("Ranking")]
    [Export(PropertyHint.GroupEnable, "")] private bool hasRankData = true;
    [Export] public RankUpListData RankUps { get; private set; } = new();
    public int ShopRank { get; set; }

    [ExportGroup("Shop Properties")]
    [Export(PropertyHint.GroupEnable, "")] private bool hasShopData = true;
    [Export] public Texture2D ItemIcon { get; private set; }
    [Export] public string ItemName { get; private set; }
    [Export(PropertyHint.MultilineText)] public string ItemDescription { get; private set; }
    [Export] public int Price { get; private set; }
    [Export(PropertyHint.Range, "0.0,1.0")]
    public double Rarity { get; private set; }
}
