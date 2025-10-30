using Elythia;
using Godot;
using Godot.Collections;

public partial class ManaParticleManager : Node
{
    public static ManaParticleManager Instance { get; private set; }

    [Export] private Dictionary<ManaSize, ManaParticleData> _particleData = new();
    [Export] private PackedScene _manaParticleScene; // The scene for all mana particles

    private ObjectPoolManager<ManaParticle> _pool;

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;

        _pool = new ObjectPoolManager<ManaParticle>();
        _pool.Scene = _manaParticleScene;
        _pool.PoolParent = this; // Assign this manager as the parent for pooled objects
        _pool.Name = "ManaParticlePool";
        AddChild(_pool);
    }

    public Array<ManaParticle> SpawnMana(int totalAmount, Vector3 position)
    {
        var spawnedParticles = new Array<ManaParticle>();
        int remaining = totalAmount;

        // The order of spawning (Large -> Medium -> Small) is important for the greedy algorithm
        // We rely on the enum order, but a more robust solution might sort the keys by value.

        // Spawn Large particles
        if (_particleData.ContainsKey(ManaSize.Large))
        {
            ManaParticleData data = _particleData[ManaSize.Large];
            int numToSpawn = remaining / data.Value;
            for (int i = 0; i < numToSpawn; i++)
            {
                spawnedParticles.Add(SpawnFromPool(data, position));
            }
            remaining %= data.Value;
        }

        // Spawn Medium particles
        if (_particleData.ContainsKey(ManaSize.Medium))
        {
            ManaParticleData data = _particleData[ManaSize.Medium];
            int numToSpawn = remaining / data.Value;
            for (int i = 0; i < numToSpawn; i++)
            {
                spawnedParticles.Add(SpawnFromPool(data, position));
            }
            remaining %= data.Value;
        }

        // Spawn Small particles
        if (_particleData.ContainsKey(ManaSize.Small))
        {
            ManaParticleData data = _particleData[ManaSize.Small];
            for (int i = 0; i < remaining; i++)
            {
                spawnedParticles.Add(SpawnFromPool(data, position));
            }
        }

        return spawnedParticles;
    }

    private ManaParticle SpawnFromPool(ManaParticleData data, Vector3 position)
    {
        ManaParticle particle = _pool.Get();
        if (particle == null) return null; // Pool is full

        // Add a random offset to the spawn position
        Vector3 offset = new Vector3((float)GD.RandRange(-0.5, 0.5), (float)GD.RandRange(0, 0.5), (float)GD.RandRange(-0.5, 0.5));
        particle.GlobalPosition = position + offset;
        particle.Initialize(data);
        return particle;
    }

    public void Release(ManaParticle particle)
    {
        if (particle == null) return;

        _pool.Release(particle);
    }
}
