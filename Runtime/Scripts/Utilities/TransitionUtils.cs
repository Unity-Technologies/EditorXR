using System;
using UnityEngine;

namespace Unity.EditorXR.Utilities
{
    /// <summary>
    /// Transition utilities
    /// </summary>
    public static class TransitionUtils
    {
        /// <summary>
        /// Helper function for animating transitions
        /// </summary>
        /// <typeparam name="T">The type of property we're animating</typeparam>
        /// <param name="time">The current time value</param>
        /// <param name="state">Whether we are seeking the end value or the start value</param>
        /// <param name="lastState">The previous value of "state"</param>
        /// <param name="changeTime">The time when state changed</param>
        /// <param name="property">The current value of the property we are animating</param>
        /// <param name="propertyStart">The start value of this transition (value of "property" when state changed)</param>
        /// <param name="startValue">The start value of the transition (seek this value when state is false)</param>
        /// <param name="endValue">The end value of the transition (seek this value when state is true)</param>
        /// <param name="totalDuration">The total desired duration of this transition</param>
        /// <param name="approximately">Delegate for determining whether two values of this property are approximately equal</param>
        /// <param name="getPercentage">Delegate for determining how far along we are in the transition
        /// (i.e. propery is X percent of the way between start and end)</param>
        /// <param name="lerp">Delegate for determining whether two values of this property are approximately equal</param>
        /// <param name="setProperty">Delegate for determining whether two values of this property are approximately equal</param>
        /// <param name="setState">Whether to set lastState equal to state at the end of the method</param>
        /// <param name="complete">Delegate to be executed when the transition completes</param>
        public static void AnimateProperty<T>(float time, bool state, ref bool lastState, ref float changeTime,
            ref T property, ref T propertyStart, T startValue, T endValue, float totalDuration,
            Func<T, T, bool> approximately, Func<T, T, T, float> getPercentage, Func<T, T, float, T> lerp,
            Action<T> setProperty, bool setState = true, Action<T> complete = null) where T : struct
        {
            if (state != lastState)
            {
                changeTime = time;
                propertyStart = property;
            }

            var timeDiff = time - changeTime;
            var source = state ? startValue : endValue;
            var target = state ? endValue : startValue;
            if (!approximately(property, target))
            {
                var duration = (1 - getPercentage(propertyStart, target, source)) * totalDuration;
                var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(timeDiff / duration);
                if (smoothedAmount > 1)
                {
                    property = target;
                    propertyStart = property;
                    if (complete != null)
                        complete(target);
                }
                else
                {
                    property = lerp(propertyStart, target, smoothedAmount);
                }

                setProperty(property);
            }

            if (setState)
                lastState = state;
        }

        /// <summary>
        /// Get the percentage of the current value between source and target
        /// </summary>
        /// <param name="current">The current value</param>
        /// <param name="target">The target value</param>
        /// <param name="source">The source value</param>
        /// <returns>The percentage</returns>
        public static float GetPercentage(float current, float target, float source)
        {
            return (current - source) / (target - source);
        }

        /// <summary>
        /// Get the percentage of the current color between source and target
        /// </summary>
        /// <param name="currentColor">The current color</param>
        /// <param name="targetColor">The target color</param>
        /// <param name="sourceColor">The source color</param>
        /// <returns>The percentage</returns>
        public static float GetPercentage(Color currentColor, Color targetColor, Color sourceColor)
        {
            var current = currentColor.grayscale * currentColor.a;
            var target = targetColor.grayscale * targetColor.a;
            var source = sourceColor.grayscale * sourceColor.a;
            return (current - source) / (target - source);
        }

        /// <summary>
        /// Return true if one color is approximately equal to another
        /// </summary>
        /// <param name="a">Color a</param>
        /// <param name="b">Color b</param>
        /// <returns>Whether a is approximately equal to b</returns>
        public static bool Approximately(Color a, Color b)
        {
            return a == b;
        }
    }
}
