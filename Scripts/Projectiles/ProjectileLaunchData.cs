namespace SpinalShatter;

using Elythia;
using Godot;

public struct ProjectileLaunchData
{
	public Node Caster { get; set; }
	public float ManaCost { get; set; }
	public Vector3 InitialVelocity { get; set; }
	public Marker3D StartPosition { get; set; }
	public float ChargeRatio { get; set; }
	public SpellData SpellData { get; set; }
	public SlotType Slot { get; set; }
}