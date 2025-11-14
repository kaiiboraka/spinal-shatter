using Godot;
using SpinalShatter;

[GlobalClass]
public partial class EnemyData : Resource
{
    [Export] public string Name { get; private set; }
    [Export] public PackedScene Scene { get; private set; }
    [Export] public int BaseCost { get; private set; } = 1;
    [Export] public EnemyRank Rank { get; private set; } = EnemyRank.Rank1_Bone;
}
