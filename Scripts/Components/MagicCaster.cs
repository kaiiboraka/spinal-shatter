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
    private bool _isCharging = false;

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("Player_Shoot"))
        {
            _isCharging = true;
            // Increase charge over time, capped at 1.0
            _currentCharge = Mathf.Min(_currentCharge + (float)delta / _maxChargeTime, 1.0f);
        }

        if (Input.IsActionJustReleased("Player_Shoot"))
        {
            if (_isCharging)
            {
                Cast();
                _isCharging = false;
                _currentCharge = 0f;
            }
        }
    }

    private void Cast()
    {
        if (_projectileScene == null || _spellOrigin == null || _manaComponent == null)
        {
            GD.PrintErr("MagicCaster is not configured correctly. ProjectileScene, SpellOrigin, or ManaComponent is missing.");
            return;
        }

        // Calculate properties based on charge
        float manaCost = Mathf.Lerp(_minManaCost, _maxManaCost, _currentCharge);

        if (!_manaComponent.HasEnoughMana(manaCost))
        {
            return;
        }

        _manaComponent.ConsumeMana(manaCost);

        Node3D projectileInstance = _projectileScene.Instantiate<Node3D>();
        
        GetTree().Root.AddChild(projectileInstance);
        
        projectileInstance.GlobalTransform = _spellOrigin.GlobalTransform;

        var projectile = projectileInstance as Projectile;
        if (projectile != null)
        { 
            float size = Mathf.Lerp(0.5f, 2.0f, _currentCharge);
            float damage = Mathf.Lerp(10f, 100f, _currentCharge);
            float speed = Mathf.Lerp(20f, 40f, _currentCharge);
            Vector3 initialVelocity = -_spellOrigin.GlobalTransform.Basis.Z * speed;

            projectile.Initialize(damage, size, initialVelocity);
        }
        else
        {
            GD.PrintErr("Projectile scene root node does not have a Projectile script attached.");
            projectileInstance.QueueFree();
        }
    }
}
