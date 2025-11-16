using Godot;
using System;
using System.Linq;
using Elythia;
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
	private RichTextLabel _timerLabel;
	private PlayerBody player; // PlayerBody instance will be set via SetPlayer method
	private float startingPlayerHealth;
	private float endingPlayerHealth;

	// --- Rewards ---
	[ExportGroup("Rewards")]
	[Export] private float moneyGivenPerSecondLeft = 5;
	[Export] private float moneyGivenPerHealthLeft = 10;
	private int moneyTimeBonus = 0;
	private int moneyHealthBonus = 0;

	// --- Spawning & Budget ---
	[ExportGroup("Spawning")]	[Export] private Dictionary<EnemyData, PackedScene> _enemyDataToSceneMap = new();
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
		if (DebugManager.Instance == null)
		{
			GD.PrintErr("WaveDirector: DebugManager.Instance is null in _Ready!");
		}
		else
		{
			DebugManager.Debug("WaveDirector: _Ready called.");
		}

		RoundTimer = GetNode<Timer>("%RoundTimer");
		_timerLabel = GetNode<RichTextLabel>("%TimerTextLabel");
		RoundTimer.Timeout += OnRoundLost;

		RoomManager.Instance.CurrentRoomChanged += OnCurrentRoomChanged;
		
		// Immediately handle the starting room if it's already set
		OnCurrentRoomChanged(RoomManager.Instance.CurrentRoom);
	}

	public void SetPlayer(PlayerBody playerInstance)
	{
		if (DebugManager.Instance == null)
		{
			GD.PrintErr("WaveDirector: DebugManager.Instance is null when setting player!");
		}

		if (playerInstance == null)
		{
			DebugManager.Error("WaveDirector: Attempted to set player with a null instance!");
			return;
		}
		player = playerInstance;
		player.HealthComponent.Died += OnRoundLost;
		DebugManager.Debug("WaveDirector: Player instance set and Died signal connected.");
	}

	public override void _Process(double delta)
	{
		if (IsRoundInProgress && RoundTimer != null)
		{
			var time = TimeSpan.FromSeconds(RoundTimer.TimeLeft);
			_timerLabel.Text = time.ToString(@"mm\:ss");
		}
		else if (_timerLabel.Text != "")
		{
			_timerLabel.Text = "";
		}
	}

	private void OnCurrentRoomChanged(LevelRoom newRoom)
	{
		DebugManager.Debug($"WaveDirector: OnCurrentRoomChanged - New Room: {newRoom?.Name ?? "null"}, IsRoundInProgress: {IsRoundInProgress}");
		if (_activeRoom != null)
		{
			_activeRoom.WaveCleared -= OnWaveCleared;
		}

		_activeRoom = newRoom;

		if (_activeRoom != null)
		{
			_activeRoom.WaveCleared += OnWaveCleared;
			// Prevent round from starting in central hub
			if (_activeRoom.IsCentralHub) // Assuming LevelRoom has an IsCentralHub property
			{
				DebugManager.Debug($"WaveDirector: Not starting round in central hub: {_activeRoom.Name}");
				// If a round was incorrectly marked as in progress, reset it
				if (IsRoundInProgress)
				{
					IsRoundInProgress = false;
					DebugManager.Debug("WaveDirector: Resetting IsRoundInProgress due to entering central hub.");
				}
				return;
			}

			// A new room has been entered, start the round if one isn't already running.
			if (!IsRoundInProgress)
			{
				OnRoundStart();
			}
		}
	}

	private void OnRoundStart()
	{
		DebugManager.Debug("WaveDirector: OnRoundStart called.");
		// Ensure player is not null before proceeding
		if (player == null)
		{
			DebugManager.Error("WaveDirector: Player instance is null. Cannot start round.");
			IsRoundInProgress = false; // Reset flag if player is null
			return;
		}

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
		DebugManager.Debug($"WaveDirector: StartNextWave - Budget: {budget}");
		var enemies = GenerateEnemyList(budget);
		DebugManager.Debug($"WaveDirector: StartNextWave - Generated {enemies.Count} enemies.");
		_activeRoom.StartSpawning(enemies);
	}

	private void OnWaveCleared()
	{
		TotalWavesCompleted++;
		_wavesCompletedThisRound++;
		DebugManager.Debug($"WaveDirector: OnWaveCleared - TotalWavesCompleted: {TotalWavesCompleted}, WavesCompletedThisRound: {_wavesCompletedThisRound}");
		
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

		var availableEnemies = _enemyDataToSceneMap
			.Select(pair => new { Data = pair.Key, Scene = pair.Value, Cost = pair.Key.BaseCost * enemyStrengthMultipliers[pair.Key.Rank] })
			.Where(e => e.Cost > 0)
			.ToList();

		if (!availableEnemies.Any())
		{
			GD.PrintErr("WaveDirector: No available enemies with a cost greater than 0!");
			return enemiesToSpawn;
		}

		// Attempt to fill the budget with a variety of enemies
		while (budget > 0 && availableEnemies.Any(e => e.Cost <= budget))
		{
			// Filter enemies that fit the remaining budget
			var affordableEnemies = availableEnemies.Where(e => e.Cost <= budget).ToList();
			if (!affordableEnemies.Any()) break; // No more affordable enemies

			// Randomly select an enemy from the affordable ones
			var chosenEnemy = affordableEnemies[GD.RandRange(0, affordableEnemies.Count - 1)];
			
			enemiesToSpawn.Add(chosenEnemy.Scene);
			budget -= chosenEnemy.Cost;
			DebugManager.Debug($"WaveDirector: GenerateEnemyList - Added {chosenEnemy.Data.Name} (Cost: {chosenEnemy.Cost}). Remaining budget: {budget}");
		}
		
		return enemiesToSpawn;
	}

	private void OnRoundTimerTimeout()
	{
		// Play timer alarm sound
	}
}