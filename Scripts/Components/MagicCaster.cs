using Godot;
using System;

public partial class MagicCaster : Node
{
    [Export]
    private PackedScene _projectileScene;

    [Export]
    private Node3D _spellOrigin;

    [Export]
    private ManaComponent _manaComponent;

    [ExportGroup("Charging")]
    [Export]
    private float _maxChargeTime = 2.0f; // Time in seconds to reach full charge
    [Export]
    private float _minManaCost = 10.0f;
    [Export]
    private float _maxManaCost = 50.0f;

    private float _currentCharge = 0f; // 0.0 to 1.0
    private Projectile _chargingProjectile = null;

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("Player_Shoot"))
        {
            StartCharge();
        }

        if (Input.IsActionPressed("Player_Shoot"))
        {
            ContinueCharge((float)delta);
        }

        if (Input.IsActionJustReleased("Player_Shoot"))
        {
            ReleaseCharge();
        }
    }

    private void StartCharge()
    {
        if (_chargingProjectile != null || _projectileScene == null || _manaComponent.CurrentMana < _minManaCost)
        {
            return;
        }

        _currentCharge = 0f;
        _chargingProjectile = _projectileScene.Instantiate<Projectile>();
        _chargingProjectile.LevelParent = PlayerBody.Instance.ParentLevel;
        _chargingProjectile.BeginCharge(_spellOrigin);
    }

    private void ContinueCharge(float delta)
    {
        if (_chargingProjectile == null) return;

        _currentCharge = Mathf.Min(_currentCharge + delta / _maxChargeTime, 1.0f);

        // Map charge (0-1) to size (0.1-1.2)
        float size = Mathf.Lerp(0.1f, 1.2f, _currentCharge);
        _chargingProjectile.UpdateChargeVisuals(size);
    }

    private void ReleaseCharge()
    {
        if (_chargingProjectile == null) return;

        float manaCost = Mathf.Lerp(_minManaCost, _maxManaCost, _currentCharge);

        if (!_manaComponent.HasEnoughMana(manaCost))
        {
            _chargingProjectile.QueueFree(); // Fizzle if not enough mana
            _chargingProjectile = null;
            return;
        }

        _manaComponent.ConsumeMana(manaCost);

        float damage = Mathf.Lerp(10f, 100f, _currentCharge);
        float speed = Mathf.Lerp(20f, 40f, _currentCharge);
        Vector3 initialVelocity = -_spellOrigin.GlobalTransform.Basis.Z * speed;

        _chargingProjectile.Launch(damage, initialVelocity);

        // Clear reference
        _chargingProjectile = null;
        _currentCharge = 0f;
    }
}
