using Godot;
using System;

[GlobalClass]
public partial class ManaComponent : Node
{
    [Signal]
    public delegate void ManaChangedEventHandler(float currentMana, float maxMana);

    [Export]
    public float MaxMana { get; set; } = 100f;

    private float _currentMana;
    public float CurrentMana
    {
        get => _currentMana;
        private set
        {
            _currentMana = Mathf.Clamp(value, 0, MaxMana);
            EmitSignalManaChanged(_currentMana, MaxMana);
        }
    }

    public override void _Ready()
    {
        CurrentMana = MaxMana;
    }

    public bool HasEnoughMana(float amount)
    {
        return CurrentMana >= amount;
    }

    public void ConsumeMana(float amount)
    {
        CurrentMana -= amount;
    }

    public void AddMana(float amount)
    {
        CurrentMana += amount;
    }

    public void RefillMana()
    {
        CurrentMana = MaxMana;
    }
}
