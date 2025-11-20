using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Elythia;
using Godot.Collections;

namespace SpinalShatter;

public partial class WaveDirector : Node
{
	public static WaveDirector Instance;

	// --- State ---
	public int CurrentRound { get; private set; } = 1;
	public int TotalWavesCompleted { get; private set; } = 0;
	public DifficultyTier SelectedDifficulty = DifficultyTier.D2_Normal;
	public bool IsRoundStarted { get; private set; } = false;
	public bool IsRoundCompleted { get; private set; } = false;

	private int _wavesCompletedThisRound = 0;
	private int _enemiesThisWave = 0;
	private LevelRoom _activeRoom;
	private LevelRoom _roundInProgressRoom;
	
	// --- Room & Door Management ---
	private readonly Godot.Collections.Dictionary<CardinalDirection, Door> _hubDoors = new();
	private readonly Godot.Collections.Dictionary<CardinalDirection, LevelRoom> _combatRooms = new();
	private CardinalDirection? _lastCompletedRoomDirection = null;


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
	[ExportGroup("Spawning")] [Export] private Godot.Collections.Dictionary<EnemyData, PackedScene> _enemyDataToSceneMap = new();
	[Export] private int wavesPerRound = 3;
	[Export(PropertyHint.ExpEasing)] private float baseBudget = 10f;
	[Export(PropertyHint.ExpEasing)] private float budgetIncreasePerWave = 2f;
	[Export(PropertyHint.ExpEasing)] private float budgetIncreasePerRound = 40f;


	// --- Difficulty Multipliers ---
	[ExportGroup("Difficulty")]
	[Export] private Godot.Collections.Dictionary<DifficultyTier, float> difficultyMultipliers = new()
	{
		{ DifficultyTier.D0_Braindead, .5f },
		{ DifficultyTier.D1_Easy, .75f },
		{ DifficultyTier.D2_Normal, 1.0f },
		{ DifficultyTier.D3_Hard, 2.0f },
		{ DifficultyTier.D4_Expert, 4.0f },
		{ DifficultyTier.D5_Brutal, 10.0f }
	};

	public Godot.Collections.Dictionary<DifficultyTier, float> DifficultyMultipliers => difficultyMultipliers;

	[Export] private Godot.Collections.Dictionary<EnemyRank, float> enemyStrengthMultipliers = new()
	{
		{ EnemyRank.Rank1_Bone, 1 },
		{ EnemyRank.Rank2_Cloth, 2 },
		{ EnemyRank.Rank3_Iron, 3 },
		{ EnemyRank.Rank4_Obsidian, 4 }
	};


	public Godot.Collections.Dictionary<EnemyRank, float> EnemyStrengthMultipliers => enemyStrengthMultipliers;

	public override void _Ready()
	{
		if (Instance != null) QueueFree();
		Instance = this;

		_hubDoors.Clear();
		_combatRooms.Clear();

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

	public override void _Process(double delta)
	{
		if (IsRoundStarted && RoundTimer != null)
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
			var combatRoomsInGroup = GetTree().GetNodesInGroup("CombatRooms").Cast<LevelRoom>();
			foreach (var room in combatRoomsInGroup)
			{
				if (_roomEnemyLabels.TryGetValue(room.Name, out var label))
				{
					int totalEnemiesForWave = (IsRoundStarted && room == _roundInProgressRoom) ? _enemiesThisWave : 0;
					label.Text = $"{room.EnemyCount} / {totalEnemiesForWave}";
				}
			}
		}
	}

	public void RegisterHubDoor(Door door)
	{
		if (!_hubDoors.TryAdd(door.DoorDirection, door)) return;
		door.PlayerDoorShut += OnHubDoorShut;
		CheckAllNodesReady();
	}

	public void RegisterCombatRoom(LevelRoom room)
	{
		if (room.IsCentralHub || !_combatRooms.TryAdd(room.RoomDirection, room)) return;

		CheckAllNodesReady();
	}

