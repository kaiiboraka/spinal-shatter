using Godot;
using System;

[GlobalClass]
public partial class HealthComponent : Node
{
    [Signal]
    public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth);

    [Signal]
    public delegate void DiedEventHandler();

    [Signal]
    public delegate void HurtEventHandler(Vector3 sourcePosition);

    [Export]
    public float MaxHealth { get; set; } = 100f;

    private float _currentHealth;
    public float CurrentHealth
    {
        get => _currentHealth;
        private set
        {
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
            EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);

            if (_currentHealth <= 0)
            {
                EmitSignal(SignalName.Died);
            }
        }
    }

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(float amount, Vector3 sourcePosition)
    {
        CurrentHealth -= amount;
        EmitSignal(SignalName.Hurt, sourcePosition);
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
    }

    public void Reset()
    {
        CurrentHealth = MaxHealth;
    }
}
