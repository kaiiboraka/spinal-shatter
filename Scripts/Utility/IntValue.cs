using Godot;

namespace Elythia;

[GlobalClass, Tool]
public partial class IntValue : ValueRange<int>
{
	public IntValue()
	{
		isRange = true;
		isFixed = false;
		min = 0;
		max = 1;
	}

	public IntValue(int minMax)
	{
		isFixed = true;
		isRange = false;

		fixedValue = minMax;
		min = minMax;
		max = minMax;
	}

	public IntValue(int min, int max)
	{
		isRange = true;
		isFixed = false;

		this.min = min;
		this.max = max;
	}


	[ExportGroup("Range Value")]
	private bool isRange = true;
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


	private int min = 0;
	[Export] public override int Min
	{
		get => min;
		set
		{
			min = Mathf.Clamp(value, AbsoluteMin, int.MaxValue);
			max = Mathf.Clamp(max, min, AbsoluteMax);
		}
	}

	private int max = 1;
	[Export] public override int Max
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

	private int fixedValue;
	[Export] public override int FixedValue
	{
		get => fixedValue;
		set => fixedValue = Mathf.Clamp(value, AbsoluteMin, AbsoluteMax);
	}

	protected override int AbsoluteMin { get; set; } = -1000000;
	protected override int AbsoluteMax { get; set; } = 1000000;

	public override int GetRandomValue()
	{
		if (isFixed) return FixedValue;
		if (Min == Max) return Min;
		return (int)GD.RandRange(Min, Max);
	}

	public override int GetLerpedValue(int weight)
	{
		if (isFixed) return FixedValue;
		if (Min == Max) return Min;
		return Mathf.Lerp(Min, Max, weight).RoundToInt();
	}


	public override string ToString()
	{
		return isFixed
			? $"Value: {FixedValue}"
			: $"Min: {Min}, Max: {Max}";
	}
}