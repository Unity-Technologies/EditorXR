
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Decorates types that need to move the camera rig
    /// </summary>
    public interface IMoveCameraRig
    {
    }

    public static class IMoveCameraRigMethods
    {
        internal delegate void MoveCameraRigDelegate(Vector3 position, Vector3? viewDirection = null);

        internal static MoveCameraRigDelegate moveCameraRig { get; set; }

        /// <summary>
        /// Method for moving the camera rig
        /// </summary>
        /// <param name="position">Target position</param>
        /// <param name="viewDirection">Target view direction in the XZ plane. Y component will be ignored</param>
        public static void MoveCameraRig(this IMoveCameraRig obj, Vector3 position, Vector3? viewDirection = null)
        {
            moveCameraRig(position, viewDirection);
        }
    }
}

