using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to check whether objects contain the VR Player objects completely
    /// </summary>
    public interface IUsesContainsVRPlayerCompletely : IFunctionalitySubscriber<IProvidesContainsVRPlayerCompletely> { }

    /// <summary>
    /// Extension methods for implementors of IUsesContainsVRPlayerCompletely
    /// </summary>
    public static class UsesContainsVRPlayerCompletelyMethods
    {
        /// <summary>
        /// Checks whether the VR Player objects are contained completely within an object's bounding box
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="gameObject">The GameObject whose bounds to check</param>
        /// <returns>True if the VR Player objects are completely contained within the bounds of the given object</returns>
        public static bool ContainsVRPlayerCompletely(this IUsesContainsVRPlayerCompletely user, GameObject gameObject)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.ContainsVRPlayerCompletely(gameObject);
#endif
        }
    }
}
