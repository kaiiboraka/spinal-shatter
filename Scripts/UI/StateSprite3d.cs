using Godot;
using System;
using Godot.Collections;

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
	[Export] private SubViewport stateViewport;
	[Export] private RichTextLabel stateText;

	[Export] private SubViewport effectViewport;

	[Export] public int MaxVisibleStacks { get; set; } = 8;
	[Export] private Dictionary<StackingEffectType, HBoxContainer> effectPanels = new();

	public AIState CurrentState
	{
		get => currentState;
		set
		{
			currentState = value;
			if(stateText != null) stateText.Text = stateEmoji[currentState];
		}
	}

	public async override void _Ready()
	{
		// Node references are now set via [Export]
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		this.Texture = effectViewport.GetTexture();

		// Initialize the display for both editor and game
		UpdateEffect(StackingEffectType.Poison, 0);
		UpdateEffect(StackingEffectType.Slow, 0);
	}

	public void UpdateEffect(StackingEffectType effectType, int count)
	{
		// Guard against running before the dictionary is populated by the editor
		if (effectPanels == null || !effectPanels.ContainsKey(effectType)) return;
		
		UpdateEffectVisuals(effectType, count);
	}

	private void UpdateEffectVisuals(StackingEffectType effectType, int count)
	{
		HBoxContainer panel = effectPanels[effectType];
		panel.Visible = count > 0;

		// Clear existing children
		foreach (var child in panel.GetChildren())
		{
			child.QueueFree();
		}

		if (count == 0) return;

		// Calculate separation based on count
		int separation;
		if (count <= 1)
		{
			separation = 0;
		}
		else
		{
			// Linearly interpolate between (2, -10) and (8, -28)
			separation = (int)Mathf.Lerp(-10.0f, -28.0f, (count - 2) / (8.0f - 2.0f));
		}
		panel.AddThemeConstantOverride("separation", separation);

		// Add new icons
		StackingEffectData effectData = EffectsComponent.EffectData[effectType];
		Texture2D icon = effectData.EffectIcon;
		
		for (int i = 0; i < Mathf.Min(count, MaxVisibleStacks); i++)
		{
			TextureRect textureRect = new TextureRect
			{
				Texture = icon,
				ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
				CustomMinimumSize = new Vector2(24, 24)
			};
			panel.AddChild(textureRect);
		}
	}
}
