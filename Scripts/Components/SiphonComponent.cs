using System.Collections.Generic;
using Godot;

namespace SpinalShatter;

public partial class SiphonComponent : Node
{
	[Export] private Area3D _siphonField;
	[Export] private Node3D _target;
	[Export] public AudioStreamPlayer3D AudioStreamPlayer_Siphon { get; private set; }

	private readonly HashSet<Pickup> _attractedPickups = new();
	private bool _isSiphoning;

	public bool CanSiphon { get; set; }

	public override void _Ready()
	{
		base._Ready();
		_siphonField.AreaEntered += OnSiphonFieldEntered;
		_siphonField.AreaExited += OnSiphonFieldExited;
		
		_siphonField.Visible = false;
		_siphonField.Monitoring = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (!CanSiphon) return;

		if (@event.IsActionPressed("Player_Siphon"))
		{
			StartSiphon();
		}

		if (@event.IsActionReleased("Player_Siphon"))
		{
			StopSiphon();
		}
	}

	private void StartSiphon()
	{
		PlayerBody.Instance.DisallowMeleeAttack();
		PlayerBody.Instance.DisallowRangedAttack();
		_siphonField.Monitoring = true;
		_siphonField.Visible = true;
		_isSiphoning = true;
		
		// Attract any pickups already inside the field
		foreach (var area in _siphonField.GetOverlappingAreas())
		{
			OnSiphonFieldEntered(area);
		}

		if (!AudioStreamPlayer_Siphon.IsPlaying())
		{
			AudioStreamPlayer_Siphon.Play(1.54f);
		}
	}

	private void StopSiphon()
	{
		PlayerBody.Instance.AllowMeleeAttack();
		PlayerBody.Instance.AllowRangedAttack();
		_isSiphoning = false;
		_siphonField.Visible = false;
		_siphonField.Monitoring = false;
		ReleaseAllPickups();
		AudioStreamPlayer_Siphon.Stop();
	}

	private void OnSiphonFieldEntered(Area3D area)
	{
		if (!_isSiphoning || area.Owner is not Pickup pickup) return;
		
		// Attract the pickup if it's not already being attracted
		if (_attractedPickups.Add(pickup) &&
			pickup.State is not (Pickup.PickupState.Collected or Pickup.PickupState.Expired))
		{
			pickup.RemoveCollisionMask3D(LayerNames.PHYSICS_3D.SOLID_GROUND_NUM);
			pickup.RemoveCollisionMask3D(LayerNames.PHYSICS_3D.SOLID_WALL_NUM);
			pickup.RemoveCollisionMask3D(LayerNames.PHYSICS_3D.PLATFORM_NUM);
			
			pickup.BeginAttraction(_target);
			pickup.Released += OnPickupReleased;
		}
	}
	
	private void OnSiphonFieldExited(Area3D area)
	{
		if (area.Owner is not Pickup pickup || !_attractedPickups.Contains(pickup)) return;

		ReleasePickup(pickup);
		_attractedPickups.Remove(pickup);
	}

	private void ReleaseAllPickups()
	{
		if (_attractedPickups.Count == 0) return;

		foreach (var pickup in _attractedPickups)
		{
			ReleasePickup(pickup);
		}
		_attractedPickups.Clear();
	}

	private void ReleasePickup(Pickup pickup)
	{
		if (!IsInstanceValid(pickup)) return;
		
		pickup.AddCollisionMask3D(LayerNames.PHYSICS_3D.SOLID_GROUND_NUM);
		pickup.AddCollisionMask3D(LayerNames.PHYSICS_3D.PLATFORM_NUM);
		pickup.AddCollisionMask3D(LayerNames.PHYSICS_3D.SOLID_WALL_NUM);

		if (pickup.State == Pickup.PickupState.Attracted)
		{
			pickup.OnSiphonRelease();
		}
		
		pickup.Released -= OnPickupReleased;
	}

	private void OnPickupReleased(Pickup pickup)
	{
		_attractedPickups.Remove(pickup);
	}
}
