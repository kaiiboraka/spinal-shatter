using Godot;
using System;

public partial class HealthComponent : Node
{
    [Signal]
    public delegate void HealthChangedEventHandler(float oldHealth, float newHealth);

    [Signal]
    public delegate void DiedEventHandler();

    [Export]
    public float MaxHealth { get; set; } = 100f;

    private float _currentHealth;
    public float CurrentHealth
    {
        get => _currentHealth;
        private set
        {
            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
            EmitSignal(SignalName.HealthChanged, oldHealth, _currentHealth);

            if (_currentHealth <= 0)
            {
                EmitSignal(SignalName.Died);
            }
        }
    }

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        EmitSignalHealthChanged(MaxHealth, MaxHealth);
    }

    public void TakeDamage(float amount)
    {
        CurrentHealth -= amount;
    }
}
