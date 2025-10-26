using System;
using Elythia;
using Godot;

public static class EnumExtensions
{
    public static HorizontalDirection GetOppositeDirection(this HorizontalDirection faceDirection)
    {
        return faceDirection switch
        {
            HorizontalDirection.Left => HorizontalDirection.Right,
            HorizontalDirection.Right => HorizontalDirection.Left,
            _ => HorizontalDirection.None
        };
    }

    public static VerticalDirection GetOppositeDirection(this VerticalDirection faceDirection)
    {
        return faceDirection switch
        {
            VerticalDirection.Up => VerticalDirection.Down,
            VerticalDirection.Down => VerticalDirection.Up,
            _ => VerticalDirection.None
        };
    }

    public static Vector2 FacingVector(this HorizontalDirection faceDirection)
    {
        return faceDirection switch
        {
            HorizontalDirection.Right => Vector2.Right,
            HorizontalDirection.Left => Vector2.Left,
            _ => Vector2.Zero
        };
    }

    public static Vector2 FacingVector(this VerticalDirection faceDirection)
    {
        return faceDirection switch
        {
            VerticalDirection.Up => Vector2.Up,
            VerticalDirection.Down => Vector2.Down,
            _ => Vector2.Zero
        };
    }

    public static int FacingInt(this HorizontalDirection faceDirection)
    {
        return faceDirection switch
        {
            HorizontalDirection.Right => 1,
            HorizontalDirection.Left => -1,
            _ => 0
        };
    }

    public static int FacingInt(this VerticalDirection faceDirection)
    {
        return faceDirection switch
        {
            VerticalDirection.Up => -1,
            VerticalDirection.Down => 1,
            _ => 0
        };
    }

    public static string GetValueSubstring(string value)
    {
        return value.Substring(value.LastIndexOf('.') + 1);
    }
}