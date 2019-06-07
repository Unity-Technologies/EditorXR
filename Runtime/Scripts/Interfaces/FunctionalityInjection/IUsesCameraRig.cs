using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class access to the camera rig
    /// </summary>
    public interface IUsesCameraRig
    {
        /// <summary>
        /// The camera rig root transform
        /// </summary>
        Transform cameraRig { set; }
    }
}
