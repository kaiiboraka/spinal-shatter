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
		if (itemData == null)
		{
			iconRect.Texture = null;
			rankText.Text = "";
			iconRect.Visible = false; // Hide icon
			rankText.Visible = false; // Hide rank
			if (priceLabel != null)
			{
				priceLabel.Visible = false;
			}
		}
		else
		{
			iconRect.Texture = itemData.ItemIcon;
			Rank = newRank; // This setter will update rankText.Text
			iconRect.Visible = true; // Show icon
			rankText.Visible = true; // Show rank

			if (priceLabel == null) return;
			if (!IsPlayerItem) // Only show price in shop context
			{
				if (itemData.ShopRank == 1) // Base item
				{
					priceLabel.Text = $"[center]${itemData.Price}[/center]";
					priceLabel.Visible = true;
				}
				else if (itemData.ShopRank > 1 && itemData.RankUps != null && (itemData.ShopRank - 2) >= 0 && (itemData.ShopRank - 2) < itemData.RankUps.Count) // Rank-up item
				{
					// itemData.ShopRank is the target rank (e.g., 2 for first rank-up).
					// RankUpData for first rank-up is at index 0. So index is ShopRank - 2.
					priceLabel.Text = $"[center]${itemData.RankUps[itemData.ShopRank - 2].RankUpPrice}[/center]";
					priceLabel.Visible = true;
				}
				else
				{
					priceLabel.Visible = false; // Hide if no valid price can be determined for shop item
				}
			}
			else // Not a shop item (player inventory)
			{
				priceLabel.Visible = false;
			}
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