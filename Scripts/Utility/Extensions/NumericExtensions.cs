#if GODOT
using MATHLIB = Godot.Mathf;
using MATHCONST = Godot.Mathf;
using Godot;
using Godot.Collections;
#endif

#if UNITY_64
using MATHLIB = Godot.Mathf;
using MATHCONST = System.Single;
using Godot;
#endif

using System;
using System.Linq;



public static class NumericExtensions
{
    /// <summary>
    /// true: 1
    /// <br/>
    /// false: 0
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public static int Int(this bool b)
    {
        return b ? 1 : 0;
    }

    /// <summary>
    /// true: 1
    /// <br/>
    /// false: -1
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public static int IntNeg(this bool b)
    {
        return b ? 1 : -1;
    }

    public static bool Bool(this int x)
    {
        return x != 0;
    }

    public static bool Between(this float x, float min, float max)
    {
        return x.InRange(min, max);
    }

    public static bool Between(this double x, double min, double max)
    {
        return x.InRange(min, max);
    }

    public static bool Between(this int x, int min, int max)
    {
        return x.InRange(min, max);
    }

    public static bool Between(this long x, long min, long max)
    {
        return x.InRange(min, max);
    }

    public static bool InRange(this int x, int min = 0, int max = Int32.MaxValue, 
                               bool includeMin = true, bool includeMax = false)
    {
        return includeMin && includeMax ? min <= x && x <= max :
            includeMin && !includeMax ? min <= x && x < max :
            !includeMin && includeMax ? min < x && x <= max :
            !includeMin && !includeMax ? min < x && x < max :
            throw new ArgumentOutOfRangeException(nameof(includeMin));
    }

    public static bool InRange(this float x, float min = 0, float max = float.MaxValue, 
                               bool includeMin = true, bool includeMax = false)
    {
        return includeMin && includeMax ? min <= x && x <= max :
            includeMin && !includeMax ? min <= x && x < max :
            !includeMin && includeMax ? min < x && x <= max :
            !includeMin && !includeMax ? min < x && x < max :
            throw new ArgumentOutOfRangeException(nameof(includeMin));
    }

    public static bool InRange(this double x, double min = 0, double max = double.MaxValue, 
                               bool includeMin = true, bool includeMax = false)
    {
        return includeMin && includeMax ? min <= x && x <= max :
            includeMin && !includeMax ? min <= x && x < max :
            !includeMin && includeMax ? min < x && x <= max :
            !includeMin && !includeMax ? min < x && x < max :
            throw new ArgumentOutOfRangeException(nameof(includeMin));
    }

    public static bool InRange(this long x, long min = 0, long max = long.MaxValue, 
                               bool includeMin = true, bool includeMax = false)
    {
        return includeMin && includeMax ? min <= x && x <= max :
            includeMin && !includeMax ? min <= x && x < max :
            !includeMin && includeMax ? min < x && x <= max :
            !includeMin && !includeMax ? min < x && x < max :
            throw new ArgumentOutOfRangeException(nameof(includeMin));
    }

    public static bool IsPos(this float x)
    {
        return x > 0;
    }
    
    public static bool IsPos(this int x)
    {
        return x > 0;
    }

    public static bool IsNeg(this float x)
    {
        return x < -0;
    }
    
    public static bool IsNeg(this int x)
    {
        return x < -0;
    }

    public static HorizontalDirection HorizontalDirection(this int x)
    {
        return (HorizontalDirection)x.Sign();
    }

    public static HorizontalDirection HorizontalDirection(this float x)
    {
        return (HorizontalDirection)x.Sign();
    }

    public static HorizontalDirection HorizontalDirection(this double x)
    {
        return (HorizontalDirection)x.Sign();
    }

    public static VerticalDirection VerticalDirection(this int x)
    {
        return (VerticalDirection)x.Sign();
    }

    public static VerticalDirection VerticalDirection(this float x)
    {
        return (VerticalDirection)x.Sign();
    }

    public static VerticalDirection VerticalDirection(this double x)
    {
        return (VerticalDirection)x.Sign();
    }


    
    public static double Lerp(this double num, float to, float weight)
    {
        return MATHLIB.Lerp(num, to, weight);
    }

    public static float Lerp(this float num, float to, float weight)
    {
        return MATHLIB.Lerp(num, to, weight);
    }

    public static double Lerp(this double num, double to, double weight)
    {
        return MATHLIB.Lerp(num, to, weight);
    }
    
