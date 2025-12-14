using Godot;

namespace Elythia;

[GlobalClass, Tool]
public partial class FloatValue : ValueRange<float>
{
	public FloatValue()
	{
		isRange = true;
		isFixed = false;
		min = 0;
		max = 1;
	}

	public FloatValue(float minMax)
	{
		isFixed = true;
		isRange = false;

		fixedValue = minMax;
		min = minMax;
		max = minMax;
	}

	public FloatValue(float min, float max)
	{
		isRange = true;
		isFixed = false;

		this.min = min;
		this.max = max;
	}

	[ExportGroup("Range Value")]
	private bool isRange = false;
	[Export(PropertyHint.GroupEnable, "")]
	public bool IsRange
	{
		get => isRange;
		set
		{
			isRange = value;
			isFixed = !value;
		}
	}

	private float min = 0;
	[Export] public override float Min
	{
		get => min;
		set
		{
			min = Mathf.Clamp(value, AbsoluteMin, float.MaxValue);
			max = Mathf.Clamp(max, min, AbsoluteMax);
		}
	}

	private float max = 1;
	[Export] public override float Max
	{
		get => max;
		set
		{
			max = Mathf.Clamp(value, AbsoluteMin, AbsoluteMax);
			min = Mathf.Clamp(min, AbsoluteMin, max);
		}
	}

	[ExportGroup("Fixed Value")]
	private bool isFixed = false;
	[Export(PropertyHint.GroupEnable, "")]
	public bool IsFixed
	{
		get => isFixed;
		set
		{
			isFixed = value;
			isRange = !value;
		}
	}

	private float fixedValue;

	[Export] public override float FixedValue
	{
		get => fixedValue;
		set => fixedValue = Mathf.Clamp(value, AbsoluteMin, AbsoluteMax);
	}

	protected override float AbsoluteMin { get; set; } = -1000000;
	protected override float AbsoluteMax { get; set; } = 1000000;

	public override float GetRandomValue()
	{
		if (IsFixed) return FixedValue;
		return Min.FloatEqualsApprox(Max)
			? Min
			: (float)GD.RandRange(Min, Max);
	}

	public override float GetLerpedValue(float weight)
	{
		if (IsFixed) return FixedValue;
		return Min.FloatEqualsApprox(Max)
			? Min
			: Mathf.Lerp(Min, Max, weight);
	}

	public override string ToString()
	{
		return IsFixed
			? $"Value: {FixedValue}"
			: $"Min: {Min}, Max: {Max}";
	}
}