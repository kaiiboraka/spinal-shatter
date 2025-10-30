using Godot;

public partial class PlayerHealthBar : ProgressBar
{
    public void OnHealthChanged(float currentHealth, float maxHealth)
    {
        MaxValue = maxHealth;
        Value = currentHealth;
    }
}
