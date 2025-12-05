namespace SpinalShatter;

using Godot;
using Elythia;
using System;

public partial class MagicCaster : Node
{
	private CastedSpellData _equippedSpell;
	private CastedSpellData _equippedAltFireSpell;

	[Export] private ManaComponent manaComponent;
	[Export] public Marker3D SpellOrigin { get; private set; }

	[ExportSubgroup("Audio", "_audio")]
	[Export] private AudioStreamPlayer3D audioPlayer_Spell;
	[Export] private AudioStreamPlayer3D audioPlayer_ChargeBack;
	[Export] private AudioStreamPlayer3D audioPlayer_ChargeBeep;

	public bool CanShoot { get; set; } = true;

	private enum CasterState
	{
		Idle,
		Charging
	}
	private CasterState _currentState = CasterState.Idle;

	private float currentChargeTime = 0f;
	private Projectile chargingProjectile = null;

	private int lastInterval = -1;
	private AudioFile sfxBeep;
	private AudioFile sfxComplete;
	
	public void SetPrimaryWeapon(CastedSpellData spellData)
	{
		_equippedSpell = spellData;
		if (_equippedSpell == null || _equippedSpell.AudioData == null)
		{
			GD.PrintErr("MagicCaster: Primary Spell or its AudioData is not set!");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}
		sfxBeep = (AudioFile)_equippedSpell.AudioData["SpellChargeBeep"];
		sfxComplete = (AudioFile)_equippedSpell.AudioData["SpellChargeComplete"];
	}

	public void SetSecondaryWeapon(CastedSpellData spellData)
	{
		_equippedAltFireSpell = spellData;
	}

	public override void _Input(InputEvent @event)
	{
		if (_equippedSpell == null || !CanShoot) return;

		switch (_currentState)
		{
			case CasterState.Idle:
				if (@event.IsActionPressed("Player_Shoot"))
				{
					BeginCharge();
				}
				break;
			case CasterState.Charging:
				if (@event.IsActionReleased("Player_Shoot"))
				{
					FirePrimary();
				}
				else if (@event.IsActionPressed("Player_AltFire"))
				{
					FireAltFire();
				}
				break;
		}
	}

	public override void _Process(double delta)
	{
		if (_currentState == CasterState.Charging)
		{
			ContinueCharge((float)delta);
		}
	}

	private void BeginCharge()
	{
		if (chargingProjectile != null || _equippedSpell.ProjectileScene == null || manaComponent.CurrentMana < _equippedSpell.ManaCostRange.Min)
		{
			return;
		}

		_currentState = CasterState.Charging;
		SetProcess(true);

		PlayerBody.Instance.PlayCastCharge();
		PlayerBody.Instance.DisallowSiphon();
		PlayerBody.Instance.DisallowMeleeAttack();

		currentChargeTime = 0f;
		lastInterval = -1;
		chargingProjectile = _equippedSpell.ProjectileScene.Instantiate<Projectile>();

		AudioManager.Play(audioPlayer_ChargeBack, (AudioFile)_equippedSpell.AudioData["SpellChargeBack"]);

		// Pass the FloatValueRange to BeginChargingProjectile to be used by Projectile.ApplyChargeAndTypeEffects
		chargingProjectile.BeginChargingProjectile(SpellOrigin, _equippedSpell);
	}

	private void ContinueCharge(float delta)
	{
		if (chargingProjectile == null) return;

		float maxChargeRatioByMana = Mathf.InverseLerp(_equippedSpell.ManaCostRange.Min, _equippedSpell.ManaCostRange.Max, manaComponent.CurrentMana);
		float maxChargeTimeByMana = maxChargeRatioByMana * _equippedSpell.MaxChargeTime.Max;

		currentChargeTime += (float)delta;
		currentChargeTime = Mathf.Min(currentChargeTime, _equippedSpell.MaxChargeTime.Max);
		currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTimeByMana);

		int currentInterval = 0;
		float intervalDuration = _equippedSpell.MaxChargeTime.Max > 0 ? _equippedSpell.MaxChargeTime.Max / _equippedSpell.ChargeIntervals : 0;
		if (intervalDuration > 0)
		{
			currentInterval = Mathf.FloorToInt(currentChargeTime / intervalDuration);
		}
		currentInterval = Mathf.Clamp(currentInterval, 0, _equippedSpell.ChargeIntervals);

