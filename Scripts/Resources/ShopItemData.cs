using Godot;

namespace SpinalShatter.Resources
{
    [GlobalClass]
    public partial class ShopItemData : Resource
    {
        [Export]
        public SpriteFrames ItemIcon { get; set; }

        [Export]
        public int Price { get; set; }

        [Export(PropertyHint.Range, "0.0,1.0")]
        public double Rarity { get; set; }
    }
}
