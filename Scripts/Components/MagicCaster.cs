namespace SpinalShatter;

using Godot;
using Elythia;
using System;

public partial class MagicCaster : Node
{
	private CastedSpellData _equippedSpell;
	private CastedSpellData _equippedAltFireSpell;
	private SlotType _activeCastingSlot = SlotType.Primary;

	[Export] private ManaComponent manaComponent;
	[Export] public Marker3D SpellOrigin { get; private set; }

	[ExportSubgroup("Audio", "_audio")] [Export]
	private AudioStreamPlayer3D audioPlayer_Spell;

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
	private CastedSpellData currentSpellData;

	public void SetPrimaryWeapon(CastedSpellData spellData)
	{
		_equippedSpell = spellData;

		sfxBeep = (AudioFile)_equippedSpell.AudioData["SpellChargeBeep"];
		sfxComplete = (AudioFile)_equippedSpell.AudioData["SpellChargeComplete"];
	}

	public void SetSecondaryWeapon(CastedSpellData spellData)
	{
		_equippedAltFireSpell = spellData;
	}

	private CastedSpellData GetActiveSpellData =>
		_activeCastingSlot switch
		{
			SlotType.Primary => _equippedSpell,
			SlotType.Secondary => _equippedAltFireSpell,
			_ => _equippedSpell // Default to primary if for some reason an unhandled SlotType is active
		};

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!CanShoot) return;

		switch (_currentState)
		{
			case CasterState.Idle:
				if (@event.IsActionPressed("Player_Shoot"))
				{
					BeginCharge(SlotType.Primary);
				}
				else if (@event.IsActionPressed("Player_AltFire"))
				{
					BeginCharge(SlotType.Secondary);
				}

				break;
			case CasterState.Charging:
				if (@event.IsActionReleased("Player_Shoot") && _activeCastingSlot == SlotType.Primary)
				{
					FireWeapon();
				}
				else if (@event.IsActionReleased("Player_AltFire") && _activeCastingSlot == SlotType.Secondary)
				{
					FireWeapon();
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

	private void BeginCharge(SlotType slot)
	{
		_activeCastingSlot = slot;
		currentSpellData = GetActiveSpellData;

		if (currentSpellData == null || chargingProjectile != null || currentSpellData.ProjectileScene == null ||
			manaComponent.CurrentMana < currentSpellData.ManaCostRange.Min)
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
		chargingProjectile = currentSpellData.ProjectileScene.Instantiate<Projectile>();

		AudioManager.Play(audioPlayer_ChargeBack, (AudioFile)currentSpellData.AudioData["SpellChargeBack"]);

		// Pass the FloatValueRange to BeginChargingProjectile to be used by Projectile.ApplyChargeAndTypeEffects
		chargingProjectile.BeginChargingProjectile(SpellOrigin, currentSpellData);
	}

	private void ContinueCharge(float delta)
	{
		if (currentSpellData == null || chargingProjectile == null) return;

		float maxChargeRatioByMana = Mathf.InverseLerp(currentSpellData.ManaCostRange.Min,
			currentSpellData.ManaCostRange.Max, manaComponent.CurrentMana);
		float maxChargeTimeByMana = maxChargeRatioByMana * currentSpellData.MaxChargeTime.Max;

		currentChargeTime += (float)delta;
		currentChargeTime = Mathf.Min(currentChargeTime, currentSpellData.MaxChargeTime.Max);
		currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTimeByMana);

		int currentInterval = 0;
		float intervalDuration = currentSpellData.MaxChargeTime.Max > 0
			? currentSpellData.MaxChargeTime.Max / currentSpellData.ChargeIntervals
			: 0;
		if (intervalDuration > 0)
		{
			currentInterval = Mathf.FloorToInt(currentChargeTime / intervalDuration);
		}

		currentInterval = Mathf.Clamp(currentInterval, 0, currentSpellData.ChargeIntervals);

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

	private void FireWeapon()
	{
		if (currentSpellData == null || chargingProjectile == null)
		{
			CancelCharge();
			return;
		}

		PlayerBody.Instance.PlayCastRelease();

		float chargeRatio = GetCurrentChargeRatio();

		float manaCost = Mathf.Lerp(currentSpellData.ManaCostRange.Min, currentSpellData.ManaCostRange.Max,
			chargeRatio);

		if (!manaComponent.HasEnoughMana(manaCost))
		{
			CancelCharge();
			return;
		}

		Vector3 initialVelocity = CalculateInitialVelocity(Mathf.Lerp(currentSpellData.SpeedRange.Min,
			currentSpellData.SpeedRange.Max, chargeRatio));

		ProjectileLaunchData launchData = new ProjectileLaunchData
		{
			Caster = PlayerBody.Instance,
			ManaCost = manaCost,
			InitialVelocity = initialVelocity,
			ChargeRatio = chargeRatio,
			StartPosition = SpellOrigin,
			SpellData = currentSpellData,
			Slot = _activeCastingSlot
		};

		audioPlayer_Spell.Play();
		chargingProjectile.Launch(launchData);

		manaComponent.ConsumeMana(manaCost);

		ResetChargeState();
	}

	private float GetCurrentChargeRatio()
	{
		if (currentSpellData == null || currentSpellData.MaxChargeTime.Max <= 0) return 0;

		float intervalDuration = currentSpellData.MaxChargeTime.Max / currentSpellData.ChargeIntervals;
		int intervalsCharged = Mathf.FloorToInt(currentChargeTime / intervalDuration);
		intervalsCharged = Mathf.Clamp(intervalsCharged, 0, currentSpellData.ChargeIntervals);
		return (float)intervalsCharged / currentSpellData.ChargeIntervals;
	}

	private Vector3 CalculateInitialVelocity(float speed)
	{
		if (currentSpellData == null) return Vector3.Zero;

		Vector3 projectileVelocity = -SpellOrigin.GlobalTransform.Basis.Z * speed;

		if (currentSpellData.UsePlayerMomentum && GetOwner() is PlayerBody player)
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
		_activeCastingSlot = SlotType.Primary; // Reset active casting slot

		PlayerBody.Instance.AllowMeleeAttack();
		PlayerBody.Instance.AllowSiphon();
	}
}