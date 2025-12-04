namespace SpinalShatter;

using Godot;
using Elythia;
using System;

public partial class MagicCaster : Node
{
	[Export] public SpellData EquippedSpell { get; private set; }
	[Export] private ManaComponent manaComponent;
	[Export] private Marker3D spellOrigin;

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

	public override void _Ready()
	{
		base._Ready();
		if (EquippedSpell == null || EquippedSpell.AudioData == null)
		{
			GD.PrintErr($"MagicCaster: EquippedSpell or its AudioData is not set!");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}
		sfxBeep = (AudioFile)EquippedSpell.AudioData["SpellChargeBeep"];
		sfxComplete = (AudioFile)EquippedSpell.AudioData["SpellChargeComplete"];
	}

	public override void _Input(InputEvent @event)
	{
		if (EquippedSpell == null || !CanShoot) return;

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
		if (chargingProjectile != null || EquippedSpell.ProjectileScene == null || manaComponent.CurrentMana < EquippedSpell.ManaCostRange.Min)
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
		chargingProjectile = EquippedSpell.ProjectileScene.Instantiate<Projectile>();

		AudioManager.Play(audioPlayer_ChargeBack, (AudioFile)EquippedSpell.AudioData["SpellChargeBack"]);

		// Pass the FloatValueRange to BeginChargingProjectile to be used by Projectile.ApplyChargeAndTypeEffects
		chargingProjectile.BeginChargingProjectile(spellOrigin, EquippedSpell);
	}

	private void ContinueCharge(float delta)
	{
		if (chargingProjectile == null) return;

		float maxChargeRatioByMana = Mathf.InverseLerp(EquippedSpell.ManaCostRange.Min, EquippedSpell.ManaCostRange.Max, manaComponent.CurrentMana);
		float maxChargeTimeByMana = maxChargeRatioByMana * EquippedSpell.MaxChargeTime;

		currentChargeTime += (float)delta;
		currentChargeTime = Mathf.Min(currentChargeTime, EquippedSpell.MaxChargeTime);
		currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTimeByMana);

		int currentInterval = 0;
		float intervalDuration = EquippedSpell.MaxChargeTime > 0 ? EquippedSpell.MaxChargeTime / EquippedSpell.ChargeIntervals : 0;
		if (intervalDuration > 0)
		{
			currentInterval = Mathf.FloorToInt(currentChargeTime / intervalDuration);
		}
		currentInterval = Mathf.Clamp(currentInterval, 0, EquippedSpell.ChargeIntervals);

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
		FireWeapon(SlotType.Primary, EquippedSpell.ProjectileScene, chargingProjectile);
	}

	private void FireAltFire()
	{
		if (EquippedSpell.AltFireProjectileScene == null)
		{
			CancelCharge();
			return;
		}

		if (!manaComponent.HasEnoughMana(EquippedSpell.AltFireManaCost))
		{
			CancelCharge();
			return;
		}
		
		var altProjectile = EquippedSpell.AltFireProjectileScene.Instantiate<Projectile>();
		FireWeapon(SlotType.Alt, EquippedSpell.AltFireProjectileScene, altProjectile);
	}

	private float GetCurrentChargeRatio()
	{
		if (EquippedSpell.MaxChargeTime <= 0) return 0;
		
		float intervalDuration = EquippedSpell.MaxChargeTime / EquippedSpell.ChargeIntervals;
		int intervalsCharged = Mathf.FloorToInt(currentChargeTime / intervalDuration);
		intervalsCharged = Mathf.Clamp(intervalsCharged, 0, EquippedSpell.ChargeIntervals);
		return (float)intervalsCharged / EquippedSpell.ChargeIntervals;
	}

	private void FireWeapon(SlotType slotType, PackedScene projectileScene, Projectile projectileInstance)
	{
		PlayerBody.Instance.PlayCastRelease();

		float chargeRatio = GetCurrentChargeRatio();
		
		float manaCost = (slotType == SlotType.Primary)
			? Mathf.Lerp(EquippedSpell.ManaCostRange.Min, EquippedSpell.ManaCostRange.Max, chargeRatio)
			: EquippedSpell.AltFireManaCost;

		if (!manaComponent.HasEnoughMana(manaCost))
		{
			CancelCharge();
			return;
		}
		
		Vector3 initialVelocity = CalculateInitialVelocity(Mathf.Lerp(EquippedSpell.SpeedRange.Min, EquippedSpell.SpeedRange.Max, chargeRatio));

		ProjectileLaunchData launchData = new ProjectileLaunchData
		{
			Caster = PlayerBody.Instance,
			ManaCost = manaCost,
			InitialVelocity = initialVelocity,
			ChargeRatio = chargeRatio,
			StartPosition = spellOrigin,
			SpellData = EquippedSpell,
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
		Vector3 projectileVelocity = -spellOrigin.GlobalTransform.Basis.Z * speed;

		if (EquippedSpell.UsePlayerMomentum && GetOwner() is PlayerBody player)
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