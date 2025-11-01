using Godot;
using System;
using Godot.Collections;

using static Enemy;

public partial class StateSprite3d : Sprite3D
{
	private Dictionary<AIState, string> stateEmoji = new()
	{
		{ AIState.Idle, "💤" },
		{ AIState.Patrolling, "👁️" },
		{ AIState.Chasing, "🏃‍♂️" },
		{ AIState.Attacking, "⚔️" },
		{ AIState.Recovery, "⌚" },
	};

	private AIState _currentState = AIState.Idle;
	private RichTextLabel _stateText;
	private SubViewport viewport;

	public AIState CurrentState
	{
		get => _currentState;
		set
		{
			_currentState = value;
			_stateText.Text = stateEmoji[_currentState];
		}
	}

	public async override void _Ready()
	{
		viewport = GetNode<SubViewport>("StateViewport");
		_stateText = viewport.GetNode<RichTextLabel>("%State_RichTextLabel");

		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		this.Texture = viewport.GetTexture();
	}


}
