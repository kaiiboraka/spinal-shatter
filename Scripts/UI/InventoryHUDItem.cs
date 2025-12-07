using System;
using Godot;
using System.Collections.Generic;
using System.Text;

namespace SpinalShatter;

[Tool]
public partial class InventoryHUDItem : Control
{
    public enum HUDFrameType
    {
        Primary,
        Secondary,
        Automatic,
        Stat
    }
    // This dictionary would be populated with actual Texture2D resources.
    // For now, it's a placeholder. The logic to use it is implemented.
    private static readonly Dictionary<HUDFrameType, Texture2D> FrameTextures = new();

    private HUDFrameType frameType;
    [Export] public HUDFrameType FrameType
    {
        get => frameType;
        set
        {
            frameType = value;
            UpdateFrameTexture();
        }
    }

    private int rank = 1;
    [Export(PropertyHint.Range, "1,10,1")] public int Rank
    {
        get => rank;
        set
        {
            rank = value;
            rankText.Text = IntToRoman(rank);
        }
    }

    private Texture2D icon;

    [Export] private Texture2D Icon
    {
        get => icon;
        set
        {
            icon = value;
            if (itemIcon != null) itemIcon.Texture = value;
        }
    }

    private TextureRect frame;
    private TextureRect itemIcon;
    private RichTextLabel rankText;

    public override void _EnterTree()
    {
        base._EnterTree();
        GetComponents();
        UpdateFrameTexture();
    }

    public override void _Ready()
    {
        GetComponents();
        UpdateFrameTexture();
    }

    private void GetComponents()
    {
        frame ??= GetNode<TextureRect>("FrameRect");
        itemIcon ??= GetNode<TextureRect>("IconRect");
        rankText ??= GetNode<RichTextLabel>("%RankLabel");
        LoadFrames();
    }

    public void ChangeDisplayData(ShopItemData itemData, int rank)
    {
        if (itemData == null)
        {
            itemIcon.Texture = null;
            rankText.Text = "";
            Visible = false;
        }
        else
        {
            itemIcon.Texture = itemData.ItemIcon?.GetFrameTexture("default", 0);
            Rank = rank;
            Visible = true;
        }
    }

    private void UpdateFrameTexture()
    {
        if (frame != null && FrameTextures.TryGetValue(frameType, out Texture2D texture))
        {
            frame.Texture = texture;
        }
    }

    private static void LoadFrames()
    {
        int count = Enum.GetValues(typeof(HUDFrameType)).Length;
        for (int i = 0; i < count; i++)
        {
            FrameTextures[(HUDFrameType)i] = GD.Load<AtlasTexture>($"res://assets/Images/UI/SlotFrame{i+1}.tres");
        }
    }

    private static readonly int[] romanValues = [10, 9, 5, 4, 1];
    private static readonly string[] romanCharacters = ["X", "IX", "V", "IV", "I"];
    private static string IntToRoman(int num)
    {
        if (num is < 1 or >= 100)
        {
            return num == 100 ? "C" : "";
        }
        var roman = new StringBuilder();

        for (int i = 0; i < romanValues.Length; i++)
        {
            // Greedily append the symbol while the number is greater than or equal to the value
            while (num >= romanValues[i])
            {
                num -= romanValues[i];
                roman.Append(romanCharacters[i]);
            }
        }
        return roman.ToString();
    }

}
