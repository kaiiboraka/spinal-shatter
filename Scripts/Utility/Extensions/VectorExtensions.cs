using Elythia;
using Godot;
using static HorizontalDirection;
using static VerticalDirection;

public static class VectorExtensions
{
	public static Vector2 FromDegrees(this Vector2 vector, float angle)
	{
		return Vector2.FromAngle(Mathf.DegToRad(angle));
	}

	/// <summary>
	/// Returns which HorizontalDirection the vector is facing based on the sign of x.
	/// </summary>
	/// <param name="vector2"></param>
	/// <returns></returns>
	public static HorizontalDirection Horizontal(this Vector2 vector2)
	{
		HorizontalDirection direction = vector2.X switch
		{
			> Mathf.Epsilon => Right,
			< -Mathf.Epsilon => Left,
			_ => HorizontalDirection.None
		};

		return direction;
	}

	/// <summary>
	/// Returns which VerticalDirection the vector is facing based on the sign of y.
	/// </summary>
	/// <param name="vector2"></param>
	/// <returns></returns>
	public static VerticalDirection Vertical(this Vector2 vector2)
	{
		VerticalDirection direction = vector2.X switch
		{
			> .001f => Up,
			< -.001f => Down,
			_ => VerticalDirection.None
		};

		return direction;
	}

	public static Vector2 ClampBounds(this Vector2 vector2, float xBound, float yBound)
	{
		if (xBound < 0) xBound *= -1;
		if (yBound < 0) yBound *= -1;
		return new Vector2(Mathf.Clamp(vector2.X, -xBound, xBound), Mathf.Clamp(vector2.Y, -yBound, yBound));
	}

	public static Vector2 MultiplyPiecewise(this Vector2 vector2, float x, float y)
	{
		return vector2 with { X = vector2.X * x, Y = vector2.Y * y };
	}

