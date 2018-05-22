
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    /// <summary>
    /// Provide the ability to request a corresponding ray origin for a node
    /// </summary>
    interface INodeToRay
   {
   }

    static class INodeToRayMethods
    {
        internal static Func<Node, Transform> requestRayOriginFromNode { private get; set; }

        /// <summary>
        /// Get the corresponding ray origin for a given node
        /// </summary>
        /// <param name="node">The node to request a ray origin for</param>
        internal static Transform RequestRayOriginFromNode(this INodeToRay obj, Node node)
        {
            return requestRayOriginFromNode(node);
        }
    }
}

