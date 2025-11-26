using Godot;

[GlobalClass]
public partial class PlayerData : Resource
{
	[Export] public float MaxHealth { get; private set; } = 100f;
}
