using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SpinalShatter;

public partial class LevelRoom : Node3D
{
	[Signal] public delegate void PlayerEnteredEventHandler(LevelRoom room);
	[Signal] public delegate void PlayerExitedEventHandler(LevelRoom room);
	[Signal] public delegate void WaveClearedEventHandler();

	[Export] private Area3D _triggerVolume;
	[Export] private Array<EnemySpawner> _spawners;
	[Export] private bool alwaysShow = false;
	[Export] public bool IsCentralHub { get; set; } = false;

	private bool _spawningFinished = false;
	
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

		foreach (var spawner in _spawners)
		{
			spawner.SpawningFinished += OnSpawningFinished;
		}

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

		_spawningFinished = false;

		// Initialize pools for all unique scenes on all potential spawners
		var uniqueScenes = enemies.Distinct();
		foreach (var spawner in _spawners)
		{
			spawner.InitializePools(uniqueScenes);
		}
		
		// Pick a single random spawner to handle the whole wave
		var chosenSpawner = _spawners[(int)(GD.Randi() % _spawners.Count)];
		chosenSpawner.StartSpawningWave(enemies);
	}

	private void OnSpawningFinished()
	{
		_spawningFinished = true;
		CheckWaveCleared();
	}
	
	private void OnEnemyDied(Enemy who)
	{
		UnregisterEnemy(who);
	}

	private void CheckWaveCleared()
	{
		if (!IsActive || !_spawningFinished)
		{
			return;
		}

		// If spawning is finished and no enemies are left, the wave is cleared
		if (_enemiesInRoom.Count == 0)
		{
			GD.Print("Wave Cleared!");
			EmitSignalWaveCleared();
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
			CheckWaveCleared();
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