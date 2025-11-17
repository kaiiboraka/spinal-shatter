using System.Collections.Generic;
using Godot;

namespace SpinalShatter;

[GlobalClass, Tool]
public partial class ManaParticleData : PickupData
{
    [Export] public SizeType SizeType { get; private set; }

	public static readonly Dictionary<SizeType, float> PickupPitch = new()
	{
		{ SizeType.Large, .6f },
		{ SizeType.Medium, .9f },
		{ SizeType.Small, 1.2f },
	};
}
