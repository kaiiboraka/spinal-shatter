using Godot;
using Elythia;

namespace SpinalShatter;

public partial class MagicCaster : Node
{
	[Export] private PackedScene _projectileScene;
	[Export] private ManaComponent _manaComponent;
	[Export] private Node3D _spellOrigin;

	[ExportSubgroup("Audio", "_audio")]
	[Export] private AudioStreamPlayer3D _audioPlayer_Spell;
	[Export] private AudioStreamPlayer3D _audioPlayer_ChargeBack;
	[Export] private AudioStreamPlayer3D _audioPlayer_ChargeBeep;
	[Export] private AudioStream _audio_Cast;
	[Export] private AudioStream _audio_Charge;
	[Export] private AudioStream _audio_ChargeComplete;

	[ExportGroup("Charging")]
	[Export] private float _maxChargeTime = 2.0f;

	[Export] private FloatValueRange ManaCostRange;
	[Export] private FloatValueRange DamageRange;
	[Export] private FloatValueRange SpeedRange;
	[Export] private FloatValueRange SizeRange;

	[Export(PropertyHint.Range, "1,16,1")] private int _chargeIntervals = 8;

	[Export] private bool usePlayerMomentum = false;

	private float _currentChargeTime = 0f;
	private Projectile _chargingProjectile = null;
	private bool _isCharging = false;
	private int _lastInterval = -1;

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("Player_Shoot"))
		{
			_isCharging = true;
			OnPressCharge();
		}
		else if (@event.IsActionReleased("Player_Shoot"))
		{
			_isCharging = false;
			OnReleaseCharge();
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

	private void OnPressCharge()
	{
		if (_chargingProjectile != null || _projectileScene == null || _manaComponent.CurrentMana < ManaCostRange.Min)
		{
			return;
		}

		_currentChargeTime = 0f;
		_lastInterval = -1;
		_chargingProjectile = _projectileScene.Instantiate<Projectile>();
		_audioPlayer_ChargeBack.Play();
		_chargingProjectile.BeginChargingProjectile(_spellOrigin, SizeRange);
	}

	private void ContinueCharge(float delta)
	{
		if (_chargingProjectile == null) return;

		// 1. Calculate max possible charge ratio based on current mana
		float maxChargeRatioByMana = Mathf.InverseLerp(ManaCostRange.Min, ManaCostRange.Max, _manaComponent.CurrentMana);
		float maxChargeTimeByMana = maxChargeRatioByMana * _maxChargeTime;

		// 2. Increment charge time, clamping by various limits
		_currentChargeTime += delta;
		_currentChargeTime = Mathf.Min(_currentChargeTime, _maxChargeTime);
		_currentChargeTime = Mathf.Min(_currentChargeTime, maxChargeTimeByMana);

		// 3. Determine current charge interval
		int currentInterval = 0;
		float intervalDuration = _maxChargeTime > 0 ? _maxChargeTime / _chargeIntervals : 0;
		if (intervalDuration > 0)
		{
			currentInterval = Mathf.FloorToInt(_currentChargeTime / intervalDuration);
		}
		currentInterval = Mathf.Clamp(currentInterval, 0, _chargeIntervals);

		float chargeRatio = 0;
		// 4. Update visuals and audio only when the interval changes
		if (currentInterval != _lastInterval)
		{
			_lastInterval = currentInterval;
			chargeRatio = _chargeIntervals > 0 ? (float)currentInterval / _chargeIntervals : 0;

			float size = Mathf.Lerp(0.1f, 1.2f, chargeRatio);
			_audioPlayer_ChargeBeep.Stream = _audio_Charge;
			_audioPlayer_ChargeBeep.PitchScale = size * 4 / 6f;

			if (currentInterval >= 1) _audioPlayer_ChargeBeep.Play();
			_chargingProjectile.Charge = chargeRatio;
			_chargingProjectile.UpdateChargeState();
		}

		if (chargeRatio >= 1.0f)
		{
			_chargingProjectile.Modulate(new Color(4,4,3,1));
			if (_audioPlayer_ChargeBeep.IsPlaying()) _audioPlayer_ChargeBeep.Stop();
			_audioPlayer_ChargeBeep.SetStream(_audio_ChargeComplete);
			_audioPlayer_ChargeBeep.Play(.15f);
		}
	}

	private void OnReleaseCharge()
	{
		if (_chargingProjectile == null)
			return;

		float intervalDuration = _maxChargeTime > 0 ? _maxChargeTime / _chargeIntervals : 0;

		HandleChargedShot(intervalDuration);
	}

	private void HandleChargedShot(float intervalDuration)
	{
		int intervalsCharged = Mathf.FloorToInt(_currentChargeTime / intervalDuration);
		intervalsCharged = Mathf.Clamp(intervalsCharged, 0, _chargeIntervals);

		float chargeRatio = (float)intervalsCharged / _chargeIntervals;
		float manaCost = Mathf.Lerp(ManaCostRange.Min, ManaCostRange.Max, chargeRatio);

		if (!_manaComponent.HasEnoughMana(manaCost))
		{
			_chargingProjectile.QueueFree();
			ResetChargeState();
			return;
		}

		float damage = Mathf.Lerp(DamageRange.Min, DamageRange.Max, chargeRatio);
		float speed = Mathf.Lerp(SpeedRange.Min, SpeedRange.Max, chargeRatio);

		Vector3 initialVelocity = CalculateInitialVelocity(speed);
		// DebugManager.Debug($"Speed: {speed}, chargeRatio: {chargeRatio}, damage: {damage}, manaCost: {manaCost}");
		ProjectileLaunchData launchData = new ProjectileLaunchData
		{
			Caster = PlayerBody.Instance,
			Damage = damage,
			ManaCost = manaCost,
			InitialVelocity = initialVelocity,
			ChargeRatio = chargeRatio,
			DamageGrowthConstant = _maxChargeTime,
			AbsoluteMaxProjectileSpeed = SpeedRange.Max,
			MaxInitialManaCost = ManaCostRange.Max,
			StartPosition = _spellOrigin.GlobalPosition,
			SizingScale = SizeRange,
		};

		_audioPlayer_Spell.Play();
		_chargingProjectile.Launch(launchData);
		_manaComponent.ConsumeMana(manaCost);
		ResetChargeState();
	}

	private Vector3 CalculateInitialVelocity(float speed)
	{
		Vector3 projectileVelocity = -_spellOrigin.GlobalTransform.Basis.Z * speed;

		if (usePlayerMomentum && GetOwner() is PlayerBody player)
		{
			Vector3 playerMomentum = player.Velocity.XZ();
			return projectileVelocity + playerMomentum;
		}

		return projectileVelocity;
	}

	private void ResetChargeState()
	{
		_chargingProjectile = null;
		_currentChargeTime = 0f;
		_lastInterval = -1;
		_audioPlayer_ChargeBack.Stop();
		_audioPlayer_ChargeBeep.Stop();
	}

}