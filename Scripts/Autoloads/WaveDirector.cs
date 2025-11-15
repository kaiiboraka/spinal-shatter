using Godot;
using System;
using System.Linq;
using Godot.Collections;

namespace SpinalShatter;

public partial class WaveDirector : Node
{
	// --- State ---
	public int CurrentRound { get; private set; } = 1;
	public int TotalWavesCompleted { get; private set; } = 0;
	public DifficultyTier SelectedDifficulty = DifficultyTier.D2_Normal;
	public bool IsRoundInProgress { get; private set; } = false;
	
	private int _wavesCompletedThisRound = 0;
	private LevelRoom _activeRoom;
	
	// --- Timers & Player ---
	private Timer RoundTimer;
	private PlayerBody player;
	private float startingPlayerHealth;
	private float endingPlayerHealth;

	// --- Rewards ---
	[ExportGroup("Rewards")]
	[Export] private float moneyGivenPerSecondLeft = 5;
	[Export] private float moneyGivenPerHealthLeft = 10;
	private int moneyTimeBonus = 0;
	private int moneyHealthBonus = 0;

	// --- Spawning & Budget ---
	[ExportGroup("Spawning")]
	[Export] private Dictionary<EnemyData, PackedScene> _enemyDataToSceneMap = new();
	[Export] private int wavesPerRound = 3;
	[Export(PropertyHint.ExpEasing)] private float baseBudget = 50f;
	[Export(PropertyHint.ExpEasing)] private float budgetIncreasePerWave = 10f;
	[Export(PropertyHint.ExpEasing)] private float budgetIncreasePerRound = 40f;


	// --- Difficulty Multipliers ---
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

	public override void _Ready()
	{
		RoundTimer = GetNode<Timer>("%RoundTimer");
		RoundTimer.Timeout += OnRoundLost;

		player = PlayerBody.Instance;
		player.HealthComponent.Died += OnRoundLost;

		RoomManager.Instance.CurrentRoomChanged += OnCurrentRoomChanged;
		// Immediately handle the starting room if it's already set
		OnCurrentRoomChanged(RoomManager.Instance.CurrentRoom);
	}

	private void OnCurrentRoomChanged(LevelRoom newRoom)
	{
		if (_activeRoom != null)
		{
			_activeRoom.WaveCleared -= OnWaveCleared;
		}

		_activeRoom = newRoom;

		if (_activeRoom != null)
		{
			_activeRoom.WaveCleared += OnWaveCleared;
			// A new room has been entered, start the round.
			OnRoundStart();
		}
	}

	private void OnRoundStart()
	{
		IsRoundInProgress = true;
		_wavesCompletedThisRound = 0;
		startingPlayerHealth = player.HealthComponent.CurrentPercent;
		RoundTimer.Start();
		
		StartNextWave();
	}

	private void StartNextWave()
	{
		if (_wavesCompletedThisRound >= wavesPerRound)
		{
			OnRoundWon();
			return;
		}
		
		var budget = CalculateBudget();
		var enemies = GenerateEnemyList(budget);
		_activeRoom.StartSpawning(enemies);
	}

	private void OnWaveCleared()
	{
		TotalWavesCompleted++;
		_wavesCompletedThisRound++;
		
		// Small delay before starting the next wave
		GetTree().CreateTimer(2.0f).Timeout += StartNextWave;
	}

	private void OnRoundWon()
	{
		IsRoundInProgress = false;
		endingPlayerHealth = player.HealthComponent.CurrentPercent;
		RoundTimer.Paused = true;

		moneyTimeBonus = (int)(moneyGivenPerSecondLeft * RoundTimer.TimeLeft * DifficultyMultipliers[SelectedDifficulty]);
		player.AddMoney(moneyTimeBonus);

		moneyHealthBonus = (int)(moneyGivenPerHealthLeft * endingPlayerHealth * DifficultyMultipliers[SelectedDifficulty]);
		player.AddMoney(moneyHealthBonus);

		CurrentRound++;
		GD.Print($"Round {CurrentRound - 1} won! Starting next round.");
		// The room's doors should now open, etc.
	}
	
	public void OnRoundLost()
	{
		IsRoundInProgress = false;
		endingPlayerHealth = player.HealthComponent.CurrentPercent;
		RoundTimer.Stop();
		GD.Print("Round Lost!");
	}

	private float CalculateBudget()
    {
        float difficultyMultiplier = difficultyMultipliers[SelectedDifficulty];
		float roundBonus = (CurrentRound - 1) * budgetIncreasePerRound;
		float waveBonus = TotalWavesCompleted * budgetIncreasePerWave;
        
        return (baseBudget + roundBonus + waveBonus) * difficultyMultiplier;
    }

	private Array<PackedScene> GenerateEnemyList(float budget)
	{
		var enemiesToSpawn = new Array<PackedScene>();
		if (_enemyDataToSceneMap.Count == 0)
		{
			GD.PrintErr("WaveDirector: No available enemies to spawn!");
			return enemiesToSpawn;
		}

		var sortedEnemies = _enemyDataToSceneMap
			.Select(pair => new { Data = pair.Key, Scene = pair.Value, Cost = pair.Key.BaseCost * enemyStrengthMultipliers[pair.Key.Rank] })
			.OrderByDescending(e => e.Cost)
			.ToList();
		
		foreach (var enemy in sortedEnemies)
		{
			if (enemy.Cost <= 0) continue;
			while (budget >= enemy.Cost)
			{
				enemiesToSpawn.Add(enemy.Scene);
				budget -= enemy.Cost;
			}
		}
		return enemiesToSpawn;
	}

	private void OnRoundTimerTimeout()
	{
		// Play timer alarm sound
	}
}