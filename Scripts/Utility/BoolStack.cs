using Godot;
using System.Diagnostics;

namespace Elythia;

public class BoolStack
{
	// [Export]
	private int stack = 0;

	public bool Bool => stack > 0;

	public BoolStack()
	{
		stack = 0;
	}

	public BoolStack(int value = 0)
	{
		stack = value;
	}

	public BoolStack(bool value = false)
	{
		stack = value.Int();
	}

	public void AddStack()
	{
		stack++;
	}

	public void RemoveStack()
	{
		stack--;
	}

	public override string ToString()
	{
		return $"{Bool.ToString()}:{stack}";
	}

	public void CombineBool(bool value)
	{
		stack += value.Int();
	}

	public static BoolStack operator +(BoolStack instance, int value)
	{
		return new BoolStack(instance.stack + value);
	}

	public static BoolStack operator -(BoolStack instance, int value)
	{
		return new BoolStack(instance.stack - value);
	}

	public static BoolStack operator ++(BoolStack instance)
	{
		return new BoolStack(instance.stack + 1);
	}

	public static BoolStack operator --(BoolStack instance)
	{
		return new BoolStack(instance.stack - 1);
	}

	public static implicit operator bool(BoolStack instance)
	{
		Debug.Assert(instance.stack >= 0, "Stack is lopsided: stack is negative!");
		return instance.Bool;
	}

	public static explicit operator BoolStack(bool instance)
	{
		return instance ? new BoolStack(true) : new BoolStack(false);
	}

	public static implicit operator int(BoolStack instance)
	{
		Debug.Assert(instance.stack >= 0, "Stack is lopsided: stack is negative!");
		return instance.stack;
	}
}