	private void CheckAllNodesReady()
	{
		// Once all doors and rooms have registered themselves, initialize the doors for the first time.
		if (_hubDoors.Count == 4 && _combatRooms.Count == 4)
		{
			foreach (var keyValuePair in _hubDoors)
			{
				keyValuePair.Value.InstantClose();
			}
			foreach (var keyValuePair in _combatRooms)
			{
				keyValuePair.Value.LevelDoor.InstantOpen();
			}
			SelectNewDoors();
		}
	}

	public void Reset()
	{
		// Reset state variables
		CurrentRound = 1;
		TotalWavesCompleted = 0;
		IsRoundStarted = false;
		IsRoundCompleted = false;
		_wavesCompletedThisRound = 0;
		_enemiesThisWave = 0;
		_activeRoom = null;
		_roundInProgressRoom = null;
		_lastCompletedRoomDirection = null;

		// Reset timers
		RoundTimer.Stop();

		// Reset UI
		ResetAllUI();
	}

	public void SetPlayer(PlayerBody playerInstance)
	{
		if (playerInstance != player)
		{
			Reset();
		}

		// Unsubscribe from old player if it exists and is valid
		if (player != null && GodotObject.IsInstanceValid(player))
		{
			player.HealthComponent.SafeUnsubscribe(HealthComponent.SignalName.OutOfHealth, ProcessRoundLostState);
			player.SafeUnsubscribe(PlayerBody.SignalName.PlayerDied, ShowGameOverMenu);
		}

		player = playerInstance;

		if (player == null)
		{
			DebugManager.Error("WaveDirector: Attempted to set player with a null instance!");
			return;
		}

		// Subscribe to new player
		player.HealthComponent.SafeSubscribe(HealthComponent.SignalName.OutOfHealth, ProcessRoundLostState);
		player.SafeSubscribe(PlayerBody.SignalName.PlayerDied, ShowGameOverMenu);
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
		}
	}

	public void StartRound(LevelRoom room)
	{
		if (player == null)
		{
			DebugManager.Error("WaveDirector: Player instance is null. Cannot start round.");
			return;
		}
		
		foreach (var hubDoor in _hubDoors.Values)
		{
			hubDoor.SystemClose();
		}

		_wavesCompletedThisRound = -1;
		_roundInProgressRoom = room;

		IsRoundStarted = true;
		IsRoundCompleted = false;
		startingPlayerHealth = player.HealthComponent.CurrentPercent;
		RoundTimer.Start();
		StartNextWave();
		_wavesCompletedThisRound = 0;
		
		_timerLabel.Visible = true;
		_activeEnemyCountTextValue.Visible = true;
		_activeEnemyCountHBoxContainer.Visible = true;
		_roomLabelsVBoxContainer.Visible = true;
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
		var enemies = GenerateEnemyList(budget);
		_enemiesThisWave = enemies.Count;
		_activeRoom.StartSpawning(enemies);
	}

	private void OnWaveCleared()
	{
		TotalWavesCompleted++;
		_wavesCompletedThisRound++;
		GetTree().CreateTimer(3.0f).Timeout += StartNextWave;
	}

	private void OnRoundWon()
	{
		IsRoundStarted = false;
		IsRoundCompleted = true;

		if (_roundInProgressRoom != null)
		{
			_roundInProgressRoom.OnRoundCompletion();
			_lastCompletedRoomDirection = _roundInProgressRoom.RoomDirection;
			
			if (_hubDoors.TryGetValue(_roundInProgressRoom.RoomDirection, out var doorToOpen))
			{
				doorToOpen.SystemOpen();
			}
		}
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
		
		GD.Print($"Round {CurrentRound - 1} won! Return to Hub.");

		_victoryLabel.Visible = true;
		_defeatLabel.Visible = false;
		_bonusContainer.Visible = true;
		_timerLabel.Visible = false;
		_activeEnemyCountTextValue.Visible = false;
		_activeEnemyCountHBoxContainer.Visible = false;
		_roomLabelsVBoxContainer.Visible = false;
		_waveRoundContainer.Visible = false;

		_timeBonusTextValue.Text = moneyTimeBonus.ToString();
		_lifeBonusTextValue.Text = moneyHealthBonus.ToString();
	}

	private void OnHubDoorShut()
	{
		if (!IsRoundStarted && IsRoundCompleted && RoomManager.Instance.CurrentRoom is { IsCentralHub: true })
		{
			ResetAllUI();
			SelectNewDoors();
			IsRoundCompleted = false;
		}
	}
	
	private void SelectNewDoors()
	{
		var allDirections = new Array<CardinalDirection>(
			System.Enum.GetValues(typeof(CardinalDirection)).Cast<CardinalDirection>()
		);
		
		if (_lastCompletedRoomDirection.HasValue)
		{
			allDirections.Remove(_lastCompletedRoomDirection.Value);
		}
    
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		allDirections.Shuffle();

		var doorsToOpen = new Array<CardinalDirection>();
		for(int i = 0; i < 2 && i < allDirections.Count; i++)
		{
			doorsToOpen.Add(allDirections[i]);
		}

		DebugManager.Debug($"Selecting new doors. Last completed: {_lastCompletedRoomDirection?.ToString() ?? "None"}. Opening: {string.Join(", ", doorsToOpen.Select(d => d.ToString()).ToArray())}");

		foreach (var direction in System.Enum.GetValues(typeof(CardinalDirection)).Cast<CardinalDirection>())
		{
			if (_hubDoors.TryGetValue(direction, out var door))
			{
				if (doorsToOpen.Contains(direction))
				{
					door.SystemOpen();
				}
				else
				{
					door.SystemClose();
				}
			}
			else
			{
				 DebugManager.Warning($"SelectNewDoors: Could not find door for direction {direction}.");
			}
		}
	}

	private void ProcessRoundLostState()
	{
		if (!IsRoundStarted && RoundTimer.IsStopped()) return;

		ResetAllUI();
		IsRoundStarted = false;
		IsRoundCompleted = false;
		_roundInProgressRoom = null;
		
		endingPlayerHealth = player.HealthComponent.CurrentHealth;
		RoundTimer.Stop();
		GD.Print("Round Lost!");

		_victoryLabel.Visible = false;
		_defeatLabel.Visible = true;
		_bonusContainer.Visible = false;
		_timerLabel.Visible = false;
		_activeEnemyCountTextValue.Visible = false;
		_activeEnemyCountHBoxContainer.Visible = false;
		_roomLabelsVBoxContainer.Visible = false;
		_waveRoundContainer.Visible = false;

		_timeBonusTextValue.Text = "0";
		_lifeBonusTextValue.Text = "0";
	}

	private void ShowGameOverMenu()
	{
		if (_levelLostMenuScene != null)
		{
			if (PlayerBody.Instance.ControlRoot.FindChild("LevelLostMenu", recursive: false) == null)
			{
				var levelLostMenu = _levelLostMenuScene.Instantiate();
				levelLostMenu.Name = "LevelLostMenu";
				PlayerBody.Instance.ControlRoot.AddChild(levelLostMenu);
			}
		}
		else
		{
			DebugManager.Error("WaveDirector: _levelLostMenuScene is not assigned!");
		}
	}

	public void OnRoundLost()
	{
		ProcessRoundLostState();
		ShowGameOverMenu();
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

		while (budget > 0 && availableEnemies.Any(e => e.Cost <= budget))
		{
			var affordableEnemies = availableEnemies.Where(e => e.Cost <= budget).ToList();
			if (!affordableEnemies.Any()) break;

			var chosenEnemy = affordableEnemies[GD.RandRange(0, affordableEnemies.Count - 1)];

			enemiesToSpawn.Add(chosenEnemy.Scene);
			budget -= chosenEnemy.Cost;
		}

		return enemiesToSpawn;
	}

	private void OnRoundTimerTimeout()
	{
		// Play timer alarm sound
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