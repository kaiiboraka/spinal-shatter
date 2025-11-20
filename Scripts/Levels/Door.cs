using Godot;
using System;
using System.Linq;
using Elythia; // Added for .Contains()

namespace SpinalShatter;

[Tool]
public partial class Door : CharacterBody3D
{
	[Signal] public delegate void DoorOpenEventHandler(Door door);
	[Signal] public delegate void PlayerDoorShutEventHandler();
	[Signal] public delegate void SystemDoorShutEventHandler();
	[Export] public CardinalDirection DoorDirection { get; set; }

	[ExportGroup("Components")]
	[Export] private AnimationPlayer animator;
	[Export] private Area3D entrance;
	[Export] private Area3D exit;

	private bool _isSystemClose = false;

	[ExportGroup("")]
	private bool isOpen = true;
	[Export] public bool IsOpen
	{
		get => isOpen;
		private set
		{
			bool was = isOpen;
			isOpen = value;
			if (!this.IsInsideTree()) return;
#if TOOLS
			if (Engine.IsEditorHint()) GetComponents();
#endif
			switch (was)
			{
				case false when isOpen:
					PlayerOpen();
					break;
				case true when !isOpen:
					PlayerClose();
					break;
			}
		}
	}

	[Export] public bool Locked { get; set; }

	private bool partOfHub;

	public override void _Ready()
	{
		GetComponents();
		if (!this.IsInGame()) return;

		partOfHub = IsInGroup("Hub");
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		TrySubscribe();
		IsOpen = isOpen;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		TryUnsubscribe();
	}

	private void GetComponents()
	{
		animator ??= GetNode<AnimationPlayer>("AnimationPlayer");
		entrance ??= GetNode<Area3D>("EntranceArea");
		exit ??= GetNode<Area3D>("ExitArea");
	}

	private void OnExitAreaBodyExited(Node3D body)
	{
		if (body is PlayerBody player && !entrance.GetOverlappingBodies().Contains(player))
		{
			// Allow closing if it's a level door, OR if it's a hub door during the post-round state.
			if (!partOfHub ||
				(partOfHub && !WaveDirector.Instance.IsRoundStarted && WaveDirector.Instance.IsRoundCompleted))
			{
				PlayerClose();
			}
		}
	}

	private void PlayerOpen()
	{
		if (isOpen) return;
		DebugManager.Debug($"Door '{Name}' PlayerOpen() called. Locked: {Locked}");
		if (Locked) return;
		animator.Play("OpenDown");
	}

	public void SystemOpen()
	{
		if (isOpen) return;
		DebugManager.Debug($"Door '{Name}' ForceOpen() called.");
		Locked = false;
		animator.Play("OpenDown");
	}

	private void PlayerClose()
	{
		if (!isOpen) return;
		DebugManager.Debug($"Door '{Name}' PlayerClose() called. Locked: {Locked}");
		if (Locked) return;
		_isSystemClose = false;
		animator.Play("CloseUp");
		if (this.IsInGame()) Locked = true;
	}

	public void SystemClose()
	{
		if (!isOpen) return;
		DebugManager.Debug($"Door '{Name}' ForceClose() called.");
		_isSystemClose = true;
		animator.Play("CloseUp");
		if (this.IsInGame()) Locked = true;
	}

	public void InstantOpen()
	{
		animator.Play("Opened");
		isOpen = true;
	}

	public void InstantClose()
	{
		animator.Play("Closed");
		isOpen =  false;
	}

	private void OnAnimationFinished(StringName animation)
	{
		if (animation == "OpenDown")
		{
			InstantOpen();
			EmitSignalDoorOpen(this);
		}
		else if (animation == "CloseUp")
		{
			InstantClose();
			EmitSignal(_isSystemClose ? SignalName.SystemDoorShut : SignalName.PlayerDoorShut);
		}
	}

	private void TrySubscribe()
	{
		if (Engine.IsEditorHint()) return;
		animator.SafeSubscribe<StringName>(AnimationPlayer.SignalName.AnimationFinished, OnAnimationFinished);
		exit?.SafeSubscribe<Node3D>(Area3D.SignalName.BodyExited, OnExitAreaBodyExited);
	}

	private void TryUnsubscribe()
	{
		if (Engine.IsEditorHint()) return;
		animator.SafeUnsubscribe<StringName>(AnimationPlayer.SignalName.AnimationFinished, OnAnimationFinished);
		exit?.SafeUnsubscribe<Node3D>(Area3D.SignalName.BodyExited, OnExitAreaBodyExited);
	}
}
