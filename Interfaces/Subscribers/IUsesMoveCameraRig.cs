using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to move the camera rig
    /// </summary>
    public interface IUsesMoveCameraRig : IFunctionalitySubscriber<IProvidesMoveCameraRig>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesMoveCameraRig
    /// </summary>
    public static class UsesMoveCameraRigMethods
    {
        /// <summary>
        /// Method for moving the camera rig
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="position">Target position</param>
        /// <param name="viewDirection">Target view direction in the XZ plane. Y component will be ignored</param>
        public static void MoveCameraRig(this IUsesMoveCameraRig user, Vector3 position, Vector3? viewDirection = null)
        {
#if !FI_AUTOFILL
            user.provider.MoveCameraRig(position, viewDirection);
#endif
        }
    }
}
