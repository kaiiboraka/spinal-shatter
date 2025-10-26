using Godot;

namespace Elythia;

[GlobalClass, Tool]
public partial class FloatValueRange : ValueRange<float>
{
	public FloatValueRange()
	{
		_min = 0;
		_max = 1;
	}

	public FloatValueRange(float min)
	{
		_min = min;
		_max = min;
	}

	public FloatValueRange(float min, float max)
	{
		_min = min;
		_max = max;
	}

	private float _min;
	[Export] public override float Min
	{
		get => _min;
		set
		{
			_min = Mathf.Clamp(value, AbsoluteMin, float.MaxValue);
			_max = Mathf.Clamp(_max, _min, AbsoluteMax);
		}
	}

	private float _max;
	[Export] public override float Max
	{
		get => _max;
		set
		{
			_max = Mathf.Clamp(value, AbsoluteMin, AbsoluteMax);
			_min = Mathf.Clamp(_min, AbsoluteMin, _max);
		}
	}

	[ExportCategory("Limits")]
	[Export] protected override float AbsoluteMin { get; set; } = 0;
	[Export] protected override float AbsoluteMax { get; set; } = 1000000;

	public override float GetRandomValue()
	{
		if (Min.FloatEqualsApprox(Max)) return Min;
		return (float)GD.RandRange(Min, Max);
	}


	public override string ToString()
	{
		return $"Min: {Min}, Max: {Max}";
	}
}