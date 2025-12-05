using Godot;
using System;

public partial class SignalBus : Node
{
	public static SignalBus Instance;


	[Signal] public delegate void GameResumedEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Instance == null) Instance = this;
		else QueueFree();

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
