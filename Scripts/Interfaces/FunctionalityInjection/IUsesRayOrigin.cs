
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// A proxy can have a single ray origin or multiple ray origins depending on the input device
    /// </summary>
    public interface IUsesRayOrigin
    {
        /// <summary>
        /// A transform at the origin of a ray, which is used as an identifier throughout the system for all sorts of
        /// things -- menu spawns, performing raycasts, direct selection, proxy node location, etc.
        /// </summary>
        Transform rayOrigin { set; }
    }
}

