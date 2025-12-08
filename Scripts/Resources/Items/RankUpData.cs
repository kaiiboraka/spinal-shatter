namespace SpinalShatter;

using Godot;
using Godot.Collections;

[GlobalClass]
public partial class RankUpData : Resource
{
    [Export] public Dictionary<StatType, float> StatModifiers { get; private set; } = new();
    [Export] public float RankUpPrice { get; private set; }
}
