using Godot;
using Elythia;

namespace SpinalShatter;

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

	private float currentChargeTime = 0f;
	private Projectile chargingProjectile = null;

	public bool IsCharging { get; private set; } = false;

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
		if (EquippedSpell == null) return;
		
		if (@event.IsActionPressed("Player_Shoot"))
		{
			OnPressCharge();
		}
		else if (@event.IsActionReleased("Player_Shoot"))
		{
			OnReleaseCharge();
		}
		else if (@event.IsActionPressed("Player_AltFire"))
		{
			OnAltFire();
		}

		SetProcess(IsCharging);
	}

	public override void _Process(double delta)
	{
		if (IsCharging)
		{
			ContinueCharge((float)delta);
		}
	}

	private void OnPressCharge()
	{
		if (!CanShoot)
		{
			IsCharging = false;
			return;
		}
		if (chargingProjectile != null || EquippedSpell.ProjectileScene == null || manaComponent.CurrentMana < EquippedSpell.ManaCostRange.Min)
		{
			return;
		}

		IsCharging = true;

		PlayerBody.Instance.PlayCastCharge();
		PlayerBody.Instance.DisallowSiphon();
		PlayerBody.Instance.DisallowMeleeAttack();

		currentChargeTime = 0f;
		lastInterval = -1;
		chargingProjectile = EquippedSpell.ProjectileScene.Instantiate<Projectile>();

		AudioManager.Play(audioPlayer_ChargeBack, (AudioFile)EquippedSpell.AudioData["SpellChargeBack"]);

		chargingProjectile.BeginChargingProjectile(spellOrigin, EquippedSpell.SizeRange);
	}

	private void ContinueCharge(float delta)
	{
		if (!CanShoot || chargingProjectile == null) return;

		// 1. Calculate max possible charge ratio based on current mana
		float maxChargeRatioByMana = Mathf.InverseLerp(EquippedSpell.ManaCostRange.Min, EquippedSpell.ManaCostRange.Max, manaComponent.CurrentMana);
		float maxChargeTimeByMana = maxChargeRatioByMana * EquippedSpell.MaxChargeTime;

		// 2. Increment charge time, clamping by various limits
		currentChargeTime += delta;
		currentChargeTime = Mathf.Min(currentChargeTime, EquippedSpell.MaxChargeTime);
		currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTimeByMana);

		// 3. Determine current charge interval
		int currentInterval = 0;
		float intervalDuration = EquippedSpell.MaxChargeTime > 0 ? EquippedSpell.MaxChargeTime / EquippedSpell.ChargeIntervals : 0;
		if (intervalDuration > 0)
		{
			currentInterval = Mathf.FloorToInt(currentChargeTime / intervalDuration);
		}
		currentInterval = Mathf.Clamp(currentInterval, 0, EquippedSpell.ChargeIntervals);

		float chargeRatio = 0;
		// 4. Update visuals and audio only when the interval changes
		if (currentInterval != lastInterval)
		{
			lastInterval = currentInterval;
			chargeRatio = EquippedSpell.ChargeIntervals > 0 ? (float)currentInterval / EquippedSpell.ChargeIntervals : 0;

			float size = Mathf.Lerp(0.1f, 1.2f, chargeRatio);

			sfxBeep.PitchScale = size * 4 / 6f;

			if (currentInterval >= 1)
			{
				AudioManager.Play(audioPlayer_ChargeBeep, sfxBeep);
				audioPlayer_ChargeBeep.Play();
			}
			chargingProjectile.Charge = chargeRatio;
			chargingProjectile.UpdateChargeState();
		}

		if (chargeRatio >= 1.0f)
		{
			if (audioPlayer_ChargeBeep.IsPlaying()) audioPlayer_ChargeBeep.Stop();
			AudioManager.Play(audioPlayer_ChargeBeep, sfxComplete);
		}
	}

	private void OnReleaseCharge()
	{
		if (!CanShoot || chargingProjectile == null) return;
		
		switch (EquippedSpell.PrimaryFire)
		{
			case PrimaryFireType.ChargedProjectile:
				FireChargedProjectile();
				break;
			default:
				GD.PrintErr($"Primary fire type '{EquippedSpell.PrimaryFire}' not implemented.");
				break;
		}

		PlayerBody.Instance.AllowMeleeAttack();
		PlayerBody.Instance.AllowSiphon();
	}
	
	private void OnAltFire()
	{
		if (EquippedSpell.AltFire == AltFireType.None) return;
		
		// Future implementation
		GD.Print($"Alt-fire triggered for {EquippedSpell.Name}");
	}

	private void FireChargedProjectile()
	{
		IsCharging = false;
		PlayerBody.Instance.PlayCastRelease();

		float intervalDuration = EquippedSpell.MaxChargeTime > 0 ? EquippedSpell.MaxChargeTime / EquippedSpell.ChargeIntervals : 0;

		int intervalsCharged = Mathf.FloorToInt(currentChargeTime / intervalDuration);
		intervalsCharged = Mathf.Clamp(intervalsCharged, 0, EquippedSpell.ChargeIntervals);

		float chargeRatio = (float)intervalsCharged / EquippedSpell.ChargeIntervals;
		float manaCost = Mathf.Lerp(EquippedSpell.ManaCostRange.Min, EquippedSpell.ManaCostRange.Max, chargeRatio);

		if (!manaComponent.HasEnoughMana(manaCost))
		{
			chargingProjectile.QueueFree();
			ResetChargeState();
			return;
		}

		float damage = Mathf.Lerp(EquippedSpell.DamageRange.Min, EquippedSpell.DamageRange.Max, chargeRatio);
		float speed = Mathf.Lerp(EquippedSpell.SpeedRange.Min, EquippedSpell.SpeedRange.Max, chargeRatio);

		Vector3 initialVelocity = CalculateInitialVelocity(speed);
		ProjectileLaunchData launchData = new ProjectileLaunchData
		{
			Caster = PlayerBody.Instance,
			Damage = damage,
			ManaCost = manaCost,
			InitialVelocity = initialVelocity,
			ChargeRatio = chargeRatio,
			DamageGrowthConstant = EquippedSpell.MaxChargeTime,
			AbsoluteMaxProjectileSpeed = EquippedSpell.SpeedRange.Max,
			MaxInitialManaCost = EquippedSpell.ManaCostRange.Max,
			StartPosition = spellOrigin,
			SizingScale = EquippedSpell.SizeRange,
		};

		audioPlayer_Spell.Play();
		chargingProjectile.Launch(launchData);
		manaComponent.ConsumeMana(manaCost);
		ResetChargeState();

		PlayerBody.Instance.AllowMeleeAttack();
		PlayerBody.Instance.AllowSiphon();
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

	private void ResetChargeState()
	{
		chargingProjectile = null;
		currentChargeTime = 0f;
		lastInterval = -1;
		audioPlayer_ChargeBack.Stop();
		audioPlayer_ChargeBeep.Stop();
	}
}