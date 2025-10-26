using System.Numerics;
using Godot;

namespace Elythia;

[Tool]
public abstract partial class ValueRange<T> : Resource where T : INumber<T>
{
	public abstract T Min { get; set; }
	public abstract T Max { get; set; }
	public abstract T GetRandomValue();
	protected abstract T AbsoluteMin { get; set; }
	protected abstract T AbsoluteMax { get; set; }
}