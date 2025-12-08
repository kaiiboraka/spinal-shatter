using System;
using Godot;
using System.Collections.Generic;
using System.Text;

namespace SpinalShatter;

[GlobalClass,Tool]
public partial class InventoryHUDItem : Control
{

	// This dictionary would be populated with actual Texture2D resources.
	// For now, it's a placeholder. The logic to use it is implemented.
	private static readonly Dictionary<SlotType, Texture2D> FrameTextures = new();

    private RichTextLabel priceLabel;
	private SlotType frameType;
	[Export] private SlotType FrameType
	{
		get => frameType;
		set
		{
			frameType = value;
			if (Engine.IsEditorHint())
			{
				GetComponents();
				UpdateFrameTexture();
			}
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

	[Export] private bool IsPlayerItem = false;

	public override void _EnterTree()
	{
		base._EnterTree();
		LoadFrames();

		GetComponents();
		UpdateFrameTexture();
	}

	public override void _Ready()
	{
		LoadFrames();

		GetComponents();
		UpdateFrameTexture();
	}

	private void GetComponents()
	{
		frameRect ??= GetNode<TextureRect>("FrameRect");
		iconRect ??= GetNode<TextureRect>("IconRect");
		rankText ??= GetNode<RichTextLabel>("%RankLabel");
        priceLabel ??= GetNode<RichTextLabel>("%Price_RichTextLabel");
	}

	public void ChangeDisplayData(ShopItemData itemData, int newRank)
	{
		// Ensure components are valid, especially in the editor
		if (iconRect == null || rankText == null || priceLabel == null)
		{
			GetComponents();
			if (iconRect == null || rankText == null || priceLabel == null)
			{
				GD.PrintErr("InventoryHUDItem: UI components not found.");
				return;
			}
		}

		if (itemData == null)
		{
			iconRect.Visible = false;
			rankText.Visible = false;
			priceLabel.Visible = false;
			return;
		}

		// Set visuals that are always shown for a valid item
		iconRect.Texture = itemData.ItemIcon;
		iconRect.Visible = true;
		
		Rank = newRank; // This updates the rankText's text
		rankText.Visible = (Rank > 0); // Only show rank if it's meaningful

		// Handle price visibility, defaulting to hidden
		priceLabel.Visible = false;
		if (IsPlayerItem)
		{
			return; // No price for player inventory items
		}
		
		// --- Logic for shop items ---
		int price = 0;
		if (itemData.ShopRank == 1) // Base item
		{
			price = itemData.Price;
		}
		else if (itemData.ShopRank > 1) // Rank-up item
		{
			int rankUpIndex = itemData.ShopRank - 2;
			if (itemData.RankUps != null && rankUpIndex >= 0 && rankUpIndex < itemData.RankUps.Count)
			{
				price = (int)itemData.RankUps[rankUpIndex].RankUpPrice;
			}
		}

		if (price > 0)
		{
			priceLabel.Text = $"[center]${price}[/center]";
			priceLabel.Visible = true;
		}
	}

	private void UpdateFrameTexture()
	{
		frameRect.Visible = frameType != SlotType.None;

		if (frameRect != null && FrameTextures.TryGetValue(frameType, out Texture2D texture))
		{
			frameRect.Texture = texture;
		}
	}

	private static void LoadFrames()
	{
		int count = Enum.GetValues(typeof(SlotType)).Length - 1; // ignore "NONE"
		for (int i = 0; i < count; i++)
		{
			FrameTextures[(SlotType)i] = GD.Load<AtlasTexture>($"res://assets/Images/UI/SlotFrame{i + 1}.tres");
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