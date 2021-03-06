using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to delete
    /// </summary>
    public interface IUsesDeleteSceneObject : IFunctionalitySubscriber<IProvidesDeleteSceneObject>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesDeleteSceneObject
    /// </summary>
    public static class UsesDeleteSceneObjectMethods
    {
        /// <summary>
        /// Remove the game object from the scene
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="go">The game object to delete from the scene</param>
        public static void DeleteSceneObject(this IUsesDeleteSceneObject user, GameObject go)
        {
#if !FI_AUTOFILL
            user.provider.DeleteSceneObject(go);
#endif
        }
    }
}
