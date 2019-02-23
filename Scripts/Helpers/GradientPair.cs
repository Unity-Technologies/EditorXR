using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    /// <summary>
    /// Gradient pair container class
    /// </summary>
    [Serializable]
    public struct GradientPair
    {
        /// <summary>
        /// First color in the gradient pair
        /// </summary>
        public Color a;

        /// <summary>
        /// Second color in the gradient pair
        /// </summary>
        public Color b;

        public GradientPair(Color a, Color b)
        {
            this.a = a;
            this.b = b;
        }

        /// <summary>
        /// Provide for lerping between two gradient pairs
        /// </summary>
        /// <param name="x">The first gradient pair</param>
        /// <param name="y">The second gradient pair</param>
        /// <param name="t">Amount for which to lerp between the first and second gradient pair</param>
        /// <returns>The lerped gradient pair result</returns>
        public static GradientPair Lerp(GradientPair x, GradientPair y, float t)
        {
            GradientPair r;
            r.a = Color.Lerp(x.a, y.a, t);
            r.b = Color.Lerp(x.b, y.b, t);
            return r;
        }
    }
}
