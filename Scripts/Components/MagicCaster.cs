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

    [ExportSubgroup("Audio","_audio")]
    [Export] private AudioStreamPlayer3D _audioPlayer_Spell;
    [Export] private AudioStreamPlayer3D _audioPlayer_Charge;
    [Export] private AudioStream _audio_Charge;
    [Export] private AudioStream _audio_Cast;

    [ExportGroup("Charging")]
    [Export] private float _maxChargeTime = 2.0f; // Time in seconds to reach full charge
    [Export] private float _minManaCost = 1.0f;
    [Export] private float _maxManaCost = 50.0f;

    private float _currentCharge = 0f; // 0.0 to 1.0
    private Projectile _chargingProjectile = null;
    private bool _isCharging = false;

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

        _currentCharge = 0f;
        _chargingProjectile = _projectileScene.Instantiate<Projectile>();
        // _chargingProjectile.LevelParent = PlayerBody.Instance.ParentLevel;
        _chargingProjectile.BeginCharge(_spellOrigin);

        _audioPlayer_Charge.Play();
    }

    private void ContinueCharge(float delta)
    {
        if (_chargingProjectile == null) return;

        // Determine the maximum charge percentage allowed by the player's current mana
        float maxChargeByMana = Mathf.InverseLerp(_minManaCost, _maxManaCost, _manaComponent.CurrentMana);
        maxChargeByMana = Mathf.Clamp(maxChargeByMana, 0f, 1f);

        // Increment charge, but clamp it by the mana limit
        float newCharge = _currentCharge + delta / _maxChargeTime;
        _currentCharge = Mathf.Min(newCharge, maxChargeByMana);

        // Map charge (0-1) to size (0.1-1.2)
        float size = Mathf.Lerp(0.1f, 1.2f, _currentCharge);
        _audioPlayer_Spell.PitchScale = size * 4 / 6f;
        _audioPlayer_Spell.Stream = _audio_Charge;
        if (!_audioPlayer_Spell.IsPlaying()) _audioPlayer_Spell.Play();
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

        _audioPlayer_Spell.PitchScale = 1;
        _audioPlayer_Spell.Stream = _audio_Cast;
        _audioPlayer_Spell.Play();

        _chargingProjectile.AudioStreamPlayer3D.PitchScale = .55f;
        _chargingProjectile.AudioStreamPlayer3D.Play();
        _chargingProjectile.Launch(damage, manaCost, initialVelocity);

        // Clear reference
        _chargingProjectile = null;
        _currentCharge = 0f;
        _audioPlayer_Charge.Stop();
    }
}
