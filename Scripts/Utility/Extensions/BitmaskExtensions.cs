using System;

public static class BitmaskExtensions
{
    // public static T SetBitFlag<T>(this T flags, T flag, bool value) where T : struct, Enum
    // {
    //     // Check if the underlying type is byte; if not, throw an exception.
    //     if (Enum.GetUnderlyingType(typeof(T)) != typeof(byte))
    //     {
    //         throw new NotSupportedException($"The underlying type of enum {typeof(T).Name} is not a byte.");
    //     }
    //
    //     byte flagsByte = Convert.ToByte(flags);
    //     byte flagByte = Convert.ToByte(flag);
    //
    //     if (value)
    //     {
    //         flagsByte |= flagByte;
    //     }
    //     else
    //     {
    //         flagsByte &= (byte)~flagByte;
    //     }
    //
    //     return (T)Convert.ChangeType(flagsByte, typeof(T));
    // }

    public static byte SetBitFlag(this byte flags, byte flag, bool value)
    {
        if (value)
        {
            flags |= flag;
        }
        else
        {
            flags &= (byte)~flag;
        }

        return flags;
    }

    public static uint SetBitFlag(this uint flags, uint FLAG, bool value)
    {
        if (value)
        {
            flags |= FLAG;
        }
        else
        {
            flags &= ~FLAG;
        }

        return flags;
    }

    public static bool HasFlag(this uint mask, int flag)
    {
        return (mask & flag) > 0;
    }

    public static bool LayerActive(this uint mask, int layer)
    {
        return (mask & layer) > 0;
    }

    public static uint WithLayer(this uint layerMask, int layer)
    {
        return layerMask | (uint)(1 << layer);
    }

    public static uint WithoutLayer(this uint layerMask, int layer)
    {
        return layerMask & ~ (uint)(1 << layer);
    }
}