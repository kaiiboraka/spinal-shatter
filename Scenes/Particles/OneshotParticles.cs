using Godot;

public partial class OneshotParticles : Node3D
{
	private Timer _freeTimer;
	private GpuParticles3D _gpuParticles;

	public override void _Ready()
	{
		base._Ready();
		_gpuParticles = GetNode<GpuParticles3D>("%Particles");
		_gpuParticles.OneShot = true;

		_freeTimer = GetNode<Timer>("FreeTimer");

		_freeTimer.WaitTime = _gpuParticles.Lifetime * 2;
		_freeTimer.Timeout += QueueFree;
	}

	public void PlayParticles(int amount = 10)
	{
		_gpuParticles.Amount = amount;
		_freeTimer.Start();
		_gpuParticles.Emitting = true;
		_gpuParticles.Restart();
	}
}
