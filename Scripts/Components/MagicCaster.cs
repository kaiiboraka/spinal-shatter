using Godot;
using Godot.Collections;
using System.Linq;
using Elythia;

public partial class MagicCaster : Node
{
	[Export] private PackedScene _projectileScene;
	[Export] private ManaComponent _manaComponent;
	[Export] private Node3D _spellOrigin;

	[ExportSubgroup("Audio", "_audio")]
	[Export] private AudioStreamPlayer3D _audioPlayer_Spell;

	[Export] private AudioStreamPlayer3D _audioPlayer_Charge;
	[Export] private AudioStream _audio_Charge;
	[Export] private AudioStream _audio_Cast;

	[ExportGroup("Charging")]
	[Export] private float _maxChargeTime = 2.0f;

	[Export(PropertyHint.Range, "1,100,1")]
	private float _minManaCost = 1.0f;

	[Export(PropertyHint.Range, "1,200,1")]
	private float _maxManaCost = 50.0f;

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

	[Export] private float _minSpeed = 20f;
	[Export] private float _maxSpeed = 40f;

	[Export] private bool usePlayerMomentum = false;

	private float _currentChargeTime = 0f;
	private Projectile _chargingProjectile = null;
	private bool _isCharging = false;
	private int _lastInterval = -1;

	public override void _Ready()
	{
		base._Ready();
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
		_currentChargeTime =
			Mathf.Min(_currentChargeTime,
				maxChargeTimeByMana); // Always clamp by mana, even if max charge time by mana is zero.

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
			_chargingProjectile.Charge = chargeRatio;
			_chargingProjectile.UpdateChargeState();
		}
	}

	private void ReleaseCharge()
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

		float manaPerInterval = _maxManaCost / _chargeIntervals;
		float manaCost = Mathf.Clamp(intervalsCharged * manaPerInterval, _minManaCost, _maxManaCost);
		float chargeRatio = (float)intervalsCharged / _chargeIntervals;

		DebugManager.Trace(
			$"manaCost: {manaCost}, manaPerInterval: {manaPerInterval},  intervalsCharged: {intervalsCharged}");
		if (!_manaComponent.HasEnoughMana(manaCost))
		{
			_chargingProjectile.QueueFree();
			ResetChargeState();
			return;
		}


		float damageMultiplier = DamageMultipliers?[intervalsCharged] ?? 1.0f;
		float damage = Mathf.Max(1.0f, manaCost * damageMultiplier);

		float speed = Mathf.Lerp(_minSpeed, _maxSpeed, chargeRatio.Clamp01());
		Vector3 initialVelocity = CalculateInitialVelocity(speed);

		float damageGrowthConstant = _maxChargeTime;
		float absoluteMaxProjectileSpeed = Mathf.Max(_minSpeed, _maxSpeed);

		DebugManager.Debug(
			$"Speed: {speed}, chargeRatio: {chargeRatio}, damageMultiplier: {damageMultiplier}, damage: {damage}");

		ProjectileLaunchData launchData = new ProjectileLaunchData
		{
			Caster = PlayerBody.Instance,
			Damage = damage,
			ManaCost = manaCost,
			InitialVelocity = initialVelocity,
			ChargeRatio = chargeRatio,
			DamageGrowthConstant = damageGrowthConstant,
			AbsoluteMaxProjectileSpeed = _maxSpeed,
			MaxInitialManaCost = _maxManaCost,
			StartPosition = _spellOrigin.GlobalPosition,
		};

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
		_audioPlayer_Charge.Stop();
		_audioPlayer_Spell.Stop();
	}

}

/* OLD PRE-REWORK MAGICCASTER.CS FOR COMPARISON

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
		if (@event.IsActionPressed("Player_Siphon"))
		{

		}
		else if (@event.IsActionPressed("Player_Shoot"))
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
		_chargingProjectile.Launch(PlayerBody.Instance.Owner, damage, manaCost, initialVelocity);

		// Clear reference
		_chargingProjectile = null;
		_currentCharge = 0f;
		_audioPlayer_Charge.Stop();
	}
}


*/