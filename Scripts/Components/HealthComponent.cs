using Godot;
using System;

[GlobalClass]
public partial class HealthComponent : Node
{
    [Signal] public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth);

    [Signal] public delegate void DiedEventHandler();

    [Signal]  public delegate void HurtEventHandler(Vector3 sourcePosition, float damage);

    [Export] public float MaxHealth { get; set; } = 100f;

    private float _currentHealth;

    private bool _isDead = false;

    public float CurrentPercent => CurrentHealth / MaxHealth;

    public float CurrentHealth
    {
        get => _currentHealth;
        private set
        {
            if (_isDead) return;

            _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
            EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);

            if (_currentHealth <= 0)
            {
                _isDead = true;
                EmitSignal(SignalName.Died);
            }
        }
    }

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public float TakeDamage(float amount, Vector3 sourcePosition)
    {
        if (_isDead) return 0;

        float previousHealth = CurrentHealth;
        CurrentHealth -= amount;
        float actualDamageDealt = previousHealth - CurrentHealth; // Calculate actual damage dealt
        EmitSignal(SignalName.Hurt, sourcePosition, actualDamageDealt);
        return actualDamageDealt;
    }

    public void Heal(float amount)
    {
        if (_isDead) return;
        CurrentHealth += amount;
    }

    public void Reset()
    {
        _isDead = false;
        CurrentHealth = MaxHealth;
    }
}
