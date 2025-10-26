using System.Linq;
using Godot;
using Godot.Collections;

namespace FPS_Mods.Scripts;

public static class Extensions
{
    public static float Lerp(this float num, float to, float weight)
    {
        return Mathf.Lerp(num, to, weight);
    }
    
    public static double Lerp(this double num, float to, float weight)
    {
        return Mathf.Lerp(num, to, weight);
    }
    
    public static double Lerp(this double num, double to, double weight)
    {
        return Mathf.Lerp(num, to, weight);
    }
    
    public static float Lerp(this float num, double to, double weight)
    {
        return (float)Mathf.Lerp(num, to, weight);
    }

    public static Vector3 XY(this Vector3 v)
    {
        return new Vector3(v.X, v.Y, 0);
    }
    
    public static Vector3 YZ(this Vector3 v)
    {
        return new Vector3(0, v.Y, v.Z);
    }
    public static Vector3 XZ(this Vector3 v)
    {
        return new Vector3(v.X, 0, v.Z);
    }

    public static float Map(this float value, float min1, float max1, float min2, float max2, bool clamp = false)
    {
        float val = min2 + (max2 - min2) * ((value - min1) / (max1 - min1));
        return clamp ? Mathf.Clamp(val, Mathf.Min(min2, max2), Mathf.Max(min2, max2)) : val;
    }

    public static Array<Node> GetAllChildren(this Node root, bool includeInternal=false)
    {
        var children = root.GetChildren(includeInternal);
        var results = children;
        foreach (var n in children)
        {
            results.AddRange(n.GetAllChildren(includeInternal));
        }
        return results;
    }
    
    public static Array<T> Select<[MustBeVariant] T>(this Array<Node> arr) where T:Node
    {
        return new Array<T>(arr.OfType<T>());
    }
    // public static Array<T> Select<[MustBeVariant] S, [MustBeVariant] T>(this Array<S> arr) where T:S
    // {
    //     return new Array<T>(arr.OfType<T>());
    // }
}