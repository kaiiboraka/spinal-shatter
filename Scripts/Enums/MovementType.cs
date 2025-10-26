namespace Elythia;

public enum MovementType
{
	/// <summary>
	/// implies stationary / frozen in place like the TargetDummy.
	/// </summary>
	None,
	/// <summary>
	/// implies limbs. might be weak wings, could glide.
	/// can probably climb, but might be quadruped / no arms.
	/// </summary>
	Walk,
	/// <summary>
	/// implies no limbs
	/// </summary>
	Jump,
	/// <summary>
	/// implies wings. "NoClip" go anywhere
	/// </summary>
	Fly,
}