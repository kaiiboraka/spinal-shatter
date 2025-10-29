using Godot;
using Godot.Collections;

public partial class EnemySpawner : Node3D
{
    [Export] private Array<PackedScene> _enemyScenes = new();
    [Export] private float _spawnInterval = 5.0f;
    [Export] private bool _spawnInRandomOrder = false;
    [Export] private bool _useGrabBag = false;

    private Timer _spawnTimer;
    private int _spawnIndex = 0;
    private Array<PackedScene> _grabBag = new();

    public override void _Ready()
    {
        _spawnTimer = new Timer();
        _spawnTimer.WaitTime = _spawnInterval;
        _spawnTimer.Autostart = true;
        _spawnTimer.Timeout += OnSpawnTimerTimeout;
        AddChild(_spawnTimer);
    }

    private void OnSpawnTimerTimeout()
    {
        if (_enemyScenes == null || _enemyScenes.Count == 0)
        {
            return;
        }

        PackedScene sceneToSpawn;

        if (_useGrabBag && _spawnInRandomOrder)
        {
            // True Grab Bag: Shuffle the list and spawn each item once.
            if (_grabBag.Count == 0)
            {
                _grabBag = _enemyScenes.Duplicate();
                _grabBag.Shuffle();
            }
            sceneToSpawn = _grabBag.PopFront();
        }
        else if (_spawnInRandomOrder)
        {
            // Simple Random: Pick any from the list, with replacement.
            sceneToSpawn = _enemyScenes[(int)(GD.Randi() % _enemyScenes.Count)];
        }
        else
        {
            // Sequential: Go through the list in order and loop.
            // This covers both (_useGrabBag = false, _spawnInRandomOrder = false)
            // and (_useGrabBag = true, _spawnInRandomOrder = false), as they are functionally identical.
            sceneToSpawn = _enemyScenes[_spawnIndex];
            _spawnIndex = (_spawnIndex + 1) % _enemyScenes.Count;
        }

        if (sceneToSpawn != null)
        {
            var newEnemy = sceneToSpawn.Instantiate<Node3D>();
            GetParent().AddChild(newEnemy);
            newEnemy.GlobalPosition = this.GlobalPosition;
        }
    }
}
