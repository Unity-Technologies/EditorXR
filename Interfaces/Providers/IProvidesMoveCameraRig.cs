using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to move the camera rig
    /// </summary>
    public interface IProvidesMoveCameraRig : IFunctionalityProvider
    {
        /// <summary>
        /// Method for moving the camera rig
        /// </summary>
        /// <param name="position">Target position</param>
        /// <param name="viewDirection">Target view direction in the XZ plane. Y component will be ignored</param>
        void MoveCameraRig(Vector3 position, Vector3? viewDirection = null);
    }
}
