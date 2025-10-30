using Godot;
using Godot.Collections;

public partial class ManaSpawner : Node3D
{
    private bool _isEnabled = true;
    [Export]
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;

            if (_spawnTimer == null) return; // Defer if not ready

            if (_isEnabled)
            {
                _spawnTimer.Start();
                OnTimerTimeout(); // Attempt an immediate spawn
            }
            else
            {
                _spawnTimer.Stop();
            }
        }
    }

    [Export] private int _maxActiveInstances = 50;
    [Export] private float _spawnInterval = 2.0f;
    
    [ExportGroup("Mana Amount")]
    [Export] private int _minManaToSpawn = 8;
    [Export] private int _maxManaToSpawn = 16;
    [Export] private bool _spawnRandomAmount = true;

    private Timer _spawnTimer;
    private int _activeInstanceCount = 0;

    public override void _Ready()
    {
        _spawnTimer = new Timer();
        _spawnTimer.WaitTime = _spawnInterval;
        _spawnTimer.Timeout += OnTimerTimeout;
        AddChild(_spawnTimer);

        if (IsEnabled)
        {
            _spawnTimer.Start();
        }
    }

    private void OnTimerTimeout()
    {
        if (!IsEnabled || _activeInstanceCount >= _maxActiveInstances)
        {
            return;
        }

        int amountToSpawn = _spawnRandomAmount
            ? GD.RandRange(_minManaToSpawn, _maxManaToSpawn)
            : _maxManaToSpawn;

        Array<ManaParticle> spawnedParticles = ManaParticleManager.Instance.SpawnMana(amountToSpawn, this.GlobalPosition);

        foreach (var particle in spawnedParticles)
        {
            if (particle == null) continue;

            if (_activeInstanceCount >= _maxActiveInstances)
            {
                // Stop if we hit the cap mid-spawn and release the extra particle
                ManaParticleManager.Instance.Release(particle);
                continue;
            }

            particle.Collected += OnManaCollected;
            particle.StopMoving();
            particle.DriftIdle();
            _activeInstanceCount++;
        }
    }

    private void OnManaCollected(ManaParticle who)
    {
        _activeInstanceCount--;
        who.Collected -= OnManaCollected;
    }
}
