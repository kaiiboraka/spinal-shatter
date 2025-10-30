using Godot;

[GlobalClass]
public partial class ManaParticleData : Resource
{
	[ExportGroup("Parameters")]
	[Export] public ManaSize Size { get; private set; }
	[Export] public int Value { get; private set; } = 1;
	[Export] public float DriftSpeed { get; private set; } = 0.5f;
	[Export] public float AttractSpeed { get; private set; } = 15.0f;
	[Export] public double Lifetime { get; private set; } = 5f;

	[ExportGroup("Visuals")]
	[Export] public Color Modulate { get; set; } = Colors.White;
	[Export] public Vector3 Scale { get; set; } = Vector3.One;
	[Export] public float CollisionShapeRadius { get; private set; } = 0.1f;
	[Export] public float AreaShapeRadius { get; private set; } = 0.1f;

	[ExportGroup("Audio")]
	[Export] public AudioStream AudioStream { get; private set; }
	[Export] public double AudioPitch { get; private set; } = 1.0f;
}