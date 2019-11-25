using Unity.Labs.ModuleLoader;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Adds Initialize and Shutdown methods to modules for when editing starts and stops
    /// </summary>
    public interface IDelayedInitializationModule : IModule
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
        /// Used to sort delayed initialization modules before calling Initialize
        /// </summary>
        int initializationOrder { get; }

        /// <summary>
        /// Used to sort delayed initialization modules before calling Shutdown
        /// </summary>
        int shutdownOrder { get; }
    }
}
