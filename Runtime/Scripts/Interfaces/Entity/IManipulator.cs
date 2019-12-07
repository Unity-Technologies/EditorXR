using System;
using Unity.Labs.EditorXR.Interfaces;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Gives decorated class access to the GameObject over which a particular ray is hovering
    /// </summary>
    public interface IManipulator
    {
        /// <summary>
        /// Delegate that processes the translation, using the vector3 passed in
        /// Caller also provides the ray origin that is doing the action, and which axes are constrained, if any
        /// </summary>
        Action<Vector3, Transform, AxisFlags> translate { set; }

        /// <summary>
        /// Delegate that processes the rotation, using the quaternion passed in
        /// </summary>
        Action<Quaternion, Transform> rotate { set; }

        /// <summary>
        /// Delegate that processes the scale, using the vector3 passed in
        /// </summary>
        Action<Vector3> scale { set; }

        /// <summary>
        /// Delegate that is called once after every drag starts
        /// </summary>
        event Action dragStarted;

        /// <summary>
        /// Delegate that is called once after every drag ends
        /// </summary>
        event Action<Transform> dragEnded;

        /// <summary>
        /// Bool denoting the drag-state of a manipulator that implements this interface
        /// </summary>
        bool dragging { get; }
    }
}
