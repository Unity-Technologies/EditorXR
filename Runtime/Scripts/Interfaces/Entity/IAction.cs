using UnityEngine;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Declares a class as an action that can be executed within the system
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// The icon representing this Action that can be displayed in menus
        /// </summary>
        Sprite icon { get; }

        /// <summary>
        /// Execute this action
        /// </summary>
        void ExecuteAction();
    }
}
