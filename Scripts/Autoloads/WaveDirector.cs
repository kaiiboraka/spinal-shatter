using Godot;
using System;
using System.Linq;
using System.Text;
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
	private int _enemiesThisWave = 0;
	private LevelRoom _activeRoom;
	private LevelRoom _roundInProgressRoom;

	// --- Timers & Player ---
	private Timer RoundTimer;
	private PlayerBody player; // PlayerBody instance will be set via SetPlayer method
	private float startingPlayerHealth;
	private float endingPlayerHealth;

	// --- UI References ---
	private Control _directorDisplay;
	private RichTextLabel _timerLabel;
	private MinMaxValuesLabel _waveMinMaxLabel;
	private RichTextLabel _roundTextValue;
	private RichTextLabel _activeEnemyCountTextValue;
	private HBoxContainer _activeEnemyCountHBoxContainer; // Added
	private VBoxContainer _roomLabelsVBoxContainer; // Added
	private readonly Godot.Collections.Dictionary<string, RichTextLabel> _roomEnemyLabels = new();
	private RichTextLabel _victoryLabel;
	private RichTextLabel _defeatLabel;
	private VBoxContainer _waveRoundContainer;
	private VBoxContainer _bonusContainer;
	private RichTextLabel _timeBonusTextValue;
	private RichTextLabel _lifeBonusTextValue;

	[ExportGroup("Menus")]
	[Export] private PackedScene _levelLostMenuScene;

	// --- Rewards ---
	[ExportGroup("Rewards")]
	[Export] private float moneyGivenPerSecondLeft = 5;

	[Export] private float moneyGivenPerHealthLeft = 10;
	private int moneyTimeBonus = 0;
	private int moneyHealthBonus = 0;

	// --- Spawning & Budget ---
	[ExportGroup("Spawning")] [Export] private Dictionary<EnemyData, PackedScene> _enemyDataToSceneMap = new();
	[Export] private int wavesPerRound = 3;
	[Export(PropertyHint.ExpEasing)] private float baseBudget = 10f;
	[Export(PropertyHint.ExpEasing)] private float budgetIncreasePerWave = 2f;
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

		// --- Initialize UI References ---
		_directorDisplay = GetNode<Control>("DirectorDisplay");
		_timerLabel = GetNode<RichTextLabel>("%TimerTextLabel");
		_waveMinMaxLabel = GetNode<MinMaxValuesLabel>("%Wave_MinMaxValuesLabel");
		_roundTextValue = GetNode<RichTextLabel>("%RoundTextValue");
		_activeEnemyCountTextValue = GetNode<RichTextLabel>("DirectorDisplay/MarginContainer/Objective_VBoxContainer/ActiveEnemyCount_HBoxContainer/ActiveEnemyCountTextValue");
		_activeEnemyCountHBoxContainer = GetNode<HBoxContainer>("%ActiveEnemyCount_HBoxContainer"); // Added
		_roomLabelsVBoxContainer = GetNode<VBoxContainer>("%RoomLabels_VBoxContainer"); // Added
		_victoryLabel = GetNode<RichTextLabel>("%VictoryLabel");
		_defeatLabel = GetNode<RichTextLabel>("%DefeatLabel");
		_waveRoundContainer = GetNode<VBoxContainer>("DirectorDisplay/MarginContainer/WaveRound_Container");
		_bonusContainer = GetNode<VBoxContainer>("DirectorDisplay/MarginContainer/Bonus_VBoxContainer");
		_timeBonusTextValue = GetNode<RichTextLabel>("%TimeBonus_TextValue");
		_lifeBonusTextValue = GetNode<RichTextLabel>("%LifeBonus_TextValue");

		_roomEnemyLabels["North"] = GetNode<RichTextLabel>("%NorthTextValue");
		_roomEnemyLabels["East"] = GetNode<RichTextLabel>("%EastTextValue");
		_roomEnemyLabels["South"] = GetNode<RichTextLabel>("%SouthTextValue");
		_roomEnemyLabels["West"] = GetNode<RichTextLabel>("%WestTextValue");
		// --- End UI Initialization ---

		RoomManager.Instance.CurrentRoomChanged += OnCurrentRoomChanged;

		// Immediately handle the starting room if it's already set
		OnCurrentRoomChanged(RoomManager.Instance.CurrentRoom);
		
		// Initial UI state
		ResetAllUI();
	}

	public void SetPlayer(PlayerBody playerInstance)
	{
		if (playerInstance == null)
		{
			DebugManager.Error("WaveDirector: Attempted to set player with a null instance!");
			return;
		}

		player = playerInstance;
		player.HealthComponent.Died += OnRoundLost;
		player.PlayerDied += OnPlayerDiedSignalReceived;

		// DebugManager.Debug("WaveDirector: Player instance set and Died signal connected.");
	}

	public override void _Process(double delta)
	{
		if (IsRoundInProgress && RoundTimer != null)
		{
			var time = TimeSpan.FromSeconds(RoundTimer.TimeLeft);
			int currentWaveNumber = _wavesCompletedThisRound + 1;
			
			// Update Timer
			_timerLabel.Text = time.ToString(@"mm\:ss");
			
			// Update Wave MinMaxValuesLabel
			_waveMinMaxLabel.TextCurrent = (_wavesCompletedThisRound >= wavesPerRound ? wavesPerRound : currentWaveNumber).ToString();
			_waveMinMaxLabel.TextMaximum = wavesPerRound.ToString();

			// Update Round Text Value
			_roundTextValue.Text = CurrentRound.ToString();
			
			// Update Active Room Enemy Count
			_activeEnemyCountTextValue.Text = _activeRoom?.EnemyCount.ToString() ?? "0";

			// Update Room-specific Breakdown
			var combatRooms = GetTree().GetNodesInGroup("CombatRooms").Cast<LevelRoom>();
			foreach (var room in combatRooms)
			{
				if (_roomEnemyLabels.TryGetValue(room.Name, out var label))
				{
					int totalEnemiesForWave = (IsRoundInProgress && room == _roundInProgressRoom) ? _enemiesThisWave : 0;
					label.Text = $"{room.EnemyCount} / {totalEnemiesForWave}";
				}
			}
		}
		else // Round is not in progress
		{
			ResetAllUI();
		}
	}

	private void OnCurrentRoomChanged(LevelRoom newRoom)
	{
		// DebugManager.Debug($"WaveDirector: OnCurrentRoomChanged - New Room: {newRoom?.Name ?? "null"}, IsRoundInProgress: {IsRoundInProgress}");
		if (_activeRoom != null)
		{
			_activeRoom.WaveCleared -= OnWaveCleared;
		}

		_activeRoom = newRoom;

		if (_activeRoom != null)
		{
			_activeRoom.WaveCleared += OnWaveCleared;

			// Only start a new round if the new room is a combat room AND no round is currently in progress anywhere.
			if (!_activeRoom.IsCentralHub && !IsRoundInProgress)
			{
				OnRoundStart();
			}
		}
	}

	private void OnRoundStart()
	{
		_wavesCompletedThisRound = -1;
		_roundInProgressRoom = _activeRoom;

		// DebugManager.Debug("WaveDirector: OnRoundStart called.");
		// Ensure player is not null before proceeding
		if (player == null)
		{
			DebugManager.Error("WaveDirector: Player instance is null. Cannot start round.");
			IsRoundInProgress = false; // Reset flag if player is null
			return;
		}

		IsRoundInProgress = true;
		startingPlayerHealth = player.HealthComponent.CurrentPercent;
		RoundTimer.Start();
		StartNextWave();
		_wavesCompletedThisRound = 0;
		
		// UI Visibility for Round Start
		_timerLabel.Visible = true;
		_activeEnemyCountTextValue.Visible = true;
		_activeEnemyCountHBoxContainer.Visible = true; // Added
		_roomLabelsVBoxContainer.Visible = true; // Added
		_waveRoundContainer.Visible = true;
	}

	private void StartNextWave()
	{
		if (_wavesCompletedThisRound >= wavesPerRound)
		{
			OnRoundWon();
			return;
		}

		var budget = CalculateBudget();

		// DebugManager.Debug($"WaveDirector: StartNextWave - Budget: {budget}");
		var enemies = GenerateEnemyList(budget);

		// DebugManager.Debug($"WaveDirector: StartNextWave - Generated {enemies.Count} enemies.");
		_enemiesThisWave = enemies.Count;
		_activeRoom.StartSpawning(enemies);
	}

	private void OnWaveCleared()
	{
		TotalWavesCompleted++;
		_wavesCompletedThisRound++;

		// DebugManager.Debug($"WaveDirector: OnWaveCleared - TotalWavesCompleted: {TotalWavesCompleted}, WavesCompletedThisRound: {_wavesCompletedThisRound}");

		// Small delay before starting the next wave
		GetTree().CreateTimer(3.0f).Timeout += StartNextWave;
	}

	private void OnRoundWon()
	{
		// ResetAllUI(); // Added
		IsRoundInProgress = false;
		_roundInProgressRoom = null;
		
		endingPlayerHealth = player.HealthComponent.CurrentHealth;

		double timeLeft = RoundTimer.TimeLeft;
		RoundTimer.Stop();

		moneyTimeBonus = (int)(moneyGivenPerSecondLeft * timeLeft * DifficultyMultipliers[SelectedDifficulty]);
		player.AddMoney(moneyTimeBonus);

		moneyHealthBonus =
			(int)(moneyGivenPerHealthLeft * endingPlayerHealth * DifficultyMultipliers[SelectedDifficulty]);
		player.AddMoney(moneyHealthBonus);

		CurrentRound++;
		if (wavesPerRound < 5)
		{
			wavesPerRound++;
		}
		
		GD.Print($"Round {CurrentRound - 1} won! Starting next round.");

		// UI Visibility for Round Won
		_victoryLabel.Visible = true;
		_defeatLabel.Visible = false; // Ensure defeat label is hidden
		_bonusContainer.Visible = true;
		_timerLabel.Visible = false;
		_activeEnemyCountTextValue.Visible = false;
		_activeEnemyCountHBoxContainer.Visible = false; // Added
		_roomLabelsVBoxContainer.Visible = false; // Added
		_waveRoundContainer.Visible = false;

		_timeBonusTextValue.Text = moneyTimeBonus.ToString();
		_lifeBonusTextValue.Text = moneyHealthBonus.ToString();

		// The room's doors should now open, etc.
	}

	public void OnRoundLost()
	{
		ResetAllUI(); // Added
		IsRoundInProgress = false;
		_roundInProgressRoom = null;
		
		endingPlayerHealth = player.HealthComponent.CurrentHealth;
		RoundTimer.Stop();
		GD.Print("Round Lost!");

		// UI Visibility for Round Lost
		_victoryLabel.Visible = false; // Ensure victory label is hidden
		_defeatLabel.Visible = true;
		_bonusContainer.Visible = false;
		_timerLabel.Visible = false;
		_activeEnemyCountTextValue.Visible = false;
		_activeEnemyCountHBoxContainer.Visible = false; // Added
		_roomLabelsVBoxContainer.Visible = false; // Added
		_waveRoundContainer.Visible = false;

		_timeBonusTextValue.Text = "0"; // No time bonus on loss
		_lifeBonusTextValue.Text = "0"; // No health bonus on loss
		
		if (_levelLostMenuScene != null)
		{
			var levelLostMenu = _levelLostMenuScene.Instantiate();
			PlayerBody.Instance.ControlRoot.AddChild(levelLostMenu);
		}
		else
		{
			DebugManager.Error("WaveDirector: _levelLostMenuScene is not assigned!");
		}
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
							  .Select(pair => new
							   {
								   Data = pair.Key, Scene = pair.Value,
								   Cost = pair.Key.BaseCost * enemyStrengthMultipliers[pair.Key.Rank]
							   })
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

			// DebugManager.Debug($"WaveDirector: GenerateEnemyList - Added {chosenEnemy.Data.Name} (Cost: {chosenEnemy.Cost}). Remaining budget: {budget}");
		}

		return enemiesToSpawn;
	}

	private void OnRoundTimerTimeout()
	{
		// Play timer alarm sound
	}

	private void OnPlayerDiedSignalReceived()
	{
		ResetAllUI(); // Hide all other UI elements first

		if (_levelLostMenuScene != null)
		{
			var levelLostMenu = _levelLostMenuScene.Instantiate();
			_directorDisplay.AddChild(levelLostMenu);
		}
		else
		{
			DebugManager.Error("WaveDirector: _levelLostMenuScene is not assigned!");
		}
	}

	private void ResetAllUI()
	{
		_timerLabel.Visible = false;
		_activeEnemyCountTextValue.Visible = false;
		_activeEnemyCountHBoxContainer.Visible = false;
		_roomLabelsVBoxContainer.Visible = false;
		_waveRoundContainer.Visible = false;
		_victoryLabel.Visible = false;
		_defeatLabel.Visible = false;
		_bonusContainer.Visible = false;
	}
}