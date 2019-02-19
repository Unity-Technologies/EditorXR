using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Decorates objects which can delete objects from the scene
    /// </summary>
    public interface IDeleteSceneObject
    {
    }

    public static class IDeleteSceneObjectMethods
    {
        internal static Action<GameObject> deleteSceneObject { get; set; }

        /// <summary>
        /// Remove the game object from the scene
        /// </summary>
        /// <param name="go">The game object to delete from the scene</param>
        public static void DeleteSceneObject(this IDeleteSceneObject obj, GameObject go)
        {
            deleteSceneObject(go);
        }
    }
}
