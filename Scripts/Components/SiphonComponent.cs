using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace SpinalShatter;

public partial class SiphonComponent : Node
{
	[Export] private Area3D _siphonField;
	[Export] private Node3D _target;
	[Export] public AudioStreamPlayer3D AudioStreamPlayer_Siphon { get; private set; }

	private HashSet<Pickup> _attractedPickups = new();

	private bool _siphonPressed;

	public override void _Ready()
	{
		base._Ready();
		_siphonField.Visible = false;
		_siphonField.Monitoring = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("Player_Shoot"))
		{
		}
		else if (@event.IsActionPressed("Player_Siphon"))
		{
			_siphonField.Monitoring = true;
			_siphonField.Visible = true;
			_siphonPressed = true;
			while (!AudioStreamPlayer_Siphon.IsPlaying()) AudioStreamPlayer_Siphon.Play(1.54f);
			AttractPickups();
		}

		if (@event.IsActionReleased("Player_Siphon"))
		{
			_siphonPressed = false;
			_siphonField.Visible = false;
			ReleaseAllPickups();
			AudioStreamPlayer_Siphon.Stop();
			_siphonField.Monitoring = false;
		}
	}

	public override void _Process(double delta)
	{
		if (_siphonPressed)
		{
			AttractPickups();
		}
		else
		{
			ReleaseAllPickups();
		}
	}

	private void AttractPickups()
	{
		if (_siphonField == null) return;

		foreach (var area in _siphonField.GetOverlappingAreas())
		{
			if (area.GetOwner() is Pickup pickup)
			{
				pickup.RemoveCollisionMask3D(LayerNames.PHYSICS_3D.SOLID_GROUND_NUM);

				// Attract the pickup if it's not already being attracted
				if (_attractedPickups.Add(pickup) &&
					pickup.State != Pickup.PickupState.Collected &&
					pickup.State != Pickup.PickupState.Expired)
				{
					// GD.Print($"{Time.GetTicksMsec()}: SiphonComponent: Attracting pickup {pickup.Name}, current state: {pickup.State}");
					pickup.Attract(_target);
					pickup.Released += OnPickupReleased;
				}
			}
		}
	}

	private void ReleaseAllPickups()
	{
		if (_attractedPickups.Count == 0) return;

		foreach (var pickup in _attractedPickups)
		{
			pickup.AddCollisionMask3D(LayerNames.PHYSICS_3D.SOLID_GROUND_NUM);
			if (IsInstanceValid(pickup) && pickup.State == Pickup.PickupState.Attracted)
			{
				pickup.DriftIdle();
			}
		}

		_attractedPickups.Clear();
	}

	private void OnPickupReleased(Pickup pickup)
	{
		// GD.Print($"{Time.GetTicksMsec()}: SiphonComponent: Removing pickup {pickup.Name} from attracted set.");
		_attractedPickups.Remove(pickup);
	}
}