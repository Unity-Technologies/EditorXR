using System;
using Unity.Labs.EditorXR.Interfaces;
using UnityEngine;

namespace Unity.Labs.EditorXR.Core
{
    /// <summary>
    /// Provide the ability to request a corresponding node for a ray origin
    /// </summary>
    interface IRayToNode
   {
   }

    static class IRayToNodeMethods
    {
        internal static Func<Transform, Node> requestNodeFromRayOrigin { private get; set; }

        /// <summary>
        /// Get the corresponding node for a given ray origin
        /// </summary>
        /// <param name="rayOrigin">The ray origin to request a node for</param>
        internal static Node RequestNodeFromRayOrigin(this IRayToNode obj, Transform rayOrigin)
        {
            return requestNodeFromRayOrigin(rayOrigin);
        }
    }
}
