using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Adds Initialize and Shutdown methods to modules for when editing starts and stops
    /// </summary>
    interface IInitializableModule : IModule
    {
        /// <summary>
        /// Called when editing starts
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called when EditingStops
        /// </summary>
        void Shutdown();

        int order { get; }
    }
}
