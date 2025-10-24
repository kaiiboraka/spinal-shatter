using Godot;
using System;
using Godot.Collections;

public partial class Reticle : CenterContainer
{
	[Export] private Array<Line2D> reticleLines;
	[ExportCategory("Visuals")]
	[Export] public float dotRadius = 1f;
	[Export] public float lineWidth = 2f;
	[Export] public Color reticle = Colors.White;
	
	[ExportCategory("Behavior")]
	[Export] public float lineLerpRate = .25f;
	[Export] public float reticleDistance = 2.0f;

	public float speed = 0f;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		QueueRedraw();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	public void moveLines()
	{
		reticleLines[0].Position = reticleLines[0].Position.Lerp(new Vector2(0, -speed * reticleDistance), lineLerpRate); // Top
		reticleLines[1].Position = reticleLines[1].Position.Lerp(new Vector2( speed * reticleDistance,0), lineLerpRate); // Right
		reticleLines[2].Position = reticleLines[2].Position.Lerp(new Vector2(0, speed * reticleDistance), lineLerpRate); // Bottom
		reticleLines[3].Position = reticleLines[3].Position.Lerp(new Vector2( -speed * reticleDistance,0), lineLerpRate); // Left
	}
	
	public override void _Draw()
	{
		base._Draw();
		DrawCircle(Vector2.Zero, dotRadius, reticle);
	}

	private void UpdateSpeed(float newSpeed)
	{
		speed = newSpeed;
	}
}
