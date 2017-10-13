using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Implemented by classes that would like to override the default highlighting behavior
    /// </summary>
    public interface ICustomHighlight
    {
        /// <summary>
        /// Method which will be called when highlighting each object
        /// </summary>
        /// <param name="go">The object which will be highlighted</param>
        /// <param name="material">The material which would be used to highlight it</param>
        /// <returns>Whether to block the normal highlight method</returns>
        bool OnHighlight(GameObject go, Material material);
    }
}
