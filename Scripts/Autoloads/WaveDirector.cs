using Godot;
using System;
using System.Linq;
using Godot.Collections;

namespace SpinalShatter;

public partial class WaveDirector : Node
{
	public int CurrentRound { get; private set; } = 1;
	public DifficultyTier SelectedDifficulty = DifficultyTier.D2_Normal;
	public bool IsRoundStarted { get; private set; } = false;
	private Timer RoundTimer;

	private int waveCurrent;
	private int waveMax;

	[ExportGroup("Rewards")]
	[Export] private float moneyGivenPerSecondLeft = 5;
	[Export] private float moneyGivenPerHealthLeft = 10;

	private int moneyTimeBonus = 0;
	private int moneyHealthBonus = 0;

	private float enemySpawnCurrency;

	private PlayerBody player;
	private float startingPlayerHealth;
	private float endingPlayerHealth;

	[ExportGroup("Spawning")]
	[Export] private Array<EnemyData> availableEnemies = new();
	[Export] private float baseBudget = 50f;

	[ExportGroup("Difficulty")]
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

	[Export] private Dictionary<EnemyRank, float> enemyStrengthMultipliers = new()
	{
		{ EnemyRank.Rank1_Bone, 1 },
		{ EnemyRank.Rank2_Cloth, 2 },
		{ EnemyRank.Rank3_Iron, 3 },
		{ EnemyRank.Rank4_Obsidian, 4 }
	};
	public Dictionary<EnemyRank, float> EnemyStrengthMultipliers => enemyStrengthMultipliers;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RoundTimer = GetNode<Timer>("%RoundTimer");
		RoundTimer.Timeout += OnRoundLost;

		player = PlayerBody.Instance;
		player.HealthComponent.Died += OnRoundLost;
	}

	private float CalculateBudget()
    {
        float difficultyMultiplier = difficultyMultipliers[SelectedDifficulty];
        // Exponential growth for round scaling, similar to the old project
        float roundMultiplier = Mathf.Pow(1.15f, CurrentRound - 1);
        return baseBudget * roundMultiplier * difficultyMultiplier;
    }

	public Array<PackedScene> GenerateEnemyList()
	{
		float budget = CalculateBudget();
		var enemiesToSpawn = new Array<PackedScene>();

		if (availableEnemies.Count == 0)
		{
			GD.PrintErr("WaveDirector: No available enemies to spawn!");
			return enemiesToSpawn;
		}

		// Cast to IEnumerable<EnemyData> to use LINQ, then calculate cost and sort.
		var sortedEnemies = availableEnemies.Cast<EnemyData>()
			.Select(data => new { Data = data, Cost = data.BaseCost * enemyStrengthMultipliers[data.Rank] })
			.OrderByDescending(e => e.Cost)
			.ToList();
		
		// Greedy algorithm: Prioritize spending budget on the most expensive units first.
		foreach (var enemy in sortedEnemies)
		{
			if (enemy.Cost <= 0) continue;

			while (budget >= enemy.Cost)
			{
				enemiesToSpawn.Add(enemy.Data.Scene);
				budget -= enemy.Cost;
			}
		}

		return enemiesToSpawn;
	}

	private void OnRoundStart()
	{
		IsRoundStarted = true;
		startingPlayerHealth = player.HealthComponent.CurrentPercent;
		RoundTimer.Start();

		// 1. Get the list of enemies to spawn
		var enemiesToSpawn = GenerateEnemyList();
		
		// 2. Get the current LevelRoom
		LevelRoom activeRoom = RoomManager.Instance.CurrentRoom;

		if (activeRoom == null)
		{
			GD.PrintErr("WaveDirector: No active room found to start spawning!");
			return;
		}
		
		// 3. Tell the room to start spawning
		activeRoom.StartSpawning(enemiesToSpawn);
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

		CurrentRound++;
	}

	private void OnRoundTimerTimeout()
	{
		// Play timer alarm sound
		// MAYBE: temporarily disconnect timer?
	}
}