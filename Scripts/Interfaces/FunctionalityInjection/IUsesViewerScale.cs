
using System;

namespace UnityEditor.Experimental.EditorVR
{
    public interface IUsesViewerScale
    {
    }

    public static class IUsesViewerScaleMethods
    {
        internal static Func<float> getViewerScale { get; set; }
        internal static Action<float> setViewerScale { get; set; }

        /// <summary>
        /// Returns the scale of the viewer object
        /// </summary>
        public static float GetViewerScale(this IUsesViewerScale obj)
        {
            return getViewerScale();
        }

        /// <summary>
        /// Set the scale of the viewer object
        /// </summary>
        /// <param name="scale">Uniform scale value in world space</param>
        public static void SetViewerScale(this IUsesViewerScale obj, float scale)
        {
            setViewerScale(scale);
        }
    }
}

