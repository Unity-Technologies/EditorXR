using System;

namespace UnityEngine.VR.Data
{
	[Serializable]
	public class IntVector3
	{
		public static readonly IntVector3 one = new IntVector3(1, 1, 1);

		public int x;
		public int y;
		public int z;

		// Adds two vectors.
		public static IntVector3 operator +(IntVector3 a, IntVector3 b)
		{
			return new IntVector3(a.x + b.x, a.y + b.y, a.z + b.z);
		}

		// Subtracts one vector from another.
		public static IntVector3 operator -(IntVector3 a, IntVector3 b)
		{
			return new IntVector3(a.x - b.x, a.y - b.y, a.z - b.z);
		}

		// Multiplies a vector by a number.
		public static IntVector3 operator *(IntVector3 a, int d)
		{
			return new IntVector3(a.x * d, a.y * d, a.z * d);
		}
		
		// Multiplies a vector by a number.
		public static IntVector3 operator *(int d, IntVector3 a)
		{
			return new IntVector3(a.x * d, a.y * d, a.z * d);
		}

		public static explicit operator IntVector3(Vector3 v)
		{
			return new IntVector3(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
		}

		public static explicit operator Vector3(IntVector3 v)
		{
			return new Vector3(v.x, v.y, v.z);
		}

		public IntVector3()
		{
		}

		public IntVector3(int _x, int _y, int _z)
		{
			x = _x;
			y = _y;
			z = _z;
		}

		public override bool Equals(object obj)
		{
			IntVector3 other = (IntVector3)obj;
			return other.x == x && other.y == y && other.z == z;
		}

		public override int GetHashCode()
		{
			return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("({0}, {1}, {2})", x, y, z);
		}
	}
}