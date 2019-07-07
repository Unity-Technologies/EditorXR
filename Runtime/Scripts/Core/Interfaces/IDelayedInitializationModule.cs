using Unity.Labs.ModuleLoader;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Adds Initialize and Shutdown methods to modules for when editing starts and stops
    /// </summary>
    interface IDelayedInitializationModule : IModule
    {
        /// <summary>
        /// Called when editing starts
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called when editing stops
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Used to sort initializable modules before calling Initialize
        /// </summary>
        int initializationOrder { get; }

        /// <summary>
        /// Used to sort initializable modules before calling Shutdown
        /// </summary>
        int shutdownOrder { get; }
    }
}
