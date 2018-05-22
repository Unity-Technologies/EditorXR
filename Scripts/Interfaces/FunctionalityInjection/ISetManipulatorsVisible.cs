
using System;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provide access to show or hide manipulator(s)
    /// </summary>
    public interface ISetManipulatorsVisible
    {
    }

    public static class ISetManipulatorsVisibleMethods
    {
        internal static Action<ISetManipulatorsVisible, bool> setManipulatorsVisible { get; set; }

        /// <summary>
        /// Show or hide the manipulator(s)
        /// </summary>
        /// <param name="requester">The requesting object that is wanting to set all manipulators visible or hidden</param>
        /// <param name="visibility">Whether the manipulators should be shown or hidden</param>
        public static void SetManipulatorsVisible(this ISetManipulatorsVisible obj, ISetManipulatorsVisible requester, bool visibility)
        {
            setManipulatorsVisible(requester, visibility);
        }
    }
}

