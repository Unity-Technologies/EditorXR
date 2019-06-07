using System;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Implementors can check whether the manipulator is in the dragging state
    /// </summary>
    public interface IGetManipulatorDragState
    {
    }

    public static class IGetManipulatorDragStateMethods
    {
        internal static Func<bool> getManipulatorDragState { get; set; }

        /// <summary>
        /// Returns whether the manipulator is in the dragging state
        /// </summary>
        public static bool GetManipulatorDragState(this IGetManipulatorDragState obj)
        {
            return getManipulatorDragState();
        }
    }
}
