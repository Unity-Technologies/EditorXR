using UnityEngine;

public static class Vector3Extensions
{
	public static Vector3 Inverse(this Vector3 vec)
	{
		return new Vector3(1 / vec.x, 1 / vec.y, 1 / vec.z);
	}
}