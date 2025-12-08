using Godot;

namespace SpinalShatter;

[GlobalClass,Tool]
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

    private InventoryHUDItem item;
    public InventoryHUDItem Item => item;

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
        item ??= GetNode<InventoryHUDItem>("%Icon_InventoryHUDItem");
    }

    private void UpdateVisualData( )
    {
        if (Engine.IsEditorHint() && IsInsideTree()) GetComponents();


        if (item != null)
        {
            item.ChangeDisplayData(Data, Data.ShopRank);
        }

    }
}

