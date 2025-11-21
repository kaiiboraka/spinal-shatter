using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace SpinalShatter;

[GlobalClass, Tool]
public partial class EnemySpawner : Node3D
{
    [Signal] public delegate void SpawningFinishedEventHandler();

    private float _spawnRadius = 10.0f;
    [Export] private float _spawnInterval = 1.0f;

    private LevelRoom _owningRoom;
    private Timer _spawnTimer;
    private Array<PackedScene> _enemiesToSpawn = new();
    private int _activeEnemyCount = 0;
    private readonly Godot.Collections.Dictionary<PackedScene, ObjectPoolManager<Node3D>> _pools = new();

    private MeshInstance3D radiusViewerInstance;
    private CylinderMesh radiusViewer;

    [Export(PropertyHint.Range, ".5, 20")]
    public float SpawnRadius
    {
        get => _spawnRadius;
        private set
        {
            _spawnRadius = Mathf.Max(value, .5f);
            SetRadius(_spawnRadius);
        }
    }


    [ExportToolButton("SetSpawnRadius", Icon = "CylinderShape3D")]
    public Callable SetSpawnRadiusButton => Callable.From(() => SetRadius(_spawnRadius));


    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;

        GetRadiusViewer();
        radiusViewerInstance.QueueFree();

        var parent = GetParent();
        while (parent is not LevelRoom)
        {
            parent = parent.GetParent();
        }
        _owningRoom = parent as LevelRoom;

        if (_owningRoom == null)
        {
            GD.PrintErr($"EnemySpawner '{Name}' is not a child of a LevelRoom. It will not function correctly.");
            SetProcess(false);
            return;
        }

        _spawnTimer = new Timer();
        AddChild(_spawnTimer);
        _spawnTimer.WaitTime = _spawnInterval;
        _spawnTimer.Timeout += OnSpawnTimerTimeout;
    }

    private void SetRadius(float newRadius)
    {
        if (!GetRadiusViewer()) return;

        radiusViewer.TopRadius = newRadius;
        radiusViewer.BottomRadius = newRadius;
    }

    public override void _Notification(int what)
    {
        if (this.IsInGame()) return;
        base._Notification(what);
        if (what == NotificationEnterTree)
        {
            SetRadius(_spawnRadius);
        }
    }

    private bool GetRadiusViewer()
    {
        if (!this.IsReady()) return false;

        radiusViewerInstance = GetNode<MeshInstance3D>("RadiusViewer");
        radiusViewer = radiusViewerInstance.Mesh as CylinderMesh;

        return radiusViewer != null;
    }

    public void StartSpawningWave(Array<PackedScene> enemies)
    {
        _enemiesToSpawn = new Array<PackedScene>(enemies);
        _enemiesToSpawn.Shuffle();
        _spawnTimer.Start();
    }

    private void OnSpawnTimerTimeout()
    {
        if (_enemiesToSpawn.Count == 0)
        {
            _spawnTimer.Stop();
            EmitSignalSpawningFinished();
            return;
        }

        var scene = _enemiesToSpawn.PopFront();
        if (scene != null)
        {
            Spawn(scene);
        }
    }

    /// <summary>
    /// Creates object pools for all the provided enemy scenes. This should be called
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

            // The ObjectPoolManager itself is a Node, so it can be added as a child
            // The PoolParent for the *pooled objects* should be the EnemySpawner itself,
            // or a dedicated container node within the spawner.
            AddChild(newPool); // Add the pool manager as a child of the spawner
            newPool.PoolParent = _owningRoom; // Pooled objects will be children of the owning room
            
            _pools[scene] = newPool;
        }
    }

    private void Spawn(PackedScene enemyScene)
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
            
            			float angle = (float)GD.Randf() * Mathf.Pi * 2;
            			float radius = Mathf.Sqrt((float)GD.Randf()) * SpawnRadius;
            			var randomOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            			newEnemy.GlobalPosition = GlobalPosition + new Vector3(randomOffset.X, 0, randomOffset.Y);
            
            			newEnemy.Activate();
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
