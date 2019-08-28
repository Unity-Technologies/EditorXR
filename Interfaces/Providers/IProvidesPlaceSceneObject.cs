using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to scene placement functionality
    /// </summary>
    public interface IProvidesPlaceSceneObject : IFunctionalityProvider
    {
        /// <summary>
        /// Method used to place objects in the scene/MiniWorld
        /// </summary>
        /// <param name="transform">Transform of the GameObject to place</param>
        /// <param name="scale">Target scale of placed object</param>
        void PlaceSceneObject(Transform transform, Vector3 scale);
    }
}
