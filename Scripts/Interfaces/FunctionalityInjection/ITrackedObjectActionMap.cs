using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provided to a tool for device input (e.g. position / rotation)
    /// </summary>
    public interface ITrackedObjectActionMap
    {
        TrackedObject trackedObjectInput { set; }
    }
}
