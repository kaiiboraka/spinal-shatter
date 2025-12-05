using Godot;

public partial class OneshotParticles : Node3D
{
	[Export] private float lifeTime = 1f;
	private Timer _freeTimer;
	private GpuParticles3D gpuParticles;
	private GpuParticles3D cloudParticles;

	public float Range;

	[Export] bool hasCloud = false;

	public override void _Ready()
	{
		base._Ready();
		gpuParticles = GetNode<GpuParticles3D>("%Particles");
		gpuParticles.OneShot = true;
		gpuParticles.Lifetime =  lifeTime;
		if (hasCloud)
		{
			cloudParticles = GetNode<GpuParticles3D>("%CloudParticles");
			cloudParticles.OneShot = true;
			cloudParticles.Lifetime = lifeTime;
		}

		_freeTimer = GetNode<Timer>("FreeTimer");

		_freeTimer.WaitTime = lifeTime * 2;
		_freeTimer.Timeout += QueueFree;

		SetInitialVelocityRange();
	}

	public void PlayParticles(float distance, float amount)
	{
		Range = distance;
		SetInitialVelocityRange();
		PlayParticles(amount);
	}

	public void PlayParticles(float amount)
	{
		int roundAmount = Mathf.Max(amount, 2).RoundToInt();
		gpuParticles.Amount = roundAmount;
		gpuParticles.Emitting = true;
		gpuParticles.Restart();
		if (hasCloud)
		{
			cloudParticles.Amount = roundAmount * 3;
			cloudParticles.Emitting = true;
			cloudParticles.Restart();
		}

		_freeTimer.Start();
	}

	private void SetInitialVelocityRange()
	{
		((ParticleProcessMaterial)gpuParticles.ProcessMaterial).InitialVelocityMax = Range * 2;
		((ParticleProcessMaterial)gpuParticles.ProcessMaterial).InitialVelocityMin = Range / 2;

		if (hasCloud)
		{
			((ParticleProcessMaterial)cloudParticles.ProcessMaterial).InitialVelocityMin = Range * 1.2f;
			((ParticleProcessMaterial)cloudParticles.ProcessMaterial).InitialVelocityMax = Range * 1.2f;
			((ParticleProcessMaterial)cloudParticles.ProcessMaterial).EmissionSphereRadius = Range / 2;
		}
	}
}