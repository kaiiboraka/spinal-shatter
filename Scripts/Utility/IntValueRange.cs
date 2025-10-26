using Godot;

namespace Elythia;

[GlobalClass, Tool]
public partial class IntValueRange : ValueRange<int>
{
	public IntValueRange()
	{
		_min = 0;
		_max = int.MaxValue;
	}

	public IntValueRange(int min)
	{
		_min = min;
		_max = min;
	}

	public IntValueRange(int min, int max)
	{
		_min = min;
		_max = max;
	}
	private int _min;
	[Export] public override int Min
	{
		get => _min;
		set
		{
			_min = Mathf.Clamp(value, AbsoluteMin, int.MaxValue);
			_max = Mathf.Clamp(_max, _min, AbsoluteMax);
		}
	}

	private int _max;
	[Export] public override int Max
	{
		get => _max;
		set
		{
			_max = Mathf.Clamp(value, AbsoluteMin, AbsoluteMax);
			_min = Mathf.Clamp(_min, AbsoluteMin, _max);
		}
	}

	[ExportCategory("Limits")]
	[Export] protected override int AbsoluteMin { get; set; } = 0;
	[Export] protected override int AbsoluteMax { get; set; } = 1000000;

	public override int GetRandomValue()
	{
		if (Min == Max) return Min;
		return (int)GD.RandRange(Min, Max);
	}

	public override string ToString()
	{
		return $"Min: {Min}, Max: {Max}";
	}
}