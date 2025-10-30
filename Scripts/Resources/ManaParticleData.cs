using Godot;

[GlobalClass]
public partial class ManaParticleData : Resource
{
	[ExportGroup("Parameters")]
	[Export] public ManaSize Size { get; private set; }
	[Export] public int Value { get; private set; } = 1;
	[Export] public float DriftSpeed { get; set; } = 0.5f;
	[Export] public float AttractSpeed { get; set; } = 15.0f;
	[Export] public double Lifetime { get; set; } = 5f;

	[ExportGroup("Visuals")]
	[Export] public Color Modulate { get; set; } = Colors.White;
	[Export] public Vector3 Scale { get; set; } = Vector3.One;
	[Export] public float CollisionShapeRadius { get; set; } = 0.1f;
	[Export] public float AreaShapeRadius { get; set; } = 0.1f;

}