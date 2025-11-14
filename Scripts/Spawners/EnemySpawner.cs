using Elythia;
using Godot;
using Godot.Collections;

public partial class EnemySpawner : Node3D
{
    [Export] private Array<PackedScene> _enemyScenes = new();
    [Export] public bool IsEnabled { get; set; } = true;

    [ExportGroup("Spawning Logic")]
    [Export] private int _maxActiveEnemies = 10;
    [Export] private float _spawnInterval = 5.0f;
    [Export] private int _enemiesPerSpawn = 1;
    [Export] private bool _spawnInRandomOrder = false;
    [Export] private bool _useGrabBag = false;

    [ExportGroup("Finite Spawning")]
    [Export] private int _totalEnemiesToSpawn = 10;
    [Export] private int _waves = 1;
    [Export] private float _timeBetweenWaves = 5.0f;

    public bool IsFinished { get; private set; }
    private int _spawnedThisWave = 0;
    private int _spawnedTotal = 0;
    private int _currentWave = 0;

    private LevelRoom _owningRoom;

    private Timer _spawnTimer;
    private Timer _waveTimer;
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

        _waveTimer = new Timer();
        _waveTimer.WaitTime = _timeBetweenWaves;
        _waveTimer.OneShot = true;
        _waveTimer.Timeout += OnWaveTimerTimeout;
        AddChild(_waveTimer);
    }

    private void OnWaveTimerTimeout()
    {
        _currentWave++;
        _spawnedThisWave = 0;
        _spawnTimer.Start();
    }

    private void OnSpawnTimerTimeout()
    {
        if (!IsEnabled || IsFinished || _activeEnemyCount >= _maxActiveEnemies || _enemyScenes == null || _enemyScenes.Count == 0)
        {
            return;
        }

        // If we are using waves, check if the current wave is finished.
        if (_waves > 1)
        {
            int enemiesPerWave = _totalEnemiesToSpawn / _waves;
            if (_spawnedThisWave >= enemiesPerWave)
            {
                _spawnTimer.Stop();
                // Don't start the next wave if all waves are done
                if (_currentWave < _waves -1)
                {
                    _waveTimer.Start();
                }
                return;
            }
        }

        for (int i = 0; i < _enemiesPerSpawn; i++)
        {
            if (_activeEnemyCount >= _maxActiveEnemies) break;
            
            // Stop spawning if we've hit the total limit
            if (_spawnedTotal >= _totalEnemiesToSpawn)
            {
                IsFinished = true;
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
                    var pos= GlobalPosition;
                    newEnemy.GlobalPosition = pos + pos.RandomRange(1) + Vector3.Up;
                    newEnemy.EnemyDied += OnEnemyDied;
                    _owningRoom.RegisterEnemy(newEnemy);
                    _activeEnemyCount++;
                    _spawnedTotal++;
                    _spawnedThisWave++;
                }
                else
                {
                    // Fallback for nodes that aren't enemies, just place them
                    GetParent().AddChild(newEnemyNode);
                    newEnemyNode.GlobalPosition = this.GlobalPosition + Vector3.Up;
                }
            }
        }
        
        // Final check to see if we're done after this spawn cycle
        if (_spawnedTotal >= _totalEnemiesToSpawn)
        {
            IsFinished = true;
        }
    }

    private void OnEnemyDied(Enemy who)
    {
        _activeEnemyCount--;
        // It's good practice to disconnect signals from objects that might be reused
        who.EnemyDied -= OnEnemyDied;
    }
}
