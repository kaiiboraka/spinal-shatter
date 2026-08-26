using Godot;
using Godot.Collections;

namespace SpinalShatter;

[GlobalClass, Tool]
public partial class RankUpListData : Resource
{
	[Export] public Array<RankUpData> RankUps { get; private set; } = new();

	public int Count => RankUps.Count;
	public int size() => Count;

	public RankUpData this[int which]
	{
		get => RankUps[which];
		set => RankUps[which] = value;
	}

	public void Add(RankUpData rankUp)
	{
		RankUps.Add(rankUp);
	}

	public void Clear()
	{
		RankUps.Clear();
	}
	public void RemoveAt(int which)
	{
		RankUps.RemoveAt(which);
	}
	public void Resize(int newSize)
	{
		RankUps.Resize(newSize);
	}

}