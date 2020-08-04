using UnityEngine.InputNew;

namespace Unity.EditorXR
{
    /// <summary>
    /// Provided to a tool for device input (e.g. position / rotation)
    /// </summary>
    public interface ITrackedObjectActionMap
    {
        /// <summary>
        /// The tracked object action map
        /// </summary>
        TrackedObject trackedObjectInput { set; }
    }
}
