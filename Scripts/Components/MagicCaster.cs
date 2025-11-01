using Godot;
using Godot.Collections;
using System;
using System.Linq;
using index = int;
public partial class MagicCaster : Node
{
    [Export] private PackedScene _projectileScene;
    [Export] private Node3D _spellOrigin;
    [Export] private ManaComponent _manaComponent;

    [ExportSubgroup("Audio", "_audio")]
    [Export] private AudioStreamPlayer3D _audioPlayer_Spell;
    [Export] private AudioStreamPlayer3D _audioPlayer_Charge;
    [Export] private AudioStream _audio_Charge;
    [Export] private AudioStream _audio_Cast;

    [ExportGroup("Charging")]
    [Export] private float _maxChargeTime = 2.0f;
    [Export(PropertyHint.Range, "1,100,1")] private float _minManaCost = 1.0f;
    [Export(PropertyHint.Range, "1,200,1")] private float _maxManaCost = 50.0f;
    [Export(PropertyHint.Range, "1,16,1")] private int _chargeIntervals = 8;

    /// <summary>
    /// Damage multiplier per mana point for each charge interval.
    /// Size should be ChargeIntervals + 1 (for 0-charge and each interval).
    /// </summary>
    private Array<float> DamageMultipliers => new(Enumerable.Range(0, _chargeIntervals + 1)
                                                            .Select(i =>
                                                                 Mathf.Pow(4,
                                                                     (i * _maxChargeTime / _chargeIntervals)))
                                                            .ToArray());
    // new Array<float>()
    // {
        // 1, 1.5f, 2, 2.5f, 3, 3.5f, 4, 4.5f, 5
    // };

    // index i = 0;
    // 16 ^ (i * _maxChargeTime / _chargeIntervals )

    [Export] private float _minSpeed = 20f;
    [Export] private float _maxSpeed = 40f;

    private float _currentChargeTime = 0f;
    private Projectile _chargingProjectile = null;
    private bool _isCharging = false;
    private index _lastInterval = -1;

