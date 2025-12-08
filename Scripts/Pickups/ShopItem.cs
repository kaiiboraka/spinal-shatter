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
            if (IsInsideTree()) // Ensure node is in the tree before deferring a call
            {
                CallDeferred(nameof(UpdateVisualData));
            }
        }
    }

    [Export] private InventoryHUDItem item;
    public InventoryHUDItem Item => item;

    public override void _EnterTree()
    {
        base._EnterTree();
        UpdateVisualData();
    }

    public override void _Ready()
    {
        UpdateVisualData();
    }

    private void UpdateVisualData()
    {
        if (Engine.IsEditorHint() && item == null)
        {
             // In editor, item might not be assigned yet. This is expected.
             return;
        }

        if (item != null)
        {
            if (Data != null)
            {
                item.ChangeDisplayData(Data, Data.ShopRank);
            }
            else
            {
                item.ChangeDisplayData(null, 0);
            }
        }
    }
}

