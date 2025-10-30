using Godot;

[GlobalClass]
public partial class ManaParticleData : Resource
{
    [ExportGroup("Parameters")]
    [Export] public ManaSize Size { get; private set; }
    [Export] private float _driftSpeed = 0.5f;
    [Export] private float _attractSpeed = 15.0f;
    [Export] private double _lifetime = 5f;

    [ExportGroup("Visuals")]
    [Export] public Color Modulate { get; set; } = Colors.White;
    [Export] public Vector3 Scale { get; set; } = Vector3.One;
    [Export] public float CollisionShapeRadius { get; set; } = 0.1f;
    [Export] public float AreaShapeRadius { get; set; } = 0.1f;
}
