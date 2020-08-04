using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provides methods and delegates used to directly select and grab scene objects
    /// </summary>
    public interface IGrabObjects : IUsesCanGrabObject
    {
        /// <summary>
        /// Transfer a held object between rayOrigins (i.e. dragging into the MiniWorld)
        /// </summary>
        /// <param name="rayOrigin">rayOrigin of current held object</param>
        /// <param name="destRayOrigin">Destination rayOrigin</param>
        /// <param name="deltaOffset">Change in position offset (added to GrabData.positionOffset)</param>
        void TransferHeldObjects(Transform rayOrigin, Transform destRayOrigin, Vector3 deltaOffset = default(Vector3));

        /// <summary>
        /// Drop objects held with a given node
        /// </summary>
        /// <param name="node">The node that is holding the objects</param>
        void DropHeldObjects(Node node);

        /// <summary>
        /// Stop acting on objects held with a given node
        /// </summary>
        /// <param name="node">The node that is holding the objects</param>
        void Suspend(Node node);

        /// <summary>
        /// Resume acting on objects held with a given node
        /// </summary>
        /// <param name="node">The node that is holding the objects</param>
        void Resume(Node node);

        /// <summary>
        /// Must be called by the implementer when an object has been grabbed
        /// Params: the rayOrign, the grabbed objects
        /// </summary>
        event Action<Transform, HashSet<Transform>> objectsGrabbed;

        /// <summary>
        /// Must be called by the implementer when objects have been dropped
        /// Params: the rayOrigin, the dropped objects
        /// </summary>
        event Action<Transform, Transform[]> objectsDropped;

        /// <summary>
        /// Must be called by the implementer when objects have been transferred
        /// Params: the source rayOrigin, the destination rayOrigin
        /// </summary>
        event Action<Transform, Transform> objectsTransferred;
    }
}
