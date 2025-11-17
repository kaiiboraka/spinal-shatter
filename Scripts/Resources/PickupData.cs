using Godot;

namespace SpinalShatter;

[GlobalClass, Tool]
public partial class PickupData : Resource
{
	[ExportGroup("Parameters")]
	[Export] public PickupType PickupType { get; set; }
	[Export] public int Value { get; private set; } = 1;
	[Export] public float DriftSpeed { get; private set; } = 0.5f;
	[Export] public float AttractSpeed { get; private set; } = 15.0f;
	[Export] public double Lifetime { get; private set; } = 5f;

	[ExportGroup("Visuals")]
	[Export] public Color Modulate { get; set; } = Colors.White;
	[Export] public Vector3 Scale { get; set; } = Vector3.One;
	[Export] public SpriteFrames SpriteFrames { get; private set; }
	[Export] public float CollisionShapeRadius { get; private set; } = 0.1f;
	[Export] public float AreaShapeRadius { get; private set; } = 0.1f;

	[ExportGroup("Audio", "Audio")]
	[Export] public AudioStream AudioStream { get; private set; }
	[Export] public double AudioPitch { get; private set; } = 1.0f;
	[Export] public float AudioVolumeDb { get; private set; } = 0;
}
