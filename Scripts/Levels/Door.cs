using Godot;
using System;
using System.Linq;
using Elythia; // Added for .Contains()

namespace SpinalShatter;

[Tool]
public partial class Door : CharacterBody3D
{
	[Signal] public delegate void DoorShutEventHandler();
	[Export] public CardinalDirection DoorDirection { get; set; }

	[ExportGroup("Components")]
	[Export] private AnimationPlayer animator;
	[Export] private Area3D entrance;
	[Export] private Area3D exit;

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

	public override void _Ready()
	{
		GetComponents();
		if (!this.IsInGame()) return;
		if (IsInGroup("HubDoors"))
		{
			WaveDirector.Instance.RegisterHubDoor(this);
		}
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

	private void PlayerOpen()
	{
		DebugManager.Debug($"Door '{Name}' PlayerOpen() called. Locked: {Locked}");
		if (Locked) return;
		animator.Play("OpenDown");
	}

	private void PlayerClose()
	{
		DebugManager.Debug($"Door '{Name}' PlayerClose() called. Locked: {Locked}");
		if (Locked) return;
		animator.Play("CloseUp");
		if (this.IsInGame()) Locked = true;
	}

	public void ForceOpen()
	{
		DebugManager.Debug($"Door '{Name}' ForceOpen() called.");
		Locked = false;
		animator.Play("OpenDown");
	}

	public void ForceClose()
	{
		DebugManager.Debug($"Door '{Name}' ForceClose() called.");
		animator.Play("CloseUp");
		if (this.IsInGame()) Locked = true;
	}

	private void OnAnimationFinished(StringName animation)
	{
		if (animation == "OpenDown")
		{
			animator.Play("Open");
			isOpen = true;
		}
		else if (animation == "CloseUp")
		{
			animator.Play("Closed"); // Fixed typo
			isOpen = false;
			EmitSignal(SignalName.DoorShut);
		}
	}

	private void OnExitAreaBodyExited(Node3D body)
	{
		if (body is PlayerBody player && !IsInGroup("HubDoors") && !entrance.GetOverlappingBodies().Contains(player))
		{
			PlayerClose();
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
