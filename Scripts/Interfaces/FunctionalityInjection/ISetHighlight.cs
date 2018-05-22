
using System.Collections;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class ability to highlight a given GameObject
    /// </summary>
    public interface ISetHighlight
    {
    }

    public static class ISetHighlightMethods
    {
        internal delegate void SetHighlightDelegate(GameObject go, bool active, Transform rayOrigin = null, Material material = null, bool force = false, float duration = 0f);

        internal delegate IEnumerator SetBlinkingHighlightDelegate(GameObject go, bool active, Transform rayOrigin = null,
            Material material = null, bool force = false, float dutyPercent = .75f, float cycleDuration = .5f);

        internal static SetHighlightDelegate setHighlight { get; set; }

        internal static SetBlinkingHighlightDelegate setBlinkingHighlight { get; set; }

        /// <summary>
        /// Method for highlighting objects
        /// </summary>
        /// <param name="go">The object to highlight</param>
        /// <param name="active">Whether to add or remove the highlight</param>
        /// <param name="rayOrigin">RayOrigin that hovered over the object (optional)</param>
        /// <param name="material">Custom material to use for this object</param>
        /// <param name="force">Force the setting or unsetting of the highlight</param>
        /// <param name="duration">The duration for which to show this highlight. Keep default value of 0 to show until explicitly hidden</param>
        public static void SetHighlight(this ISetHighlight obj, GameObject go, bool active, Transform rayOrigin = null, Material material = null, bool force = false, float duration = 0f)
        {
            setHighlight(go, active, rayOrigin, material, force, duration);
        }

        /// <summary>
        /// Method for highlighting objects
        /// </summary>
        /// <param name="go">The object to highlight</param>
        /// <param name="active">Whether to add or remove the highlight</param>
        /// <param name="rayOrigin">RayOrigin that hovered over the object (optional)</param>
        /// <param name="material">Custom material to use for this object</param>
        /// <param name="force">Force the setting or unsetting of the highlight</param>
        /// <param name="duration">The duration for which to show this highlight. Keep default value of 0 to show until explicitly hidden</param>
        public static IEnumerator SetBlinkingHighlight(this ISetHighlight obj, GameObject go, bool active, Transform rayOrigin = null,
            Material material = null, bool force = false, float dutyPercent = 0.75f, float cycleDuration = .8f)
        {
            return setBlinkingHighlight(go, active, rayOrigin, material, force, dutyPercent, cycleDuration);
        }
    }
}

