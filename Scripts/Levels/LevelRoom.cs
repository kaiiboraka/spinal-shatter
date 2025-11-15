using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using Elythia;

public partial class LevelRoom : Node3D
{
	[Signal] public delegate void PlayerEnteredEventHandler(LevelRoom room);
	[Signal] public delegate void PlayerExitedEventHandler(LevelRoom room);
	[Signal] public delegate void RoundWonEventHandler();

	[Export] private Area3D _triggerVolume;
	[Export] private Array<EnemySpawner> _spawners;
	[Export] private bool alwaysShow = false;
	[Export] private float _spawnInterval = 1.0f;

	private Timer _spawnTimer;
	private Array<PackedScene> _enemiesToSpawn = new();
	
	public bool IsActive { get; private set; }
	private readonly List<Enemy> _enemiesInRoom = new();

	public override void _Ready()
	{
		if (_triggerVolume != null)
		{
			_triggerVolume.BodyEntered += OnBodyEntered;
			_triggerVolume.BodyExited += OnBodyExited;
		}

		_spawners ??= new Array<EnemySpawner>();
		foreach (var child in GetChildren())
		{
			if (child is EnemySpawner spawner)
			{
				_spawners.Add(spawner);
			}
		}
		
		_spawnTimer = new Timer();
		AddChild(_spawnTimer);
		_spawnTimer.WaitTime = _spawnInterval;
		_spawnTimer.Timeout += OnSpawnTimerTimeout;

		// Find any enemies that are pre-placed in the room in the editor
		FindEnemiesRecursively(this);

		RoomManager.Instance.RegisterRoom(this);
	}

	public void StartSpawning(Array<PackedScene> enemies)
	{
		if (_spawners.Count == 0)
		{
			GD.PrintErr($"LevelRoom '{Name}' has no spawners, cannot start spawning.");
			return;
		}

		_enemiesToSpawn = new Array<PackedScene>(enemies);
		_enemiesToSpawn.Shuffle();

		// Initialize pools for all unique scenes
		var uniqueScenes = _enemiesToSpawn.Distinct();
		foreach (var spawner in _spawners)
		{
			spawner.InitializePools(uniqueScenes);
		}
		
		_spawnTimer.Start();
	}

	private void OnSpawnTimerTimeout()
	{
		if (_enemiesToSpawn.Count == 0)
		{
			_spawnTimer.Stop();
			CheckRoundWon(); // Check if the round is won now that spawning is complete
			return;
		}

		var scene = _enemiesToSpawn.PopFront();
		if (scene != null)
		{
			// Pick a random spawner
			var spawner = _spawners[GD.Randi() % _spawners.Count];
			spawner.Spawn(scene);
		}
	}
	
	private void OnEnemyDied(Enemy who)
	{
		UnregisterEnemy(who);
	}

	private void CheckRoundWon()
	{
		if (!IsActive) return;

		// If we are still spawning, the round isn't won yet.
		if (_enemiesToSpawn.Count > 0 || !_spawnTimer.IsStopped())
		{
			return;
		}

		// If all spawners are finished and no enemies are left, the round is won
		if (_enemiesInRoom.Count == 0)
		{
			GD.Print("Round Won!");
			EmitSignal(SignalName.RoundWon);
		}
	}

	private void FindEnemiesRecursively(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			if (child is Enemy enemy)
			{
				RegisterEnemy(enemy);
			}
			else
			{
				FindEnemiesRecursively(child);
			}
		}
	}

	public void RegisterEnemy(Enemy enemy)
	{
		if (_enemiesInRoom.Contains(enemy)) return;
		_enemiesInRoom.Add(enemy);
		enemy.AssociatedRoom = this;
		enemy.EnemyDied += OnEnemyDied;
	}

	public void UnregisterEnemy(Enemy enemy)
	{
		if (_enemiesInRoom.Remove(enemy))
		{
			enemy.EnemyDied -= OnEnemyDied;
			CheckRoundWon();
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		//DebugManager.Trace($"{body.Name} entered {this.Name}");
		if (body is PlayerBody)
		{
			Activate();
			EmitSignalPlayerEntered(this);
		}
		else if (body is Enemy enemy && enemy.AssociatedRoom != this)
		{
			enemy.AssociatedRoom?.UnregisterEnemy(enemy);
			RegisterEnemy(enemy);
		}
	}

	private void OnBodyExited(Node3D body)
	{
		//DebugManager.Trace($"{body.Name} entered {this.Name}");
		if (body is PlayerBody)
		{
			EmitSignalPlayerExited(this);
			Deactivate();
		}
	}

	public void ShowRoom()
	{
		Visible = true;
	}

	public void HideRoom()
	{
		Visible = alwaysShow;
	}

	public void Activate()
	{
		IsActive = true;
		ShowRoom();
		
		// Pre-placed enemies are activated here
		foreach (var enemy in _enemiesInRoom)
		{
			enemy.Activate();
		}
	}

	public void Deactivate()
	{
		IsActive = false;
		if (!alwaysShow) HideRoom();

		// Deactivate any remaining enemies
		foreach (var enemy in _enemiesInRoom)
		{
			enemy.Deactivate();
		}
	}
}