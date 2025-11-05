using Godot;
using Godot.Collections;
using System.Collections.Generic;
using Elythia;

public partial class LevelRoom : Node3D
{
	[Signal] public delegate void PlayerEnteredEventHandler(LevelRoom room);

	[Signal] public delegate void PlayerExitedEventHandler(LevelRoom room);

	[Export] private Area3D _triggerVolume;
	[Export] private Array<EnemySpawner> _spawners;
	[Export] private bool alwaysShow = false;

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

		// Find any enemies that are pre-placed in the room in the editor
		FindEnemiesRecursively(this);

		RoomManager.Instance.RegisterRoom(this);
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
	}

	public void UnregisterEnemy(Enemy enemy)
	{
		_enemiesInRoom.Remove(enemy);
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
		foreach (var enemySpawner in _spawners)
		{
			enemySpawner.IsEnabled = true;
		}

		foreach (var enemy in _enemiesInRoom)
		{
			enemy.Activate();
		}
	}

	public void Deactivate()
	{
		IsActive = false;
		if (!alwaysShow) HideRoom();
		foreach (var enemySpawner in _spawners)
		{
			enemySpawner.IsEnabled = false;
		}

		foreach (var enemy in _enemiesInRoom)
		{
			enemy.Deactivate();
		}
	}
}