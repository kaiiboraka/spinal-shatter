using Elythia;
using Godot;

namespace SpinalShatter;

[GlobalClass, Tool]
public partial class StackingEffectData : Resource
{
	private float intensityPerStack;
	private float durationPerStack;
	private float maxIntensity;
	private float maxDuration;
	private int maxStacks;

	[ExportCategory("Metadata")]
	[Export] public StackingEffectType EffectType {get; private set;}
	[Export] public Texture2D EffectIcon {get; private set;}

	[ExportCategory("Effect Data")]
	[ExportGroup("Stack Data")]
	[Export(PropertyHint.Range, "1,100,1")] public int MaxStacks
	{
		get => maxStacks;
		private set
		{
			maxStacks = Mathf.Max(1, value);
			maxDuration = maxStacks * DurationPerStack;
			maxIntensity = maxStacks * IntensityPerStack;
		}
	}

	[Export] public float MaxDuration
	{
		get => maxDuration;
		private set
		{
			maxDuration = value;
			durationPerStack = maxDuration / MaxStacks;
		}
	}

	[Export] public float MaxIntensity
	{
		get => maxIntensity;
		private set
		{
			maxIntensity = value;
			intensityPerStack = maxIntensity / MaxStacks;
		}
	}

	[Export] public float DurationPerStack
	{
		get => durationPerStack;
		private set
		{
			durationPerStack = value;
			maxDuration = durationPerStack * MaxStacks;
		}
	}

	[Export] public float IntensityPerStack
	{
		get => intensityPerStack;
		private set
		{
			intensityPerStack = value;
			maxIntensity = intensityPerStack * MaxStacks;
		}
	}

	[ExportGroup("Ticking Data")]
	[Export(PropertyHint.GroupEnable, "")] public bool HasTickEffect { get; private set; }
	[Export] public float TimeBetweenTicks { get; private set; } = 0.25f;
}