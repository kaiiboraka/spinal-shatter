using Godot;
using SpinalShatter;

public partial class CameraTransition : Node
{
	public static CameraTransition Instance;

	private Camera3D camera3D;

	private Tween tween;
	private bool transitioning = false;

	[Signal] public delegate void TransitionFinishedEventHandler();

	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
		else QueueFree();

		// Initialize();
	}

	public void Initialize(Camera3D playerCamera)
	{
		camera3D = (Camera3D)playerCamera.Duplicate();
		camera3D.Clear();
		if (camera3D.Owner != null) camera3D.Owner.RemoveChild(camera3D);
		GetTree().Root.GetNode<Node3D>("/root/LevelRoot").CallDeferred(Node.MethodName.AddChild, camera3D);;

		// Ensure helper camera is disabled at start
		if (camera3D != null) camera3D.Current = false;

		// Create persistent tween (Godot 4 style)
		tween = CreateTween();
		tween.Stop(); // We'll control it manually
	}

	// Simple instant switch (no animation)
	public void SwitchCamera(Camera3D from, Camera3D to)
	{
		if (from != null) from.Current = false;
		if (to != null) to.Current = true;
	}

	// Smooth 3D camera transition
	public void TransitionCamera3D(Camera3D from, Camera3D to, float duration = 1.0f)
	{
		if (transitioning || camera3D == null || from == null || to == null)
			return;

		transitioning = true;

		// Copy initial state
		camera3D.Fov = from.Fov;
		camera3D.CullMask = from.CullMask;

		// Snap to start and activate
		camera3D.GlobalTransform = from.GlobalTransform;
		camera3D.Current = true;

		// Fresh tween
		tween?.Kill();
		tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.SetEase(Tween.EaseType.InOut);

		// Parallel transform and FOV
		tween.Parallel().TweenProperty(camera3D, "global_transform", to.GlobalTransform, duration);
		tween.Parallel().TweenProperty(camera3D, "fov", to.Fov, duration);

		// tween.Finished += () =>
		// {
		// 	to.Current = true;
		// 	transitioning = false;
		// };

		tween.Finished += On3DFinished;
		return;

		void On3DFinished()
		{
			tween.Finished -= On3DFinished;
			to.MakeCurrent(); // true
			transitioning = false;
			EmitSignalTransitionFinished();
		}
	}

// Optional: Clean up on exit
	public override void _ExitTree()
	{
		tween?.Kill();
	}
}