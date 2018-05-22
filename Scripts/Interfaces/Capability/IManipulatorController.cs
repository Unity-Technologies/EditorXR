
namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provide access to the system to show/hide and check drag state of manipulators on this tool / workspace / etc.
    /// </summary>
    public interface IManipulatorController
    {
        /// <summary>
        /// Show or hide the manipulator(s)
        /// </summary>
        bool manipulatorVisible { set; }

        /// <summary>
        /// Whether the manipulator(s) are in a drag state
        /// </summary>
        bool manipulatorDragging { get; }
    }
}

