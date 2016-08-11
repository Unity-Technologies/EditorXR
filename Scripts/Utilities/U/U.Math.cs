namespace UnityEngine.VR.Utilities
{
	using UnityEngine;

	/// <summary>
	/// EditorVR Utilities
	/// </summary>
	public static partial class U
	{
		/// <summary>
		/// Math related EditorVR utilities
		/// </summary>
		public static class Math
		{
			// snaps value to a unit. unit can be any number.
			// for example, with a unit of 0.2, 0.41 -> 0.4, and 0.52 -> 0.6
			public static float SnapValueToUnit(float value, float unit)
			{
				float mult = value / unit;
				// find lower and upper boundaries of snapping
				int lowerMult = Mathf.FloorToInt(mult);
				int upperMult = Mathf.CeilToInt(mult);
				float lowerBoundary = lowerMult * unit;
				float upperBoundary = upperMult * unit;
				// figure out which is closest
				float diffWithLower = value - lowerBoundary;
				float diffWithHigher = upperBoundary - value;
				return (diffWithLower < diffWithHigher) ? lowerBoundary : upperBoundary;
			}

			public static Vector3 SnapValuesToUnit(Vector3 values, float unit)
			{
				return new Vector3(SnapValueToUnit(values.x, unit),
								   SnapValueToUnit(values.y, unit),
								   SnapValueToUnit(values.z, unit));
			}

			// calculates easing
			// if snap is zero, no snapping is applied
			public static float Ease(float val, float valEnd, float easeDivider, float snap)
			{
				val += (valEnd - val) / easeDivider;
				if (snap != 0 && Mathf.Abs(val - valEnd) < snap) val = valEnd;
				return val;
			}

			// Like map in Processing.
			// E1 and S1 must be different, else it will break
			// val, in a, in b, out a, out b
			public static float Map(float val, float ia, float ib, float oa, float ob)
			{
				return oa + (ob - oa) * ((val - ia) / (ib - ia));
			}

			// Like map, but eases in.
			public static float MapInCubic(float val, float ia, float ib, float oa, float ob)
			{
				float t = (val - ia);
				float d = (ib - ia);
				t /= d;
				return oa + (ob - oa) * (t) * t * t;
			}

			// Like map, but eases out.
			public static float MapOutCubic(float val, float ia, float ib, float oa, float ob)
			{
				float t = (val - ia);
				float d = (ib - ia);
				t = (t / d) - 1;
				return oa + (ob - oa) * (t * t * t + 1);
			}

			// Like map, but eases in.
			public static float MapInSin(float val, float ia, float ib, float oa, float ob)
			{
				return oa + (ob - oa) * (1.0f - Mathf.Cos(((val - ia) / (ib - ia)) * Mathf.PI / 2));
			}

			// from http://wiki.unity3d.com/index.php/3d_Math_functions
			//create a vector of direction "vector" with length "size"
			public static Vector3 SetVectorLength(Vector3 vector, float size)
			{

				//normalize the vector
				Vector3 vectorNormalized = Vector3.Normalize(vector);

				//scale the vector
				return vectorNormalized *= size;
			}

			// from http://wiki.unity3d.com/index.php/3d_Math_functions
			//Get the intersection between a line and a plane. 
			//If the line and plane are not parallel, the function outputs true, otherwise false.
			public static bool LinePlaneIntersection(out Vector3 intersection, Vector3 linePoint, Vector3 lineVec, Vector3 planeNormal, Vector3 planePoint)
			{

				float length;
				float dotNumerator;
				float dotDenominator;
				Vector3 vector;
				intersection = Vector3.zero;

				//calculate the distance between the linePoint and the line-plane intersection point
				dotNumerator = Vector3.Dot((planePoint - linePoint), planeNormal);
				dotDenominator = Vector3.Dot(lineVec, planeNormal);

				//line and plane are not parallel
				if (dotDenominator != 0.0f)
				{
					length = dotNumerator / dotDenominator;

					//create a vector from the linePoint to the intersection point
					vector = SetVectorLength(lineVec, length);

					//get the coordinates of the line-plane intersection point
					intersection = linePoint + vector;

					return true;
				}

				//output not valid
				else
				{
					return false;
				}
			}

			// from http://wiki.unity3d.com/index.php/3d_Math_functions
			//This function returns a point which is a projection from a point to a line.
			//The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
			public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineDirection, Vector3 point)
			{
				//get vector from point on line to point in space
				Vector3 linePointToPoint = point - linePoint;
				float t = Vector3.Dot(linePointToPoint, lineDirection);
				return linePoint + lineDirection * t;
			}
		}
	}
}