	public static Vector2 FreezePiecewise(this Vector2 vector2, bool freezeX, bool freezeY)
	{
		return MultiplyPiecewise(vector2, (!freezeX).Int(), (!freezeY).Int());
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

	public static Vector3 RandomRange(this Vector3 v, float radius)
	{
		return new Vector3((float)GD.RandRange(-radius, radius), (float)GD.RandRange(-radius, radius), (float)GD.RandRange(-radius, radius));
	}

	#region Vector Rounders


	public static Vector2 WithPrecision(this Vector2 vector, int precision = 0)
	{
		return vector with {
			X = vector.X.RoundToPrecision(precision),
			Y = vector.Y.RoundToPrecision(precision)
		};
	}


	public static Vector3 WithPrecision(this Vector3 vector, int precision = 0)
	{
		return vector with {
			X = vector.X.RoundToPrecision(precision),
			Y = vector.Y.RoundToPrecision(precision),
			Z = vector.Z.RoundToPrecision(precision)
		};
	}

	public static Vector4 WithPrecision(this Vector4 vector, int precision = 0)
	{
		return vector with {
			X = vector.X.RoundToPrecision(precision),
			Y = vector.Y.RoundToPrecision(precision),
			Z = vector.Z.RoundToPrecision(precision),
			W = vector.W.RoundToPrecision(precision)
		};
	}

	public static Vector2 RoundToPrecision(this Vector2 vector, int precision = 0)
	{
		vector.X = vector.X.RoundToPrecision(precision);
		vector.Y = vector.Y.RoundToPrecision(precision);
		return vector;
	}

	public static Vector3 RoundToPrecision(this Vector3 vector, int precision = 0)
	{
		vector.X = vector.X.RoundToPrecision(precision);
		vector.Y = vector.Y.RoundToPrecision(precision);
		vector.Z = vector.Z.RoundToPrecision(precision);
		return vector;
	}

	public static Vector4 RoundToPrecision(this Vector4 vector, int precision = 0)
	{
		vector.X = vector.X.RoundToPrecision(precision);
		vector.Y = vector.Y.RoundToPrecision(precision);
		vector.Z = vector.Z.RoundToPrecision(precision);
		vector.W = vector.W.RoundToPrecision(precision);
		return vector;
	}

	#endregion

	#region Vector Flippers

	public static Vector2 FlipX(this Vector2 vector)
	{
		vector.X *= -1;
		return vector;
	}

	public static Vector2 FlipY(this Vector2 vector)
	{
		vector.Y *= -1;
		return vector;
	}

	public static Vector2 FaceLeft(this Vector2 vector)
	{
		return vector.X > 0 ? vector.FlipX() : vector;
	}

	public static Vector2 FaceRight(this Vector2 vector)
	{
		return vector.X < 0 ? vector.FlipX() : vector;
	}

	public static Vector2 FaceDown(this Vector2 vector)
	{
		return vector.Y.IsNeg() ? vector.FlipY() : vector;
	}

	public static Vector2 FaceUp(this Vector2 vector)
	{
		return vector.Y.IsPos() ? vector.FlipY() : vector;
	}

	public static Vector2I FlipX(this Vector2I vector)
	{
		vector.X *= -1;
		return vector;
	}

	public static Vector2I FlipY(this Vector2I vector)
	{
		vector.Y *= -1;
		return vector;
	}

	public static Vector2I FaceLeft(this Vector2I vector)
	{
		return vector.X > 0 ? vector.FlipX() : vector;
	}

	public static Vector2I FaceRight(this Vector2I vector)
	{
		return vector.X < 0 ? vector.FlipX() : vector;
	}

	public static Vector2I FaceDown(this Vector2I vector)
	{
		return vector.Y > 0 ? vector.FlipY() : vector;
	}

	public static Vector2I FaceUp(this Vector2I vector)
	{
		return vector.Y < 0 ? vector.FlipY() : vector;
	}


	public static Vector3 FlipX(this Vector3 vector)
	{
		vector.X *= -1;
		return vector;
	}

	public static Vector3 FlipY(this Vector3 vector)
	{
		vector.Y *= -1;
		return vector;
	}

	public static Vector3 FlipZ(this Vector3 vector)
	{
		vector.Z *= -1;
		return vector;
	}

	public static Vector3 FaceLeft(this Vector3 vector)
	{
		return vector.X > 0 ? vector.FlipX() : vector;
	}

	public static Vector3 FaceRight(this Vector3 vector)
	{
		return vector.X < 0 ? vector.FlipX() : vector;
	}

	public static Vector3 FaceDown(this Vector3 vector)
	{
		return vector.Y > 0 ? vector.FlipY() : vector;
	}

	public static Vector3 FaceUp(this Vector3 vector)
	{
		return vector.Y < 0 ? vector.FlipY() : vector;
	}

	public static Vector3 FaceZNeg(this Vector3 vector)
	{
		return vector.Z > 0 ? vector.FlipZ() : vector;
	}

	public static Vector3 FaceZPos(this Vector3 vector)
	{
		return vector.Z < 0 ? vector.FlipZ() : vector;
	}


	public static Vector3I FlipX(this Vector3I vector)
	{
		vector.X *= -1;
		return vector;
	}

	public static Vector3I FlipY(this Vector3I vector)
	{
		vector.Y *= -1;
		return vector;
	}

	public static Vector3I FlipZ(this Vector3I vector)
	{
		vector.Z *= -1;
		return vector;
	}

	public static Vector3I FaceLeft(this Vector3I vector)
	{
		return vector.X > 0 ? vector.FlipX() : vector;
	}

	public static Vector3I FaceRight(this Vector3I vector)
	{
		return vector.X < 0 ? vector.FlipX() : vector;
	}

	public static Vector3I FaceDown(this Vector3I vector)
	{
		return vector.Y > 0 ? vector.FlipY() : vector;
	}


	public static Vector3I FaceUp(this Vector3I vector)
	{
		return vector.Y < 0 ? vector.FlipY() : vector;
	}

	public static Vector3I FaceZNeg(this Vector3I vector)
	{
		return vector.Z > 0 ? vector.FlipZ() : vector;
	}

	public static Vector3I FaceZPos(this Vector3I vector)
	{
		return vector.Z < 0 ? vector.FlipZ() : vector;
	}


	public static Vector4 FlipX(this Vector4 vector)
	{
		vector.X *= -1;
		return vector;
	}

	public static Vector4 FlipY(this Vector4 vector)
	{
		vector.Y *= -1;
		return vector;
	}

	public static Vector4 FlipZ(this Vector4 vector)
	{
		vector.Z *= -1;
		return vector;
	}

	public static Vector4 FlipW(this Vector4 vector)
	{
		vector.W *= -1;
		return vector;
	}

	public static Vector4 FaceLeft(this Vector4 vector)
	{
		return vector.X > 0 ? vector.FlipX() : vector;
	}

	public static Vector4 FaceRight(this Vector4 vector)
	{
		return vector.X < 0 ? vector.FlipX() : vector;
	}

	public static Vector4 FaceDown(this Vector4 vector)
	{
		return vector.Y > 0 ? vector.FlipY() : vector;
	}

	public static Vector4 FaceUp(this Vector4 vector)
	{
		return vector.Y < 0 ? vector.FlipY() : vector;
	}

	public static Vector4 FaceZNeg(this Vector4 vector)
	{
		return vector.Z > 0 ? vector.FlipZ() : vector;
	}

	public static Vector4 FaceZPos(this Vector4 vector)
	{
		return vector.Z < 0 ? vector.FlipZ() : vector;
	}

	public static Vector4 FaceWNeg(this Vector4 vector)
	{
		return vector.W > 0 ? vector.FlipW() : vector;
	}

	public static Vector4 FaceWPos(this Vector4 vector)
	{
		return vector.W < 0 ? vector.FlipW() : vector;
	}

	#endregion

	public static bool IsAbove(this Vector2 self, Vector2 target)
	{
		return self.Y < target.Y;
	}

	public static bool IsBelow(this Vector2 self, Vector2 target)
	{
		return self.Y > target.Y;
	}

	public static bool IsAbove(this Vector2 self, Node2D target)
	{
		return self.Y < target.GlobalPosition.Y;
	}

	public static bool IsBelow(this Vector2 self, Node2D target)
	{
		return self.Y > target.GlobalPosition.Y;
	}

	public static bool IsBehind(this Vector2 self, Node2D target)
	{
		return target.FacingLeft() && self.IsRightOf(target.GlobalPosition) ||
			   target.FacingRight() && self.IsLeftOf(target.GlobalPosition);
	}

	public static bool IsLeftOf(this Vector2 self, Vector2 target)
	{
		return self.X < target.X;
	}

	public static bool IsRightOf(this Vector2 self, Vector2 target)
	{
		return self.X > target.X;
	}

	public static bool CloseTo(this Vector2 vec, Vector2 other, float threshold)
	{
		return vec.DistanceTo(other) < threshold;
	}

	public static bool CloseTo(this Vector3 vec, Vector3 other, float threshold)
	{
		return vec.DistanceTo(other) < threshold;
	}

	public static float HorizontalDistanceTo(this Vector3 self, Vector3 other)
	{
		return Mathf.Abs(self.X - other.X);
	}

	public static float HorizontalDistanceTo(this Vector2 self, Vector2 other)
	{
		return Mathf.Abs(self.X - other.X);
	}

	public static float VerticalDistanceTo(this Vector3 self, Vector3 other)
	{
		return Mathf.Abs(self.Y - other.Y);
	}

	public static float VerticalDistanceTo(this Vector2 self, Vector2 other)
	{
		return Mathf.Abs(self.Y - other.Y);
	}


}