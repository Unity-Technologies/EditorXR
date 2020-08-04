using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to check whether objects contain the VR Player objects completely
    /// </summary>
    public interface IProvidesContainsVRPlayerCompletely : IFunctionalityProvider
    {
        /// <summary>
        /// Checks whether the VR Player objects are contained completely within an object's bounding box
        /// </summary>
        /// <param name="gameObject">The GameObject whose bounds to check</param>
        /// <returns>True if the VR Player objects are completely contained within the bounds of the given object</returns>
        bool ContainsVRPlayerCompletely(GameObject gameObject);
    }
}
