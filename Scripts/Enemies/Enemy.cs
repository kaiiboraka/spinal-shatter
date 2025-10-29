using Godot;

public partial class Enemy : CharacterBody3D
{
    [Export] private HealthComponent _healthComponent;

    [Export] private Sprite3D _sprite;

    [Export] private ProgressBar _healthBar;

    [Export] private Area3D _hurtbox;

    [Export] public int ManaToDrop { get; private set; } = 10;

    public override void _Ready()
    {
        _healthComponent.Died += OnDied;
        _healthComponent.HealthChanged += OnHealthChanged;

        _hurtbox.BodyEntered += OnHurtboxBodyEntered;

        // Initialize Health Bar
        _healthBar.MaxValue = _healthComponent.MaxHealth;
        _healthBar.Value = _healthComponent.CurrentHealth;
    }

    private void OnHurtboxBodyEntered(Node3D body)
    {
        if (body is Projectile projectile)
        {
            TakeDamage(projectile.Damage);
            projectile.QueueFree();
        }
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        _healthBar.Value = newHealth;
    }

    public void TakeDamage(float amount)
    {
        _healthComponent.TakeDamage(amount);
        FlashRed();
    }

    private void FlashRed()
    {
        var tween = GetTree().CreateTween();
        tween.TweenProperty(_sprite, "modulate", Colors.Red, 0.1);
        tween.TweenProperty(_sprite, "modulate", Colors.White, 0.1);
    }

    private void OnDied()
    {
        ManaParticleManager.Instance.SpawnMana(ManaToDrop, this.GlobalPosition);
        QueueFree();
    }
}
