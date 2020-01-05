using System;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Declares a class as a Workspace within the system
    /// </summary>
    public interface IWorkspace : IVacuumable, ICustomActionMap
    {
        /// <summary>
        /// First-time setup; will be called after Awake and ConnectInterfaces
        /// </summary>
        void Setup();

        /// <summary>
        /// Close the workspace
        /// </summary>
        void Close();

        /// <summary>
        /// Call this in OnDestroy to inform the system
        /// </summary>
        event Action<IWorkspace> destroyed;

        /// <summary>
        /// Bounding box for entire workspace, including UI handles
        /// </summary>
        Bounds outerBounds { get; }

        /// <summary>
        /// Bounding box for workspace content (ignores value.center)
        /// </summary>
        Bounds contentBounds { get; set; }
    }
}
