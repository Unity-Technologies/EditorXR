using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    /// <summary>
    /// Extended math related utilities (named MathUtilsExt to not collide with MathUtils)
    /// </summary>
    static class MathUtilsExt
    {
        // from http://wiki.unity3d.com/index.php/3d_Math_functions
        //create a vector of direction "vector" with length "size"
        public static Vector3 SetVectorLength(Vector3 vector, float size)
        {
            //normalize the vector
            var vectorNormalized = Vector3.Normalize(vector);

            //scale the vector
            vectorNormalized *= size;

            return vectorNormalized;
        }

        // from http://wiki.unity3d.com/index.php/3d_Math_functions
        //Get the intersection between a line and a plane.
        //If the line and plane are not parallel, the function outputs true, otherwise false.
        public static bool LinePlaneIntersection(out Vector3 intersection, Vector3 linePoint, Vector3 lineVec,
            Vector3 planeNormal, Vector3 planePoint)
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
            if (!Mathf.Approximately(dotDenominator, 0.0f))
            {
                length = dotNumerator / dotDenominator;

                //create a vector from the linePoint to the intersection point
                vector = SetVectorLength(lineVec, length);

                //get the coordinates of the line-plane intersection point
                intersection = linePoint + vector;

                return true;
            }

            //output not valid
            return false;
        }

        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            // This will have us converge on 98% of our target value within the smooth time
            // Reference: http://devblog.aliasinggames.com/smoothdamp/
            var correctSmoothTime = smoothTime / 3f;
            return Mathf.SmoothDamp(current, target, ref currentVelocity, correctSmoothTime, maxSpeed, deltaTime);
        }

        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime,
            float maxSpeed, float deltaTime)
        {
            // This will have us converge on 98% of our target value within the smooth time
            // Reference: http://devblog.aliasinggames.com/smoothdamp/
            var correctSmoothTime = smoothTime / 3f;
            return Vector3.SmoothDamp(current, target, ref currentVelocity, correctSmoothTime, maxSpeed, deltaTime);
        }

        /// <summary>
        /// Get the position and rotation difference between two objects for the purpose of maintaining that offset
        /// </summary>
        /// <param name="from">The object whose position will be changing (parent)</param>
        /// <param name="to">The object whose position will be updated (child)</param>
        /// <param name="positionOffset">The position vector from "from" to "to"</param>
        /// <param name="rotationOffset">The rotation which will rotate "from" to "to"</param>
        public static void GetTransformOffset(Transform from, Transform to, out Vector3 positionOffset,
            out Quaternion rotationOffset)
        {
            var inverseRotation = Quaternion.Inverse(from.rotation);
            positionOffset = inverseRotation * (to.position - from.position);
            rotationOffset = inverseRotation * to.rotation;
        }

        /// <summary>
        /// Set the position and rotation of the "child" transform given an offset from the parent (for independent transforms)
        /// </summary>
        /// <param name="parent">The transform we are offsetting from</param>
        /// <param name="child">The transform whose position we are setting</param>
        /// <param name="positionOffset">The position offset (local position)</param>
        /// <param name="rotationOffset">The rotation offset (local rotation)</param>
        public static void SetTransformOffset(Transform parent, Transform child, Vector3 positionOffset,
            Quaternion rotationOffset)
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
        public static void LerpTransform(Transform source, Vector3 targetPosition, Quaternion targetRotation,
            float t = 1f)
        {
            source.position = Vector3.Lerp(source.position, targetPosition, t);
            source.rotation = Quaternion.Slerp(source.rotation, targetRotation, t);
        }

        /// <summary>
        /// Perform a smooth in+out interpolation of a 0-1 lerp value
        /// </summary>
        /// <param name="lerpAmount">The 0-1 lerp value that is to be shaped by this function</param>
        /// <returns>The 0-1 smoothed lerp value</returns>
        public static float SmoothInOutLerpFloat(float lerpAmount)
        {
            // https://www.wolframalpha.com/input/?i=t%5E3+*+(t+*+(6+*+t+-+15)+%2B+10)
            // t^3 * (t * (6 * t - 15) + 10)
            return Mathf.Pow(lerpAmount, 3) * (lerpAmount * (6f * lerpAmount - 15f) + 10f);
        }

        /// <summary>
        /// Get a Vector3 corresponding to the axis described by this AxisFlags
        /// </summary>
        /// <returns>The axis</returns>
        public static Vector3 GetAxis(this AxisFlags @this)
        {
            return new Vector3(
                (@this & AxisFlags.X) != 0 ? 1 : 0,
                (@this & AxisFlags.Y) != 0 ? 1 : 0,
                (@this & AxisFlags.Z) != 0 ? 1 : 0
            );
        }
    }
}
