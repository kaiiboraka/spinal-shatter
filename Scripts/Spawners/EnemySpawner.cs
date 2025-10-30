using Elythia;
using Godot;
using Godot.Collections;

public partial class EnemySpawner : Node3D
{
    [Export] private bool _isEnabled = true;
    [Export] private int _maxActiveEnemies = 10;
    [Export] private Array<PackedScene> _enemyScenes = new();
    [Export] private float _spawnInterval = 5.0f;
    [Export] private bool _spawnInRandomOrder = false;
    [Export] private bool _useGrabBag = false;

    private Timer _spawnTimer;
    private int _spawnIndex = 0;
    private int _activeEnemyCount = 0;
    private Array<PackedScene> _grabBag = new();
    private Dictionary<PackedScene, ObjectPoolManager<Node3D>> _pools = new();

    public override void _Ready()
    {
        // Create a single parent node for all enemies spawned by this spawner
        var unifiedPoolParent = new Node3D { Name = "PooledEnemies" };
        AddChild(unifiedPoolParent);

        // Create a pool for each unique scene
        foreach (var scene in _enemyScenes)
        {
            if (scene != null && !_pools.ContainsKey(scene))
            {
                var newPool = new ObjectPoolManager<Node3D>();
                newPool.Scene = scene;
                newPool.PoolParent = unifiedPoolParent;
                newPool.Name = $"{scene.ResourcePath.GetFile().GetBaseName()}Pool";
                _pools[scene] = newPool;
                AddChild(newPool);
            }
        }

        _spawnTimer = new Timer();
        _spawnTimer.WaitTime = _spawnInterval;
        _spawnTimer.Autostart = true;
        _spawnTimer.Timeout += OnSpawnTimerTimeout;
        AddChild(_spawnTimer);
    }

    private void OnSpawnTimerTimeout()
    {
        if (!_isEnabled || _activeEnemyCount >= _maxActiveEnemies || _enemyScenes == null || _enemyScenes.Count == 0)
        {
            return;
        }

        PackedScene sceneToSpawn;

        if (_useGrabBag && _spawnInRandomOrder)
        {
            if (_grabBag.Count == 0)
            {
                _grabBag = _enemyScenes.Duplicate();
                _grabBag.Shuffle();
            }
            sceneToSpawn = _grabBag.PopFront();
        }
        else if (_spawnInRandomOrder)
        {
            sceneToSpawn = _enemyScenes[(int)(GD.Randi() % _enemyScenes.Count)];
        }
        else
        {
            sceneToSpawn = _enemyScenes[_spawnIndex];
            _spawnIndex = (_spawnIndex + 1) % _enemyScenes.Count;
        }

        if (sceneToSpawn != null && _pools.ContainsKey(sceneToSpawn))
        {
            var pool = _pools[sceneToSpawn];
            var newEnemyNode = pool.Get();

            if (newEnemyNode is Enemy newEnemy)
            {
                newEnemy.OwningPool = pool;
                newEnemy.GlobalPosition = this.GlobalPosition;
                newEnemy.EnemyDied += OnEnemyDied;
                _activeEnemyCount++;
            }
            else
            {
                // Fallback for nodes that aren't enemies, just place them
                GetParent().AddChild(newEnemyNode);
                newEnemyNode.GlobalPosition = this.GlobalPosition;
            }
        }
    }

    private void OnEnemyDied(Enemy who)
    {
        _activeEnemyCount--;
        // It's good practice to disconnect signals from objects that might be reused
        who.EnemyDied -= OnEnemyDied;
    }
}
