namespace SpinalShatter;

using Godot;

public partial class AutomaticCaster : Node
{
    private AutomaticSpellData _spellData;
    private Timer _fireRateTimer;
    private Marker3D _spellOrigin;

    public override void _Ready()
    {
        base._Ready();
        _fireRateTimer = new Timer();
        AddChild(_fireRateTimer);
        _fireRateTimer.Timeout += OnFireTimerTimeout;
    }

    public void Initialize(Marker3D spellOrigin)
    {
        _spellOrigin = spellOrigin;
    }

    public void SetAutomaticWeapon(AutomaticSpellData spellData)
    {
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
    }

    private void OnFireTimerTimeout()
    {
        if (_spellData == null || _spellOrigin == null || !IsInstanceValid(PlayerBody.Instance) || PlayerBody.Instance.DeadNow)
        {
            return;
        }

        // TODO: Add targeting logic here. For now, fire straight ahead.
        var projectile = _spellData.ProjectileScene.Instantiate<Projectile>();
        
        float speed = _spellData.SpeedRange.Min; // Or average, or some other logic
        Vector3 initialVelocity = -_spellOrigin.GlobalTransform.Basis.Z * speed;
        
        if (_spellData.UsePlayerMomentum)
        {
            initialVelocity += PlayerBody.Instance.Velocity.XZ();
        }
        
        ProjectileLaunchData launchData = new ProjectileLaunchData
        {
            Caster = PlayerBody.Instance,
            ManaCost = 0, // Automatic weapons are free
            InitialVelocity = initialVelocity,
            ChargeRatio = 0, // No charge for automatic weapons
            StartPosition = _spellOrigin,
            SpellData = _spellData,
            Slot = SlotType.Automatic
        };

        projectile.Launch(launchData);
    }
}
