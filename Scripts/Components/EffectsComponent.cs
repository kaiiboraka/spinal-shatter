using Godot;
using Godot.Collections;

namespace SpinalShatter;

public partial class EffectsComponent : Node
{
    // --- Static Data ---
    public static readonly Dictionary<StackingEffectType, StackingEffectData> EffectData = new()
    {
        { StackingEffectType.Poison, GD.Load<StackingEffectData>("res://assets/Resources/EffectData/EffectData_Poison.tres") },
        { StackingEffectType.Slow, GD.Load<StackingEffectData>("res://assets/Resources/EffectData/EffectData_Slow.tres") }
    };

    // --- Inner Class for Runtime State ---
    private partial class ActiveEffect : RefCounted
    {
        public StackingEffectData Data { get; }
        public int Stacks { get; set; }
        public double RemainingDuration { get; set; }
        public double TimeSinceLastTick { get; set; }

        public ActiveEffect(StackingEffectData data)
        {
            Data = data;
        }
    }

    // --- Instance Data ---
    private readonly Dictionary<StackingEffectType, ActiveEffect> activeEffects = new();

    // --- Signals ---
    [Signal] public delegate void EffectStacksChangedEventHandler(StackingEffectType type, int newStackCount);
    [Signal] public delegate void EffectTickedEventHandler(StackingEffectType type, float intensity);
    [Signal] public delegate void EffectExpiredEventHandler(StackingEffectType type);

    public override void _Process(double delta)
    {
        if (activeEffects.Count == 0) return;

        var expiredEffects = new Array<StackingEffectType>();

        foreach (var entry in activeEffects)
        {
            var effect = entry.Value;
            effect.RemainingDuration -= delta;

            if (effect.RemainingDuration <= 0)
            {
                expiredEffects.Add(entry.Key);
                continue;
            }

            if (!effect.Data.HasTickEffect) continue;
            effect.TimeSinceLastTick += delta;

            if (!(effect.TimeSinceLastTick >= effect.Data.TimeBetweenTicks)) continue;
            effect.TimeSinceLastTick -= effect.Data.TimeBetweenTicks;

            var intensity = effect.Stacks * effect.Data.IntensityPerStack;
            EmitSignalEffectTicked(entry.Key, intensity);
        }

        foreach (var effectType in expiredEffects)
        {
            RemoveEffect(effectType, activeEffects[effectType].Stacks, isExpiration: true);
        }
    }
    
    public void ApplyEffect(StackingEffectType effectType, int stacks = 1)
    {
        if (!EffectData.TryGetValue(effectType, out var data)) return;

        if (!activeEffects.TryGetValue(effectType, out var activeEffect))
        {
            activeEffect = new ActiveEffect(data);
            activeEffects[effectType] = activeEffect;
        }

        int oldStacks = activeEffect.Stacks;
        int newStacks = Mathf.Min(oldStacks + stacks, data.MaxStacks);
        activeEffect.Stacks = newStacks;

        // Refresh duration, capped at max duration
        float durationToAdd = stacks * data.DurationPerStack;
        activeEffect.RemainingDuration = Mathf.Min(activeEffect.RemainingDuration + durationToAdd, data.MaxDuration);
        
        if (oldStacks != newStacks)
        {
            EmitSignalEffectStacksChanged(effectType, newStacks);
        }
    }

    private void RemoveEffect(StackingEffectType effectType, int stacks, bool isExpiration = false)
    {
        if (!activeEffects.TryGetValue(effectType, out var activeEffect)) return;

        int newStackCount = isExpiration ? 0 : (activeEffect.Stacks - stacks).AtLeastZero();

        if (newStackCount == 0)
        {
            activeEffects.Remove(effectType);
            EmitSignalEffectExpired(effectType);
        }
        else
        {
            activeEffect.Stacks = newStackCount;
        }
        
        EmitSignalEffectStacksChanged(effectType, newStackCount);
    }
}
