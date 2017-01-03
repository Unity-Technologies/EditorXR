namespace UnityEngine.Experimental.EditorVR.Utilities
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

			public static Vector3 CalculateCubicBezierPoint(float t, Vector3[] points)
			{
				if (points.Length != 4)
					return Vector3.zero;

				var u = 1f - t;
				var tt = t * t;
				var uu = u * u;
				var uuu = uu * u;
				var ttt = tt * t;

				//first term
				var p = uuu * points[0];
				//second term
				p += 3f * uu * t * points[1];
				//third term
				p += 3f * u * tt * points[2];
				//fourth term
				p += ttt * points[3];

				return p;
			}

			public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
			{
				// This will have us converge on 98% of our target value within the smooth time
				// Reference: http://devblog.aliasinggames.com/smoothdamp/
				var correctSmoothTime = smoothTime / 3f;
				return Mathf.SmoothDamp(current, target, ref currentVelocity, correctSmoothTime, maxSpeed, deltaTime);
			}

			public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
			{
				// This will have us converge on 98% of our target value within the smooth time
				// Reference: http://devblog.aliasinggames.com/smoothdamp/
				var correctSmoothTime = smoothTime / 3f;
				return Vector3.SmoothDamp(current, target, ref currentVelocity, correctSmoothTime, maxSpeed, deltaTime);
			}

			/// <summary>
			/// Returns a rotation which only contains the yaw component of the given rotation
			/// </summary>
			/// <param name="rotation">The rotation we would like to constrain</param>
			/// <returns>A yaw-only rotation which matches the input's yaw</returns>
			public static Quaternion ConstrainYawRotation(Quaternion rotation)
			{
				var euler = rotation.eulerAngles;
				euler.x = 0;
				euler.z = 0;
				return Quaternion.Euler(euler);
			}

			/// <summary>
			/// Get the position and rotatoin difference between two objects for the purpose of maintaining that offset
			/// </summary>
			/// <param name="from">The object whose position will be changing (parent)</param>
			/// <param name="to">The object whose position will be updated (child)</param>
			/// <param name="positionOffset">The position vector from "from" to "to"</param>
			/// <param name="rotationOffset">The rotation which will rotate "from" to "to"</param>
			public static void GetTransformOffset(Transform from, Transform to, out Vector3 positionOffset, out Quaternion rotationOffset)
			{
				var inverseRotation = Quaternion.Inverse(from.rotation);
				positionOffset = inverseRotation * (to.transform.position - from.position);
				rotationOffset = inverseRotation * to.transform.rotation;
			}

			/// <summary>
			/// Set the position and rotation of the "child" transform given an offset from the parent (for independent transforms)
			/// </summary>
			/// <param name="parent">The transform we are offsetting from</param>
			/// <param name="child">The transform whose position we are setting</param>
			/// <param name="positionOffset">The position offset (local position)</param>
			/// <param name="rotationOffset">The rotation offset (local rotation)</param>
			public static void SetTransformOffset(Transform parent, Transform child, Vector3 positionOffset, Quaternion rotationOffset)
			{
				child.position = parent.position + parent.rotation * positionOffset;
				child.rotation = parent.rotation * rotationOffset;
			}

			/// <summary>
			/// Interpolates a source transform towards a destination
			/// </summary>
			/// <param name="source">The source Transform we are interpolating</param>
			/// <param name="targetPosition">The target position</param>
			/// <param name="targetRotation">The target rotation</param>
			/// <param name="t">Interpolation parameter for smooth transitions (Optional)</param>
			public static void LerpTransform(Transform source, Vector3 targetPosition, Quaternion targetRotation, float t = 1f)
			{
				source.position = Vector3.Lerp(source.position, targetPosition, t);
				source.rotation = Quaternion.Slerp(source.rotation, targetRotation, t);
			}
		}
	}
}