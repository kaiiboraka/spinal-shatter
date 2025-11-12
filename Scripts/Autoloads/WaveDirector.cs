using Godot;
using System;
using Godot.Collections;

namespace SpinalShatter;

public partial class WaveDirector : Node
{
	public DifficultyTier SelectedDifficulty = DifficultyTier.D2_Normal;
	public bool IsRoundStarted { get; private set; } = false;
	private Timer RoundTimer;

	private int waveCurrent;
	private int waveMax;

	[Export] private float moneyGivenPerSecondLeft = 5;
	[Export] private float moneyGivenPerHealthLeft = 10;

	private int moneyTimeBonus = 0;
	private int moneyHealthBonus = 0;

	private float enemySpawnCurrency;

	private PlayerBody player;
	private float startingPlayerHealth;
	private float endingPlayerHealth;

	[Export] private Dictionary<DifficultyTier, float> difficultyMultipliers = new()
	{
		{ DifficultyTier.D0_Braindead, .5f },
		{ DifficultyTier.D1_Easy, .75f },
		{ DifficultyTier.D2_Normal, 1.0f },
		{ DifficultyTier.D3_Hard, 2.0f },
		{ DifficultyTier.D4_Expert, 4.0f },
		{ DifficultyTier.D5_Brutal, 10.0f }
	};
	public Dictionary<DifficultyTier, float> DifficultyMultipliers => difficultyMultipliers;

	[Export] private Dictionary<EnemyStrengthTier, float> enemyStrengthMultipliers = new()
	{
		{ EnemyStrengthTier.Tier1_Bone, 1 },
		{ EnemyStrengthTier.Tier2_Cloth, 2 },
		{ EnemyStrengthTier.Tier3_Iron, 3 },
		{ EnemyStrengthTier.Tier4_Obsidian, 4 }
	};
	public Dictionary<EnemyStrengthTier, float> EnemyStrengthMultipliers => enemyStrengthMultipliers;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RoundTimer = GetNode<Timer>("%RoundTimer");
		RoundTimer.Timeout += OnRoundLost;

		player = PlayerBody.Instance;
		player.HealthComponent.Died += OnRoundLost;
	}

	private void OnRoundStart()
	{
		IsRoundStarted = true;
		startingPlayerHealth = player.HealthComponent.CurrentPercent;
		RoundTimer.Start();
	}

	private void OnRoundEnd()
	{
		IsRoundStarted = false;
		endingPlayerHealth = player.HealthComponent.CurrentPercent;
	}

	public void OnRoundLost()
	{
		OnRoundEnd();

		RoundTimer.Stop();
	}

	private void OnRoundWon()
	{
		OnRoundEnd();

		RoundTimer.Paused = true;

		moneyTimeBonus = (int)(moneyGivenPerSecondLeft * RoundTimer.TimeLeft * DifficultyMultipliers[SelectedDifficulty]);
		player.AddMoney(moneyTimeBonus);

		moneyHealthBonus = (int)(moneyGivenPerHealthLeft * endingPlayerHealth * DifficultyMultipliers[SelectedDifficulty]);
		player.AddMoney(moneyHealthBonus);
	}

	private void OnRoundTimerTimeout()
	{
		// Play timer alarm sound
		// MAYBE: temporarily disconnect timer?
	}
}