using Elythia;
using Godot;
using Godot.Collections;

public partial class ManaParticleManager : Node
{
    public static ManaParticleManager Instance { get; private set; }

    [Export] private Dictionary<ManaSize, PackedScene> _particleScenes = new();
    [Export] private Dictionary<ManaSize, int> _manaValues = new();

    private Dictionary<ManaSize, ObjectPoolManager<ManaParticle>> _pools = new();

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;

        // Setup internal pools for each defined particle size
        foreach (var (size, scene) in _particleScenes)
        {
            var newPool = new ObjectPoolManager<ManaParticle>();
            newPool.Scene = scene;
            newPool.PoolParent = newPool; // Assign the shared parent
            newPool.Name = $"{size}ParticlePool";
            _pools[size] = newPool;
            AddChild(newPool);
        }
    }

    public Array<ManaParticle> SpawnMana(int totalAmount, Vector3 position)
    {
        var spawnedParticles = new Array<ManaParticle>();
        int remaining = totalAmount;

        // The order of spawning (Large -> Medium -> Small) is important for the greedy algorithm
        // We rely on the enum order, but a more robust solution might sort the keys by value.

        // Spawn Large particles
        if (_manaValues.ContainsKey(ManaSize.Large) && _pools.ContainsKey(ManaSize.Large))
        {
            int value = _manaValues[ManaSize.Large];
            int numToSpawn = remaining / value;
            for (int i = 0; i < numToSpawn; i++)
            {
                spawnedParticles.Add(SpawnFromPool(ManaSize.Large, position, value));
            }
            remaining %= value;
        }

        // Spawn Medium particles
        if (_manaValues.ContainsKey(ManaSize.Medium) && _pools.ContainsKey(ManaSize.Medium))
        {
            int value = _manaValues[ManaSize.Medium];
            int numToSpawn = remaining / value;
            for (int i = 0; i < numToSpawn; i++)
            {
                spawnedParticles.Add(SpawnFromPool(ManaSize.Medium, position, value));
            }
            remaining %= value;
        }

        // Spawn Small particles
        if (_manaValues.ContainsKey(ManaSize.Small) && _pools.ContainsKey(ManaSize.Small))
        {
            int value = _manaValues[ManaSize.Small];
            for (int i = 0; i < remaining; i++)
            {
                spawnedParticles.Add(SpawnFromPool(ManaSize.Small, position, value));
            }
        }

        return spawnedParticles;
    }

    private ManaParticle SpawnFromPool(ManaSize size, Vector3 position, int manaValue)
    {
        // var pool = ;
        ManaParticle particle = _pools[size].Get();
        if (particle == null) return null; // Pool is full

        // Add a random offset to the spawn position
        Vector3 offset = new Vector3((float)GD.RandRange(-0.5, 0.5), (float)GD.RandRange(0, 0.5), (float)GD.RandRange(-0.5, 0.5));
        particle.GlobalPosition = position + offset;
        particle.Initialize(manaValue);
        return particle;
    }

    public void Release(ManaParticle particle)
    {
        if (particle == null) return;

        if (_pools.ContainsKey(particle.Size))
        {
            _pools[particle.Size].Release(particle);
        }
        else
        {
            GD.PrintErr($"Attempted to release a particle of size {particle.Size} but no pool exists for it.");
            particle.QueueFree(); // Failsafe
        }
    }
}
