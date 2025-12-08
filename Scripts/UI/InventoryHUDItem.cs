using System;
using Godot;
using System.Collections.Generic;
using System.Text;

namespace SpinalShatter;

[GlobalClass,Tool]
public partial class InventoryHUDItem : Control
{
	public enum HUDFrameType
	{
		Primary,
		Secondary,
		Automatic,
		Stat,
		None
	}

	// This dictionary would be populated with actual Texture2D resources.
	// For now, it's a placeholder. The logic to use it is implemented.
	private static readonly Dictionary<HUDFrameType, Texture2D> FrameTextures = new();

    private RichTextLabel priceLabel;
	private HUDFrameType frameType;
	[Export] public HUDFrameType FrameType
	{
		get => frameType;
		set
		{
			frameType = value;
#if TOOLS
			if (Engine.IsEditorHint()) GetComponents();
#endif
			UpdateFrameTexture();
		}
	}

	private int rank = 1;
	[Export(PropertyHint.Range, "0,10,1")] public int Rank
	{
		get => rank;
		set
		{
			rank = value;
			if (this.IsInGame())
			{
				rankText.Text = IntToRoman(rank);
			}
			else
			{
				if (rankText == null) GetComponents();
				if (rankText != null) rankText.Text = IntToRoman(rank);
			}
		}
	}

	private Texture2D icon;
	[Export] private Texture2D Icon
	{
		get => icon;
		set
		{
			icon = value;
			if (iconRect != null) iconRect.Texture = value;
		}
	}

	private TextureRect frameRect;
	private TextureRect iconRect;
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
		frameRect ??= GetNode<TextureRect>("FrameRect");
		iconRect ??= GetNode<TextureRect>("IconRect");
		rankText ??= GetNode<RichTextLabel>("%RankLabel");
        priceLabel ??= GetNode<RichTextLabel>("%Price_RichTextLabel");
		LoadFrames();
	}

	public void ChangeDisplayData(ShopItemData itemData, int newRank)
	{
		if (itemData == null)
		{
			iconRect.Texture = null;
			rankText.Text = "";
			Visible = false;
			if (priceLabel != null)
			{
				priceLabel.Visible = false;
			}
		}
		else
		{
			iconRect.Texture = itemData.ItemIcon;//?.GetFrameTexture("default", 0);
			Rank = newRank;
			Visible = true;
			if (priceLabel != null)
			{
				priceLabel.Visible = true;
				priceLabel.Text = $"[center]${itemData.RankUps[Rank].RankUpPrice}[/center]";
			}
		}
	}

	private void UpdateFrameTexture()
	{
		frameRect.Visible = frameType != HUDFrameType.None;

		if (frameRect != null && FrameTextures.TryGetValue(frameType, out Texture2D texture))
		{
			frameRect.Texture = texture;
		}
	}

	private static void LoadFrames()
	{
		int count = Enum.GetValues(typeof(HUDFrameType)).Length - 1; // ignore "NONE"
		for (int i = 0; i < count; i++)
		{
			FrameTextures[(HUDFrameType)i] = GD.Load<AtlasTexture>($"res://assets/Images/UI/SlotFrame{i + 1}.tres");
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