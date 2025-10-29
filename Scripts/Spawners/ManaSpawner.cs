using Godot;

public partial class ManaSpawner : Node3D
{
    [Export] private float _spawnInterval = 2.0f;
    [Export] private int _manaAmountToSpawn = 16;

    public override void _Ready()
    {
        var timer = new Timer();
        timer.WaitTime = _spawnInterval;
        timer.Autostart = true;
        timer.Timeout += OnTimerTimeout;
        AddChild(timer);
    }

    private void OnTimerTimeout()
    {
        ManaParticleManager.Instance.SpawnMana(_manaAmountToSpawn, this.GlobalPosition);
    }
}