		if (currentInterval != lastInterval)
		{
			lastInterval = currentInterval;
			float chargeRatio = GetCurrentChargeRatio();
			
			sfxBeep.PitchScale = Mathf.Lerp(0.5f, 1.5f, chargeRatio);

			if (currentInterval >= 1)
			{
				AudioManager.Play(audioPlayer_ChargeBeep, sfxBeep);
			}
			chargingProjectile.UpdateChargeAmount(chargeRatio);

			if (chargeRatio >= 1.0f)
			{
				if (audioPlayer_ChargeBeep.IsPlaying()) audioPlayer_ChargeBeep.Stop();
				AudioManager.Play(audioPlayer_ChargeBeep, sfxComplete);
			}
		}
	}

	private void FirePrimary()
	{
		if (chargingProjectile == null) return;
		FireWeapon(SlotType.Primary, _equippedSpell, chargingProjectile);
	}

	private void FireAltFire()
	{
		if (_equippedAltFireSpell == null || _equippedAltFireSpell.ProjectileScene == null)
		{
			CancelCharge();
			return;
		}

		if (!manaComponent.HasEnoughMana(_equippedAltFireSpell.ManaCostRange.Min))
		{
			CancelCharge();
			return;
		}
		
		var altProjectile = _equippedAltFireSpell.ProjectileScene.Instantiate<Projectile>();
		FireWeapon(SlotType.Alt, _equippedAltFireSpell, altProjectile);
	}

	private float GetCurrentChargeRatio()
	{
		if (_equippedSpell.MaxChargeTime.Max <= 0) return 0;
		
		float intervalDuration = _equippedSpell.MaxChargeTime.Max / _equippedSpell.ChargeIntervals;
		int intervalsCharged = Mathf.FloorToInt(currentChargeTime / intervalDuration);
		intervalsCharged = Mathf.Clamp(intervalsCharged, 0, _equippedSpell.ChargeIntervals);
		return (float)intervalsCharged / _equippedSpell.ChargeIntervals;
	}

	private void FireWeapon(SlotType slotType, CastedSpellData spellData, Projectile projectileInstance)
	{
		PlayerBody.Instance.PlayCastRelease();

		float chargeRatio = GetCurrentChargeRatio();
		
		float manaCost = Mathf.Lerp(spellData.ManaCostRange.Min, spellData.ManaCostRange.Max, chargeRatio);

		if (!manaComponent.HasEnoughMana(manaCost))
		{
			CancelCharge();
			return;
		}
		
		Vector3 initialVelocity = CalculateInitialVelocity(Mathf.Lerp(spellData.SpeedRange.Min, spellData.SpeedRange.Max, chargeRatio));

		ProjectileLaunchData launchData = new ProjectileLaunchData
		{
			Caster = PlayerBody.Instance,
			ManaCost = manaCost,
			InitialVelocity = initialVelocity,
			ChargeRatio = chargeRatio,
			StartPosition = SpellOrigin,
			SpellData = spellData,
			Slot = slotType
		};

		audioPlayer_Spell.Play();
		projectileInstance.Launch(launchData);
		
		manaComponent.ConsumeMana(manaCost);
		
		if (slotType == SlotType.Alt && chargingProjectile != null)
		{
			chargingProjectile.QueueFree();
		}
		ResetChargeState();
	}

	private Vector3 CalculateInitialVelocity(float speed)
	{
		Vector3 projectileVelocity = -SpellOrigin.GlobalTransform.Basis.Z * speed;

		if (_equippedSpell.UsePlayerMomentum && GetOwner() is PlayerBody player)
		{
			Vector3 playerMomentum = player.Velocity.XZ();
			return projectileVelocity + playerMomentum;
		}

		return projectileVelocity;
	}

	private void CancelCharge()
	{
		if (chargingProjectile != null)
		{
			chargingProjectile.QueueFree();
		}
		ResetChargeState();
	}

	private void ResetChargeState()
	{
		_currentState = CasterState.Idle;
		SetProcess(false);
		
		chargingProjectile = null;
		currentChargeTime = 0f;
		lastInterval = -1;
		audioPlayer_ChargeBack.Stop();
		audioPlayer_ChargeBeep.Stop();
		
		PlayerBody.Instance.AllowMeleeAttack();
		PlayerBody.Instance.AllowSiphon();
	}
}