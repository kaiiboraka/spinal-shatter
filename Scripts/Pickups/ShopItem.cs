using Godot;
using SpinalShatter.Resources;

namespace SpinalShatter;

public partial class ShopItem : Node3D
{
    [Export] public ShopItemData Data { get; set; }

    private RichTextLabel priceLabel;

    public override void _Ready()
    {
        if (Data == null)
        {
            GD.PrintErr("ShopItemData not set for ShopItem.");
            return;
        }

        var animatedSprite = GetNode<AnimatedSprite3D>("ItemSprite_AnimatedSprite3D");
        if (animatedSprite != null)
        {
            animatedSprite.SpriteFrames = Data.ItemIcon;
        }

        priceLabel = GetNode<RichTextLabel>("%Price_RichTextLabel");
        if (priceLabel != null)
        {
            priceLabel.Text = $"[center]${Data.Price}[/center]";
        }
    }
}

