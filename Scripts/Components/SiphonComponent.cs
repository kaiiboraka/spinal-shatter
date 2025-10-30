using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class SiphonComponent : Node
{
	[Export] private Area3D _siphonField;
	[Export] private Node3D _target;
	[Export] public AudioStreamPlayer3D AudioStreamPlayer_Siphon { get; private set; }

	private HashSet<ManaParticle> _attractedParticles = new();

	private bool _siphonPressed;

	public override void _Ready()
	{
		base._Ready();
		_siphonField.Visible = false;
		_siphonField.Monitoring = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("Player_Siphon"))
		{
			_siphonField.Monitoring = true;
			_siphonField.Visible = true;
			_siphonPressed = true;
			while (!AudioStreamPlayer_Siphon.IsPlaying()) AudioStreamPlayer_Siphon.Play(1.54f);
			AttractParticles();
		}

		if (@event.IsActionReleased("Player_Siphon"))
		{
			_siphonPressed = false;
			_siphonField.Visible = false;
			ReleaseAllParticles();
			AudioStreamPlayer_Siphon.Stop();
			_siphonField.Monitoring = false;
		}
	}

	public override void _Process(double delta)
	{
		if (_siphonPressed)
		{
			AttractParticles();
		}
		else
		{
			ReleaseAllParticles();
		}
	}

	private void AttractParticles()
	{
		if (_siphonField == null) return;

		foreach (var area in _siphonField.GetOverlappingAreas())
		{
			if (area.GetOwner() is ManaParticle particle)
			{
				// Attract the particle if it's not already being attracted
				if (_attractedParticles.Add(particle) && particle.State != ManaParticle.ManaParticleState.Collected &&
					particle.State != ManaParticle.ManaParticleState.Expired)
				{
					// GD.Print($"{Time.GetTicksMsec()}: SiphonComponent: Attracting particle {particle.Name}, current state: {particle.State}");
					particle.Attract(_target);
					particle.Released += OnParticleReleased;
				}
			}
		}
	}

	private void ReleaseAllParticles()
	{
		if (_attractedParticles.Count == 0) return;

		foreach (var particle in _attractedParticles)
		{
			if (IsInstanceValid(particle) && particle.State == ManaParticle.ManaParticleState.Attracted)
			{
				particle.DriftIdle();
			}
		}

		_attractedParticles.Clear();
	}

	private void OnParticleReleased(ManaParticle particle)
	{
		// GD.Print($"{Time.GetTicksMsec()}: SiphonComponent: Removing particle {particle.Name} from attracted set.");
		_attractedParticles.Remove(particle);
	}
}