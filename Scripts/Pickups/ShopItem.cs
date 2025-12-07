using Godot;

namespace SpinalShatter;

[Tool]
public partial class ShopItem : Node3D
{
    private ShopItemData data;
    [Export] public ShopItemData Data
    {
        get => data;
        set
        {
            data = value;
            UpdateVisualData();
        }
    }

    private RichTextLabel priceLabel;
    private Sprite3D sprite;

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Engine.IsEditorHint() && IsInsideTree()) UpdateVisualData();
    }

    public override void _Ready()
    {
        if (Data == null)
        {
            GD.PushWarning("ShopItemData not set for ShopItem.");
            return;
        }
        GetComponents();
        UpdateVisualData();
    }

    private void GetComponents()
    {
        sprite ??= GetNode<Sprite3D>("ItemSprite_Sprite3D");
        priceLabel ??= GetNode<RichTextLabel>("%Price_RichTextLabel");
    }

    private void UpdateVisualData( )
    {
        if (Engine.IsEditorHint() && IsInsideTree()) GetComponents();


        if (sprite != null)
        {
            sprite.Texture = Data.ItemIcon;
        }

        if (priceLabel != null)
        {
            priceLabel.Text = $"[center]${Data.Price}[/center]";
        }
    }
}