    public static float Lerp(this float num, double to, double weight)
    {
        return (float)MATHLIB.Lerp(num, to, weight);
    }

    public static float MoveTowardZero(this float x, float rate)
    {
        return Mathf.MoveToward(x, 0, rate);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="original"></param>
    /// <param name="max"></param>
    /// <param name="min"></param>
    /// <returns></returns>
    public static int RandomRangeFromMax(this int max, int min = 0)
    {
        // int i = int.Min(original, max);
        // var rand = (int)(GD.Randi() % (max + 1) + min);
	    // return Mathf.Clamp(rand, min, max);
	    return (int)(GD.Randi() % (max + 1) + min);
    }

    public static int RandomRangeFromMin(this int min, int max)
    {
        // int i = int.Min(original, max);
        // var rand = (int)(GD.Randi() % (max + 1) + min);
        // return Mathf.Clamp(rand, min, max);
        return (int)(GD.Randi() % (max + 1) + min);
    }

    public static float RandomRangeFromMax(this float max, float min = 0, bool centerAtZero = false)
    {
        var roll = GD.Randf() * max;
        if (centerAtZero)
        {
            var offset = max / 2f;
            return (min == 0 ? roll - max / 2 : Mathf.Clamp(roll, min, max)) - offset;
        }
        else
        {
            return min == 0 ? roll : Mathf.Clamp(roll, min, max);
        }

    }

    public static float RandomRangeFromMin(this float min, float max, bool centerAtZero = false)
    {
        var roll = GD.Randf() * max;
        if (centerAtZero)
        {
            var offset = max / 2f;
            return (min == 0 ? roll - max / 2 : Mathf.Clamp(roll, min, max)) - offset;
        }
        else
        {
            return min == 0 ? roll : Mathf.Clamp(roll, min, max);
        }
    }

    public static bool IsZero(this float num) => num is > -MATHCONST.Epsilon and < MATHCONST.Epsilon;

    public static bool FloatEqualsApprox(this float x, float y, float tolerance = MATHCONST.Epsilon)
    {
        return MATHLIB.Abs(x - y) <= tolerance;
    }

    public static bool FloatCloseTo(this float num, float closeTo, float byTolerance = MATHCONST.Epsilon)
    {
        return num >= MATHLIB.Abs(closeTo - byTolerance);
    }

    public static float EaseInOut(this float x, float easeAmount)
    {
        if (easeAmount == 0) return x;
        // GD.Print("EASING");
        // TODO : clamp for proper inout behavior (need it > 1. 0<>1 is OutIn)
        float a = easeAmount + 1;
        return MATHLIB.Pow(x, a) / (MATHLIB.Pow(x, a) + MATHLIB.Pow(1 - x, a));
    }
    
    public static float RoundToPrecision(this float value, int places = 2)
    {
        var tens = Mathf.Pow(10, Mathf.Max(0, places));
        float rounded = Mathf.Round(value * tens);
        return rounded / tens;
    }
    
    public static double RoundToPrecision(this double value, int places = 2)
    {
        var tens = Mathf.Pow(10, Mathf.Max(0, places));
        double rounded = Mathf.Round(value * tens);
        return rounded / tens;
    }

    public static float Pow(this float x, float pow)
    {
        return Mathf.Pow(x, pow);
    }
    
    public static float Pow(this int x, int pow)
    {
        return Mathf.Pow(x, pow);
    }
    
    public static float Pow(this int x, float pow)
    {
        return Mathf.Pow(x, pow);
    }
    
    public static double Pow(this double x, double pow)
    {
        return Mathf.Pow(x, pow);
    }

    public static float Sqrt(this float x)
    {
        return MATHLIB.Sqrt(x);
    }
    
    public static double Sqrt(this double x)
    {
        return MATHLIB.Sqrt(x);
    }

    public static float Sqrt(this int x)
    {
        return MATHLIB.Sqrt(x);
    }

    public static int Clamp(this int x, int min, int max)
    {
        return MATHLIB.Clamp(x, min, max);
    }

    public static float Clamp(this float x, float min, float max)
    {
        return MATHLIB.Clamp(x, min, max);
    }

    public static double Clamp(this double x, double min, double max)
    {
        return MATHLIB.Clamp(x, min, max);
    }
    
    public static int Clamp01(this int x)
    {
        return MATHLIB.Clamp(x, 0,1);
    }

    public static float Clamp01(this float x)
    {
        return MATHLIB.Clamp(x, 0f,1f);
    }

    public static double Clamp01(this double x)
    {
        return MATHLIB.Clamp(x, 0,1);
    }


    public static float Ceiling(this float x)
    {
        return MATHLIB.Ceil(x);
    }

    public static float Floor(this float x)
    {
        return MATHLIB.Floor(x);
    }

    public static int CeilingToInt(this float x)
    {
        return MATHLIB.CeilToInt(x);
    }

    public static int FloorToInt(this float x)
    {
        return MATHLIB.FloorToInt(x);
    }

    public static double Ceiling(this double x)
    {
        return MATHLIB.Ceil(x);
    }

    public static double Floor(this double x)
    {
        return MATHLIB.Floor(x);
    }

    public static int CeilingToInt(this double x)
    {
        return MATHLIB.CeilToInt(x);
    }

    public static int FloorToInt(this double x)
    {
        return MATHLIB.FloorToInt(x);
    }

    public static float Round(this float x)
    {
        return MATHLIB.Round(x);
    }

    public static int RoundToInt(this float x)
    {
        return MATHLIB.RoundToInt(x);
    }

    public static double Round(this double x)
    {
        return MATHLIB.Round(x);
    }

    public static int RoundToInt(this double x)
    {
        return MATHLIB.RoundToInt(x);
    }


    public static int AtLeastZero(this int x)
    {
        return x < 0 ? 0 : x;
    }

    public static float AtLeastZero(this float x)
    {
        return x < 0f ? 0f : x;
    }

    public static double AtLeastZero(this double x)
    {
        return x < 0f ? 0f : x;
    }

    public static float PercentClamp(this float x)
    {
        return Mathf.Clamp(x, 0, Constants.MAX_PERCENT);
    }

    public static int PercentClamp(this int x)
    {
        return (int)Mathf.Clamp(x, 0, Constants.MAX_PERCENT);
    }

    public static float MsToSec(this float x, int precision = 2)
    {
        var amount = (x / 1000f);
        return precision > 0 ? amount.RoundToPrecision(precision) : (int)amount;
    }

    public static double MsToSec(this double x, int precision = 2)
    {
        var amount = (x / 1000d);
        return precision > 0 ? amount.RoundToPrecision(precision) : (int)amount;
    }

    public static double Sign(this double x)
    {
        return Mathf.Sign(x);
    }
    public static float Sign(this float x)
    {
        return Mathf.Sign(x);
    }
    public static int Sign(this int x)
    {
        return Math.Sign(x);
    }

    public static int TrueMod(this int x, int mod)
    {
        return ((x % mod) + mod) % mod;
    }

    public static float Map(this float value, float oldLow, float oldHigh, float newLow, float newHigh, bool clamp = false)
    {
        float val = newLow + (newHigh - newLow) * ((value - oldLow) / (oldHigh - oldLow));
        return !clamp ? val : MATHLIB.Clamp(val, MATHLIB.Min(newLow, newHigh), MATHLIB.Max(newLow, newHigh));
    }

    public static double Map(this double value, double oldLow, double oldHigh, double newLow, double newHigh, bool clamp = false)
    {
        double val = newLow + (newHigh - newLow) * ((value - oldLow) / (oldHigh - oldLow));
        return !clamp ? val : MATHLIB.Clamp(val, MATHLIB.Min(newLow, newHigh), MATHLIB.Max(newLow, newHigh));
    }

    public static int Map(this int value, int oldLow, int oldHigh, int newLow, int newHigh, bool clamp = false)
    {
        int val = newLow + (newHigh - newLow) * ((value - oldLow) / (oldHigh - oldLow));
        return !clamp ? val : MATHLIB.Clamp(val, MATHLIB.Min(newLow, newHigh), MATHLIB.Max(newLow, newHigh));
    }

    public static float LerpMap(this float value, float oldLow, float oldHigh, float newLow, float newHigh,
                                bool clamp = false)
    {
        float t = Mathf.InverseLerp(oldLow, oldHigh, value);
        return Mathf.Lerp(newLow, newHigh, t);
    }

    public static double LerpMap(this double value, double oldLow, double oldHigh, double newLow, double newHigh,
                                bool clamp = false)
    {
        double t = Mathf.InverseLerp(oldLow, oldHigh, value);
        return Mathf.Lerp(newLow, newHigh, t);
    }

    public static int LerpMap(this int value, int oldLow, int oldHigh, int newLow, int newHigh,
                                bool clamp = false)
    {
        float t = Mathf.InverseLerp(oldLow, oldHigh, value);
        return (int)Mathf.Lerp(newLow, newHigh, t);
    }

    
}

