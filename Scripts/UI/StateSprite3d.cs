using Godot;
using System;
using Godot.Collections;
using SpinalShatter.Scripts.Enums;

namespace SpinalShatter;

public partial class StateSprite3d : Sprite3D
{
	private Dictionary<AIState, string> stateEmoji = new()
	{
		{ AIState.Idle, "💤" },
		{ AIState.Patrolling, "👁️" },
		{ AIState.Chasing, "🏃‍♂️" },
		{ AIState.Attacking, "⚔️" },
		{ AIState.Recovery, "⌚" },
		{ AIState.Dying , "☠️" }
	};

	private AIState currentState = AIState.Idle;
	private SubViewport stateViewport;
	private RichTextLabel stateText;

	private SubViewport effectViewport;
	private PanelContainer panelContainer;

	[Export] public int MaxVisibleStacks { get; set; } = 8;
	private HBoxContainer effectsContainer;
	private Dictionary<StackingEffectType, PanelContainer> effectPanels = new();

	public AIState CurrentState
	{
		get => currentState;
		set
		{
			currentState = value;
			stateText.Text = stateEmoji[currentState];
		}
	}

	private Dictionary<StackingEffectType, int> ActiveEffectCounts = new()
	{
		{ StackingEffectType.Poison, 0 },
		{ StackingEffectType.Slow, 0 },
	};

	private static Dictionary<StackingEffectType, Texture2D> Icons = new()
	{
		{ StackingEffectType.Poison, GD.Load<Texture2D>("res://Assets/Images/UI/PoisonIcon.png") },
		{ StackingEffectType.Slow, GD.Load<Texture2D>("res://Assets/Images/UI/SlowIcon.png") },
	};

	public async override void _Ready()
	{
		stateViewport = GetNode<SubViewport>("StateViewport");
		stateText = stateViewport.GetNode<RichTextLabel>("MarginContainer/State_RichTextLabel");

		effectViewport = GetNode<SubViewport>("EffectViewport");
		effectsContainer = effectViewport.GetNode<HBoxContainer>("EffectsContainer");
		effectPanels[StackingEffectType.Poison] = effectsContainer.GetNode<PanelContainer>("PoisonEffectsPanel");
		effectPanels[StackingEffectType.Slow] = effectsContainer.GetNode<PanelContainer>("SlowEffectsPanel");

		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		this.Texture = effectViewport.GetTexture();

		// Initialize the display
		UpdateEffectVisuals(StackingEffectType.Poison);
		UpdateEffectVisuals(StackingEffectType.Slow);
	}

	public bool AddStackingEffect(StackingEffectType effectType, int stacks = 1)
	{
		if (stacks <= 0) return false;
		ActiveEffectCounts[effectType] += stacks;
		return true;
	}

	public bool RemoveStackingEffect(StackingEffectType effectType, int stacks = 1)
	{
		if (stacks <= 0) return false;
		if (ActiveEffectCounts[effectType] <= 0) return false;
		ActiveEffectCounts[effectType] = (ActiveEffectCounts[effectType] - stacks).AtLeastZero();
		UpdateEffectVisuals(effectType);
		return true;
	}

	private void UpdateEffectVisuals(StackingEffectType effectType)
	{
		int count = ActiveEffectCounts[effectType];
		PanelContainer panel = effectPanels[effectType];

		panel.Visible = count > 0;
		if (count == 0)
		{
			// Clear all children if no stacks
			foreach (var child in panel.GetChildren())
			{
				child.QueueFree();
			}
			return;
		}

		// Clear existing children
		foreach (var child in panel.GetChildren())
		{
			child.QueueFree();
		}

		Texture2D icon = Icons[effectType];
		const int ICON_WIDTH = 24; // Assuming icon width for positioning
		const float SPACING_REDUCTION_FACTOR = 0.5f; // How much spacing reduces per stack

		for (int i = 0; i < Mathf.Min(count, MaxVisibleStacks); i++)
		{
			TextureRect textureRect = new TextureRect();
			textureRect.Texture = icon;
			textureRect.ExpandMode = TextureRect.ExpandModeEnum.FitWidth; // or .KeepAspect
			textureRect.CustomMinimumSize = new Vector2(ICON_WIDTH, ICON_WIDTH); // or icon.GetSize() if available

			// Calculate position with decreasing spacing
			float xPos = i * (ICON_WIDTH - (i * SPACING_REDUCTION_FACTOR));
			textureRect.Position = new Vector2(xPos, 0);

			panel.AddChild(textureRect);
		}

		// Adjust the HBoxContainer's size to fit the panels if needed
		// This might need more sophisticated layout management depending on desired behavior.
		// For now, let's just make sure the panel itself is wide enough.
		// panel.CustomMinimumSize = new Vector2(
		// 	Mathf.Min(count, MaxVisibleStacks) * ICON_WIDTH - Mathf.Max(0, Mathf.Min(count, MaxVisibleStacks) - 1) * (SPACING_REDUCTION_FACTOR * (Mathf.Min(count, MaxVisibleStacks) - 1)),
		// 	ICON_WIDTH
		// );
	}


}
