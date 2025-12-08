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
            _dataDirty = true; // Mark data as dirty
        }
    }

    [Export] private InventoryHUDItem item;
    public InventoryHUDItem Item => item;

    private bool _dataDirty = false;

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
        // GetComponents();
        UpdateVisualData();
    }

    // private void GetComponents()
    // {
    //     // item is assigned via node_paths, no need to GetNode here.
    // }

    private void UpdateVisualData( )
    {
        // if (Engine.IsEditorHint() && IsInsideTree()) GetComponents();


        if (item != null)
        {
            item.ChangeDisplayData(Data, Data.ShopRank);
        }

    }
}

