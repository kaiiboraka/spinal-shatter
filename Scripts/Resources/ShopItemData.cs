using Godot;

namespace SpinalShatter;

[GlobalClass, Tool]
public partial class ShopItemData : Resource
{
    [ExportGroup("Shop Properties")]
    [Export] public string ItemName { get; private set; }

    [Export(PropertyHint.MultilineText)]
    public string ItemDescription { get; private set; }

    [Export] public Texture2D ItemIcon { get; private set; }

    [Export] public int Price { get; private set; }

    [Export(PropertyHint.Range, "0.0,1.0")]
    public double Rarity { get; private set; }

    [ExportGroup("Ranking")]
    [Export] public Godot.Collections.Array<RankUpData> RankUps { get; private set; }
}