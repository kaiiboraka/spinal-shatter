using Elythia;
using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class EnemySpawner : Node3D
{
    private LevelRoom _owningRoom;
    private int _activeEnemyCount = 0;
    private static readonly Godot.Collections.Dictionary<PackedScene, ObjectPoolManager<Node3D>> _pools = new();

    public override void _Ready()
    {
        _owningRoom = GetParent<LevelRoom>();
        if (_owningRoom == null)
        {
            GD.PrintErr($"EnemySpawner '{Name}' is not a child of a LevelRoom. It will not function correctly.");
            SetProcess(false);
        }
    }

    /// <summary>
    /// Creates object pools for all the provided enemy scenes. This should be called by the WaveDirector
    /// before a round begins to ensure all needed enemies are ready.
    /// </summary>
    public void InitializePools(IEnumerable<PackedScene> enemyScenes)
    {
        foreach (var scene in enemyScenes)
        {
            if (scene == null || _pools.ContainsKey(scene)) continue;

            var newPool = new ObjectPoolManager<Node3D>
            {
                Scene = scene,
                Name = $"{scene.ResourcePath.GetFile().GetBaseName()}Pool"
            };

            var subParent = new Node3D { Name = $"{newPool.Name}_Container" };
            newPool.AddChild(subParent);
            newPool.PoolParent = subParent;
            
            _pools[scene] = newPool;
            GetTree().Root.AddChild(newPool);
        }
    }

    /// <summary>
    /// Spawns a single enemy instance using the provided scene.
    /// </summary>
    public void Spawn(PackedScene enemyScene)
    {
        if (enemyScene == null || !_pools.ContainsKey(enemyScene))
        {
            GD.PrintErr($"EnemySpawner: Scene is null or no pool exists for '{enemyScene?.ResourcePath}'.");
            return;
        }

        var pool = _pools[enemyScene];
        var newEnemyNode = pool.Get();

        if (newEnemyNode is Enemy newEnemy)
        {
            newEnemy.OwningPool = pool;
            var pos = GlobalPosition;
            newEnemy.GlobalPosition = pos + pos.RandomRange(1) + Vector3.Up;
            newEnemy.EnemyDied += OnEnemyDied;
            _owningRoom.RegisterEnemy(newEnemy);
            _activeEnemyCount++;
        }
        else
        {
            GD.PrintErr($"Failed to spawn node of type Enemy from scene '{enemyScene.ResourcePath}'. Node is type '{newEnemyNode.GetType().Name}'");
            // Fallback for nodes that aren't enemies, just place them
            GetParent().AddChild(newEnemyNode);
            newEnemyNode.GlobalPosition = this.GlobalPosition + Vector3.Up;
        }
    }

    private void OnEnemyDied(Enemy who)
    {
        _activeEnemyCount--;
        // It's good practice to disconnect signals from objects that might be reused
        who.EnemyDied -= OnEnemyDied;
    }
}

