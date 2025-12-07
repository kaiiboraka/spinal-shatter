using Godot;

namespace SpinalShatter;

public partial class ShopItem : Node3D
{
    [Export] public ShopItemData Data { get; set; }

    private RichTextLabel priceLabel;

    public override void _Ready()
    {
        if (Data == null)
        {
            GD.PushWarning("ShopItemData not set for ShopItem.");
            return;
        }

        var sprite = GetNode<Sprite3D>("ItemSprite_Sprite3D");
        if (sprite != null)
        {
            sprite.Texture = Data.ItemIcon;
        }

        priceLabel = GetNode<RichTextLabel>("%Price_RichTextLabel");
        if (priceLabel != null)
        {
            priceLabel.Text = $"[center]${Data.Price}[/center]";
        }
    }
}

