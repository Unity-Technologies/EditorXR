#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    /// <summary>
    /// Transition utilities
    /// </summary>
    public static class TransitionUtils
    {
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

        public static float GetPercentage(float current, float target, float source)
        {
            return (current - source) / (target - source);
        }

        public static float GetPercentage(Color currentColor, Color targetColor, Color sourceColor)
        {
            var current = currentColor.grayscale * currentColor.a;
            var target = targetColor.grayscale * targetColor.a;
            var source = sourceColor.grayscale * sourceColor.a;
            return (current - source) / (target - source);
        }

        public static bool Approximately(Color a, Color b)
        {
            return a == b;
        }
    }
}
#endif
