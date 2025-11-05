using Elythia;
using Godot;
using Godot.Collections;

public partial class EnemySpawner : Node3D
{
    [Export] private Array<PackedScene> _enemyScenes = new();
    [Export] public bool IsEnabled { get; set; } = true;
    [Export] private int _maxActiveEnemies = 10;
    [Export] private float _spawnInterval = 5.0f;
    [Export] private int _enemiesPerSpawn = 1;
    [Export] private bool _spawnInRandomOrder = false;
    [Export] private bool _useGrabBag = false;

    private LevelRoom _owningRoom;

    private Timer _spawnTimer;
    private int _spawnIndex = 0;
    private int _activeEnemyCount = 0;
    private Array<PackedScene> _grabBag = new();
    private static Dictionary<PackedScene, ObjectPoolManager<Node3D>> _pools = new();


    public override void _Ready()
    {
        _owningRoom = GetParent<LevelRoom>();
        if (_owningRoom == null)
        {
            GD.PrintErr($"EnemySpawner '{Name}' is not a child of a LevelRoom. It will not function correctly.");
            IsEnabled = false;
            return;
        }

        // Create a pool for each unique scene
        foreach (var scene in _enemyScenes)
        {
            if (scene == null || _pools.ContainsKey(scene)) continue;
            var newPool = new ObjectPoolManager<Node3D>();
            newPool.Scene = scene;
            Node3D subParent = new Node3D();
            newPool.CallDeferred(Node.MethodName.AddChild, subParent);
            newPool.PoolParent = subParent;
            subParent.Name = $"{scene.ResourcePath.GetFile().GetBaseName()}NodePool";
            newPool.Name = $"{scene.ResourcePath.GetFile().GetBaseName()}Pool";
            _pools[scene] = newPool;
            GetTree().Root.CallDeferred(Node.MethodName.AddChild, newPool);
        }

        _spawnTimer = new Timer();
        _spawnTimer.WaitTime = _spawnInterval;
        _spawnTimer.Autostart = true;
        _spawnTimer.Timeout += OnSpawnTimerTimeout;
        AddChild(_spawnTimer);
    }

    private void OnSpawnTimerTimeout()
    {
        if (!IsEnabled || _activeEnemyCount >= _maxActiveEnemies || _enemyScenes == null || _enemyScenes.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _enemiesPerSpawn; i++)
        {
            if (_activeEnemyCount >= _maxActiveEnemies) break;

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
                    var pos= GlobalPosition;
                    newEnemy.GlobalPosition = pos + pos.RandomRange(1) + Vector3.Up;
                    newEnemy.EnemyDied += OnEnemyDied;
                    _owningRoom.RegisterEnemy(newEnemy);
                    _activeEnemyCount++;
                }
                else
                {
                    // Fallback for nodes that aren't enemies, just place them
                    GetParent().AddChild(newEnemyNode);
                    newEnemyNode.GlobalPosition = this.GlobalPosition + Vector3.Up;
                }
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
