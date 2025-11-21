using Godot;
using Elythia;

namespace SpinalShatter;

public partial class MagicCaster : Node
{
	[Export] private PackedScene projectileScene;
	[Export] private ManaComponent manaComponent;
	[Export] private Marker3D spellOrigin;

	[ExportSubgroup("Audio", "_audio")]
	[Export] private AudioData AudioData;
	[Export] private AudioStreamPlayer3D audioPlayer_Spell;
	[Export] private AudioStreamPlayer3D audioPlayer_ChargeBack;
	[Export] private AudioStreamPlayer3D audioPlayer_ChargeBeep;

	[ExportGroup("Charging")]
	[Export] private float maxChargeTime = 2.0f;

	[Export] private FloatValueRange ManaCostRange;
	[Export] private FloatValueRange DamageRange;
	[Export] private FloatValueRange SpeedRange;
	[Export] private FloatValueRange SizeRange;

	[Export(PropertyHint.Range, "1,16,1")] private int chargeIntervals = 8;

	[Export] private bool usePlayerMomentum = false;

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
		sfxBeep = (AudioFile)AudioData["SpellChargeBeep"];
		sfxComplete = (AudioFile)AudioData["SpellChargeComplete"];
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("Player_Shoot"))
		{
			OnPressCharge();
		}
		else if (@event.IsActionReleased("Player_Shoot"))
		{
			OnReleaseCharge();
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
		if (chargingProjectile != null || projectileScene == null || manaComponent.CurrentMana < ManaCostRange.Min)
		{
			return;
		}

		IsCharging = true;

		PlayerBody.Instance.PlayCastCharge();
		PlayerBody.Instance.DisallowSiphon();
		PlayerBody.Instance.DisallowMeleeAttack();

		currentChargeTime = 0f;
		lastInterval = -1;
		chargingProjectile = projectileScene.Instantiate<Projectile>();

		AudioManager.Play(audioPlayer_ChargeBack, AudioData["SpellChargeBack"]);

		chargingProjectile.BeginChargingProjectile(spellOrigin, SizeRange);
	}

	private void ContinueCharge(float delta)
	{
		if (!CanShoot) return;
		if (chargingProjectile == null) return;

		// 1. Calculate max possible charge ratio based on current mana
		float maxChargeRatioByMana = Mathf.InverseLerp(ManaCostRange.Min, ManaCostRange.Max, manaComponent.CurrentMana);
		float maxChargeTimeByMana = maxChargeRatioByMana * maxChargeTime;

		// 2. Increment charge time, clamping by various limits
		currentChargeTime += delta;
		currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTime);
		currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTimeByMana);

		// 3. Determine current charge interval
		int currentInterval = 0;
		float intervalDuration = maxChargeTime > 0 ? maxChargeTime / chargeIntervals : 0;
		if (intervalDuration > 0)
		{
			currentInterval = Mathf.FloorToInt(currentChargeTime / intervalDuration);
		}
		currentInterval = Mathf.Clamp(currentInterval, 0, chargeIntervals);

		float chargeRatio = 0;
		// 4. Update visuals and audio only when the interval changes
		if (currentInterval != lastInterval)
		{
			lastInterval = currentInterval;
			chargeRatio = chargeIntervals > 0 ? (float)currentInterval / chargeIntervals : 0;

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
		if (!CanShoot) return;
		if (chargingProjectile == null) return;

		IsCharging = false;

		PlayerBody.Instance.PlayCastRelease();

		float intervalDuration = maxChargeTime > 0 ? maxChargeTime / chargeIntervals : 0;

		int intervalsCharged = Mathf.FloorToInt(currentChargeTime / intervalDuration);
		intervalsCharged = Mathf.Clamp(intervalsCharged, 0, chargeIntervals);

		float chargeRatio = (float)intervalsCharged / chargeIntervals;
		float manaCost = Mathf.Lerp(ManaCostRange.Min, ManaCostRange.Max, chargeRatio);

		if (!manaComponent.HasEnoughMana(manaCost))
		{
			chargingProjectile.QueueFree();
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
			DamageGrowthConstant = maxChargeTime,
			AbsoluteMaxProjectileSpeed = SpeedRange.Max,
			MaxInitialManaCost = ManaCostRange.Max,
			StartPosition = spellOrigin,
			SizingScale = SizeRange,
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

		if (usePlayerMomentum && GetOwner() is PlayerBody player)
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