    public override void _Ready()
    {
        base._Ready();

        GD.Print($"MagicCaster Damage Array: {DamageMultipliers}" );
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("Player_Shoot"))
        {
            _isCharging = true;
            StartCharge();
        }
        else if (@event.IsActionReleased("Player_Shoot"))
        {
            _isCharging = false;
            ReleaseCharge();
        }
        SetProcess(_isCharging);
    }

    public override void _Process(double delta)
    {
        if (_isCharging)
        {
            ContinueCharge((float)delta);
        }
    }

    private void StartCharge()
    {
        if (_chargingProjectile != null || _projectileScene == null || _manaComponent.CurrentMana < _minManaCost)
        {
            return;
        }

        _currentChargeTime = 0f;
        _lastInterval = -1;
        _chargingProjectile = _projectileScene.Instantiate<Projectile>();
        _chargingProjectile.BeginCharge(_spellOrigin);

        _audioPlayer_Charge.Play();
    }

    private void ContinueCharge(float delta)
    {
        if (_chargingProjectile == null) return;

        // 1. Calculate max possible charge time based on current mana
        float manaPerInterval = _chargeIntervals > 0 ? _maxManaCost / _chargeIntervals : 0;
        int maxIntervalsByMana = 0;
        if (manaPerInterval > 0)
        {
            maxIntervalsByMana = Mathf.FloorToInt(_manaComponent.CurrentMana / manaPerInterval);
        }
        float intervalDuration = _maxChargeTime > 0 ? _maxChargeTime / _chargeIntervals : 0;
        float maxChargeTimeByMana = intervalDuration > 0 ? maxIntervalsByMana * intervalDuration : 0;

        // 2. Increment charge time, clamping by various limits
        _currentChargeTime += delta;
        _currentChargeTime = Mathf.Min(_currentChargeTime, _maxChargeTime);
        if (maxChargeTimeByMana > 0)
        {
            _currentChargeTime = Mathf.Min(_currentChargeTime, maxChargeTimeByMana);
        }

        // 3. Determine current charge interval
        int currentInterval = 0;
        if (intervalDuration > 0)
        {
            currentInterval = Mathf.FloorToInt(_currentChargeTime / intervalDuration);
        }
        currentInterval = Mathf.Clamp(currentInterval, 0, _chargeIntervals);

        // 4. Update visuals and audio only when the interval changes
        if (currentInterval != _lastInterval)
        {
            _lastInterval = currentInterval;
            float chargeRatio = _chargeIntervals > 0 ? (float)currentInterval / _chargeIntervals : 0;

            float size = Mathf.Lerp(0.1f, 1.2f, chargeRatio);
            _audioPlayer_Spell.PitchScale = size * 4 / 6f;
            _audioPlayer_Spell.Stream = _audio_Charge;
            if (!_audioPlayer_Spell.IsPlaying()) _audioPlayer_Spell.Play();
            _chargingProjectile.UpdateChargeVisuals(size);
        }
    }

    private void ReleaseCharge()
    {
        if (_chargingProjectile == null) return;

        // 1. Calculate final interval and mana cost
        float intervalDuration = _maxChargeTime > 0 ? _maxChargeTime / _chargeIntervals : 0;
        // int intervalsCharged = 0;
        if (intervalDuration == 0 || _currentChargeTime < intervalDuration)   
        {
            // intervalsCharged = Mathf.FloorToInt(_currentChargeTime / intervalDuration);
            if (!_manaComponent.HasEnoughMana(_minManaCost))  
            {
                _chargingProjectile.QueueFree();
                ResetChargeState();
                return;
            }
            
            _manaComponent.ConsumeMana(_minManaCost);

            float damageMultiplier = (DamageMultipliers != null && DamageMultipliers.Count > 0) ? DamageMultipliers[0] : 1.0f;
            float damage = Mathf.Max(1.0f, _minManaCost * damageMultiplier);
            float speed = _minSpeed;
            Vector3 initialVelocity = -_spellOrigin.GlobalTransform.Basis.Z * speed;

            // Play sounds and launch
            _audioPlayer_Spell.PitchScale = 1;
            _audioPlayer_Spell.Stream = _audio_Cast;
            _audioPlayer_Spell.Play();

            _chargingProjectile.AudioStreamPlayer3D.PitchScale = .55f;
            _chargingProjectile.AudioStreamPlayer3D.Play();
            _chargingProjectile.Launch(GetOwner<Node>(), damage, _minManaCost, initialVelocity);

            ResetChargeState();
            return;  
        // intervalsCharged = Mathf.Clamp(intervalsCharged, 0, _chargeIntervals);

        // float manaCost;
        // if (intervalsCharged == 0)
        // {
            // manaCost = _minManaCost;
        }
        // else
        // {
            // Handle a charged shot (at least one interval has passed)  
        int intervalsCharged = Mathf.FloorToInt(_currentChargeTime / intervalDuration);   
        intervalsCharged = Mathf.Clamp(intervalsCharged, 1, _chargeIntervals); // At least 1 interval has passed 
        float manaPerInterval = _chargeIntervals > 0 ? _maxManaCost / _chargeIntervals : 0;
            // manaCost = intervalsCharged * manaPerInterval;
        // }
        // manaCost = Mathf.Max(manaCost, _minManaCost);

        // 2. Check for mana and fizzle if not enough
        if (!_manaComponent.HasEnoughMana(manaCost))
        {
            _chargingProjectile.QueueFree();
            ResetChargeState();
            return;
        }

        // 3. Consume mana and calculate damage
        _manaComponent.ConsumeMana(manaCost);

        float damageMultiplier = 1.0f;
        if (DamageMultipliers != null && intervalsCharged < DamageMultipliers.Count)
        {
            damageMultiplier = DamageMultipliers[intervalsCharged];
        }
        float damage = Mathf.Max(1.0f, manaCost * damageMultiplier);

        // 4. Calculate speed and launch
        float chargeRatio = _chargeIntervals > 0 ? (float)intervalsCharged / _chargeIntervals : 0;
        float speed = Mathf.Lerp(_minSpeed, _maxSpeed, chargeRatio);
        Vector3 initialVelocity = -_spellOrigin.GlobalTransform.Basis.Z * speed;

        // 5. Play sounds and launch
        _audioPlayer_Spell.PitchScale = 1;
        _audioPlayer_Spell.Stream = _audio_Cast;
        _audioPlayer_Spell.Play();

        _chargingProjectile.AudioStreamPlayer3D.PitchScale = .55f;
        _chargingProjectile.AudioStreamPlayer3D.Play();
        _chargingProjectile.Launch(GetOwner<Node>(), damage, manaCost, initialVelocity);

        // 6. Reset state
        ResetChargeState();
    }

    private void ResetChargeState()
    {
        _chargingProjectile = null;
        _currentChargeTime = 0f;
        _lastInterval = -1;
        _audioPlayer_Charge.Stop();
        _audioPlayer_Spell.Stop();
    }
}
