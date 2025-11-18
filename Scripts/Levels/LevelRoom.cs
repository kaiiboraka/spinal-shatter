using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using Elythia;

namespace SpinalShatter;

public partial class LevelRoom : Node3D
{
	[Signal] public delegate void PlayerEnteredEventHandler(LevelRoom room);
	[Signal] public delegate void PlayerExitedEventHandler(LevelRoom room);
	[Signal] public delegate void WaveClearedEventHandler();

	[Export] private bool alwaysShow = false;
	[Export] public bool IsCentralHub { get; set; } = false;

	private Array<EnemySpawner> _spawners;
	private Area3D _triggerVolume;

	private bool _spawningFinished = false;
	
	public bool IsActive { get; private set; }
	public List<Enemy> EnemiesInRoom { get; private set; } = new();
	public int EnemyCount => EnemiesInRoom.Count;

	public override void _Ready()
	{
		_triggerVolume = GetNode<Area3D>("%LevelRegion");
		if (_triggerVolume != null)
		{
			_triggerVolume.BodyEntered += OnBodyEntered;
			_triggerVolume.BodyExited += OnBodyExited;
		}

		var spawnerRoot = GetNode<Node3D>("%Spawners");

		_spawners ??= new Array<EnemySpawner>();
		foreach (var child in spawnerRoot.GetChildren())
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
		// DebugManager.Debug($"LevelRoom: {Name} OnEnemyDied called for {who.Name}.");
		UnregisterEnemy(who);
	}

	private void CheckWaveCleared()
	{
		if (!IsActive || !_spawningFinished)
		{
			return;
		}

		// If spawning is finished and no enemies are left, the wave is cleared
		if (EnemiesInRoom.Count == 0)
		{
			EmitSignalWaveCleared();
			GD.Print("Wave Cleared!");
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
		if (EnemiesInRoom.Contains(enemy)) return;
		EnemiesInRoom.Add(enemy);
		enemy.AssociatedRoom = this;
		enemy.EnemyDied += OnEnemyDied;
		// DebugManager.Debug($"LevelRoom: {Name} Registered enemy {enemy.Name}. Total enemies: {_enemiesInRoom.Count}");
	}

	public void UnregisterEnemy(Enemy enemy)
	{
		if (EnemiesInRoom.Remove(enemy))
		{
			enemy.EnemyDied -= OnEnemyDied;
			// DebugManager.Debug($"LevelRoom: {Name} Unregistered enemy {enemy.Name}. Total enemies: {_enemiesInRoom.Count}");
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
		foreach (var enemy in EnemiesInRoom)
		{
			enemy.Activate();
		}
	}

	public void Deactivate()
	{
		IsActive = false;
		if (!alwaysShow) HideRoom();

		// Deactivate any remaining enemies
		foreach (var enemy in EnemiesInRoom)
		{
			enemy.Deactivate();
		}
	}
}