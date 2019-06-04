using Unity.Labs.ModuleLoader;

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

        /// <summary>
        /// Used to sort initializable modules before calling Instantiate. Shutdown is called according to module order
        /// </summary>
        int order { get; }
    }
}
