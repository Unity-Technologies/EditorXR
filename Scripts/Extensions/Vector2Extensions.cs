using UnityEngine;

public static class Vector2Extensions
{
	public static Vector2 Inverse(this Vector2 vec)
	{
		return new Vector2(1 / vec.x, 1 / vec.y);
	}

	public static float MinComponent(this Vector2 vec)
	{
		return Mathf.Min(vec.x, vec.y);
	}

	public static float MaxComponent(this Vector2 vec)
	{
		return Mathf.Max(vec.x, vec.y);
	}

	public static Vector2 Abs(this Vector2 vec)
	{
		vec.x = Mathf.Abs(vec.x);
		vec.y = Mathf.Abs(vec.y);
		return vec;
	}
}