using Godot;

namespace SpinalShatter;

public partial class OneshotParticles : Node3D
{
	[Export] private float lifeTime = 1f;
	private Timer _freeTimer;
	private GpuParticles3D gpuParticles;
	private GpuParticles3D cloudParticles;
	private MeshInstance3D sphereMesh;

	public float Range;

	[Export] bool hasCloud = false;
	[Export] bool hasSphere = false;

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

		if (hasSphere)
		{
			sphereMesh	= GetNode<MeshInstance3D>("Sphere_MeshInstance3D");
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

		if (hasSphere)
		{
			((SphereMesh)sphereMesh.Mesh).Radius = Range;
			((SphereMesh)sphereMesh.Mesh).Height = Range * 2;

			Tween alphaTween = CreateTween();
			var material = sphereMesh.GetActiveMaterial(0) as StandardMaterial3D;

			alphaTween.TweenProperty(material,
						   "albedo_color:a", 0.0f, lifeTime)
					  .From(5f)

					   // alphaTween.TweenMethod(
					   // 			   Callable.From<float>(alpha =>
					   // 				   material.AlbedoColor = new Color(
					   // 					   material.AlbedoColor.R,
					   // 					   material.AlbedoColor.G,
					   // 					   material.AlbedoColor.B,
					   // 					   alpha)),
					   // 			   64f, 0.0f, lifeTime)
					  .SetTrans(Tween.TransitionType.Linear)
					  .SetEase(Tween.EaseType.Out);

			Tween sizeTween = CreateTween();
			sizeTween.Parallel().TweenProperty(sphereMesh, "mesh:radius", Range/2, lifeTime)
					 .SetTrans(Tween.TransitionType.Quint)
					 .SetEase(Tween.EaseType.In);
			sizeTween.Parallel().TweenProperty(sphereMesh, "mesh:height", Range, lifeTime)
					 .SetTrans(Tween.TransitionType.Quint)
					 .SetEase(Tween.EaseType.In);

		}
		_freeTimer.Start();
	}

	private static void SetMaterialAlpha(StandardMaterial3D material, float alpha)
	{
		// Preserve RGB, only change alpha
		Color current = material.AlbedoColor;
		material.AlbedoColor = new Color(current.R, current.G, current.B, alpha);
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