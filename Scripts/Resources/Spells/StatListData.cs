using Elythia;
using Godot;
using Godot.Collections;

namespace SpinalShatter;

[GlobalClass, Tool]
public partial class StatListData : Resource
{
	[Export] public Dictionary<StatType, FloatValue> StatRanges { get; private set; }

	public FloatValue this[StatType type]
	{
		get => StatRanges[type];
		set => StatRanges[type] = value;
	}
	public int Count => StatRanges.Count;
	public int size() => Count;

	public void Add(StatType type, FloatValue value)
	{
		StatRanges.Add(type, value);
	}
	public void Add(StatType type, float value)
	{
		StatRanges.Add(type, new FloatValue(value));
	}

}