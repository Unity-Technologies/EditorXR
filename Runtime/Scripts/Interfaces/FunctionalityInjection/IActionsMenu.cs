using System;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Decorates a class that wants to receive menu actions
    /// </summary>
    public interface IActionsMenu : IMenu
    {
        /// <summary>
        /// Collection of actions that can be performed
        /// </summary>
        List<ActionMenuData> menuActions { set; }

        /// <summary>
        /// Delegate called when any item was selected in the alternate menu
        /// </summary>
        event Action<Transform> itemWasSelected;
    }
}
