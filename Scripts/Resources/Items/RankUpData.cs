namespace SpinalShatter;

using Godot;
using Godot.Collections;

[GlobalClass]
public partial class RankUpData : Resource
{
    // This dictionary holds the stat modifications for this rank.
    // The key is the StatType to modify, and the value is the new value to set.
    // Note: Godot doesn't export Dictionaries directly, so this would be populated in code.
    public Dictionary<StatType, float> StatModifiers { get; set; } = new();
}
