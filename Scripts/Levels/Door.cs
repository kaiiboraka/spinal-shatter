using Godot;
using System;
using System.Linq; // Added for .Contains()

namespace SpinalShatter;

[Tool]
public partial class Door : CharacterBody3D
{
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
					Open();
					break;
				case true when !isOpen:
					Close();
					break;
			}
		}
	}

	public bool Locked { get; set; }

	public override void _Ready()
	{
		GetComponents();
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		TrySubscribe();
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

	public void Open()
	{
		if (Locked) return;
		animator.Play("OpenDown");
	}

	public void Close()
	{
		if (Locked) return;
		animator.Play("CloseUp");
		Locked = true;
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
		}
	}

    private void OnExitAreaBodyExited(Node3D body)
    {
        if (body is PlayerBody player && !entrance.GetOverlappingBodies().Contains(player))
        {
            Close();
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
