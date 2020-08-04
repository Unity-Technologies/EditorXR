using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to scene placement
    /// </summary>
    public interface IUsesPlaceSceneObject : IFunctionalitySubscriber<IProvidesPlaceSceneObject>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesPlaceSceneObject
    /// </summary>
    public static class UsesScenePlacementMethods
    {
        /// <summary>
        /// Method used to place objects in the scene/MiniWorld
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="transform">Transform of the GameObject to place</param>
        /// <param name="scale">Target scale of placed object</param>
        public static void PlaceSceneObject(this IUsesPlaceSceneObject user, Transform transform, Vector3 scale)
        {
#if !FI_AUTOFILL
            user.provider.PlaceSceneObject(transform, scale);
#endif
        }
    }
}
