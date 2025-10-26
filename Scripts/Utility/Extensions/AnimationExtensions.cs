using Elythia;
using Godot;

public static class AnimationExtensions
{
    public const string stupid = "ANIMATIONS KEEP BREAKING AND I DON'T KNOW WHY";

    /// <summary>
    /// Null Safe get on Animation's GetName()
    /// </summary>
    /// <param name="anim"></param>
    /// <returns></returns>
    public static StringName AnimName(this Animation anim) => anim != null ? anim.GetName() : "";

    public static void SetResourceName(this Animation anim)
    {
        if (!anim.ResourceName.IsNullOrEmpty()) return;
        // path = anim.ResourcePath;
        // path = path
        // .Substring(path.LastIndexOf('/') +1 )
        // .Substring(path.LastIndexOf('_') +1 )
        // .Substring(0, path.Length - 4);

        var path = anim.ResourcePath; // path://to/file/Name_Anim.res
        path = path[(path.LastIndexOf('/') + 1)..]; // Name_Anim.res
        path = path[(path.IndexOf('_') + 1)..]; // Anim.res
        path = path[..(path.LastIndexOf('.'))]; // Anim
        // path = path[..^4]; // Anim
        anim.ResourceName = path;
        ResourceSaver.Save(anim);
    }
    //+ "_" + anim.GetInstanceId();

    // public static Node2D SourcePosition(this AttackContext context)
    // {
    //     if (context.Ability != null)
    //     {
    //         if (context.Ability.Caster is Node2D node2DAbility) return node2DAbility;
    //     }
    //     else if (context.Attacker is Node2D node2DAttacker) return node2DAttacker;
    //
    //     return null;
    // }

    public static void TryChangeAnimation(this AnimatedSprite2D sprite, string animation)
    {
        var currentAnim = sprite.Animation;
        if (currentAnim != animation && sprite.SpriteFrames.HasAnimation(animation))
        {
            sprite.Animation = animation;
        }
    }

    public static SignalAwaiter WaitForAnimationFinished(this Node parent, AnimationPlayer player)
    {
        return parent.ToSignal(player, AnimationPlayer.SignalName.AnimationFinished);
    }

    public static SignalAwaiter WaitForAnimationFinished(this Node parent, AnimatedSprite2D player)
    {
        return parent.ToSignal(player, AnimationPlayer.SignalName.AnimationFinished);
    }

    public static SignalAwaiter WaitForAnimationFinished(this AnimationPlayer player)
    {
        return player.ToSignal(player, AnimationPlayer.SignalName.AnimationFinished);
    }

    public static SignalAwaiter WaitForAnimationFinished(this AnimatedSprite2D player)
    {
        return player.ToSignal(player, AnimationPlayer.SignalName.AnimationFinished);
    }
}