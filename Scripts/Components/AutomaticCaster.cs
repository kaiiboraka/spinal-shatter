using System.Linq;
using Elythia;

namespace SpinalShatter;

using Godot;

public partial class AutomaticCaster : Node
{
    private SpellData _spellData;
    private Timer _fireRateTimer;
    [Export] private Marker3D _spellOrigin;

    private PlayerBody player;

    public override void _Ready()
    {
        base._Ready();
        _fireRateTimer = new Timer();
        AddChild(_fireRateTimer);
        _fireRateTimer.Timeout += OnFireTimerTimeout;
    }

    public void SetAutomaticWeapon(SpellData spellData)
    {
        if (spellData.Slot != SlotType.Automatic)
        {
            GD.PrintErr("Set Automatic Weapon: Invalid slot type");
            return;
        }
        _spellData = spellData;
        if (_spellData != null)
        {
            _fireRateTimer.WaitTime = 1.0f / _spellData.FireRate;
            if (_fireRateTimer.IsStopped())
            {
                _fireRateTimer.Start();
            }
        }
        else
        {
            _fireRateTimer.Stop();
        }

        player = PlayerBody.Instance;
    }

    private void OnFireTimerTimeout()
    {
        if (_spellData == null || _spellOrigin == null || !IsInstanceValid(player) || player.DeadNow)
        {
            // Debug messages are assumed to be handled or not needed based on previous implementation.
            return;
        }

        float speed = _spellData.SpeedRange.FixedValue; // Using Min for simplicity, can be averaged or other logic
        var (target, initialVelocity) = GetFiringDirection();
        if (!target) return;
        initialVelocity *= speed;

        if (_spellData.UsePlayerMomentum)
        {
            initialVelocity += player.Velocity.XZ();
        }

        var projectile = _spellData.ProjectileScene.Instantiate<Projectile>();

        ProjectileLaunchData launchData = new ProjectileLaunchData
        {
            Caster = player,
            ManaCost = 0, // Automatic weapons are free
            InitialVelocity = initialVelocity,
            ChargeRatio = 1, // No charge for automatic weapons
            StartPosition = _spellOrigin,
            SpellData = _spellData,
            Slot = SlotType.Automatic
        };

        projectile.Launch(launchData);
    }

    private (bool, Vector3) GetFiringDirection()
    {
        switch (_spellData.Weapon)
        {
            case WeaponType.Orb:
                var (targetAcquired, targetPosition) = TargetNearestEnemy();
                if (targetAcquired)
                {
                    return (true, _spellOrigin.GlobalPosition.DirectionTo(targetPosition));
                }
                return (false, -_spellOrigin.GlobalTransform.Basis.Z);
                // Fall-through to default if no target is acquired

            // Future weapon types can have their own targeting logic here.
            // case WeaponType.SomeOtherWeapon:
            //     return SomeOtherTargetingLogic();

            default:
                // Default to firing straight ahead if no specific logic is defined or no target found
                return (false, -_spellOrigin.GlobalTransform.Basis.Z);
        }
    }

    private (bool, Vector3) TargetNearestEnemy()
    {
        Vector3 closestEnemyPosition = Vector3.Zero;
        float closestDistanceSquared = float.MaxValue;
        bool targetAcquired = false;

        if (RoomManager.Instance.CurrentRoom.EnemiesInRoom.IsNullOrEmpty()) return (false, Vector3.Zero);

        // Find nearest enemy within range
        foreach (Enemy enemy in WaveDirector.Instance.CurrentRoom.EnemiesInRoom)
        {
            if (enemy.DeadNow) continue;

            Vector3 enemyPosition =  enemy.HurtboxPosition;
            float distanceSquaredToPlayer = enemyPosition.DistanceSquaredTo(player.GlobalPosition);

            if (distanceSquaredToPlayer >= _spellData.TargetingRange.Squared()) continue;
            if (distanceSquaredToPlayer >= closestDistanceSquared) continue;

            closestDistanceSquared = distanceSquaredToPlayer;
            closestEnemyPosition = enemyPosition;
            targetAcquired = true;
        }
        return (targetAcquired, closestEnemyPosition);
    }
}
